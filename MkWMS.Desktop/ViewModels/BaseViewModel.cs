using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Models;
using MkWMS.Desktop.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Linq;

namespace MkWMS.Desktop.ViewModels;

public partial class BaseViewModel : ObservableObject
{
    [ObservableProperty]
    private bool isBusy;

    [ObservableProperty]
    private string? errorMessage;

    protected void SetError(string message)
    {
        ErrorMessage = message;
    }

    protected void ClearError()
    {
        ErrorMessage = null;
    }
}