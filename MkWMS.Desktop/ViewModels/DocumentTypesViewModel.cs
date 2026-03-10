using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Models;
using MkWMS.Desktop.Services;
using MkWMS.Desktop.Views.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using MkWMS.Desktop.Views;
using System;
using System.Threading.Tasks;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace MkWMS.Desktop.ViewModels;

public partial class DocumentTypesViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;

    [ObservableProperty]
    private ObservableCollection<DocumentTypeDto> documentTypes = new();

    public DocumentTypesViewModel(ApiClient apiClient)
    {
        _apiClient = apiClient;
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        IsBusy = true;
        ClearError();

        try
        {
            var list = await _apiClient.GetDocumentTypesAsync();
            DocumentTypes = new ObservableCollection<DocumentTypeDto>(list ?? new List<DocumentTypeDto>()); 
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Refresh() => LoadAsync();
}