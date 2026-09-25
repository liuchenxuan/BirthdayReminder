using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using ClassIsland.BirthdayReminder.Models;
using ClassIsland.Shared.Helpers;

namespace ClassIsland.BirthdayReminder.Services;

/// <summary>
/// 排序方式
/// </summary>
public enum BirthdaySortMode
{
    NameAsc,
    NameDesc,
    NextBirthdayAsc,
    NextBirthdayDesc,
    AgeAsc,
    AgeDesc,
    CategoryAsc
}

/// <summary>
/// 负责生日数据的持久化存储、增删改查、搜索筛选排序、导入导出与备份还原。
/// </summary>
public class BirthdayStorageService
{
    private readonly BirthdayCalculatorService _calculator;
    private readonly string _configFolder;

    private string PeopleFilePath => Path.Combine(_configFolder, "People.json");
    private string SettingsFilePath => Path.Combine(_configFolder, "Settings.json");
    private string LogFilePath => Path.Combine(_configFolder, "ReminderLog.json");
    private string BackupsFolder => Path.Combine(_configFolder, "Backups");

    public ObservableCollection<BirthdayPerson> People { get; private set; } = new();

    public PluginSettings Settings { get; private set; } = new();

    public BirthdayStorageService(string pluginConfigFolder, BirthdayCalculatorService calculator)
    {
        _calculator = calculator;
        _configFolder = pluginConfigFolder;
        Directory.CreateDirectory(_configFolder);
        Directory.CreateDirectory(BackupsFolder);
        Load();
    }

    #region 加载与保存

    public void Load()
    {
        var list = ConfigureFileHelper.LoadConfig<List<BirthdayPerson>>(PeopleFilePath);
        People = new ObservableCollection<BirthdayPerson>(list);
        Settings = ConfigureFileHelper.LoadConfig<PluginSettings>(SettingsFilePath);
    }

    public void SavePeople()
    {
        ConfigureFileHelper.SaveConfig(PeopleFilePath, People.ToList(), true);
    }

    public void SaveSettings()
    {
        ConfigureFileHelper.SaveConfig(SettingsFilePath, Settings, true);
    }

    #endregion

    #region 增删改查

    public void Add(BirthdayPerson person)
    {
        People.Add(person);
        if (!Settings.Categories.Contains(person.Category) && !string.IsNullOrWhiteSpace(person.Category))
        {
            Settings.Categories.Add(person.Category);
            SaveSettings();
        }
        SavePeople();
    }

    public void Update(BirthdayPerson person)
    {
        if (!Settings.Categories.Contains(person.Category) && !string.IsNullOrWhiteSpace(person.Category))
        {
            Settings.Categories.Add(person.Category);
            SaveSettings();
        }
        SavePeople();
    }

    public void Remove(BirthdayPerson person)
    {
        People.Remove(person);
        SavePeople();
    }

    /// <summary>
    /// 根据 Id 替换一条记录（用于编辑保存），若找不到则追加为新记录。
    /// </summary>
    public void ReplacePerson(BirthdayPerson updated)
    {
        var index = People.ToList().FindIndex(p => p.Id == updated.Id);
        if (index >= 0)
        {
            People[index] = updated;
        }
        else
        {
            People.Add(updated);
        }

        if (!Settings.Categories.Contains(updated.Category) && !string.IsNullOrWhiteSpace(updated.Category))
        {
            Settings.Categories.Add(updated.Category);
            SaveSettings();
        }
        SavePeople();
    }

    public void RemoveRange(IEnumerable<BirthdayPerson> people)
    {
        foreach (var p in people.ToList())
        {
            People.Remove(p);
        }
        SavePeople();
    }

    #endregion

    #region 搜索 / 筛选 / 排序

    /// <summary>
    /// 综合搜索关键字、分类筛选与排序方式，返回展示用的列表
    /// </summary>
    public List<BirthdayPerson> Query(string? keyword, string? category, BirthdaySortMode sortMode, DateTime today)
    {
        IEnumerable<BirthdayPerson> query = People;

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var kw = keyword.Trim();
            query = query.Where(p =>
                p.Name.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                p.Notes.Contains(kw, StringComparison.OrdinalIgnoreCase) ||
                p.Category.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "全部分类")
        {
            query = query.Where(p => p.Category == category);
        }

        query = sortMode switch
        {
            BirthdaySortMode.NameAsc => query.OrderBy(p => p.Name),
            BirthdaySortMode.NameDesc => query.OrderByDescending(p => p.Name),
            BirthdaySortMode.NextBirthdayAsc => query.OrderBy(p => _calculator.GetDaysUntilNextBirthday(p, today)),
            BirthdaySortMode.NextBirthdayDesc => query.OrderByDescending(p => _calculator.GetDaysUntilNextBirthday(p, today)),
            BirthdaySortMode.AgeAsc => query.OrderBy(p => _calculator.GetCurrentAge(p, today) ?? int.MaxValue),
            BirthdaySortMode.AgeDesc => query.OrderByDescending(p => _calculator.GetCurrentAge(p, today) ?? -1),
            BirthdaySortMode.CategoryAsc => query.OrderBy(p => p.Category).ThenBy(p => p.Name),
            _ => query
        };

        return query.ToList();
    }

    public List<string> GetAllCategories()
    {
        var set = new List<string> { "全部分类" };
        set.AddRange(Settings.Categories);
        foreach (var c in People.Select(p => p.Category).Distinct())
        {
            if (!set.Contains(c))
            {
                set.Add(c);
            }
        }
        return set;
    }

    #endregion

    #region 导入导出

    public void ExportExcel(string path) => Helpers.ExcelHelper.Export(path, People);

    public void ExportExcelTemplate(string path) => Helpers.ExcelHelper.ExportTemplate(path);

    /// <summary>
    /// 从 Excel 导入，返回 (成功导入数量, 错误信息列表)
    /// </summary>
    public (int Count, List<string> Errors) ImportExcel(string path, bool replaceAll)
    {
        var imported = Helpers.ExcelHelper.Import(path, out var errors);
        if (replaceAll)
        {
            People = new ObservableCollection<BirthdayPerson>(imported);
        }
        else
        {
            foreach (var p in imported)
            {
                People.Add(p);
            }
        }

        foreach (var category in imported.Select(p => p.Category).Distinct())
        {
            if (!Settings.Categories.Contains(category))
            {
                Settings.Categories.Add(category);
            }
        }

        SavePeople();
        SaveSettings();
        return (imported.Count, errors);
    }

    #endregion

    #region 备份与还原

    /// <summary>
    /// 创建备份压缩包，返回备份文件路径
    /// </summary>
    public string CreateBackup()
    {
        SavePeople();
        SaveSettings();
        var fileName = $"BirthdayReminder-Backup-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
        var backupPath = Path.Combine(BackupsFolder, fileName);

        using var zip = ZipFile.Open(backupPath, ZipArchiveMode.Create);
        if (File.Exists(PeopleFilePath))
        {
            zip.CreateEntryFromFile(PeopleFilePath, "People.json");
        }
        if (File.Exists(SettingsFilePath))
        {
            zip.CreateEntryFromFile(SettingsFilePath, "Settings.json");
        }
        return backupPath;
    }

    public List<string> GetBackups()
    {
        return Directory.Exists(BackupsFolder)
            ? Directory.GetFiles(BackupsFolder, "*.zip").OrderByDescending(f => f).ToList()
            : new List<string>();
    }

    /// <summary>
    /// 从备份压缩包还原数据
    /// </summary>
    public void RestoreBackup(string backupZipPath)
    {
        using var zip = ZipFile.OpenRead(backupZipPath);
        var peopleEntry = zip.GetEntry("People.json");
        var settingsEntry = zip.GetEntry("Settings.json");
        peopleEntry?.ExtractToFile(PeopleFilePath, true);
        settingsEntry?.ExtractToFile(SettingsFilePath, true);
        Load();
    }

    /// <summary>
    /// 从任意路径的备份 zip 导入（例如用户从别的电脑拷贝过来的备份文件）
    /// </summary>
    public void RestoreBackupFromExternalFile(string externalZipPath)
    {
        var fileName = $"BirthdayReminder-Restore-{DateTime.Now:yyyyMMdd-HHmmss}.zip";
        var localCopy = Path.Combine(BackupsFolder, fileName);
        File.Copy(externalZipPath, localCopy, true);
        RestoreBackup(localCopy);
    }

    #endregion

    #region 防重复提醒日志

    public List<ReminderLogEntry> LoadReminderLog()
    {
        return ConfigureFileHelper.LoadConfig<List<ReminderLogEntry>>(LogFilePath);
    }

    public void SaveReminderLog(List<ReminderLogEntry> log)
    {
        // 只保留最近 400 天的记录，避免文件无限增长
        var cutoff = DateTime.Now.AddDays(-400);
        var filtered = log.Where(e => DateTime.TryParse(e.FiredOnDate, out var d) && d >= cutoff).ToList();
        ConfigureFileHelper.SaveConfig(LogFilePath, filtered, true);
    }

    #endregion
}
