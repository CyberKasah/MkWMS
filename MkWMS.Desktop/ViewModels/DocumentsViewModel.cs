using CommunityToolkit.Mvvm.Input;
using MkWMS.API.DTOs;
using MkWMS.Desktop.Services;
using MkWMS.Desktop.Views.Dialogs;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace MkWMS.Desktop.ViewModels;

public partial class DocumentsViewModel : BaseCrudViewModel<DocumentDto>
{
    private readonly NavigationService _navigation;
    public CounterpartiesViewModel CounterpartiesVM { get; }

    public DocumentsViewModel(ApiClient api, NavigationService navigation, CounterpartiesViewModel counterpartiesVM) : base(api, "documents")
    {
        _navigation = navigation;
        CounterpartiesVM = counterpartiesVM;
        _ = LoadAsync();
    }

    [RelayCommand]
private void GoToDetails()
{
    if (SelectedItem == null) return;

    // Теперь передаём CounterpartiesVM, чтобы при возврате табы работали
    _navigation.Navigate(new DocumentDetailsViewModel(
        _api, 
        _navigation, 
        SelectedItem.Id, 
        CounterpartiesVM));   // ← добавлено
}

    [RelayCommand]
    private async Task CreateNew()
    {
        var vm = new CreateDocumentViewModel(_api);
        var dialog = new CreateDocumentDialog { DataContext = vm };
        if (dialog.ShowDialog() == true) await LoadAsync();
    }

    [RelayCommand]
    private async Task Post()
    {
        if (SelectedItem == null) return;
        IsLoading = true;
        try
        {
            if (await _api.PostDocumentAsync(SelectedItem.Id)) await LoadAsync();
            else SetError("Не удалось провести документ");
        }
        catch (Exception ex) { SetError(ex.Message); }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task Unpost()
    {
        if (SelectedItem == null) return;
        IsLoading = true;
        try
        {
            if (await _api.UnpostDocumentAsync(SelectedItem.Id)) await LoadAsync();
            else SetError("Не удалось отменить проведение");
        }
        catch (Exception ex) { SetError(ex.Message); }
        finally { IsLoading = false; }
    }

    // ====================== ПЕЧАТЬ И PDF ======================

    [RelayCommand(CanExecute = nameof(CanExecuteDocumentAction))]
    private async Task PrintDocumentAsync(string printType) // ← Теперь принимает параметр!
    {
        if (SelectedItem == null || string.IsNullOrEmpty(printType)) return;

        IsLoading = true;
        try
        {
            // Прокидываем тип в API (раньше тут был хардкод "torg12")
            var pdfBytes = await _api.GetPrintFormAsync(SelectedItem.Id, printType);

            if (pdfBytes == null || pdfBytes.Length == 0)
            {
                SetError("Не удалось сгенерировать PDF");
                return;
            }

            var tempPath = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"{printType}_{SelectedItem.DocumentNumber ?? SelectedItem.Id.ToString()}.pdf");

            await System.IO.File.WriteAllBytesAsync(tempPath, pdfBytes);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(tempPath)
            {
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            SetError($"Ошибка печати: {ex.Message}");
        }
        finally { IsLoading = false; }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteDocumentAction))]
    private async Task DownloadPdfAsync()
    {
        if (SelectedItem == null) return;

        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = $"Документ_{SelectedItem.DocumentNumber ?? SelectedItem.Id.ToString()}.pdf",
            Filter = "PDF файлы (*.pdf)|*.pdf",
            DefaultExt = ".pdf"
        };

        if (dialog.ShowDialog() == true)
        {
            IsLoading = true;
            try
            {
                var pdfBytes = await _api.GetPrintFormAsync(SelectedItem.Id, "torg12");
                if (pdfBytes != null)
                {
                    await System.IO.File.WriteAllBytesAsync(dialog.FileName, pdfBytes);
                    MessageBox.Show("PDF успешно сохранён", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка сохранения: {ex.Message}");
            }
            finally { IsLoading = false; }
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteDocumentAction))]
    private async Task UploadScanAsync()
    {
        if (SelectedItem == null) return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Изображения и PDF (*.jpg;*.png;*.pdf)|*.jpg;*.png;*.pdf",
            Title = "Выберите скан документа"
        };

        if (dialog.ShowDialog() == true)
        {
            IsLoading = true;
            try
            {
                if (await _api.UploadDocumentScanAsync(SelectedItem.Id, dialog.FileName))
                {
                    MessageBox.Show("Скан успешно загружен", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    SetError("Не удалось загрузить скан");
                }
            }
            catch (Exception ex)
            {
                SetError($"Ошибка загрузки скана: {ex.Message}");
            }
            finally { IsLoading = false; }
        }

    }
    protected override void OnPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        // Когда меняется выбранный документ, заставляем кнопки перепроверить свою доступность
        if (e.PropertyName == nameof(SelectedItem))
        {
            PrintDocumentCommand.NotifyCanExecuteChanged();
            DownloadPdfCommand.NotifyCanExecuteChanged();
            UploadScanCommand.NotifyCanExecuteChanged();
            PostCommand.NotifyCanExecuteChanged();
            UnpostCommand.NotifyCanExecuteChanged();
            GoToDetailsCommand.NotifyCanExecuteChanged();
        }
    }
    private bool CanExecuteDocumentAction => SelectedItem != null && SelectedItem.Id > 0;
}