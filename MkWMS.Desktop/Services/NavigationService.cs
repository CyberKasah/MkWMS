using MkWMS.Desktop.ViewModels;

namespace MkWMS.Desktop.Services;

public class NavigationService
{
    private BaseViewModel? _currentViewModel;

    // Оставляем только один чистый конструктор
    public NavigationService() { }

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