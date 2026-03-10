using Microsoft.EntityFrameworkCore;
using MkWMS.Data.Context;
using MkWMS.Data.Entities;
using MkWMS.Data.Enums;

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
        await SeedTestProductsAsync();

        await _context.SaveChangesAsync();
        Console.WriteLine("DataSeeder выполнен успешно!");
    }

    private async Task SeedRolesAsync()
    {
        if (await _context.Roles.AnyAsync()) return;

        _context.Roles.AddRange(
            new Role { Name = "Администратор" },
            new Role { Name = "Менеджер" },
            new Role { Name = "Оператор" }
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
            WarehouseId = 1 // будет создан ниже
        };

        _context.Users.Add(admin);
        await _context.SaveChangesAsync();

        // Назначаем роль Администратор (Id = 1)
        _context.UserRoles.Add(new UserRole { UserId = admin.Id, RoleId = 1 });
    }

    private async Task SeedWarehousesAsync()
    {
        if (await _context.Warehouses.AnyAsync()) return;

        _context.Warehouses.Add(new Warehouse
        {
            Name = "Главный склад",
            Address = "г. Москва, ул. Примерная, д. 1",
            IsActive = true
        });
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
                CreatedDate = DateTime.UtcNow
            },
            new Product
            {
                Name = "Молоко 3.2% 1л",
                Article = "MLK-001",
                Barcode = "4601234567890",
                Unit = "л",
                UseSerialNumbers = false,
                UseBatches = true,
                CreatedDate = DateTime.UtcNow
            },
            new Product
            {
                Name = "Кабель HDMI 2м",
                Article = "CBL-HDMI-2",
                Barcode = "9876543210987",
                Unit = "шт",
                UseSerialNumbers = false,
                UseBatches = false,
                CreatedDate = DateTime.UtcNow
            }
        );
    }
}