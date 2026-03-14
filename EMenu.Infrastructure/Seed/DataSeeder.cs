using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EMenu.Domain.Constants;
using EMenu.Domain.Entities;
using EMenu.Domain.Enums;
using EMenu.Infrastructure.Data;

namespace EMenu.Infrastructure.Seed
{
    public static class DataSeeder
    {
        public static void Seed(AppDbContext context)
        {
            context.Database.EnsureCreated();

            var requiredRoles = new[] { AppRoles.Admin, AppRoles.Staff, AppRoles.Kitchen };
            var existingRoles = context.Roles
                .Select(x => x.RoleName)
                .ToHashSet();

            var missingRoles = requiredRoles
                .Where(x => !existingRoles.Contains(x))
                .Select(x => new Role { RoleName = x })
                .ToList();

            if (missingRoles.Any())
            {
                context.Roles.AddRange(missingRoles);
                context.SaveChanges();
            }

            if (!context.Users.Any())
            {
                var admin = new User
                {
                    UserName = "admin",
                    Password = BCrypt.Net.BCrypt.HashPassword("Admin123"),
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                context.Users.Add(admin);
                context.SaveChanges();

                var role = context.Roles.First(x => x.RoleName == AppRoles.Admin);

                context.UserRoles.Add(new UserRole
                {
                    UserID = admin.UserID,
                    RoleID = role.RoleID
                });

                context.SaveChanges();
            }

            if (!context.Users.Any(x => x.UserName == "System"))
            {
                var staffUser = new User
                {
                    UserName = "System",
                    Password = BCrypt.Net.BCrypt.HashPassword("System123"),
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                context.Users.Add(staffUser);
                context.SaveChanges();

                var staffRole = context.Roles.First(x => x.RoleName == AppRoles.Staff);

                context.UserRoles.Add(new UserRole
                {
                    UserID = staffUser.UserID,
                    RoleID = staffRole.RoleID
                });

                context.SaveChanges();

                context.Staffs.Add(new Staff
                {
                    StaffName = "System",
                    Phone = "090000001",
                    Email = "system@restaurant.com",
                    UserID = staffUser.UserID
                });

                context.SaveChanges();
            }

            if (!context.Categories.Any())
            {
                context.Categories.AddRange(
                    new Category { CategoryName = "Food" },
                    new Category { CategoryName = "Drink" },
                    new Category { CategoryName = "Combo" }
                );

                context.SaveChanges();
            }

            if (!context.Customers.Any())
            {
                context.Customers.AddRange(
                    new Customer
                    {
                        Name = "Unknown customer",
                        Sex = "Male",
                        Email = "uc@gmail.com",
                        Phone = "0900000001",
                        BirthYear = 1995,
                        CreatedAt = DateTime.Now
                    },
                    new Customer
                    {
                        Name = "Tran Thi B",
                        Sex = "Female",
                        Email = "b@gmail.com",
                        Phone = "0900000002",
                        BirthYear = 1998,
                        CreatedAt = DateTime.Now
                    },
                    new Customer
                    {
                        Name = "Le Van C",
                        Sex = "Male",
                        Email = "c@gmail.com",
                        Phone = "0900000003",
                        BirthYear = 1992,
                        CreatedAt = DateTime.Now
                    }
                );

                context.SaveChanges();
            }

            if (!context.RestaurantTables.Any())
            {
                context.RestaurantTables.AddRange(
                    new RestaurantTable { TableName = "B1", Capacity = 4, Status = 0 },
                    new RestaurantTable { TableName = "B2", Capacity = 4, Status = 0 },
                    new RestaurantTable { TableName = "B3", Capacity = 6, Status = 0 },
                    new RestaurantTable { TableName = "B4", Capacity = 6, Status = 0 }
                );

                context.SaveChanges();
            }

            if (!context.Products.Any())
            {
                var food = context.Categories.First(x => x.CategoryName == "Food");
                var drink = context.Categories.First(x => x.CategoryName == "Drink");

                context.Products.AddRange(
                    new Product
                    {
                        ProductName = "Burger",
                        Description = "Beef burger",
                        Image = "burger.jpg",
                        Price = 5,
                        ProductType = ProductType.Single,
                        CategoryID = food.CategoryID,
                        IsAvailable = true
                    },
                    new Product
                    {
                        ProductName = "Pizza",
                        Description = "Italian pizza",
                        Image = "pizza.jpg",
                        Price = 8,
                        ProductType = ProductType.Single,
                        CategoryID = food.CategoryID,
                        IsAvailable = true
                    },
                    new Product
                    {
                        ProductName = "Coke",
                        Description = "Coca cola drink",
                        Image = "coke.jpg",
                        Price = 2,
                        ProductType = ProductType.Single,
                        CategoryID = drink.CategoryID,
                        IsAvailable = true
                    }
                );

                context.SaveChanges();
            }
        }
    }
}
