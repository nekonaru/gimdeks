using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GimDeks.Core.Interfaces;

namespace GimDeks.App.ViewModels;

public sealed class AutomationRow
{
    public required AutomationRule Rule { get; init; }
    public string Summary => $"{Rule.Nama}: {Rule.Field} {Rule.Operator} {Rule.Nilai} -> {Rule.Aksi} {Rule.Argumen}";
}

public partial class AutomationViewModel : ObservableObject, IRefreshable
{
    private readonly IAutomationRuleRepository _rules;
    private readonly Services.AutomationService _service;

    public AutomationViewModel(IAutomationRuleRepository rules, Services.AutomationService service)
    {
        _rules = rules;
        _service = service;
    }

    public ObservableCollection<AutomationRow> Rows { get; } = new();
    public IReadOnlyList<string> Fields { get; } = new[] { "status", "nama", "ukuran_gb", "favorit", "rating", "path_exe" };
    public IReadOnlyList<string> Operators { get; } = new[] { "equals", "contains", "greater", "less", "empty", "notempty" };
    public IReadOnlyList<string> Actions { get; } = new[] { "set_status", "set_favorite", "set_priority", "set_rating" };

    [ObservableProperty] private string _newName = "";
    [ObservableProperty] private string _newField = "status";
    [ObservableProperty] private string _newOperator = "equals";
    [ObservableProperty] private string _newValue = "";
    [ObservableProperty] private string _newAction = "set_favorite";
    [ObservableProperty] private string _newArgument = "1";
    [ObservableProperty] private string _statusText = "";

    public Task RefreshAsync() => LoadAsync();

    [RelayCommand]
    private async Task LoadAsync()
    {
        Rows.Clear();
        var rules = await Task.Run(() => _rules.GetAllAsync());
        foreach (var rule in rules)
        {
            Rows.Add(new AutomationRow { Rule = rule });
        }
        StatusText = Services.Loc.Format("Ui_Auto_Count", Rows.Count);
    }

    [RelayCommand]
    private async Task AddAsync()
    {
        if (string.IsNullOrWhiteSpace(NewName))
        {
            return;
        }
        await _rules.AddAsync(new AutomationRule(0, NewName.Trim(), NewField, NewOperator, NewValue, NewAction, NewArgument, true, 0));
        NewName = "";
        await LoadAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(AutomationRow? row)
    {
        if (row is null) return;
        await _rules.DeleteAsync(row.Rule.Id);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task ToggleAsync(AutomationRow? row)
    {
        if (row is null) return;
        await _rules.SetActiveAsync(row.Rule.Id, !row.Rule.Aktif);
        await LoadAsync();
    }

    [RelayCommand]
    private async Task RunAsync()
    {
        var changed = await Task.Run(() => _service.ApplyAsync());
        StatusText = Services.Loc.Format("Ui_Auto_Applied", changed);
    }
}
