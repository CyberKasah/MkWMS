using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MkWMS.Desktop.ViewModels;

public partial class DocumentDetailsViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private readonly NavigationService _navigation;
    private readonly int _documentId;
    private readonly CounterpartiesViewModel _counterpartiesVM;

    [ObservableProperty] private DocumentDto? _document;


    [ObservableProperty] private ObservableCollection<DocumentItemDto> _items = new();


    [ObservableProperty] private decimal _totalQuantity;
    [ObservableProperty] private decimal _totalVat;
    [ObservableProperty] private decimal _totalSum;

    public DocumentDetailsViewModel(
        ApiClient apiClient,
        NavigationService navigation,
        int documentId,
        CounterpartiesViewModel counterpartiesVM)
    {
        _apiClient = apiClient;
        _navigation = navigation;
        _documentId = documentId;
        _counterpartiesVM = counterpartiesVM;

        _ = LoadDocumentDetailsAsync();
    }

    [RelayCommand]
    private async Task LoadDocumentDetailsAsync()
    {
        if (IsLoading) return;
        IsLoading = true;
        ClearError();

        try
        {

            var doc = await _apiClient.GetDocumentByIdAsync(_documentId);

            if (doc != null)
            {
                Document = doc;


                Items.Clear();
                if (doc.Items != null)
                {
                    foreach (var item in doc.Items)
                    {
                        Items.Add(item);
                    }


                    CalculateTotals();
                }
            }
            else
            {
                SetError("Не удалось загрузить детали документа");
            }
        }
        catch (Exception ex)
        {
            SetError($"Ошибка при загрузке: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void CalculateTotals()
    {
        TotalQuantity = Items.Sum(x => x.Quantity);
        TotalVat = Items.Sum(x => x.VatSum);

        TotalSum = Items.Sum(x => (x.Quantity * (x.Price ?? 0)) + x.VatSum);
    }

    [RelayCommand]
    private void GoBack()
    {

        _navigation.Navigate(new DocumentsViewModel(_apiClient, _navigation, _counterpartiesVM));
    }

    [RelayCommand]
    private async Task Refresh()
    {
        await LoadDocumentDetailsAsync();
    }
}