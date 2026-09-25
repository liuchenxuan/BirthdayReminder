using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.BirthdayReminder.Models;

/// <summary>
/// 代表一条生日记录（一个人）。
/// </summary>
public class BirthdayPerson : ObservableRecipient
{
    private string _id = Guid.NewGuid().ToString("N");
    private string _name = "";
    private int _birthYear = DateTime.Now.Year - 10;
    private bool _hasKnownYear = true;
    private int _birthMonth = 1;
    private int _birthDay = 1;
    private string _category = "家人";
    private string _notes = "";
    private ObservableCollection<int> _reminderOffsets = new() { 0, 1, 3, 7 };
    private bool _useCustomOffsets = false;
    private int _customOffsetDays = 15;
    private bool _isEnabled = true;

    /// <summary>
    /// 记录唯一 ID
    /// </summary>
    public string Id
    {
        get => _id;
        set { if (value == _id) return; _id = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 姓名
    /// </summary>
    public string Name
    {
        get => _name;
        set { if (value == _name) return; _name = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayBirthday)); }
    }

    /// <summary>
    /// 出生年份。当 <see cref="HasKnownYear"/> 为 false 时此值仅用于内部计算，不会对外显示。
    /// </summary>
    public int BirthYear
    {
        get => _birthYear;
        set { if (value == _birthYear) return; _birthYear = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayBirthday)); }
    }

    /// <summary>
    /// 是否已知出生年份（决定是否显示/计算年龄）
    /// </summary>
    public bool HasKnownYear
    {
        get => _hasKnownYear;
        set { if (value == _hasKnownYear) return; _hasKnownYear = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayBirthday)); }
    }

    /// <summary>
    /// 出生月份 (1-12)
    /// </summary>
    public int BirthMonth
    {
        get => _birthMonth;
        set { if (value == _birthMonth) return; _birthMonth = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayBirthday)); }
    }

    /// <summary>
    /// 出生日期 (1-31)
    /// </summary>
    public int BirthDay
    {
        get => _birthDay;
        set { if (value == _birthDay) return; _birthDay = value; OnPropertyChanged(); OnPropertyChanged(nameof(DisplayBirthday)); }
    }

    /// <summary>
    /// 分类（如：家人、朋友、同事、学生……可自定义）
    /// </summary>
    public string Category
    {
        get => _category;
        set { if (value == _category) return; _category = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 备注
    /// </summary>
    public string Notes
    {
        get => _notes;
        set { if (value == _notes) return; _notes = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 这个人使用的提醒时机（提前天数集合，0 表示当天）。
    /// 常见取值：0（当天）、1、3、7、30，也可以加入自定义天数。
    /// </summary>
    public ObservableCollection<int> ReminderOffsets
    {
        get => _reminderOffsets;
        set { if (Equals(value, _reminderOffsets)) return; _reminderOffsets = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 是否启用了独立于全局的自定义提前提醒天数
    /// </summary>
    public bool UseCustomOffsets
    {
        get => _useCustomOffsets;
        set { if (value == _useCustomOffsets) return; _useCustomOffsets = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 自定义提前提醒天数
    /// </summary>
    public int CustomOffsetDays
    {
        get => _customOffsetDays;
        set { if (value == _customOffsetDays) return; _customOffsetDays = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 是否启用这条记录的提醒
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set { if (value == _isEnabled) return; _isEnabled = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 用于界面展示的生日文本
    /// </summary>
    public string DisplayBirthday => HasKnownYear
        ? $"{BirthYear:D4}-{BirthMonth:D2}-{BirthDay:D2}"
        : $"--{BirthMonth:D2}-{BirthDay:D2}（未知年份）";
}
