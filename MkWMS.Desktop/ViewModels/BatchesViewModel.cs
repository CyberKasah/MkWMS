using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Services;
using System;
using System.Threading.Tasks;

namespace MkWMS.Desktop.ViewModels;

public partial class BatchesViewModel : BaseCrudViewModel<BatchDto>
{
    public BatchesViewModel(ApiClient api)
        : base(api, ApiEndpoints.Batches)
    {
        _ = LoadAsync();
    }

    [RelayCommand]
    public void CreateNew()
    {
        ClearError(); // Используем метод базы

        SelectedItem = new BatchDto
        {
            Id = 0,
            BatchNumber = $"BAT-{DateTime.Now:yyyyMMdd-HHmmss}",
            ProductionDate = DateTime.Now.Date,
            ExpirationDate = DateTime.Now.Date.AddMonths(6),
            ProductId = 0
        };
    }


}