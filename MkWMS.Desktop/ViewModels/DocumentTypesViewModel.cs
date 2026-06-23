using CommunityToolkit.Mvvm.Input;
using MkWMS.Desktop.Services;
using MkWMS.API.DTOs;

namespace MkWMS.Desktop.ViewModels;

public partial class DocumentTypesViewModel : BaseCrudViewModel<DocumentTypeDto>
{
    public DocumentTypesViewModel(ApiClient api) : base(api, "document-types")
    {
        _ = LoadAsync();
    }

    // Метод Refresh просто вызывает базовый LoadAsync
    [RelayCommand]
    private async Task RefreshAsync() => await LoadAsync();
}