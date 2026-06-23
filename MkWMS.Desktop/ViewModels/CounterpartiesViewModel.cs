using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Services;
using System;

namespace MkWMS.Desktop.ViewModels;

/// <summary>
/// Управление контрагентами (Клиенты/Поставщики)
/// </summary>
public partial class CounterpartiesViewModel : BaseCrudViewModel<CounterpartyDto>
{
    public CounterpartiesViewModel(ApiClient api) : base(api, "counterparties")
    {
        _ = LoadAsync();
    }

    [RelayCommand]
    public void CreateNew()
    {
        ClearError();

        SelectedItem = new CounterpartyDto
        {
            Id = 0,
            Name = string.Empty,
            IsSupplier = true,
            IsCustomer = false
        };
    }

    // Команды Cancel и Refresh наследуются из BaseCrudViewModel автоматически
}