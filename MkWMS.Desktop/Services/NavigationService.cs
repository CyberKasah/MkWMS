using MkWMS.Desktop.ViewModels;
using System;

namespace MkWMS.Desktop.Services;

public class NavigationService
{
    private BaseViewModel? _currentViewModel;
    private object value;

    public NavigationService()
    {
    }

    public NavigationService(object value)
    {
        this.value = value;
    }

    public BaseViewModel? CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            _currentViewModel = value;
            ViewModelChanged?.Invoke();
        }
    }

    public event Action? ViewModelChanged;

    public void Navigate(BaseViewModel viewModel)
    {
        CurrentViewModel = viewModel;
    }
}