using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using EMenu.Application.Abstractions.Repositories;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;
using EMenu.Infrastructure.Data;
using Microsoft.AspNetCore.WebUtilities;
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

    [Fact]
    public async Task PaymentVNPay_RedirectsToSandbox_WithSignedQuery()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateClient(factory);
        AddAuth(client, "Staff");

        var (sessionId, _) = await SeedCashCheckoutData(factory);

        var response = await client.PostAsync($"/Payment/VNPay?sessionId={sessionId}", new StringContent(string.Empty));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var location = response.Headers.Location!.ToString();
        Assert.StartsWith("https://sandbox.vnpayment.vn/paymentv2/vpcpay.html", location, StringComparison.OrdinalIgnoreCase);

        var uri = new Uri(location);
        var query = QueryHelpers.ParseQuery(uri.Query);

        Assert.False(string.IsNullOrWhiteSpace(query["vnp_SecureHash"].ToString()));
        Assert.False(string.IsNullOrWhiteSpace(query["vnp_TxnRef"].ToString()));
    }

    [Fact]
    public async Task VNPayReturn_WithValidSignatureAndSuccessCode_FinalizesPayment()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateClient(factory);

        var (sessionId, orderId) = await SeedCashCheckoutData(factory);
        var txnRef = BuildTxnRefForTest(sessionId, orderId);
        var callbackUrl = BuildSignedVnPayReturnUrl(new Dictionary<string, string>
        {
            ["vnp_Amount"] = "100000",
            ["vnp_ResponseCode"] = "00",
            ["vnp_TransactionStatus"] = "00",
            ["vnp_TxnRef"] = txnRef,
            ["vnp_TransactionNo"] = "246801357",
            ["vnp_PayDate"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
            ["vnp_OrderInfo"] = $"Payment session {sessionId} order {orderId}"
        });

        var response = await client.GetAsync(callbackUrl);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("VNPay Payment Success", html, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var invoice = await db.Invoices.FirstOrDefaultAsync(x => x.OrderID == orderId);
        Assert.NotNull(invoice);

        var payment = await db.Payments.FirstOrDefaultAsync(x => x.InvoiceID == invoice!.InvoiceID);
        Assert.NotNull(payment);
        Assert.Equal("VNPay", payment!.Method);

        var session = await db.OrderSessions.FirstAsync(x => x.OrderSessionID == sessionId);
        Assert.Equal(0, session.Status);
    }

    [Fact]
    public async Task VNPayReturn_WithInvalidSignature_DoesNotFinalizePayment()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateClient(factory);

        var (sessionId, orderId) = await SeedCashCheckoutData(factory);
        var txnRef = BuildTxnRefForTest(sessionId, orderId);
        var callbackUrl = BuildSignedVnPayReturnUrl(new Dictionary<string, string>
        {
            ["vnp_Amount"] = "100000",
            ["vnp_ResponseCode"] = "00",
            ["vnp_TransactionStatus"] = "00",
            ["vnp_TxnRef"] = txnRef,
            ["vnp_TransactionNo"] = "246801357",
            ["vnp_PayDate"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
            ["vnp_OrderInfo"] = $"Payment session {sessionId} order {orderId}"
        }, overrideSecureHash: "invalidhash");

        var response = await client.GetAsync(callbackUrl);
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("VNPay Payment Failed", html, StringComparison.OrdinalIgnoreCase);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var invoice = await db.Invoices.FirstOrDefaultAsync(x => x.OrderID == orderId);
        Assert.Null(invoice);
    }

    [Fact]
    public async Task VNPayReturn_DuplicateCallback_DoesNotCreateDuplicateInvoice()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateClient(factory);

        var (sessionId, orderId) = await SeedCashCheckoutData(factory);
        var txnRef = BuildTxnRefForTest(sessionId, orderId);
        var callbackUrl = BuildSignedVnPayReturnUrl(new Dictionary<string, string>
        {
            ["vnp_Amount"] = "100000",
            ["vnp_ResponseCode"] = "00",
            ["vnp_TransactionStatus"] = "00",
            ["vnp_TxnRef"] = txnRef,
            ["vnp_TransactionNo"] = "246801357",
            ["vnp_PayDate"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
            ["vnp_OrderInfo"] = $"Payment session {sessionId} order {orderId}"
        });

        var firstResponse = await client.GetAsync(callbackUrl);
        var secondResponse = await client.GetAsync(callbackUrl);

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, secondResponse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var invoiceCount = await db.Invoices.CountAsync(x => x.OrderID == orderId);
        Assert.Equal(1, invoiceCount);
    }
    [Fact]
    public async Task SessionTransfer_MovesOrdersAndUpdatesTableStates()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateClient(factory);
        AddAuth(client, "Staff");

        var seed = await SeedTransferScenario(factory);

        var response = await client.PostAsJsonAsync("/api/session/transfer", new
        {
            sourceTableId = seed.sourceTableId,
            targetTableId = seed.targetTableId,
            actor = "integration-test"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sourceTable = await db.RestaurantTables.FirstAsync(x => x.TableID == seed.sourceTableId);
        var targetTable = await db.RestaurantTables.FirstAsync(x => x.TableID == seed.targetTableId);
        var sourceSession = await db.OrderSessions.FirstAsync(x => x.OrderSessionID == seed.sourceSessionId);
        var targetSession = await db.OrderSessions
            .Where(x => x.TableID == seed.targetTableId && x.Status == 1)
            .OrderByDescending(x => x.OrderSessionID)
            .FirstAsync();

        var order = await db.Orders.FirstAsync(x => x.OrderID == seed.sourceOrderId);

        Assert.Equal(0, sourceTable.Status);
        Assert.Equal(1, targetTable.Status);
        Assert.Equal(0, sourceSession.Status);
        Assert.Equal(targetSession.OrderSessionID, order.OrderSessionID);
    }

    [Fact]
    public async Task SessionMerge_OccupiedTarget_UsesTargetSessionAndClosesSource()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateClient(factory);
        AddAuth(client, "Staff");

        var seed = await SeedMergeOccupiedScenario(factory);

        var response = await client.PostAsJsonAsync("/api/session/merge", new
        {
            sourceTableId = seed.sourceTableId,
            targetTableId = seed.targetTableId,
            actor = "integration-test"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sourceSession = await db.OrderSessions.FirstAsync(x => x.OrderSessionID == seed.sourceSessionId);
        var targetSession = await db.OrderSessions.FirstAsync(x => x.OrderSessionID == seed.targetSessionId);
        var sourceOrder = await db.Orders.FirstAsync(x => x.OrderID == seed.sourceOrderId);
        var sourceTable = await db.RestaurantTables.FirstAsync(x => x.TableID == seed.sourceTableId);
        var targetTable = await db.RestaurantTables.FirstAsync(x => x.TableID == seed.targetTableId);

        Assert.Equal(0, sourceSession.Status);
        Assert.Equal(1, targetSession.Status);
        Assert.Equal(seed.targetCustomerId, targetSession.CustomerID);
        Assert.Equal(seed.targetSessionId, sourceOrder.OrderSessionID);
        Assert.Equal(0, sourceTable.Status);
        Assert.Equal(1, targetTable.Status);
    }

    [Fact]
    public async Task SessionMerge_ReservedTarget_ReturnsBadRequest_AndLeavesSourceUnchanged()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateClient(factory);
        AddAuth(client, "Staff");

        var seed = await SeedMergeReservedTargetScenario(factory);

        var response = await client.PostAsJsonAsync("/api/session/merge", new
        {
            sourceTableId = seed.sourceTableId,
            targetTableId = seed.targetTableId,
            actor = "integration-test"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var sourceSession = await db.OrderSessions.FirstAsync(x => x.OrderSessionID == seed.sourceSessionId);
        var sourceOrder = await db.Orders.FirstAsync(x => x.OrderID == seed.sourceOrderId);
        var sourceTable = await db.RestaurantTables.FirstAsync(x => x.TableID == seed.sourceTableId);

        Assert.Equal(1, sourceSession.Status);
        Assert.Equal(seed.sourceSessionId, sourceOrder.OrderSessionID);
        Assert.Equal(1, sourceTable.Status);
    }

    [Fact]
    public async Task OrderItemRepository_GetTopProductsByCategory_OnlyReturnsPaidAvailableNonCancelledTopItems()
    {
        using var factory = new CustomWebApplicationFactory();

        var seed = await SeedMenuRecommendationScenario(factory);

        using var scope = factory.Services.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderItemRepository>();

        var recommendations = repository.GetTopProductsByCategory(3);

        var primaryCategoryItems = recommendations
            .Where(x => x.CategoryId == seed.PrimaryCategoryId)
            .ToList();
        var secondaryCategoryItems = recommendations
            .Where(x => x.CategoryId == seed.SecondaryCategoryId)
            .ToList();

        Assert.Equal(3, primaryCategoryItems.Count);
        Assert.Equal(
            [seed.PrimaryTopProductName, seed.PrimarySecondProductName, seed.PrimaryThirdProductName],
            primaryCategoryItems.Select(x => x.ProductName).ToArray());
        Assert.DoesNotContain(primaryCategoryItems, x => x.ProductName == seed.UnavailableProductName);
        Assert.DoesNotContain(primaryCategoryItems, x => x.ProductName == seed.UnpaidProductName);

        Assert.Single(secondaryCategoryItems);
        Assert.Equal(seed.SecondaryTopProductName, secondaryCategoryItems[0].ProductName);
    }

    [Fact]
    public async Task MenuPage_RendersBestSellerSection_ForCategoriesWithSalesData()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = CreateClient(factory);

        var seed = await SeedMenuRecommendationScenario(factory);

        var response = await client.GetAsync("/Menu?sessionId=1");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, CountOccurrences(html, "Best sellers"));
        Assert.Contains(seed.PrimaryTopProductName, html, StringComparison.Ordinal);
        Assert.Contains(seed.PrimarySecondProductName, html, StringComparison.Ordinal);
        Assert.Contains(seed.SecondaryTopProductName, html, StringComparison.Ordinal);
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

    private static async Task<(int sourceTableId, int targetTableId, int sourceSessionId, int sourceOrderId)> SeedTransferScenario(
        CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tables = await db.RestaurantTables
            .Where(x => x.Status == 0)
            .OrderBy(x => x.TableID)
            .Take(2)
            .ToListAsync();

        var sourceTable = tables[0];
        var targetTable = tables[1];

        var customer = await db.Customers.FirstAsync();
        var staff = await db.Staffs.FirstAsync();
        var product = await db.Products.FirstAsync(x => x.IsAvailable);

        sourceTable.Status = 1;

        var sourceSession = new OrderSession
        {
            TableID = sourceTable.TableID,
            CustomerID = customer.CustomerID,
            StartTime = DateTime.Now,
            Status = 1
        };

        db.OrderSessions.Add(sourceSession);
        await db.SaveChangesAsync();

        var sourceOrder = new Order
        {
            OrderSessionID = sourceSession.OrderSessionID,
            StaffID = staff.StaffID,
            Status = OrderStatus.Pending,
            TotalAmount = product.Price,
            CreatedTime = DateTime.Now
        };

        db.Orders.Add(sourceOrder);
        await db.SaveChangesAsync();

        db.OrderProducts.Add(new OrderProduct
        {
            OrderID = sourceOrder.OrderID,
            ProductID = product.ProductID,
            Quantity = 1,
            Price = product.Price,
            Status = OrderItemStatus.Pending
        });
        await db.SaveChangesAsync();

        return (sourceTable.TableID, targetTable.TableID, sourceSession.OrderSessionID, sourceOrder.OrderID);
    }

    private static async Task<(
        int sourceTableId,
        int targetTableId,
        int sourceSessionId,
        int targetSessionId,
        int sourceOrderId,
        int targetCustomerId)> SeedMergeOccupiedScenario(CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tables = await db.RestaurantTables
            .Where(x => x.Status == 0)
            .OrderBy(x => x.TableID)
            .Take(2)
            .ToListAsync();

        var sourceTable = tables[0];
        var targetTable = tables[1];

        var customers = await db.Customers.OrderBy(x => x.CustomerID).Take(2).ToListAsync();
        var sourceCustomer = customers[0];
        var targetCustomer = customers[1];
        var staff = await db.Staffs.FirstAsync();
        var product = await db.Products.FirstAsync(x => x.IsAvailable);

        sourceTable.Status = 1;
        targetTable.Status = 1;

        var sourceSession = new OrderSession
        {
            TableID = sourceTable.TableID,
            CustomerID = sourceCustomer.CustomerID,
            StartTime = DateTime.Now,
            Status = 1
        };

        var targetSession = new OrderSession
        {
            TableID = targetTable.TableID,
            CustomerID = targetCustomer.CustomerID,
            StartTime = DateTime.Now,
            Status = 1
        };

        db.OrderSessions.AddRange(sourceSession, targetSession);
        await db.SaveChangesAsync();

        var sourceOrder = new Order
        {
            OrderSessionID = sourceSession.OrderSessionID,
            StaffID = staff.StaffID,
            Status = OrderStatus.Pending,
            TotalAmount = product.Price,
            CreatedTime = DateTime.Now
        };

        db.Orders.Add(sourceOrder);
        await db.SaveChangesAsync();

        db.OrderProducts.Add(new OrderProduct
        {
            OrderID = sourceOrder.OrderID,
            ProductID = product.ProductID,
            Quantity = 1,
            Price = product.Price,
            Status = OrderItemStatus.Pending
        });
        await db.SaveChangesAsync();

        return (
            sourceTable.TableID,
            targetTable.TableID,
            sourceSession.OrderSessionID,
            targetSession.OrderSessionID,
            sourceOrder.OrderID,
            targetCustomer.CustomerID);
    }

    private static async Task<(int sourceTableId, int targetTableId, int sourceSessionId, int sourceOrderId)> SeedMergeReservedTargetScenario(
        CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var tables = await db.RestaurantTables
            .Where(x => x.Status == 0)
            .OrderBy(x => x.TableID)
            .Take(2)
            .ToListAsync();

        var sourceTable = tables[0];
        var targetTable = tables[1];

        var customer = await db.Customers.FirstAsync();
        var staff = await db.Staffs.FirstAsync();
        var product = await db.Products.FirstAsync(x => x.IsAvailable);

        sourceTable.Status = 1;
        targetTable.Status = 2;

        var sourceSession = new OrderSession
        {
            TableID = sourceTable.TableID,
            CustomerID = customer.CustomerID,
            StartTime = DateTime.Now,
            Status = 1
        };

        db.OrderSessions.Add(sourceSession);
        await db.SaveChangesAsync();

        var sourceOrder = new Order
        {
            OrderSessionID = sourceSession.OrderSessionID,
            StaffID = staff.StaffID,
            Status = OrderStatus.Pending,
            TotalAmount = product.Price,
            CreatedTime = DateTime.Now
        };

        db.Orders.Add(sourceOrder);
        await db.SaveChangesAsync();

        db.OrderProducts.Add(new OrderProduct
        {
            OrderID = sourceOrder.OrderID,
            ProductID = product.ProductID,
            Quantity = 1,
            Price = product.Price,
            Status = OrderItemStatus.Pending
        });
        await db.SaveChangesAsync();

        return (sourceTable.TableID, targetTable.TableID, sourceSession.OrderSessionID, sourceOrder.OrderID);
    }

    private static async Task<MenuRecommendationSeedResult> SeedMenuRecommendationScenario(CustomWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var staff = await db.Staffs.FirstAsync();
        var customer = await db.Customers.FirstAsync();
        var table = await db.RestaurantTables.FirstAsync(x => x.Status == 0);

        table.Status = 1;

        var primaryCategoryName = $"IT Drinks {Guid.NewGuid():N}";
        var secondaryCategoryName = $"IT Desserts {Guid.NewGuid():N}";

        var primaryCategory = new Category { CategoryName = primaryCategoryName };
        var secondaryCategory = new Category { CategoryName = secondaryCategoryName };

        db.Categories.AddRange(primaryCategory, secondaryCategory);
        await db.SaveChangesAsync();

        var primaryTop = new Product
        {
            ProductName = $"IT Top Tea {Guid.NewGuid():N}",
            Image = "tea.jpg",
            Price = 30_000m,
            Description = "Top seller",
            IsAvailable = true,
            ProductType = ProductType.Single,
            CategoryID = primaryCategory.CategoryID
        };
        var primarySecond = new Product
        {
            ProductName = $"IT Top Coffee {Guid.NewGuid():N}",
            Image = "coffee.jpg",
            Price = 35_000m,
            Description = "Second seller",
            IsAvailable = true,
            ProductType = ProductType.Single,
            CategoryID = primaryCategory.CategoryID
        };
        var primaryThird = new Product
        {
            ProductName = $"IT Top Juice {Guid.NewGuid():N}",
            Image = "juice.jpg",
            Price = 32_000m,
            Description = "Third seller",
            IsAvailable = true,
            ProductType = ProductType.Single,
            CategoryID = primaryCategory.CategoryID
        };
        var unavailableProduct = new Product
        {
            ProductName = $"IT Hidden Soda {Guid.NewGuid():N}",
            Image = "soda.jpg",
            Price = 28_000m,
            Description = "Unavailable seller",
            IsAvailable = false,
            ProductType = ProductType.Single,
            CategoryID = primaryCategory.CategoryID
        };
        var unpaidProduct = new Product
        {
            ProductName = $"IT Unpaid Milk {Guid.NewGuid():N}",
            Image = "milk.jpg",
            Price = 25_000m,
            Description = "Unpaid seller",
            IsAvailable = true,
            ProductType = ProductType.Single,
            CategoryID = primaryCategory.CategoryID
        };
        var secondaryTop = new Product
        {
            ProductName = $"IT Top Cake {Guid.NewGuid():N}",
            Image = "cake.jpg",
            Price = 42_000m,
            Description = "Dessert seller",
            IsAvailable = true,
            ProductType = ProductType.Single,
            CategoryID = secondaryCategory.CategoryID
        };

        db.Products.AddRange(
            primaryTop,
            primarySecond,
            primaryThird,
            unavailableProduct,
            unpaidProduct,
            secondaryTop);
        await db.SaveChangesAsync();

        var session = new OrderSession
        {
            TableID = table.TableID,
            CustomerID = customer.CustomerID,
            StartTime = DateTime.Now,
            Status = 1
        };

        db.OrderSessions.Add(session);
        await db.SaveChangesAsync();

        await AddOrderWithSingleItemAsync(db, session.OrderSessionID, staff.StaffID, primaryTop, 9, isPaid: true);
        await AddOrderWithSingleItemAsync(db, session.OrderSessionID, staff.StaffID, primarySecond, 7, isPaid: true);
        await AddOrderWithSingleItemAsync(db, session.OrderSessionID, staff.StaffID, primaryThird, 5, isPaid: true);
        await AddOrderWithSingleItemAsync(db, session.OrderSessionID, staff.StaffID, unavailableProduct, 20, isPaid: true);
        await AddOrderWithSingleItemAsync(db, session.OrderSessionID, staff.StaffID, unpaidProduct, 15, isPaid: false);
        await AddOrderWithSingleItemAsync(db, session.OrderSessionID, staff.StaffID, secondaryTop, 4, isPaid: true);
        await AddOrderWithSingleItemAsync(
            db,
            session.OrderSessionID,
            staff.StaffID,
            primaryThird,
            99,
            isPaid: true,
            status: OrderItemStatus.Cancelled);

        return new MenuRecommendationSeedResult(
            primaryCategory.CategoryID,
            secondaryCategory.CategoryID,
            primaryTop.ProductName,
            primarySecond.ProductName,
            primaryThird.ProductName,
            secondaryTop.ProductName,
            unavailableProduct.ProductName,
            unpaidProduct.ProductName);
    }

    private static async Task AddOrderWithSingleItemAsync(
        AppDbContext db,
        int sessionId,
        int staffId,
        Product product,
        int quantity,
        bool isPaid,
        OrderItemStatus status = OrderItemStatus.Pending)
    {
        var order = new Order
        {
            OrderSessionID = sessionId,
            StaffID = staffId,
            Status = isPaid ? OrderStatus.Completed : OrderStatus.Pending,
            TotalAmount = product.Price * quantity,
            CreatedTime = DateTime.Now
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        db.OrderProducts.Add(new OrderProduct
        {
            OrderID = order.OrderID,
            ProductID = product.ProductID,
            Quantity = quantity,
            Price = product.Price,
            Status = status
        });

        if (isPaid)
        {
            db.Invoices.Add(new Invoice
            {
                OrderID = order.OrderID,
                CreatedDate = DateTime.Now,
                TotalAmount = product.Price * quantity
            });
        }

        await db.SaveChangesAsync();
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var index = 0;

        while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }

    private static string BuildTxnRefForTest(int sessionId, int orderId)
    {
        return $"S{sessionId}O{orderId}T1234567890123R1234";
    }

    private static string BuildSignedVnPayReturnUrl(
        Dictionary<string, string> parameters,
        string? overrideSecureHash = null)
    {
        var signData = string.Join("&", parameters
            .OrderBy(x => x.Key, StringComparer.Ordinal)
            .Select(x => $"{WebUtility.UrlEncode(x.Key)}={WebUtility.UrlEncode(x.Value)}"));

        var secureHash = overrideSecureHash ?? ComputeHmacSha512("TEST", signData);
        parameters["vnp_SecureHash"] = secureHash;

        return QueryHelpers.AddQueryString("/Payment/VNPayReturn", parameters);
    }

    private static string ComputeHmacSha512(string key, string data)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));

        return BitConverter.ToString(hash)
            .Replace("-", "")
            .ToLowerInvariant();
    }

    private sealed record MenuRecommendationSeedResult(
        int PrimaryCategoryId,
        int SecondaryCategoryId,
        string PrimaryTopProductName,
        string PrimarySecondProductName,
        string PrimaryThirdProductName,
        string SecondaryTopProductName,
        string UnavailableProductName,
        string UnpaidProductName);
}




