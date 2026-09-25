using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.BirthdayReminder.Models;

/// <summary>
/// 插件全局设置
/// </summary>
public class PluginSettings : ObservableRecipient
{
    private bool _isEnabled = true;
    private int _checkIntervalSeconds = 30;

    private bool _isDesktopPopupEnabled = true;
    private bool _isBannerEnabled = true;
    private bool _isFullScreenEffectEnabled = true;
    private bool _isSpeechEnabled = true;

    private bool _isTodaySpecialAnimationEnabled = true;

    private ObservableCollection<int> _defaultReminderOffsets = new() { 0, 1, 3, 7 };

    private string _templateOnDay = "🎉 今天是 {name} 的生日！{ageText}生日快乐呀～";
    private string _templateBefore = "🎂 还有 {days} 天就是 {name} 的生日啦，{ageText}别忘了准备礼物哦～";

    private int _desktopPopupDurationSeconds = 12;
    private ObservableCollection<string> _categories = new() { "家人", "朋友", "同事", "同学", "其他" };

    /// <summary>
    /// 插件总开关
    /// </summary>
    public bool IsEnabled
    {
        get => _isEnabled;
        set { if (value == _isEnabled) return; _isEnabled = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 生日检查间隔（秒）。默认每 30 秒检查一次是否有生日需要提醒。
    /// </summary>
    public int CheckIntervalSeconds
    {
        get => _checkIntervalSeconds;
        set { if (value == _checkIntervalSeconds) return; _checkIntervalSeconds = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 是否启用桌面弹窗提醒
    /// </summary>
    public bool IsDesktopPopupEnabled
    {
        get => _isDesktopPopupEnabled;
        set { if (value == _isDesktopPopupEnabled) return; _isDesktopPopupEnabled = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 是否启用 ClassIsland 横幅提醒
    /// </summary>
    public bool IsBannerEnabled
    {
        get => _isBannerEnabled;
        set { if (value == _isBannerEnabled) return; _isBannerEnabled = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 是否启用全屏强调动画（依赖横幅提醒）
    /// </summary>
    public bool IsFullScreenEffectEnabled
    {
        get => _isFullScreenEffectEnabled;
        set { if (value == _isFullScreenEffectEnabled) return; _isFullScreenEffectEnabled = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 是否启用语音播报
    /// </summary>
    public bool IsSpeechEnabled
    {
        get => _isSpeechEnabled;
        set { if (value == _isSpeechEnabled) return; _isSpeechEnabled = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 生日当天是否播放特殊强调动画（无论全屏动画开关是否打开，当天都会强制加强提醒效果）
    /// </summary>
    public bool IsTodaySpecialAnimationEnabled
    {
        get => _isTodaySpecialAnimationEnabled;
        set { if (value == _isTodaySpecialAnimationEnabled) return; _isTodaySpecialAnimationEnabled = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 新增记录时默认使用的提醒时机（提前天数），0 表示当天
    /// </summary>
    public ObservableCollection<int> DefaultReminderOffsets
    {
        get => _defaultReminderOffsets;
        set { if (Equals(value, _defaultReminderOffsets)) return; _defaultReminderOffsets = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 生日当天使用的提醒文本模板。支持占位符：{name} {age} {ageText} {category} {days} {date}
    /// </summary>
    public string TemplateOnDay
    {
        get => _templateOnDay;
        set { if (value == _templateOnDay) return; _templateOnDay = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 提前提醒使用的文本模板。支持占位符：{name} {age} {ageText} {category} {days} {date}
    /// </summary>
    public string TemplateBefore
    {
        get => _templateBefore;
        set { if (value == _templateBefore) return; _templateBefore = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 桌面弹窗显示时长（秒）
    /// </summary>
    public int DesktopPopupDurationSeconds
    {
        get => _desktopPopupDurationSeconds;
        set { if (value == _desktopPopupDurationSeconds) return; _desktopPopupDurationSeconds = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 已使用过的分类列表，方便在界面中下拉选择
    /// </summary>
    public ObservableCollection<string> Categories
    {
        get => _categories;
        set { if (Equals(value, _categories)) return; _categories = value; OnPropertyChanged(); }
    }
}
