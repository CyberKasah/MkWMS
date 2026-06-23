using System.Threading.Tasks;
using System.Windows;
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

        var result = MessageBox.Show(
            $"Вы уверены, что хотите полностью удалить склад '{SelectedItem.Name}' и все связанные данные?",
            "Внимание",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            await DeleteAsync();
            if (string.IsNullOrEmpty(ErrorMessage))
            {
                MessageBox.Show("Склад удален.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    // Проверка, что выбран существующий в БД объект
    private bool IsEntitySelected => SelectedItem != null && SelectedItem.Id > 0;

    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (e.PropertyName == nameof(SelectedItem))
        {
            // Обновляем состояние кнопок при смене выбора
            DeleteWarehouseCommand.NotifyCanExecuteChanged();
        }
    }
}