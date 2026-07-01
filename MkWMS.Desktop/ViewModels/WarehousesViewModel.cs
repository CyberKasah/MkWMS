using System.Threading.Tasks;
using System.Windows;
using MkWMS.Desktop.Views.Dialogs;
using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Services;

namespace MkWMS.Desktop.ViewModels;

public partial class WarehousesViewModel : BaseCrudViewModel<WarehouseDto>
{
    public DepartmentsViewModel DepartmentsVM { get; }
    public StorageLocationsViewModel StorageLocationsVM { get; }

    public WarehousesViewModel(ApiClient api) : base(api, "warehouses")
    {
        DepartmentsVM = new DepartmentsViewModel(api);
        StorageLocationsVM = new StorageLocationsViewModel(api);
        _ = LoadAsync();
    }

    [RelayCommand]
    private void AddNew()
    {
        ClearError();
        SelectedItem = new WarehouseDto
        {
            Id = 0,
            IsActive = true,
            Name = string.Empty
        };
    }

    [RelayCommand(CanExecute = nameof(IsEntitySelected))]
    private async Task DeleteWarehouseAsync()
    {
        if (SelectedItem == null || SelectedItem.Id <= 0) return;

        var confirmed = AppMessageBoxWindow.Confirm(
            $"Вы уверены, что хотите полностью удалить склад '{SelectedItem.Name}' и все связанные данные?",
            "Внимание");

        if (confirmed)
        {
            await DeleteAsync();
            if (string.IsNullOrEmpty(ErrorMessage))
            {
                AppMessageBoxWindow.Show("Склад удалён.", "Готово", AppMessageBoxIcon.Success);
            }
        }
    }


    private bool IsEntitySelected => SelectedItem != null && SelectedItem.Id > 0;

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(SelectedItem))
        {

            DeleteWarehouseCommand.NotifyCanExecuteChanged();
        }
    }
}