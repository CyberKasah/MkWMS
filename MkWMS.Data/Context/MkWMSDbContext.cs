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
    public DbSet<StorageLocation> StorageLocations => Set<StorageLocation>();
    public DbSet<Counterparty> Counterparties => Set<Counterparty>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();








    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>().HaveColumnType("timestamptz");
        configurationBuilder.Properties<DateTime?>().HaveColumnType("timestamptz");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Роли");
            entity.Property(p => p.Name).HasColumnName("Название");
        });


        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Пользователи");
            entity.Property(p => p.Login).HasColumnName("Логин");
            entity.Property(p => p.PasswordHash).HasColumnName("ХешПароля");
            entity.Property(p => p.FullName).HasColumnName("ФИО");
            entity.Property(p => p.IsActive).HasColumnName("Активен");


            entity.Property(p => p.CreatedDate).HasColumnName("ДатаСоздания").HasDefaultValueSql("now()");
            entity.Property(u => u.RequiresPasswordChange).HasColumnName("ТребуетсяСменаПароля");

            entity.HasIndex(u => u.Login).IsUnique().HasDatabaseName("UX_Пользователь_Логин");
            entity.HasOne(u => u.Warehouse).WithMany().HasForeignKey(u => u.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });


        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.ToTable("ПользователиРоли");
            entity.HasKey(ur => ur.Id);
            entity.Property(ur => ur.UserId).HasColumnName("ПользовательId");
            entity.Property(ur => ur.RoleId).HasColumnName("РольId");

            entity.HasOne(ur => ur.User).WithMany(u => u.UserRoles).HasForeignKey(ur => ur.UserId);
            entity.HasOne(ur => ur.Role).WithMany(r => r.UserRoles).HasForeignKey(ur => ur.RoleId);
            entity.HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique().HasDatabaseName("UX_Пользователь_Роль");
        });


        modelBuilder.Entity<Counterparty>(entity =>
        {
            entity.ToTable("Контрагенты");
            entity.Property(p => p.Name).HasColumnName("Название");
            entity.Property(p => p.IsSupplier).HasColumnName("Поставщик");
            entity.Property(p => p.IsCustomer).HasColumnName("Покупатель");
            entity.Property(p => p.Address).HasColumnName("Адрес");
        });


        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.ToTable("Склады");
            entity.Property(p => p.Name).HasColumnName("Название");
            entity.Property(p => p.Address).HasColumnName("Адрес");
            entity.Property(p => p.IsActive).HasColumnName("Активен");
        });


        modelBuilder.Entity<Department>(entity =>
        {
            entity.ToTable("Подразделения");
            entity.Property(p => p.Name).HasColumnName("Название");
            entity.Property(p => p.WarehouseId).HasColumnName("СкладId");
            entity.HasOne(d => d.Warehouse).WithMany(w => w.Departments).HasForeignKey(d => d.WarehouseId).OnDelete(DeleteBehavior.Cascade);
        });


        modelBuilder.Entity<DocumentType>(entity =>
        {
            entity.ToTable("ТипыДокументов");
            entity.Property(p => p.Name).HasColumnName("Название");
        });


        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("Документы");
            entity.Property(p => p.DocumentNumber).HasColumnName("НомерДокумента");
            entity.Property(p => p.Comment).HasColumnName("Комментарий");


            entity.Property(p => p.CreatedDate).HasColumnName("ДатаСоздания").HasDefaultValueSql("now()");

            entity.Property(p => p.CreatedByUserId).HasColumnName("СозданПользователемId");
            entity.Property(p => p.DocumentTypeId).HasColumnName("ТипДокументаId");
            entity.Property(p => p.WarehouseId).HasColumnName("СкладId");
            entity.Property(p => p.DepartmentId).HasColumnName("ПодразделениеId");
            modelBuilder.Entity<Document>().Property(p => p.CounterpartyId).HasColumnName("КонтрагентId");
            modelBuilder.Entity<Document>().Property(p => p.BaseDocumentId).HasColumnName("ОснованиеId");
            modelBuilder.Entity<Document>().Property(p => p.ExternalNumber).HasColumnName("ВходящийНомер");
            modelBuilder.Entity<Document>().Property(p => p.ExternalDate).HasColumnName("ВходящаяДата");
            modelBuilder.Entity<Document>().Property(p => p.FilePath).HasColumnName("ПутьКФайлу");

            modelBuilder.Entity<Document>().HasOne(d => d.Counterparty).WithMany().HasForeignKey(d => d.CounterpartyId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Document>().HasOne(d => d.BaseDocument).WithMany().HasForeignKey(d => d.BaseDocumentId).OnDelete(DeleteBehavior.Restrict);

            entity.Property(d => d.Status)
                .HasColumnName("Статус")
                .HasConversion(
                    v => v == DocumentStatus.Draft ? "Черновик" :
                         v == DocumentStatus.Posted ? "Проведен" : "Отменен",
                    v => v == "Черновик" ? DocumentStatus.Draft :
                         v == "Проведен" ? DocumentStatus.Posted : DocumentStatus.Cancelled
                );

            entity.HasOne(d => d.DocumentType).WithMany(dt => dt.Documents).HasForeignKey(d => d.DocumentTypeId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Warehouse).WithMany().HasForeignKey(d => d.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.Department).WithMany().HasForeignKey(d => d.DepartmentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(d => d.CreatedByUser).WithMany(u => u.CreatedDocuments).HasForeignKey(d => d.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(d => d.DocumentNumber).HasDatabaseName("IX_Документы_Номер");
        });


        modelBuilder.Entity<DocumentItem>(entity =>
        {
            entity.ToTable("СтрокиДокументов");

            entity.Property(p => p.DocumentId).HasColumnName("ДокументId");
            entity.Property(p => p.ProductId).HasColumnName("ТоварId");
            entity.Property(p => p.BatchId).HasColumnName("ПартияId");
            entity.Property(p => p.SerialNumberId).HasColumnName("СерийныйНомерId");
            entity.Property(p => p.Quantity).HasColumnName("Количество").HasPrecision(18, 4);
            entity.Property(p => p.Price).HasColumnName("Цена").HasPrecision(18, 4);

            entity.Property(di => di.VatSum)
                  .HasColumnName("СуммаНДС")
                  .HasPrecision(18, 2)
                  .HasDefaultValue(0);

            entity.Property(p => p.StorageLocationId).HasColumnName("ЯчейкаId");
            entity.HasOne(di => di.StorageLocation).WithMany().HasForeignKey(di => di.StorageLocationId).OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(di => di.DocumentId).HasDatabaseName("IX_СтрокиДокументов_Документ");
        });


        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Товары");
            entity.Property(p => p.Name).HasColumnName("Название");
            entity.Property(p => p.Article).HasColumnName("Артикул");
            entity.Property(p => p.Barcode).HasColumnName("Штрихкод");
            entity.Property(p => p.Unit).HasColumnName("ЕдиницаИзмерения");
            entity.Property(p => p.UseSerialNumbers).HasColumnName("ИспользоватьСерийныеНомера");
            entity.Property(p => p.UseBatches).HasColumnName("ИспользоватьПартии");


            entity.Property(p => p.CreatedDate).HasColumnName("ДатаСоздания").HasDefaultValueSql("now()");
            modelBuilder.Entity<Product>().Property(p => p.RfidBaseTag).HasColumnName("RFID_BaseTag");

            entity.Property(p => p.PurchasePrice).HasColumnName("ЦенаЗакупки");
            entity.Property(p => p.RetailPrice).HasColumnName("ЦенаРозничная");

            entity.Property(p => p.VatRate)
                  .HasColumnName("СтавкаНДС")
                  .HasColumnType("decimal(5,2)")
                  .HasDefaultValue(22m);






            entity.HasIndex(p => p.Barcode)
                  .IsUnique()
                  .HasDatabaseName("UX_Товары_Штрихкод")
                  .HasFilter("\"Штрихкод\" IS NOT NULL");
        });


        modelBuilder.Entity<Batch>(entity =>
        {
            entity.ToTable("Партии");
            entity.Property(p => p.BatchNumber).HasColumnName("НомерПартии");
            entity.Property(p => p.ProductionDate).HasColumnName("ДатаПроизводства");
            entity.Property(p => p.ExpirationDate).HasColumnName("СрокГодности");
            entity.Property(p => p.ProductId).HasColumnName("ТоварId");
            entity.Property(b => b.VsdUuid).HasColumnName("VsdUuid");
        });


        modelBuilder.Entity<SerialNumber>().ToTable("СерийныеНомера");
        modelBuilder.Entity<SerialNumber>().Property(p => p.Number).HasColumnName("СерийныйНомер");
        modelBuilder.Entity<SerialNumber>().Property(p => p.Status).HasColumnName("Статус");
        modelBuilder.Entity<SerialNumber>().Property(p => p.ProductId).HasColumnName("ТоварId");
        modelBuilder.Entity<SerialNumber>().Property(p => p.RfidTag).HasColumnName("RFID_Tag");
        modelBuilder.Entity<SerialNumber>().Property(p => p.DataMatrix).HasColumnName("DataMatrix");


        modelBuilder.Entity<StockMovement>(entity =>
        {
            entity.ToTable("ДвиженияТоваров");
            entity.Property(p => p.DocumentId).HasColumnName("ДокументId");
            entity.Property(p => p.ProductId).HasColumnName("ТоварId");
            entity.Property(p => p.WarehouseId).HasColumnName("СкладId");
            entity.Property(p => p.BatchId).HasColumnName("ПартияId");
            entity.Property(p => p.SerialNumberId).HasColumnName("СерийныйНомерId");
            entity.Property(p => p.QuantityChange).HasColumnName("ИзменениеКоличество").HasPrecision(18, 4);


            entity.Property(p => p.MovementDate).HasColumnName("ДатаДвижения").HasDefaultValueSql("now()");

            entity.HasOne(sm => sm.Document).WithMany().HasForeignKey(sm => sm.DocumentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(sm => sm.Product).WithMany().HasForeignKey(sm => sm.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(sm => sm.Warehouse).WithMany().HasForeignKey(sm => sm.WarehouseId).OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<StockMovement>().Property(p => p.StorageLocationId).HasColumnName("ЯчейкаId");
            modelBuilder.Entity<StockMovement>().HasOne(sm => sm.StorageLocation).WithMany().HasForeignKey(sm => sm.StorageLocationId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(sm => sm.ProductId).HasDatabaseName("IX_Движения_Товар");
            entity.HasIndex(sm => sm.WarehouseId).HasDatabaseName("IX_Движения_Склад");
            entity.Property(p => p.Price).HasColumnName("Цена").HasPrecision(18, 4);
        });


        modelBuilder.Entity<StockBalance>(entity =>
        {
            entity.ToTable("Остатки");
            entity.Property(p => p.ProductId).HasColumnName("ТоварId");
            entity.Property(p => p.WarehouseId).HasColumnName("СкладId");
            entity.Property(p => p.BatchId).HasColumnName("ПартияId");
            entity.Property(p => p.Quantity).HasColumnName("Количество").HasPrecision(18, 4);
            modelBuilder.Entity<StockBalance>().Property(p => p.StorageLocationId).HasColumnName("ЯчейкаId");
            modelBuilder.Entity<StockBalance>().HasOne(sb => sb.StorageLocation).WithMany().HasForeignKey(sb => sb.StorageLocationId).OnDelete(DeleteBehavior.Restrict);




            entity.HasIndex(sb => new { sb.ProductId, sb.WarehouseId, sb.BatchId, sb.StorageLocationId }).HasDatabaseName("IX_Остатки_ТоварСкладПартияЯчейка");
            entity.HasIndex(sb => new { sb.ProductId, sb.WarehouseId }).HasDatabaseName("IX_Остатки_ТоварСклад");
        });


        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.ToTable("ЖурналДействий");
            entity.Property(p => p.UserId).HasColumnName("ПользовательId");
            entity.Property(p => p.Action).HasColumnName("Действие");


            entity.Property(p => p.ActionDate).HasColumnName("ДатаДействия").HasDefaultValueSql("now()");

            modelBuilder.Entity<AuditLog>().Property(p => p.EntityName).HasColumnName("Сущность");
            modelBuilder.Entity<AuditLog>().Property(p => p.EntityId).HasColumnName("IdЗаписи");
            modelBuilder.Entity<AuditLog>().Property(p => p.ChangesJson).HasColumnName("ИзмененияJSON");
            entity.HasOne(a => a.User).WithMany().HasForeignKey(a => a.UserId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StorageLocation>(entity =>
        {
            entity.ToTable("ЯчейкиХранения");
            entity.Property(p => p.WarehouseId).HasColumnName("СкладId");
            entity.Property(p => p.Name).HasColumnName("Название");
            entity.Property(p => p.RfidTag).HasColumnName("RFID_Метка");
            entity.Property(p => p.CellType).HasColumnName("ТипЯчейки");
            entity.HasOne(s => s.Warehouse).WithMany().HasForeignKey(s => s.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        });


        modelBuilder.Entity<Counterparty>(entity =>
        {
            entity.ToTable("Контрагенты");
            entity.Property(p => p.Name).HasColumnName("Название");
            entity.Property(p => p.IsSupplier).HasColumnName("Поставщик");
            entity.Property(p => p.IsCustomer).HasColumnName("Покупатель");
            entity.Property(p => p.Address).HasColumnName("Адрес");


            entity.Property(p => p.INN).HasColumnName("ИНН");
            entity.Property(p => p.KPP).HasColumnName("КПП");
        });


        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("ТокеныОбновления");
            entity.HasKey(rt => rt.Id);
            entity.Property(rt => rt.UserId).HasColumnName("ПользовательId");
            entity.Property(rt => rt.Token).HasColumnName("Токен").HasMaxLength(200).IsRequired();
            entity.Property(rt => rt.ExpiresAt).HasColumnName("ДатаИстечения");
            entity.Property(rt => rt.CreatedAt).HasColumnName("ДатаСоздания");
            entity.Property(rt => rt.RevokedAt).HasColumnName("ДатаОтзыва");
            entity.Property(rt => rt.ReplacedByToken).HasColumnName("ЗамененТокеном").HasMaxLength(200);

            entity.HasOne(rt => rt.User).WithMany().HasForeignKey(rt => rt.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(rt => rt.Token).IsUnique().HasDatabaseName("UX_ТокеныОбновления_Токен");
            entity.HasIndex(rt => rt.UserId).HasDatabaseName("IX_ТокеныОбновления_Пользователь");
        });






        modelBuilder.HasSequence<int>("documentnumberseq").StartsAt(1).IncrementsBy(1);
    }
}