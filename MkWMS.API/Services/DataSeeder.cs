using Microsoft.EntityFrameworkCore;
using MkWMS.Data.Context;
using MkWMS.Data.Entities;
using MkWMS.Data.Constants;

namespace MkWMS.API.Services;

public class DataSeeder
{
    private readonly MkWMSDbContext _context;

    public DataSeeder(MkWMSDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedAdminUserAsync();
        await SeedWarehousesAsync();
        await SeedDepartmentsAsync();
        await SeedDocumentTypesAsync();
        await SeedStorageLocationsAsync();
        await SeedCounterpartiesAsync();
        await SeedTestProductsAsync();

        await _context.SaveChangesAsync();
        Console.WriteLine("DataSeeder выполнен успешно!");
    }

    private async Task SeedRolesAsync()
    {
        if (await _context.Roles.AnyAsync()) return;





        _context.Roles.AddRange(
            new Role { Name = RoleNames.Admin },
            new Role { Name = RoleNames.Manager },
            new Role { Name = RoleNames.Operator }
        );
    }

    private async Task SeedAdminUserAsync()
    {
        if (await _context.Users.AnyAsync(u => u.Login == "admin")) return;

        var admin = new User
        {
            Login = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
            FullName = "Системный администратор",
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            RequiresPasswordChange = true,
            WarehouseId = 1
        };

        _context.Users.Add(admin);
        await _context.SaveChangesAsync();


        _context.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = 1 });
    }

    private async Task SeedWarehousesAsync()
    {
        if (await _context.Warehouses.AnyAsync()) return;

        _context.Warehouses.AddRange(
            new Warehouse
            {
                Name = "Главный склад",
                Address = "г. Москва, ул. Примерная, д. 1",
                IsActive = true
            },
            new Warehouse
            {
                Name = "Склад №2 (резерв)",
                Address = "г. Москва, ул. Складская, д. 14",
                IsActive = true
            }
        );
    }

    private async Task SeedDepartmentsAsync()
    {
        if (await _context.Departments.AnyAsync()) return;

        _context.Departments.Add(new Department
        {
            Name = "Основное подразделение",
            WarehouseId = 1
        });
    }

    private async Task SeedDocumentTypesAsync()
    {
        if (await _context.DocumentTypes.AnyAsync()) return;

        _context.DocumentTypes.AddRange(
            new DocumentType { Name = "Приход" },
            new DocumentType { Name = "Расход" },
            new DocumentType { Name = "Перемещение" },
            new DocumentType { Name = "Инвентаризация" },
            new DocumentType { Name = "Списание" }
        );
    }



    private async Task SeedStorageLocationsAsync()
    {
        if (await _context.StorageLocations.AnyAsync()) return;

        _context.StorageLocations.AddRange(
            new StorageLocation { WarehouseId = 1, Name = "А1-01", RfidTag = "RFID-A1-01", CellType = "Хранение" },
            new StorageLocation { WarehouseId = 1, Name = "А1-02", RfidTag = "RFID-A1-02", CellType = "Хранение" },
            new StorageLocation { WarehouseId = 1, Name = "Приемка-1", RfidTag = "RFID-PR-01", CellType = "Приемка" },
            new StorageLocation { WarehouseId = 1, Name = "Отгрузка-1", RfidTag = "RFID-OT-01", CellType = "Отгрузка" },
            new StorageLocation { WarehouseId = 2, Name = "Б1-01", RfidTag = "RFID-B1-01", CellType = "Хранение" },
            new StorageLocation { WarehouseId = 2, Name = "Б1-02", RfidTag = "RFID-B1-02", CellType = "Хранение" }
        );
    }


    private async Task SeedCounterpartiesAsync()
    {
        if (await _context.Counterparties.AnyAsync()) return;

        _context.Counterparties.AddRange(
            new Counterparty { Name = "ООО «ПродТорг»", INN = "7701234567", KPP = "770101001", Address = "г. Москва, Складской пр-д, 5", IsSupplier = true, IsCustomer = false },
            new Counterparty { Name = "ИП Сидоров А.В.", INN = "770312345678", Address = "г. Москва, ул. Рыночная, 2", IsSupplier = true, IsCustomer = true },
            new Counterparty { Name = "ООО «КурБер»", INN = "9705012345", KPP = "770501001", Address = "г. Москва, ул. Кулинарная, 10", IsSupplier = false, IsCustomer = true }
        );
    }

    private async Task SeedTestProductsAsync()
    {
        if (await _context.Products.AnyAsync()) return;



        _context.Products.AddRange(
            new Product
            {
                Name = "Ноутбук Lenovo ThinkPad",
                Article = "LEN-TP-X1",
                Barcode = "1234567890123",
                Unit = "шт",
                UseSerialNumbers = true,
                UseBatches = false,
                CreatedDate = DateTime.UtcNow,
                PurchasePrice = 65000m,
                RetailPrice = 84500m,
                VatRate = 20m,
                IsMarked = false,
                IsVet = false,
                RfidBaseTag = "RFID-PROD-001"
            },
            new Product
            {
                Name = "Молоко 3.2% 1л",
                Article = "MLK-001",
                Barcode = "4601234567890",
                Unit = "л",
                UseSerialNumbers = false,
                UseBatches = true,
                CreatedDate = DateTime.UtcNow,
                PurchasePrice = 65m,
                RetailPrice = 89m,
                VatRate = 10m,
                IsMarked = true,
                IsVet = true,
                RfidBaseTag = "RFID-PROD-002"
            },
            new Product
            {
                Name = "Кабель HDMI 2м",
                Article = "CBL-HDMI-2",
                Barcode = "9876543210987",
                Unit = "шт",
                UseSerialNumbers = false,
                UseBatches = false,
                CreatedDate = DateTime.UtcNow,
                PurchasePrice = 350m,
                RetailPrice = 590m,
                VatRate = 20m,
                IsMarked = false,
                IsVet = false,
                RfidBaseTag = "RFID-PROD-003"
            }
        );
    }
}
