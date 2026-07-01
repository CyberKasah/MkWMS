using CommunityToolkit.Mvvm.ComponentModel;

namespace MkWMS.API.DTOs;

public partial class CreateDocumentDto : ObservableObject
{
    [ObservableProperty] private string? number;
    [ObservableProperty] private int documentTypeId;
    [ObservableProperty] private int warehouseId;
    [ObservableProperty] private int? baseDocumentId;
    [ObservableProperty] private int? counterpartyId;
    [ObservableProperty] private string? externalNumber;
    [ObservableProperty] private DateTime? externalDate;
    [ObservableProperty] private string? comment;
    public List<CreateDocumentItemDto> Items { get; set; } = new();
}

public partial class CreateDocumentItemDto : ObservableObject
{
    [ObservableProperty] private int productId;
    [ObservableProperty] private int? batchId;
    [ObservableProperty] private int? serialNumberId;
    [ObservableProperty] private int? storageLocationId;
    [ObservableProperty] private decimal quantity;
    [ObservableProperty] private decimal price;
    [ObservableProperty] private decimal vatSum;






    public decimal SubTotal => Quantity * Price;
    public decimal TotalSum => (Quantity * Price) + VatSum;

    partial void OnQuantityChanged(decimal value)
    {
        OnPropertyChanged(nameof(SubTotal));
        OnPropertyChanged(nameof(TotalSum));
    }
    partial void OnPriceChanged(decimal value)
    {
        OnPropertyChanged(nameof(SubTotal));
        OnPropertyChanged(nameof(TotalSum));
    }
    partial void OnVatSumChanged(decimal value) => OnPropertyChanged(nameof(TotalSum));
}
