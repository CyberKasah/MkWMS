namespace MkWMS.Desktop.Services;

public static class ApiEndpoints
{
    // --- Auth ---
    public const string Login = "auth/login";
    public const string ChangePassword = "auth/change-password";

    // --- Справочники (Каталог) ---
    public const string Products = "products";
    public const string Batches = "batches";
    public const string SerialNumbers = "serialnumbers";
    public const string Warehouses = "warehouses";
    public const string Departments = "departments";
    public const string StorageLocations = "storagelocations";
    public const string Counterparties = "counterparties";
    public const string DocumentTypes = "documenttypes";

    // --- Документооборот и Склад ---
    public const string Documents = "documents";
    public const string Inventory = "inventory";
    public const string StockBalances = "stockbalances";
    public const string StockMovements = "stockmovements";

    // --- Администрирование ---
    public const string Users = "users";
    public const string Roles = "roles";
    public const string AuditLogs = "auditlogs";

    // --- Сервисные модули v2.0 ---
    public const string Reports = "reports";
    public const string Print = "print";
    public const string Files = "files";
    public const string Excel = "excel";
}