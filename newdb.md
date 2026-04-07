EMENU DATABASE

1. USER MANAGEMENT
   Roles
   Roles

- RoleID INT PK
- RoleName VARCHAR(255)

  Users
  Users

- UserID INT PK
- UserName VARCHAR(255)
- Password VARCHAR(255)
- IsActive BIT
- CreatedAt DATETIME2

  UserRoles
  UserRoles

- UserRoleID INT PK
- UserID INT FK → Users.UserID
- RoleID INT FK → Roles.RoleID

  Staff
  Staff

- StaffID INT PK
- StaffName VARCHAR(255)
- Phone VARCHAR(20)
- Email VARCHAR(255)
- UserID INT FK → Users.UserID

  Shifts
  Shifts

- ShiftID INT PK
- StartTime TIME
- EndTime TIME

  ShiftLogs
  ShiftLogs

- ShiftLogID INT PK
- StaffID INT FK → Staff.StaffID
- ShiftID INT FK → Shifts.ShiftID

2. HR EXTENSION (NEW)

Timekeeping
Timekeeping

- Id INT PK
- StaffID INT FK → Staff.StaffID
- Date DATE
- CheckIn DATETIME2
- CheckOut DATETIME2

  Wage
  Wage

- Id INT PK
- StaffID INT FK → Staff.StaffID (UNIQUE)
- BaseSalary DECIMAL(10,2)
- HourlyRate DECIMAL(10,2)

3. MENU MANAGEMENT

Categories
Categories

- CategoryID INT PK
- CategoryName VARCHAR(255)

  Products
  Products

- ProductID INT PK
- ProductName VARCHAR(255)
- Image VARCHAR(MAX)
- Price DECIMAL(10,2)
- Description VARCHAR(MAX)
- IsAvailable BIT
- ProductType INT (1=Single, 2=Combo)
- CategoryID INT FK → Categories.CategoryID

  ComboProducts
  ComboProducts

- ComboProductID INT PK
- ComboID INT FK → Products.ProductID
- ProductID INT FK → Products.ProductID
- Quantity INT

4. INVENTORY (NEW)

Ingredients
Ingredients

- IngredientID INT PK
- Name NVARCHAR(100)
- Unit NVARCHAR(20)
- StockQuantity DECIMAL(10,2)
- MinStock DECIMAL(10,2)

  IngredientProducts
  IngredientProducts

- Id INT PK
- ProductID INT FK → Products.ProductID
- IngredientID INT FK → Ingredients.IngredientID
- Quantity DECIMAL(10,2)

5. SUPPLIER & IMPORT (NEW)

Suppliers
Suppliers

- SupplierID INT PK
- Name NVARCHAR(100)
- Phone VARCHAR(20)
- Email VARCHAR(100)

  Receipts
  Receipts

- ReceiptID INT PK
- SupplierID INT FK → Suppliers.SupplierID
- StaffID INT FK → Staff.StaffID
- CreatedDate DATETIME2

  ReceiptIngredients
  ReceiptIngredients

- Id INT PK
- ReceiptID INT FK → Receipts.ReceiptID
- IngredientID INT FK → Ingredients.IngredientID
- Quantity DECIMAL(10,2)
- Price DECIMAL(10,2)

6. RESTAURANT TABLE
   RestaurantTables
   RestaurantTables

- TableID INT PK
- TableName VARCHAR(255)
- Capacity INT
- Status INT (0=Available,1=Occupied,2=Reserved)

7. RESERVATION (NEW)
   Reservations
   Reservations

- ReservationID INT PK
- CustomerID INT FK → Customers.CustomerID
- TableID INT FK → RestaurantTables.TableID
- ReservationTime DATETIME2
- NumberOfGuests INT
- Status INT (0=Pending,1=Confirmed,2=Cancelled)

8. CUSTOMER

Customers
Customers

- CustomerID INT PK
- Name VARCHAR(255)
- Sex VARCHAR(10)
- Email VARCHAR(255)
- Phone VARCHAR(20)
- BirthYear INT
- CreatedAt DATETIME2

9. ORDER SYSTEM

OrderSessions
OrderSessions

- OrderSessionID INT PK
- StartTime DATETIME2
- EndTime DATETIME2
- Status INT
- TableID INT FK → RestaurantTables.TableID
- CustomerID INT FK → Customers.CustomerID

  Orders
  Orders

- OrderID INT PK
- Status INT
- CreatedTime DATETIME2
- TotalAmount DECIMAL(10,2)
- OrderSessionID INT FK → OrderSessions.OrderSessionID
- StaffID INT FK → Staff.StaffID

  OrderProducts
  OrderProducts

- OrderProductID INT PK
- OrderID INT FK → Orders.OrderID
- ProductID INT FK → Products.ProductID
- Quantity INT
- Price DECIMAL(10,2)
- Status INT

10. PAYMENT SYSTEM

Invoices
Invoices

- InvoiceID INT PK
- CreatedDate DATETIME2
- TotalAmount DECIMAL(10,2)
- OrderID INT FK → Orders.OrderID

  Payments
  Payments

- PaymentID INT PK
- Method VARCHAR(50)
- Amount DECIMAL(10,2)
- Status INT
- PaymentTime DATETIME2
- InvoiceID INT FK → Invoices.InvoiceID

11. QUAN HỆ TỔNG QUÁT
    Inventory Flow
    Product → IngredientProducts → Ingredients
    Suppliers → Receipts → ReceiptIngredients → Ingredients
    Business Flow
    Customer → Reservation → Table
    Customer → OrderSession → Orders → OrderProducts → Invoice → Payment
    Staff Flow
    Staff → Orders
    Staff → Receipts
    Staff → Timekeeping → Wage
