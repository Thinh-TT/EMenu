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

            EnsureRoles(context);

            var adminUser = EnsureUser(
                context,
                userName: "admin",
                password: "Admin123",
                isActive: true);
            EnsureUserRole(context, adminUser.UserID, AppRoles.Admin);

            var staffOne = EnsureStaff(
                context,
                userName: "System",
                password: "System123",
                staffName: "System Staff",
                phone: "0900000001",
                email: "system@emenu.local");

            var staffTwo = EnsureStaff(
                context,
                userName: "staff01",
                password: "Staff123",
                staffName: "Cashier Staff",
                phone: "0900000002",
                email: "staff01@emenu.local");

            var categories = EnsureCategories(context);

            EnsureCustomers(context);
            EnsureRestaurantTables(context);

            var shifts = EnsureShifts(context);
            EnsureShiftLog(context, staffOne.StaffID, shifts["Morning"]);
            EnsureShiftLog(context, staffTwo.StaffID, shifts["Evening"]);

            var products = EnsureProducts(context, categories);
            EnsureComboProducts(context, products);
        }

        private static void EnsureRoles(AppDbContext context)
        {
            var requiredRoles = new[]
            {
                AppRoles.Admin,
                AppRoles.Staff,
                AppRoles.Kitchen
            };

            var existingRoleNames = context.Roles
                .Select(x => x.RoleName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missingRoles = requiredRoles
                .Where(roleName => !existingRoleNames.Contains(roleName))
                .Select(roleName => new Role { RoleName = roleName })
                .ToList();

            if (missingRoles.Count == 0)
            {
                return;
            }

            context.Roles.AddRange(missingRoles);
            context.SaveChanges();
        }

        private static User EnsureUser(
            AppDbContext context,
            string userName,
            string password,
            bool isActive)
        {
            var user = context.Users.FirstOrDefault(x => x.UserName == userName);

            if (user == null)
            {
                user = new User
                {
                    UserName = userName,
                    Password = BCrypt.Net.BCrypt.HashPassword(password),
                    IsActive = isActive,
                    CreatedAt = DateTime.Now
                };

                context.Users.Add(user);
                context.SaveChanges();

                return user;
            }

            if (!user.IsActive)
            {
                user.IsActive = true;
                context.SaveChanges();
            }

            return user;
        }

        private static void EnsureUserRole(AppDbContext context, int userId, string roleName)
        {
            var roleId = context.Roles
                .Where(x => x.RoleName == roleName)
                .Select(x => x.RoleID)
                .First();

            var hasRole = context.UserRoles.Any(x => x.UserID == userId && x.RoleID == roleId);

            if (hasRole)
            {
                return;
            }

            context.UserRoles.Add(new UserRole
            {
                UserID = userId,
                RoleID = roleId
            });

            context.SaveChanges();
        }

        private static Staff EnsureStaff(
            AppDbContext context,
            string userName,
            string password,
            string staffName,
            string phone,
            string email)
        {
            var user = EnsureUser(context, userName, password, isActive: true);
            EnsureUserRole(context, user.UserID, AppRoles.Staff);

            var staff = context.Staffs.FirstOrDefault(x => x.UserID == user.UserID);

            if (staff != null)
            {
                return staff;
            }

            staff = new Staff
            {
                StaffName = staffName,
                Phone = phone,
                Email = email,
                UserID = user.UserID
            };

            context.Staffs.Add(staff);
            context.SaveChanges();

            return staff;
        }

        private static Dictionary<string, int> EnsureCategories(AppDbContext context)
        {
            var categoryNames = new[] { "Food", "Drink", "Combo" };

            var existingCategoryNames = context.Categories
                .Select(x => x.CategoryName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var missingCategories = categoryNames
                .Where(categoryName => !existingCategoryNames.Contains(categoryName))
                .Select(categoryName => new Category { CategoryName = categoryName })
                .ToList();

            if (missingCategories.Count > 0)
            {
                context.Categories.AddRange(missingCategories);
                context.SaveChanges();
            }

            return context.Categories
                .Where(x => categoryNames.Contains(x.CategoryName))
                .ToDictionary(x => x.CategoryName, x => x.CategoryID, StringComparer.OrdinalIgnoreCase);
        }

        private static void EnsureCustomers(AppDbContext context)
        {
            var customerSeeds = new[]
            {
                new CustomerSeed("Guest Customer", "Male", "guest@emenu.local", "0911000001", 1995),
                new CustomerSeed("Nguyen Minh Anh", "Female", "minh.anh@emenu.local", "0911000002", 1998),
                new CustomerSeed("Tran Quoc Bao", "Male", "quoc.bao@emenu.local", "0911000003", 1993),
                new CustomerSeed("Le Thu Ha", "Female", "thu.ha@emenu.local", "0911000004", 1997),
                new CustomerSeed("Pham Gia Huy", "Male", "gia.huy@emenu.local", "0911000005", 1996)
            };

            var existingPhones = context.Customers
                .Where(x => x.Phone != null)
                .Select(x => x.Phone!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var newCustomers = customerSeeds
                .Where(seed => !existingPhones.Contains(seed.Phone))
                .Select(seed => new Customer
                {
                    Name = seed.Name,
                    Sex = seed.Sex,
                    Email = seed.Email,
                    Phone = seed.Phone,
                    BirthYear = seed.BirthYear,
                    CreatedAt = DateTime.Now
                })
                .ToList();

            if (newCustomers.Count == 0)
            {
                return;
            }

            context.Customers.AddRange(newCustomers);
            context.SaveChanges();
        }

        private static void EnsureRestaurantTables(AppDbContext context)
        {
            var tableSeeds = new[]
            {
                new TableSeed("T01", 2),
                new TableSeed("T02", 2),
                new TableSeed("T03", 4),
                new TableSeed("T04", 4),
                new TableSeed("T05", 4),
                new TableSeed("T06", 4),
                new TableSeed("T07", 6),
                new TableSeed("T08", 6),
                new TableSeed("T09", 8),
                new TableSeed("T10", 8)
            };

            var existingTableNames = context.RestaurantTables
                .Select(x => x.TableName)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var newTables = tableSeeds
                .Where(seed => !existingTableNames.Contains(seed.TableName))
                .Select(seed => new RestaurantTable
                {
                    TableName = seed.TableName,
                    Capacity = seed.Capacity,
                    Status = 0
                })
                .ToList();

            if (newTables.Count == 0)
            {
                return;
            }

            context.RestaurantTables.AddRange(newTables);
            context.SaveChanges();
        }

        private static Dictionary<string, int> EnsureShifts(AppDbContext context)
        {
            var shiftSeeds = new[]
            {
                new ShiftSeed("Morning", new TimeSpan(8, 0, 0), new TimeSpan(16, 0, 0)),
                new ShiftSeed("Evening", new TimeSpan(16, 0, 0), new TimeSpan(22, 0, 0))
            };

            var existingShiftLookup = context.Shifts
                .ToList()
                .ToDictionary(
                    x => $"{x.StartTime}-{x.EndTime}",
                    x => x.ShiftID,
                    StringComparer.OrdinalIgnoreCase);

            var newShifts = shiftSeeds
                .Where(seed => !existingShiftLookup.ContainsKey($"{seed.StartTime}-{seed.EndTime}"))
                .Select(seed => new Shift
                {
                    StartTime = seed.StartTime,
                    EndTime = seed.EndTime
                })
                .ToList();

            if (newShifts.Count > 0)
            {
                context.Shifts.AddRange(newShifts);
                context.SaveChanges();
            }

            var shifts = context.Shifts.ToList();

            return shiftSeeds.ToDictionary(
                seed => seed.Name,
                seed => shifts
                    .First(x => x.StartTime == seed.StartTime && x.EndTime == seed.EndTime)
                    .ShiftID,
                StringComparer.OrdinalIgnoreCase);
        }

        private static void EnsureShiftLog(AppDbContext context, int staffId, int shiftId)
        {
            var exists = context.ShiftLogs.Any(x => x.StaffID == staffId && x.ShiftID == shiftId);

            if (exists)
            {
                return;
            }

            context.ShiftLogs.Add(new ShiftLog
            {
                StaffID = staffId,
                ShiftID = shiftId
            });

            context.SaveChanges();
        }

        private static Dictionary<string, Product> EnsureProducts(
            AppDbContext context,
            IReadOnlyDictionary<string, int> categories)
        {
            var productSeeds = new List<ProductSeed>
            {
                new("Burger", "Classic beef burger with fresh vegetables.", "burger.jpg", 55000m, ProductType.Single, "Food"),
                new("Pizza", "Thin crust pizza with mozzarella and tomato sauce.", "pizza.jpg", 89000m, ProductType.Single, "Food"),
                new("Fried Chicken", "Crispy fried chicken served hot.", "friedchicken.jpg", 69000m, ProductType.Single, "Food"),
                new("Spaghetti Carbonara", "Creamy pasta with bacon and parmesan.", "spaghetticarbonara.jpg", 75000m, ProductType.Single, "Food"),
                new("Seafood Fried Rice", "Fried rice with shrimp, squid and vegetables.", "seafoodfriedrice.jpg", 68000m, ProductType.Single, "Food"),
                new("Beef Steak", "Pan-seared beef steak with black pepper sauce.", "beefsteak.jpg", 159000m, ProductType.Single, "Food"),
                new("Chicken Caesar Salad", "Romaine lettuce, grilled chicken and Caesar dressing.", "chickencaesarsalad.jpg", 59000m, ProductType.Single, "Food"),
                new("French Fries", "Golden French fries with light seasoning.", "frenchfries.jpg", 35000m, ProductType.Single, "Food"),
                new("Grilled Pork Rice", "Charcoal grilled pork served with steamed rice.", "grilledporkrice.jpg", 52000m, ProductType.Single, "Food"),
                new("Pho Bo", "Vietnamese beef noodle soup.", "phobo.jpg", 65000m, ProductType.Single, "Food"),
                new("Bun Cha", "Grilled pork with vermicelli and herbs.", "buncha.jpg", 60000m, ProductType.Single, "Food"),
                new("Spring Rolls", "Fresh spring rolls with dipping sauce.", "springrolls.jpg", 45000m, ProductType.Single, "Food"),
                new("Coke", "Chilled Coca-Cola.", "coke.jpg", 18000m, ProductType.Single, "Drink"),
                new("Sprite", "Refreshing lemon-lime soda.", "sprite.jpg", 18000m, ProductType.Single, "Drink"),
                new("Orange Juice", "Fresh orange juice without added sugar.", "orangejuice.jpg", 32000m, ProductType.Single, "Drink"),
                new("Lemon Tea", "Iced lemon tea.", "lemontea.jpg", 25000m, ProductType.Single, "Drink"),
                new("Iced Americano", "Black coffee served over ice.", "icedamericano.jpg", 39000m, ProductType.Single, "Drink"),
                new("Mineral Water", "Bottled mineral water.", "mineralwater.jpg", 12000m, ProductType.Single, "Drink"),
                new("Peach Tea", "Peach-flavored iced tea.", "peachtea.jpg", 29000m, ProductType.Single, "Drink"),
                new("Mango Smoothie", "Blended mango smoothie.", "mangosmoothie.jpg", 42000m, ProductType.Single, "Drink"),
                new("Burger Combo", "Burger set with fries and a drink.", "burgercombo.jpg", 99000m, ProductType.Combo, "Combo"),
                new("Pizza Combo", "Pizza set with salad and a drink.", "pizzacombo.jpg", 129000m, ProductType.Combo, "Combo"),
                new("Vietnamese Lunch Combo", "A local set for lunch rush.", "vietnameselunchcombo.jpg", 109000m, ProductType.Combo, "Combo"),
                new("Family Sharing Combo", "A larger combo for two to three guests.", "familysharingcombo.jpg", 189000m, ProductType.Combo, "Combo"),
                new("Light Meal Combo", "Balanced light meal with drink.", "lightmealcombo.jpg", 95000m, ProductType.Combo, "Combo")
            };

            var productNames = productSeeds
                .Select(x => x.ProductName)
                .ToList();

            var existingProducts = context.Products
                .Where(x => productNames.Contains(x.ProductName))
                .ToDictionary(x => x.ProductName, x => x, StringComparer.OrdinalIgnoreCase);

            foreach (var seed in productSeeds)
            {
                if (!existingProducts.TryGetValue(seed.ProductName, out var product))
                {
                    context.Products.Add(new Product
                    {
                        ProductName = seed.ProductName,
                        Description = seed.Description,
                        Image = seed.Image,
                        Price = seed.Price,
                        ProductType = seed.ProductType,
                        CategoryID = categories[seed.CategoryName],
                        IsAvailable = true
                    });

                    continue;
                }

                product.Description = seed.Description;
                product.Image = seed.Image;
                product.Price = seed.Price;
                product.ProductType = seed.ProductType;
                product.CategoryID = categories[seed.CategoryName];
                product.IsAvailable = true;
            }

            context.SaveChanges();

            return context.Products
                .Where(x => productNames.Contains(x.ProductName))
                .ToDictionary(x => x.ProductName, x => x, StringComparer.OrdinalIgnoreCase);
        }

        private static void EnsureComboProducts(
            AppDbContext context,
            IReadOnlyDictionary<string, Product> products)
        {
            var comboSeeds = new[]
            {
                new ComboSeed("Burger Combo", new[] { new ComboItemSeed("Burger", 1), new ComboItemSeed("French Fries", 1), new ComboItemSeed("Coke", 1) }),
                new ComboSeed("Pizza Combo", new[] { new ComboItemSeed("Pizza", 1), new ComboItemSeed("Chicken Caesar Salad", 1), new ComboItemSeed("Sprite", 1) }),
                new ComboSeed("Vietnamese Lunch Combo", new[] { new ComboItemSeed("Pho Bo", 1), new ComboItemSeed("Spring Rolls", 1), new ComboItemSeed("Peach Tea", 1) }),
                new ComboSeed("Family Sharing Combo", new[] { new ComboItemSeed("Fried Chicken", 1), new ComboItemSeed("Seafood Fried Rice", 1), new ComboItemSeed("Orange Juice", 1), new ComboItemSeed("Mineral Water", 1) }),
                new ComboSeed("Light Meal Combo", new[] { new ComboItemSeed("Grilled Pork Rice", 1), new ComboItemSeed("Spring Rolls", 1), new ComboItemSeed("Lemon Tea", 1) })
            };

            foreach (var comboSeed in comboSeeds)
            {
                var combo = products[comboSeed.ComboName];

                foreach (var itemSeed in comboSeed.Items)
                {
                    var product = products[itemSeed.ProductName];

                    // The current ComboService uses ProductID as the combo root
                    // and ComboID as the linked product ID.
                    var comboProduct = context.ComboProducts.FirstOrDefault(x =>
                        x.ProductID == combo.ProductID &&
                        x.ComboID == product.ProductID);

                    if (comboProduct == null)
                    {
                        context.ComboProducts.Add(new ComboProduct
                        {
                            ProductID = combo.ProductID,
                            ComboID = product.ProductID,
                            Quantity = itemSeed.Quantity
                        });

                        continue;
                    }

                    if (comboProduct.Quantity != itemSeed.Quantity)
                    {
                        comboProduct.Quantity = itemSeed.Quantity;
                    }
                }
            }

            context.SaveChanges();
        }

        private sealed record CustomerSeed(
            string Name,
            string Sex,
            string Email,
            string Phone,
            int BirthYear);

        private sealed record TableSeed(string TableName, int Capacity);

        private sealed record ShiftSeed(string Name, TimeSpan StartTime, TimeSpan EndTime);

        private sealed record ProductSeed(
            string ProductName,
            string Description,
            string Image,
            decimal Price,
            ProductType ProductType,
            string CategoryName);

        private sealed record ComboSeed(string ComboName, ComboItemSeed[] Items);

        private sealed record ComboItemSeed(string ProductName, int Quantity);
    }
}
