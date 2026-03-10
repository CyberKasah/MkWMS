using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.Desktop.Services;
using MkWMS.Desktop.Views;
using System.Windows;
using System;
using System.Threading.Tasks;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Models;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;

namespace MkWMS.Desktop.ViewModels;

public partial class DocumentDetailsViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;
    private readonly int _documentId;

    [ObservableProperty]
    private DocumentDto document = new();

    public DocumentDetailsViewModel(ApiClient apiClient, int documentId)
    {
        _apiClient = apiClient;
        _documentId = documentId;
        _ = LoadDocumentAsync();
    }

    [RelayCommand]
    private async Task LoadDocumentAsync()
    {
        IsBusy = true;
        ClearError();

        try
        {
            var doc = await _apiClient.GetDocumentByIdAsync(_documentId);
            if (doc != null)
                Document = doc;
            else
                SetError("Документ не найден");
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
    private async Task PostDocumentAsync()
    {
        IsBusy = true;
        ClearError();

        try
        {
            var success = await _apiClient.PostDocumentAsync(_documentId);
            if (success)
            {
                MessageBox.Show("Документ успешно проведён!", "Успех");
                await LoadDocumentAsync();
            }
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
    private async Task UnpostDocumentAsync()
    {
        IsBusy = true;
        ClearError();

        try
        {
            var success = await _apiClient.UnpostDocumentAsync(_documentId);
            if (success)
            {
                MessageBox.Show("Проведение отменено", "Готово");
                await LoadDocumentAsync();
            }
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
}