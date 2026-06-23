using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections;
using System.Linq;
using System.ComponentModel.DataAnnotations;

namespace MkWMS.Desktop.ViewModels;

public partial class BaseViewModel : ObservableValidator
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotLoading))]
    private bool _isLoading;

    public bool IsNotLoading => !IsLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    protected void SetError(string message)
    {
        ErrorMessage = message;
    }

    protected void ClearError()
    {
        ErrorMessage = null;
    }

    protected bool Validate()
    {
        ValidateAllProperties();

        if (HasErrors)
        {
            var firstError = GetErrors()
                .Cast<ValidationResult>()
                .FirstOrDefault();

            ErrorMessage = firstError?.ErrorMessage;
            return false;
        }

        ClearError();
        return true;
    }
    protected void ResetStatus()
    {
        IsLoading = false;
        ClearError();
    }

}
