using EMenu.Application.Abstractions.Configurations;
using EMenu.Application.Services;
using EMenu.Infrastructure.Data;
using EMenu.Infrastructure.DependencyInjection;
using EMenu.Infrastructure.Seed;
using EMenu.Web.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(defaultConnection))
{
    throw new InvalidOperationException(
        "Missing ConnectionStrings:DefaultConnection. Configure it with dotnet user-secrets for local development or environment variables for deployed environments.");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(defaultConnection));

builder.Services.AddInfrastructureRepositories();

var vnPayConfig = builder.Configuration
    .GetSection("VNPay")
    .Get<VNPayConfig>() ?? new VNPayConfig();

builder.Services.AddSingleton(vnPayConfig);

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<PasswordService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<SessionService>();
builder.Services.AddScoped<KitchenService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<StaffService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<TableService>();
builder.Services.AddScoped<MenuService>();
builder.Services.AddScoped<ComboService>();
builder.Services.AddScoped<QrService>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<BillService>();
builder.Services.AddScoped<VNPayService>();
builder.Services.AddScoped<PaymentService>();

builder.Services
    .AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
});
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
})
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHub<OrderHub>("/orderHub");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();
    var passwordService = scope.ServiceProvider
        .GetRequiredService<PasswordService>();

    DataSeeder.Seed(context);
    passwordService.MigrateLegacyPasswords();
}

app.Run();

public partial class Program
{
}

