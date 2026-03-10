// ViewModels/DocumentsViewModel.cs
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

public partial class DocumentsViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;

    [ObservableProperty] private ObservableCollection<DocumentDto> documents = new();
    [ObservableProperty] private DocumentDto? selectedDocument;
    [ObservableProperty] private string searchText = string.Empty;

    public DocumentsViewModel(ApiClient apiClient)
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
            var req = new PagedRequestDto { Page = 1, PageSize = 50, Search = SearchText };
            var result = await _apiClient.GetDocumentsAsync(req);
            Documents = new ObservableCollection<DocumentDto>(result?.Items ?? []);
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
    private async Task PostDocument()
    {
        if (SelectedDocument == null) return;
        try
        {
            var success = await _apiClient.PostDocumentAsync(SelectedDocument.Id);
            if (success) await LoadAsync();
        }
        catch (Exception ex) { SetError(ex.Message); }
    }

    [RelayCommand]
    private async Task UnpostDocument()
    {
        if (SelectedDocument == null) return;
        try
        {
            var success = await _apiClient.UnpostDocumentAsync(SelectedDocument.Id);
            if (success) await LoadAsync();
        }
        catch (Exception ex) { SetError(ex.Message); }
    }

    [RelayCommand]
    private void CreateNewDocument()
    {
        var vm = new CreateDocumentViewModel(_apiClient);
        var dialog = new CreateDocumentDialog { DataContext = vm };
        if (dialog.ShowDialog() == true)
            LoadAsync();
    }
}