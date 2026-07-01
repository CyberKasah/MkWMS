using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Services;

namespace MkWMS.Desktop.ViewModels;

public partial class StorageLocationsViewModel : BaseCrudViewModel<StorageLocationDto>
{
    public StorageLocationsViewModel(ApiClient api) : base(api, "storagelocations")
    {
        _ = LoadAsync();
    }

    [RelayCommand]
    public void CreateNew()
    {
        ClearError();
        SelectedItem = new StorageLocationDto
        {
            Id = 0,
            WarehouseId = 0,
            Name = string.Empty
        };
    }
}