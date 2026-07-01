using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Services;
using MkWMS.Desktop.Views.Dialogs;
using System.Threading.Tasks;
using System.Windows;

namespace MkWMS.Desktop.ViewModels;

public partial class UsersViewModel : BaseCrudViewModel<UserDto>
{
    public RolesViewModel RolesVM { get; }
    public AuditLogsViewModel AuditLogsVM { get; }

    public UsersViewModel(ApiClient api) : base(api, "users")
    {
        RolesVM = new RolesViewModel(api);
        AuditLogsVM = new AuditLogsViewModel(api);
        _ = LoadAsync();
    }

    [RelayCommand]
    private async Task OpenEditDialogAsync(UserDto? user)
    {
        ClearError();


        var dialogVm = user == null
            ? new EditUserViewModel(_api)
            : new EditUserViewModel(_api, user);

        var dialog = new EditUserDialog(dialogVm)
        {
            Owner = Application.Current.MainWindow
        };

        if (dialog.ShowDialog() == true)
        {
            await LoadAsync();
        }
    }
}