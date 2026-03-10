// ViewModels/UsersViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Models;
using MkWMS.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;

namespace MkWMS.Desktop.ViewModels;

public partial class UsersViewModel : BaseViewModel
{
    private readonly ApiClient _apiClient;

    [ObservableProperty]
    private ObservableCollection<UserDto> users = new();

    [ObservableProperty]
    private UserDto? selectedUser;

    [ObservableProperty]
    private string searchText = string.Empty;

    public UsersViewModel(ApiClient apiClient)
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

            var result = await _apiClient.GetUsersAsync(req);
            Users = new ObservableCollection<UserDto>(result?.Items ?? []);
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

    [RelayCommand]
    private void CreateUser() => MessageBox.Show("Создание пользователя будет добавлено позже", "Инфо");

    [RelayCommand]
    private void EditUser() => MessageBox.Show("Редактирование пользователя будет добавлено позже", "Инфо");

    [RelayCommand]
    private async Task DeleteUser()
    {
        if (SelectedUser == null) return;

        if (MessageBox.Show($"Удалить пользователя {SelectedUser.Login}?", "Подтверждение",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
            return;

        IsBusy = true;
        ClearError();

        try
        {
            var success = await _apiClient.DeleteUserAsync(SelectedUser.Id);
            if (success)
                await LoadAsync();
            else
                SetError("Не удалось удалить пользователя");
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