using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MidiControllerFrontend.Services;
using System.Collections.ObjectModel;

namespace MidiControllerFrontend.ViewModels;

public sealed partial class TemplatesViewModel : ObservableObject
{
    private readonly ApiClient _api;

    public ObservableCollection<string> TemplateNames { get; } = new();

    public string? SelectedTemplate
    {
        get => _selectedTemplate;
        set
        {
            if (SetProperty(ref _selectedTemplate, value))
                DeleteCommand.NotifyCanExecuteChanged();
        }
    }
    private string? _selectedTemplate;

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }
    private string _statusMessage = string.Empty;

    public TemplatesViewModel(ApiClient api) => _api = api;

    [RelayCommand]
    public async Task RefreshAsync()
    {
        var names = await _api.ListTemplateNamesAsync();
        TemplateNames.Clear();
        foreach (var n in names) TemplateNames.Add(n);
        StatusMessage = TemplateNames.Count == 0 ? "Keine Templates vorhanden." : string.Empty;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task DeleteAsync()
    {
        if (SelectedTemplate is null) return;
        await _api.DeleteTemplateAsync(SelectedTemplate);
        await RefreshAsync();
    }

    private bool HasSelection => SelectedTemplate is not null;
}
