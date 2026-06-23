using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Services;

namespace MkWMS.Desktop.ViewModels;

public partial class RolesViewModel : BaseCrudViewModel<RoleDto>
{
    public RolesViewModel(ApiClient api) : base(api, "roles")
    {
        _ = LoadAsync();
    }

    [RelayCommand]
    public void CreateNew()
    {
        ClearError();
        SelectedItem = new RoleDto();
    }

    // Все остальные команды (Save, Delete, Cancel, Refresh) работают из базового класса
}