using EMenu.Application.Services;
using EMenu.Infrastructure.Data;
using EMenu.Infrastructure.Seed;
using EMenu.Web.Hubs;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddScoped<AuthService>();

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


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();
builder.Services.AddControllers()
.AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler =
        System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});

// Add services to the container.
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapHub<OrderHub>("/orderHub");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    DataSeeder.Seed(context);
}

app.Run();
