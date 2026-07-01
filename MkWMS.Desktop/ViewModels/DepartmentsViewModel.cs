using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MkWMS.Desktop.ViewModels;

public partial class DepartmentsViewModel : BaseCrudViewModel<DepartmentDto>
{
    [ObservableProperty]
    private ObservableCollection<WarehouseDto> _warehouses = new();

    public DepartmentsViewModel(ApiClient api) : base(api, "departments")
    {


        _ = Task.WhenAll(
            LoadAsync(),
            LoadWarehousesAsync()
        );
    }

    private async Task LoadWarehousesAsync()
    {
        try
        {
            var result = await _api.GetPagedAsync<WarehouseDto>("warehouses", new PagedRequestDto { PageSize = 100 });
            if (result?.Items != null)
            {
                Warehouses = new ObservableCollection<WarehouseDto>(result.Items);
            }
        }
        catch (System.Exception ex)
        {
            SetError($"Ошибка загрузки списка складов: {ex.Message}");
        }
    }

    [RelayCommand]
    public void CreateNew()
    {
        ClearError();
        SelectedItem = new DepartmentDto
        {
            Id = 0,
            Name = string.Empty,
            WarehouseId = 0
        };
    }
}