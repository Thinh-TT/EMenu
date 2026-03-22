using System.Net;
using System.Net.Http.Json;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;
using EMenu.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EMenu.Tests.Integration;

public class ApiIntegrationTests
{
    [Fact]
    public async Task SessionStart_ReturnsOk_ForStaffRole()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateClient(factory);
        AddAuth(client, "Staff");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var table = await db.RestaurantTables.FirstAsync(x => x.Status == 0);
        var customer = await db.Customers.FirstAsync();

        var response = await client.PostAsync($"/api/session/start?tableId={table.TableID}&customerId={customer.CustomerID}", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        db.ChangeTracker.Clear();
        var createdSession = await db.OrderSessions
            .Where(x => x.TableID == table.TableID && x.CustomerID == customer.CustomerID)
            .OrderByDescending(x => x.OrderSessionID)
            .FirstOrDefaultAsync();

        Assert.NotNull(createdSession);
        Assert.Equal(1, createdSession!.Status);
    }

    [Fact]
    public async Task SessionStart_ReturnsUnauthorized_WithoutAuth()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateClient(factory);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var table = await db.RestaurantTables.FirstAsync(x => x.Status == 0);
        var customer = await db.Customers.FirstAsync();

        var response = await client.PostAsync($"/api/session/start?tableId={table.TableID}&customerId={customer.CustomerID}", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SubmitOrder_CreatesOrderAndItems_ForActiveSession()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateClient(factory);

        int sessionId;
        List<(int productId, decimal price)> products;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var table = await db.RestaurantTables.FirstAsync(x => x.Status == 0);
            var customer = await db.Customers.FirstAsync();

            table.Status = 1;

            var session = new OrderSession
            {
                TableID = table.TableID,
                CustomerID = customer.CustomerID,
                StartTime = DateTime.Now,
                Status = 1
            };

            db.OrderSessions.Add(session);
            await db.SaveChangesAsync();

            sessionId = session.OrderSessionID;
            products = await db.Products
                .Where(x => x.IsAvailable)
                .Select(x => new ValueTuple<int, decimal>(x.ProductID, x.Price))
                .Take(2)
                .ToListAsync();
        }

        var items = new[]
        {
            new { productId = products[0].productId, quantity = 2 },
            new { productId = products[1].productId, quantity = 1 }
        };

        var response = await client.PostAsJsonAsync($"/api/order/submit?sessionId={sessionId}", items);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var verifyScope = factory.Services.CreateScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();

        var order = await verifyDb.Orders
            .Where(x => x.OrderSessionID == sessionId)
            .OrderByDescending(x => x.OrderID)
            .FirstOrDefaultAsync();

        Assert.NotNull(order);

        var orderItemCount = await verifyDb.OrderProducts.CountAsync(x => x.OrderID == order!.OrderID);
        Assert.Equal(2, orderItemCount);
        Assert.True(order.TotalAmount > 0);
    }

    [Fact]
    public async Task KitchenUpdateStatus_ReturnsForbidden_ForUnauthorizedRole()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateClient(factory);
        AddAuth(client, "Guest");

        var orderProductId = await SeedPendingOrderItem(factory);

        var response = await client.PutAsync($"/api/kitchen/update-status?orderProductId={orderProductId}&status=Preparing", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task KitchenUpdateStatus_UpdatesStatus_ForKitchenRole()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateClient(factory);
        AddAuth(client, "Kitchen");

        var orderProductId = await SeedPendingOrderItem(factory);

        var response = await client.PutAsync($"/api/kitchen/update-status?orderProductId={orderProductId}&status=Preparing", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var item = await db.OrderProducts.FirstAsync(x => x.OrderProductID == orderProductId);
        Assert.Equal(OrderItemStatus.Preparing, item.Status);
    }

    [Fact]
    public async Task PaymentCash_CompletesPaymentAndRedirects_ForStaffRole()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateClient(factory);
        AddAuth(client, "Staff");

        var (sessionId, orderId) = await SeedCashCheckoutData(factory);

        var response = await client.PostAsync($"/Payment/Cash?sessionId={sessionId}", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Table", response.Headers.Location?.ToString());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var invoice = await db.Invoices.FirstOrDefaultAsync(x => x.OrderID == orderId);
        Assert.NotNull(invoice);

        var payment = await db.Payments.FirstOrDefaultAsync(x => x.InvoiceID == invoice!.InvoiceID);
        Assert.NotNull(payment);

        var session = await db.OrderSessions.FirstAsync(x => x.OrderSessionID == sessionId);
        Assert.Equal(0, session.Status);
    }

    private static HttpClient CreateClient(CustomWebApplicationFactory factory)
    {
        return factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });
    }

    private static void AddAuth(HttpClient client, string role, string userId = "1")
    {
        client.DefaultRequestHeaders.Remove("X-Test-Role");
        client.DefaultRequestHeaders.Remove("X-Test-UserId");
        client.DefaultRequestHeaders.Add("X-Test-Role", role);
        client.DefaultRequestHeaders.Add("X-Test-UserId", userId);
    }

    private static async Task<int> SeedPendingOrderItem(CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var staff = await db.Staffs.FirstAsync();
        var table = await db.RestaurantTables.FirstAsync(x => x.Status == 0);
        var customer = await db.Customers.FirstAsync();
        var product = await db.Products.FirstAsync(x => x.IsAvailable);

        table.Status = 1;

        var session = new OrderSession
        {
            TableID = table.TableID,
            CustomerID = customer.CustomerID,
            StartTime = DateTime.Now,
            Status = 1
        };

        db.OrderSessions.Add(session);
        await db.SaveChangesAsync();

        var order = new Order
        {
            OrderSessionID = session.OrderSessionID,
            StaffID = staff.StaffID,
            Status = OrderStatus.Pending,
            TotalAmount = product.Price,
            CreatedTime = DateTime.Now
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        var orderProduct = new OrderProduct
        {
            OrderID = order.OrderID,
            ProductID = product.ProductID,
            Quantity = 1,
            Price = product.Price,
            Status = OrderItemStatus.Pending
        };

        db.OrderProducts.Add(orderProduct);
        await db.SaveChangesAsync();

        return orderProduct.OrderProductID;
    }

    private static async Task<(int sessionId, int orderId)> SeedCashCheckoutData(CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var staff = await db.Staffs.FirstAsync();
        var table = await db.RestaurantTables.FirstAsync(x => x.Status == 0);
        var customer = await db.Customers.FirstAsync();
        var product = await db.Products.FirstAsync(x => x.IsAvailable);

        table.Status = 1;

        var session = new OrderSession
        {
            TableID = table.TableID,
            CustomerID = customer.CustomerID,
            StartTime = DateTime.Now,
            Status = 1
        };

        db.OrderSessions.Add(session);
        await db.SaveChangesAsync();

        var order = new Order
        {
            OrderSessionID = session.OrderSessionID,
            StaffID = staff.StaffID,
            Status = OrderStatus.Pending,
            TotalAmount = 0,
            CreatedTime = DateTime.Now
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        db.OrderProducts.Add(new OrderProduct
        {
            OrderID = order.OrderID,
            ProductID = product.ProductID,
            Quantity = 2,
            Price = product.Price,
            Status = OrderItemStatus.Pending
        });

        await db.SaveChangesAsync();

        return (session.OrderSessionID, order.OrderID);
    }
}

