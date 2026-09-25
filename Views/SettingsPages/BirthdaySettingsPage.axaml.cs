using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ClassIsland.BirthdayReminder.Models;
using ClassIsland.BirthdayReminder.Services;
using ClassIsland.Core;
using ClassIsland.Core.Abstractions.Controls;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Controls;
using ClassIsland.Core.Enums.SettingsWindow;
using ClassIsland.Platforms.Abstraction;

namespace ClassIsland.BirthdayReminder.Views.SettingsPages;

/// <summary>
/// 生日提醒插件的主设置页面：包含总览看板、提醒方式、提醒文案、名单管理与数据管理。
/// </summary>
[SettingsPageInfo("tech.classisland.plugin.birthdayreminder.settings", "生日提醒", "\uEF27", "\uEF26", SettingsPageCategory.External)]
public partial class BirthdaySettingsPage : SettingsPageBase, INotifyPropertyChanged
{
    private readonly BirthdayStorageService _storage;
    private readonly BirthdayCalculatorService _calculator;
    private readonly BirthdayReminderEngine _engine;
    private readonly DispatcherTimer _uiTimer;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public BirthdaySettingsPage(BirthdayStorageService storage, BirthdayCalculatorService calculator, BirthdayReminderEngine engine)
    {
        _storage = storage;
        _calculator = calculator;
        _engine = engine;

        InitializeComponent();
        DataContext = this;

        SortOptions = new ObservableCollection<string>
        {
            "生日临近优先", "生日临近靠后", "姓名 A→Z", "姓名 Z→A", "年龄从小到大", "年龄从大到小", "按分类"
        };
        _selectedSortOption = SortOptions[0];
        _selectedCategory = "全部分类";

        RefreshCategoryOptions();
        RefreshDashboard();
        RefreshList();
        RefreshBackupList();

        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(60) };
        _uiTimer.Tick += (_, _) =>
        {
            RefreshDashboard();
            RefreshList();
        };
        _uiTimer.Start();
        Unloaded += (_, _) => _uiTimer.Stop();
    }

    #region 提醒方式设置（直接读写插件全局设置，修改后立即保存）

    public bool IsPluginEnabled
    {
        get => _storage.Settings.IsEnabled;
        set { _storage.Settings.IsEnabled = value; _storage.SaveSettings(); OnPropertyChanged(); }
    }

    public bool IsDesktopPopupEnabled
    {
        get => _storage.Settings.IsDesktopPopupEnabled;
        set { _storage.Settings.IsDesktopPopupEnabled = value; _storage.SaveSettings(); OnPropertyChanged(); }
    }

    public bool IsBannerEnabled
    {
        get => _storage.Settings.IsBannerEnabled;
        set { _storage.Settings.IsBannerEnabled = value; _storage.SaveSettings(); OnPropertyChanged(); }
    }

    public bool IsFullScreenEffectEnabled
    {
        get => _storage.Settings.IsFullScreenEffectEnabled;
        set { _storage.Settings.IsFullScreenEffectEnabled = value; _storage.SaveSettings(); OnPropertyChanged(); }
    }

    public bool IsSpeechEnabled
    {
        get => _storage.Settings.IsSpeechEnabled;
        set { _storage.Settings.IsSpeechEnabled = value; _storage.SaveSettings(); OnPropertyChanged(); }
    }

    public bool IsTodaySpecialAnimationEnabled
    {
        get => _storage.Settings.IsTodaySpecialAnimationEnabled;
        set { _storage.Settings.IsTodaySpecialAnimationEnabled = value; _storage.SaveSettings(); OnPropertyChanged(); }
    }

    public decimal DesktopPopupDurationSeconds
    {
        get => _storage.Settings.DesktopPopupDurationSeconds;
        set { _storage.Settings.DesktopPopupDurationSeconds = (int)value; _storage.SaveSettings(); OnPropertyChanged(); }
    }

    public decimal CheckIntervalSeconds
    {
        get => _storage.Settings.CheckIntervalSeconds;
        set
        {
            _storage.Settings.CheckIntervalSeconds = (int)value;
            _storage.SaveSettings();
            _engine.ApplyInterval();
            OnPropertyChanged();
        }
    }

    #endregion

    #region 提醒文案与默认提醒时机

    public string TemplateOnDay
    {
        get => _storage.Settings.TemplateOnDay;
        set { _storage.Settings.TemplateOnDay = value; OnPropertyChanged(); }
    }

    public string TemplateBefore
    {
        get => _storage.Settings.TemplateBefore;
        set { _storage.Settings.TemplateBefore = value; OnPropertyChanged(); }
    }

    public bool DefaultOffset0
    {
        get => _storage.Settings.DefaultReminderOffsets.Contains(0);
        set { ToggleDefaultOffset(0, value); OnPropertyChanged(); }
    }

    public bool DefaultOffset1
    {
        get => _storage.Settings.DefaultReminderOffsets.Contains(1);
        set { ToggleDefaultOffset(1, value); OnPropertyChanged(); }
    }

    public bool DefaultOffset3
    {
        get => _storage.Settings.DefaultReminderOffsets.Contains(3);
        set { ToggleDefaultOffset(3, value); OnPropertyChanged(); }
    }

    public bool DefaultOffset7
    {
        get => _storage.Settings.DefaultReminderOffsets.Contains(7);
        set { ToggleDefaultOffset(7, value); OnPropertyChanged(); }
    }

    public bool DefaultOffset30
    {
        get => _storage.Settings.DefaultReminderOffsets.Contains(30);
        set { ToggleDefaultOffset(30, value); OnPropertyChanged(); }
    }

    private void ToggleDefaultOffset(int offset, bool enabled)
    {
        var list = _storage.Settings.DefaultReminderOffsets;
        if (enabled && !list.Contains(offset))
        {
            list.Add(offset);
        }
        else if (!enabled && list.Contains(offset))
        {
            list.Remove(offset);
        }
    }

    private async void SaveTemplateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _storage.SaveSettings();
        await CommonTaskDialogs.ShowDialog("已保存", "提醒文案与默认提醒时机已保存。", this);
    }

    #endregion

    #region 总览看板

    private string _nextBirthdayText = "暂无数据";
    public string NextBirthdayText
    {
        get => _nextBirthdayText;
        set { _nextBirthdayText = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> TodayBirthdayNames { get; } = new();

    private bool _hasNoTodayBirthday = true;
    public bool HasNoTodayBirthday
    {
        get => _hasNoTodayBirthday;
        set { _hasNoTodayBirthday = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> ThisMonthBirthdays { get; } = new();

    private bool _hasNoMonthBirthday = true;
    public bool HasNoMonthBirthday
    {
        get => _hasNoMonthBirthday;
        set { _hasNoMonthBirthday = value; OnPropertyChanged(); }
    }

    private void RefreshDashboard()
    {
        var today = DateTime.Now;

        var upcoming = _calculator.GetNextUpcoming(_storage.People.Where(p => p.IsEnabled), today);
        if (upcoming.Count == 0)
        {
            NextBirthdayText = "暂无数据，请先添加生日记录";
        }
        else if (upcoming[0].Days == 0)
        {
            NextBirthdayText = $"🎉 今天就是 {string.Join("、", upcoming.Select(x => x.Person.Name))} 的生日！";
        }
        else
        {
            NextBirthdayText = $"{string.Join("、", upcoming.Select(x => x.Person.Name))} · 还有 {upcoming[0].Days} 天";
        }

        TodayBirthdayNames.Clear();
        foreach (var p in _calculator.GetBirthdaysToday(_storage.People, today))
        {
            var age = _calculator.GetUpcomingAge(p, today);
            TodayBirthdayNames.Add(age.HasValue ? $"🎂 {p.Name}（{p.Category}）· 今天满 {age} 岁" : $"🎂 {p.Name}（{p.Category}）");
        }
        HasNoTodayBirthday = TodayBirthdayNames.Count == 0;

        ThisMonthBirthdays.Clear();
        foreach (var p in _calculator.GetBirthdaysThisMonth(_storage.People, today))
        {
            var days = _calculator.GetDaysUntilNextBirthday(p, today);
            var when = days == 0 ? "今天" : $"还有 {days} 天";
            ThisMonthBirthdays.Add($"{p.BirthMonth:D2}-{p.BirthDay:D2}　{p.Name}（{p.Category}）· {when}");
        }
        HasNoMonthBirthday = ThisMonthBirthdays.Count == 0;
    }

    private void RefreshDashboardButton_OnClick(object? sender, RoutedEventArgs e) => RefreshDashboard();

    #endregion

    #region 名单管理：搜索 / 筛选 / 排序 / 增删改

    public ObservableCollection<string> CategoryOptions { get; } = new();

    private string _selectedCategory;
    public string SelectedCategory
    {
        get => _selectedCategory;
        set { _selectedCategory = value; OnPropertyChanged(); RefreshList(); }
    }

    public ObservableCollection<string> SortOptions { get; private set; } = new();

    private string _selectedSortOption;
    public string SelectedSortOption
    {
        get => _selectedSortOption;
        set { _selectedSortOption = value; OnPropertyChanged(); RefreshList(); }
    }

    private string _searchText = "";
    public string SearchText
    {
        get => _searchText;
        set { _searchText = value; OnPropertyChanged(); RefreshList(); }
    }

    public ObservableCollection<BirthdayRowViewModel> DisplayRows { get; } = new();

    private void RefreshCategoryOptions()
    {
        var current = SelectedCategory;
        CategoryOptions.Clear();
        foreach (var c in _storage.GetAllCategories())
        {
            CategoryOptions.Add(c);
        }
        if (!string.IsNullOrEmpty(current) && CategoryOptions.Contains(current))
        {
            _selectedCategory = current;
            OnPropertyChanged(nameof(SelectedCategory));
        }
    }

    private BirthdaySortMode MapSortMode(string label) => label switch
    {
        "姓名 A→Z" => BirthdaySortMode.NameAsc,
        "姓名 Z→A" => BirthdaySortMode.NameDesc,
        "生日临近靠后" => BirthdaySortMode.NextBirthdayDesc,
        "年龄从小到大" => BirthdaySortMode.AgeAsc,
        "年龄从大到小" => BirthdaySortMode.AgeDesc,
        "按分类" => BirthdaySortMode.CategoryAsc,
        _ => BirthdaySortMode.NextBirthdayAsc
    };

    private void RefreshList()
    {
        var today = DateTime.Now;
        var sortMode = MapSortMode(SelectedSortOption ?? "");
        var result = _storage.Query(SearchText, SelectedCategory, sortMode, today);

        DisplayRows.Clear();
        foreach (var p in result)
        {
            var age = _calculator.GetCurrentAge(p, today);
            var ageText = age.HasValue ? $"{age} 岁" : "未知";
            var days = _calculator.GetDaysUntilNextBirthday(p, today);
            var countdownText = days == 0 ? "今天🎉" : $"还有 {days} 天";
            var offsetsText = string.Join(",", _calculator.GetEffectiveOffsets(p, _storage.Settings.DefaultReminderOffsets).Select(o => o == 0 ? "当天" : $"提前{o}天"));
            DisplayRows.Add(new BirthdayRowViewModel(p, ageText, countdownText, offsetsText));
        }
    }

    private void RefreshListButton_OnClick(object? sender, RoutedEventArgs e)
    {
        RefreshCategoryOptions();
        RefreshList();
    }

    private async void AddButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var owner = GetOwnerWindow();
        if (owner == null) return;
        var window = new PersonEditWindow(null, _storage.Settings.Categories);
        var result = await window.ShowDialog<BirthdayPerson?>(owner);
        if (result == null) return;
        _storage.Add(result);
        RefreshCategoryOptions();
        RefreshList();
        RefreshDashboard();
    }

    private async void EditButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (PeopleGrid.SelectedItem is not BirthdayRowViewModel row)
        {
            await CommonTaskDialogs.ShowDialog("提示", "请先在下方列表中选择一行要编辑的记录。", this);
            return;
        }
        await EditRow(row);
    }

    private async void PeopleGrid_OnDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (PeopleGrid.SelectedItem is BirthdayRowViewModel row)
        {
            await EditRow(row);
        }
    }

    private async Task EditRow(BirthdayRowViewModel row)
    {
        var owner = GetOwnerWindow();
        if (owner == null) return;
        var window = new PersonEditWindow(row.Person, _storage.Settings.Categories);
        var result = await window.ShowDialog<BirthdayPerson?>(owner);
        if (result == null) return;
        _storage.ReplacePerson(result);
        RefreshCategoryOptions();
        RefreshList();
        RefreshDashboard();
    }

    private async void DeleteButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var rows = PeopleGrid.SelectedItems.Cast<BirthdayRowViewModel>().ToList();
        if (rows.Count == 0)
        {
            await CommonTaskDialogs.ShowDialog("提示", "请先在下方列表中选择要删除的记录。", this);
            return;
        }
        _storage.RemoveRange(rows.Select(r => r.Person));
        RefreshList();
        RefreshDashboard();
    }

    #endregion

    #region 导入导出 / 备份还原

    private string _importResultText = "";
    public string ImportResultText
    {
        get => _importResultText;
        set { _importResultText = value; OnPropertyChanged(); HasImportResult = !string.IsNullOrWhiteSpace(value); }
    }

    private bool _hasImportResult;
    public bool HasImportResult
    {
        get => _hasImportResult;
        set { _hasImportResult = value; OnPropertyChanged(); }
    }

    private string _backupResultText = "";
    public string BackupResultText
    {
        get => _backupResultText;
        set { _backupResultText = value; OnPropertyChanged(); HasBackupResult = !string.IsNullOrWhiteSpace(value); }
    }

    private bool _hasBackupResult;
    public bool HasBackupResult
    {
        get => _hasBackupResult;
        set { _hasBackupResult = value; OnPropertyChanged(); }
    }

    public ObservableCollection<string> BackupFiles { get; } = new();

    private string? _selectedBackup;
    public string? SelectedBackup
    {
        get => _selectedBackup;
        set { _selectedBackup = value; OnPropertyChanged(); }
    }

    private void RefreshBackupList()
    {
        BackupFiles.Clear();
        foreach (var f in _storage.GetBackups())
        {
            BackupFiles.Add(f);
        }
        SelectedBackup = BackupFiles.FirstOrDefault();
    }

    private Window? GetOwnerWindow() => TopLevel.GetTopLevel(this) as Window ?? AppBase.Current.GetRootWindow();

    private async void ExportTemplateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var owner = GetOwnerWindow();
        if (owner == null) return;
        var path = await PlatformServices.FilePickerService.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "下载生日名单导入模板",
            SuggestedFileName = "生日提醒-导入模板.xlsx",
            DefaultExtension = ".xlsx",
            FileTypeChoices = new[] { new FilePickerFileType("Excel 工作簿") { Patterns = new[] { "*.xlsx" } } }
        }, owner);
        if (string.IsNullOrEmpty(path)) return;
        _storage.ExportExcelTemplate(path);
        ImportResultText = $"模板已保存到：{path}";
    }

    private async void ExportButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var owner = GetOwnerWindow();
        if (owner == null) return;
        var path = await PlatformServices.FilePickerService.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "导出生日名单",
            SuggestedFileName = $"生日名单-{DateTime.Now:yyyyMMdd}.xlsx",
            DefaultExtension = ".xlsx",
            FileTypeChoices = new[] { new FilePickerFileType("Excel 工作簿") { Patterns = new[] { "*.xlsx" } } }
        }, owner);
        if (string.IsNullOrEmpty(path)) return;
        _storage.ExportExcel(path);
        ImportResultText = $"已导出 {_storage.People.Count} 条记录到：{path}";
    }

    private async void ImportMergeButton_OnClick(object? sender, RoutedEventArgs e) => await ImportExcel(false);

    private async void ImportReplaceButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var owner = GetOwnerWindow();
        if (owner == null) return;
        await CommonTaskDialogs.ShowDialog("即将覆盖导入", "接下来选择的 Excel 文件将完全替换当前所有生日记录，建议先创建一次备份。请在弹出的文件选择框中选择要导入的文件。", this);
        await ImportExcel(true);
    }

    private async Task ImportExcel(bool replaceAll)
    {
        var owner = GetOwnerWindow();
        if (owner == null) return;
        var paths = await PlatformServices.FilePickerService.OpenFilesPickerAsync(new FilePickerOpenOptions
        {
            Title = "选择要导入的 Excel 文件",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("Excel 工作簿") { Patterns = new[] { "*.xlsx" } } }
        }, owner);
        if (paths.Count == 0) return;

        try
        {
            var (count, errors) = _storage.ImportExcel(paths[0], replaceAll);
            ImportResultText = errors.Count == 0
                ? $"导入成功，共 {count} 条记录。"
                : $"导入完成，成功 {count} 条，{errors.Count} 条记录有问题：\n" + string.Join("\n", errors.Take(10));
        }
        catch (Exception ex)
        {
            ImportResultText = $"导入失败：{ex.Message}";
        }

        RefreshCategoryOptions();
        RefreshList();
        RefreshDashboard();
    }

    private async void BackupButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var path = _storage.CreateBackup();
        RefreshBackupList();
        BackupResultText = $"备份已创建：{Path.GetFileName(path)}";
        await CommonTaskDialogs.ShowDialog("备份完成", $"已创建备份文件：\n{path}", this);
    }

    private async void RestoreButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(SelectedBackup))
        {
            await CommonTaskDialogs.ShowDialog("提示", "请先在下拉框中选择一个备份文件。", this);
            return;
        }
        _storage.RestoreBackup(SelectedBackup);
        AfterRestore();
        await CommonTaskDialogs.ShowDialog("还原完成", "已从所选备份还原数据。", this);
    }

    private async void RestoreExternalButton_OnClick(object? sender, RoutedEventArgs e)
    {
        var owner = GetOwnerWindow();
        if (owner == null) return;
        var paths = await PlatformServices.FilePickerService.OpenFilesPickerAsync(new FilePickerOpenOptions
        {
            Title = "选择备份压缩包 (.zip)",
            AllowMultiple = false,
            FileTypeFilter = new[] { new FilePickerFileType("备份压缩包") { Patterns = new[] { "*.zip" } } }
        }, owner);
        if (paths.Count == 0) return;

        _storage.RestoreBackupFromExternalFile(paths[0]);
        AfterRestore();
        await CommonTaskDialogs.ShowDialog("还原完成", "已从外部备份文件还原数据。", this);
    }

    private void AfterRestore()
    {
        RefreshCategoryOptions();
        RefreshList();
        RefreshDashboard();
        RefreshBackupList();
        _engine.ApplyInterval();

        // 插件全局设置对象在还原后被重新创建，通知界面刷新所有相关绑定
        foreach (var name in new[]
                 {
                     nameof(IsPluginEnabled), nameof(IsDesktopPopupEnabled), nameof(IsBannerEnabled),
                     nameof(IsFullScreenEffectEnabled), nameof(IsSpeechEnabled), nameof(IsTodaySpecialAnimationEnabled),
                     nameof(DesktopPopupDurationSeconds), nameof(CheckIntervalSeconds), nameof(TemplateOnDay),
                     nameof(TemplateBefore), nameof(DefaultOffset0), nameof(DefaultOffset1), nameof(DefaultOffset3),
                     nameof(DefaultOffset7), nameof(DefaultOffset30)
                 })
        {
            OnPropertyChanged(name);
        }
    }

    #endregion

    #region 测试效果

    private void TestPopupButton_OnClick(object? sender, RoutedEventArgs e) => _engine.RunTest("popup");
    private void TestBannerButton_OnClick(object? sender, RoutedEventArgs e) => _engine.RunTest("banner");
    private void TestFullScreenButton_OnClick(object? sender, RoutedEventArgs e) => _engine.RunTest("fullscreen");
    private void TestSpeechButton_OnClick(object? sender, RoutedEventArgs e) => _engine.RunTest("speech");
    private void TestTodayButton_OnClick(object? sender, RoutedEventArgs e) => _engine.RunTest("today");

    #endregion
}
