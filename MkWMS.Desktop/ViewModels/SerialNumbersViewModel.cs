using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Services;

namespace MkWMS.Desktop.ViewModels;

public partial class SerialNumbersViewModel : BaseCrudViewModel<SerialNumberDto>
{
    public SerialNumbersViewModel(ApiClient api) : base(api, "serialnumbers")
    {
        _ = LoadAsync();
    }

    [RelayCommand]
    public void CreateNew()
    {
        ClearError();
        SelectedItem = new SerialNumberDto
        {
            Id = 0,
            Status = "НаСкладе",
            Number = string.Empty
        };
    }
}