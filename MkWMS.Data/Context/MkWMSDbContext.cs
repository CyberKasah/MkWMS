using Microsoft.EntityFrameworkCore;
using MkWMS.Data.Entities;
using MkWMS.Data.Enums;

namespace MkWMS.Data.Context;

public class MkWMSDbContext : DbContext
{
    public MkWMSDbContext(DbContextOptions<MkWMSDbContext> options)
        : base(options) { }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<DocumentType> DocumentTypes => Set<DocumentType>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentItem> DocumentItems => Set<DocumentItem>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Batch> Batches => Set<Batch>();
    public DbSet<SerialNumber> SerialNumbers => Set<SerialNumber>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockBalance> StockBalances => Set<StockBalance>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ===== РОЛИ =====
        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("ПользователиРоли");
            entity.HasKey(ur => ur.Id);  // если есть PK Id
            entity.Property(ur => ur.UserId).HasColumnName("ПользовательId");
            entity.Property(ur => ur.RoleId).HasColumnName("РольId");
            entity.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId);
            entity.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId);
        });
        modelBuilder.Entity<UserRole>().HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique().HasDatabaseName("UX_Пользователь_Роль");
        // Убедись, что User и Role тоже правильно настроены
        modelBuilder.Entity<User>().ToTable("Пользователи");
        modelBuilder.Entity<User>().HasOne(u => u.Warehouse).WithMany().HasForeignKey(u => u.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Role>().ToTable("Роли");
        modelBuilder.Entity<Role>().Property(p => p.Name).HasColumnName("Название");

        // ===== ПОЛЬЗОВАТЕЛИ =====
        modelBuilder.Entity<User>().ToTable("Пользователи");
        modelBuilder.Entity<User>().Property(p => p.Login).HasColumnName("Логин");
        modelBuilder.Entity<User>().HasIndex(u => u.Login).IsUnique().HasDatabaseName("UX_Пользователь_Логин");
        modelBuilder.Entity<User>().Property(p => p.PasswordHash).HasColumnName("ХешПароля");
        modelBuilder.Entity<User>().Property(p => p.FullName).HasColumnName("ФИО");
        modelBuilder.Entity<User>().Property(p => p.IsActive).HasColumnName("Активен");
        modelBuilder.Entity<User>().Property(p => p.CreatedDate).HasColumnName("ДатаСоздания");
        modelBuilder.Entity<User>().Property(u => u.RequiresPasswordChange).HasColumnName("ТребуетсяСменаПароля");

        // ===== USER ROLES =====
        modelBuilder.Entity<UserRole>().ToTable("ПользователиРоли");

        // ===== СКЛАДЫ =====
        modelBuilder.Entity<Warehouse>().ToTable("Склады");
        modelBuilder.Entity<Warehouse>().Property(p => p.Name).HasColumnName("Название");
        modelBuilder.Entity<Warehouse>().Property(p => p.Address).HasColumnName("Адрес");
        modelBuilder.Entity<Warehouse>().Property(p => p.IsActive).HasColumnName("Активен");

        // ===== ПОДРАЗДЕЛЕНИЯ =====
        modelBuilder.Entity<Department>().ToTable("Подразделения");
        modelBuilder.Entity<Department>().Property(p => p.Name).HasColumnName("Название");
        modelBuilder.Entity<Department>().Property(p => p.WarehouseId).HasColumnName("СкладId");
        modelBuilder.Entity<Department>().HasOne(d => d.Warehouse).WithMany(w => w.Departments).HasForeignKey(d => d.WarehouseId).OnDelete(DeleteBehavior.Cascade);

        // ===== ТИПЫ ДОКУМЕНТОВ =====
        modelBuilder.Entity<DocumentType>().ToTable("ТипыДокументов");
        modelBuilder.Entity<DocumentType>().Property(p => p.Name).HasColumnName("Название");

        // ===== ДОКУМЕНТЫ =====
        modelBuilder.Entity<Document>().ToTable("Документы");
        modelBuilder.Entity<Document>().Property(p => p.DocumentNumber).HasColumnName("НомерДокумента");
        modelBuilder.Entity<Document>().Property(p => p.Status).HasColumnName("Статус");
        modelBuilder.Entity<Document>().Property(d => d.Status).HasConversion<int>();
        modelBuilder.Entity<Document>().Property(p => p.Comment).HasColumnName("Комментарий");
        modelBuilder.Entity<Document>().Property(p => p.CreatedDate).HasColumnName("ДатаСоздания");
        modelBuilder.Entity<Document>().Property(p => p.CreatedByUserId).HasColumnName("СозданПользователемId");
        modelBuilder.Entity<Document>().Property(p => p.DocumentTypeId).HasColumnName("ТипДокументаId");
        modelBuilder.Entity<Document>().Property(p => p.WarehouseId).HasColumnName("СкладId");
        modelBuilder.Entity<Document>().Property(p => p.DepartmentId).HasColumnName("ПодразделениеId");
        modelBuilder.Entity<Document>().HasOne(d => d.DocumentType).WithMany(dt => dt.Documents).HasForeignKey(d => d.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Document>().HasOne(d => d.Warehouse).WithMany().HasForeignKey(d => d.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Document>().HasOne(d => d.Department).WithMany().HasForeignKey(d => d.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Document>().HasOne(d => d.CreatedByUser).WithMany(u => u.CreatedDocuments).HasForeignKey(d => d.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);



        // ===== СТРОКИ ДОКУМЕНТОВ =====
        modelBuilder.Entity<DocumentItem>().ToTable("СтрокиДокументов");
        modelBuilder.Entity<DocumentItem>().Property(p => p.Quantity).HasColumnName("Количество");
        modelBuilder.Entity<DocumentItem>().Property(p => p.Price).HasColumnName("Цена");
        modelBuilder.Entity<DocumentItem>().Property(p => p.DocumentId).HasColumnName("ДокументId");
        modelBuilder.Entity<DocumentItem>().Property(p => p.ProductId).HasColumnName("ТоварId");
        modelBuilder.Entity<DocumentItem>().Property(p => p.BatchId).HasColumnName("ПартияId");
        modelBuilder.Entity<DocumentItem>().Property(p => p.SerialNumberId).HasColumnName("СерийныйНомерId");
        modelBuilder.Entity<DocumentItem>().Property(d => d.Quantity).HasPrecision(18, 4);
        modelBuilder.Entity<DocumentItem>().Property(d => d.Price).HasPrecision(18, 4);

        // ===== ТОВАРЫ =====
        modelBuilder.Entity<Product>().ToTable("Товары");
        modelBuilder.Entity<Product>().Property(p => p.Name).HasColumnName("Название");
        modelBuilder.Entity<Product>().Property(p => p.Article).HasColumnName("Артикул");
        modelBuilder.Entity<Product>().Property(p => p.Barcode).HasColumnName("Штрихкод");
        modelBuilder.Entity<Product>().Property(p => p.Unit).HasColumnName("ЕдиницаИзмерения");
        modelBuilder.Entity<Product>().Property(p => p.UseSerialNumbers).HasColumnName("ИспользоватьСерийныеНомера");
        modelBuilder.Entity<Product>().Property(p => p.UseBatches).HasColumnName("ИспользоватьПартии");
        modelBuilder.Entity<Product>().Property(p => p.CreatedDate).HasColumnName("ДатаСоздания");
        modelBuilder.Entity<Product>().HasIndex(p => p.Barcode).IsUnique().HasDatabaseName("UX_Товары_Штрихкод").HasFilter("[Штрихкод] IS NOT NULL");

        // ===== ПАРТИИ =====
        modelBuilder.Entity<Batch>().ToTable("Партии");
        modelBuilder.Entity<Batch>().Property(p => p.BatchNumber).HasColumnName("НомерПартии");
        modelBuilder.Entity<Batch>().Property(p => p.ProductionDate).HasColumnName("ДатаПроизводства");
        modelBuilder.Entity<Batch>().Property(p => p.ExpirationDate).HasColumnName("СрокГодности");
        modelBuilder.Entity<Batch>().Property(p => p.ProductId).HasColumnName("ТоварId");

        // ===== СЕРИЙНЫЕ НОМЕРА =====
        modelBuilder.Entity<SerialNumber>().ToTable("СерийныеНомера");
        modelBuilder.Entity<SerialNumber>().Property(p => p.Number).HasColumnName("СерийныйНомер");
        modelBuilder.Entity<SerialNumber>().Property(p => p.Status).HasColumnName("Статус");
        modelBuilder.Entity<SerialNumber>().Property(p => p.ProductId).HasColumnName("ТоварId");

        // ===== ДВИЖЕНИЯ =====
        modelBuilder.Entity<StockMovement>().ToTable("ДвиженияТоваров");
        modelBuilder.Entity<StockMovement>().Property(p => p.DocumentId).HasColumnName("ДокументId");
        modelBuilder.Entity<StockMovement>().Property(p => p.ProductId).HasColumnName("ТоварId");
        modelBuilder.Entity<StockMovement>().Property(p => p.WarehouseId).HasColumnName("СкладId");
        modelBuilder.Entity<StockMovement>().Property(p => p.BatchId).HasColumnName("ПартияId");
        modelBuilder.Entity<StockMovement>().Property(p => p.SerialNumberId).HasColumnName("СерийныйНомерId");
        modelBuilder.Entity<StockMovement>().Property(p => p.QuantityChange).HasColumnName("ИзменениеКоличество");
        modelBuilder.Entity<StockMovement>().Property(p => p.MovementDate).HasColumnName("ДатаДвижения");
        modelBuilder.Entity<StockMovement>().Property(sm => sm.QuantityChange).HasPrecision(18, 4);
        modelBuilder.Entity<StockMovement>().HasOne(sm => sm.Document).WithMany().HasForeignKey(sm => sm.DocumentId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<StockMovement>().HasOne(sm => sm.Product).WithMany().HasForeignKey(sm => sm.ProductId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<StockMovement>().HasOne(sm => sm.Warehouse).WithMany().HasForeignKey(sm => sm.WarehouseId).OnDelete(DeleteBehavior.Restrict);

        // ===== ОСТАТКИ =====
        modelBuilder.Entity<StockBalance>().ToTable("Остатки");
        modelBuilder.Entity<StockBalance>().Property(p => p.ProductId).HasColumnName("ТоварId");
        modelBuilder.Entity<StockBalance>().Property(p => p.WarehouseId).HasColumnName("СкладId");
        modelBuilder.Entity<StockBalance>().Property(p => p.BatchId).HasColumnName("ПартияId");
        modelBuilder.Entity<StockBalance>().Property(p => p.Quantity).HasColumnName("Количество");
        modelBuilder.Entity<StockBalance>().Property(sb => sb.Quantity).HasPrecision(18, 4);
        modelBuilder.Entity<StockBalance>().HasIndex(sb => new { sb.ProductId, sb.WarehouseId, sb.BatchId }).IsUnique().HasDatabaseName("UX_Остатки_ТоварСкладПартия");

        // ===== ЖУРНАЛ =====
        modelBuilder.Entity<AuditLog>().ToTable("ЖурналДействий");
        modelBuilder.Entity<AuditLog>().Property(p => p.UserId).HasColumnName("ПользовательId");
        modelBuilder.Entity<AuditLog>().Property(p => p.Action).HasColumnName("Действие");
        modelBuilder.Entity<AuditLog>().Property(p => p.ActionDate).HasColumnName("ДатаДействия");
        modelBuilder.Entity<AuditLog>().HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Document>().HasIndex(d => d.DocumentNumber).HasDatabaseName("IX_Документы_Номер");
        modelBuilder.Entity<Product>().HasIndex(p => p.Barcode).HasDatabaseName("IX_Товары_Штрихкод");
        modelBuilder.Entity<DocumentItem>().HasIndex(di => di.DocumentId).HasDatabaseName("IX_СтрокиДокументов_Документ");
        modelBuilder.Entity<StockMovement>().HasIndex(sm => sm.ProductId).HasDatabaseName("IX_Движения_Товар");
        modelBuilder.Entity<StockMovement>().HasIndex(sm => sm.WarehouseId).HasDatabaseName("IX_Движения_Склад");
        modelBuilder.Entity<StockBalance>().HasIndex(sb => new { sb.ProductId, sb.WarehouseId }).HasDatabaseName("IX_Остатки_ТоварСклад");

    }
}