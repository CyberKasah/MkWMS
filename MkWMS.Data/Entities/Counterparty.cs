namespace MkWMS.Data.Entities;

public class Counterparty
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? INN { get; set; }
    public string? KPP { get; set; }
    public string? Address { get; set; }
    public bool IsSupplier { get; set; }
    public bool IsCustomer { get; set; }
}