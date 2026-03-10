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

public partial class DepartmentsViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;

    [ObservableProperty]
    private ObservableCollection<DepartmentDto> departments = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    public DepartmentsViewModel(ApiClient apiClient)
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
            var req = new PagedRequestDto
            {
                Page = 1,
                PageSize = 50,
                Search = SearchText
            };

            var result = await _apiClient.GetDepartmentsAsync(req);
            Departments = new ObservableCollection<DepartmentDto>(result?.Items ?? []);
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