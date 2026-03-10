using CommunityToolkit.Mvvm.ComponentModel;

namespace MkWMS.Desktop.Models;

public partial class RoleSelectionModel : ObservableObject
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    [ObservableProperty]
    private bool isSelected;
}