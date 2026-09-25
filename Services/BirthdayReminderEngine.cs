using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ClassIsland.BirthdayReminder.Helpers;
using ClassIsland.BirthdayReminder.Models;
using ClassIsland.BirthdayReminder.Services.NotificationProviders;
using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Models.Notification;
using ClassIsland.Shared;
using ClassIsland.Shared.Models.Notification;
// 项目开启了 ImplicitUsings（会隐式 global using System.Threading），
// System.Threading.Timer 与 System.Timers.Timer 同名会产生歧义（CS0104），这里显式指定使用 System.Timers.Timer。
using Timer = System.Timers.Timer;
// ClassIsland.Shared.Models.Notification 中还有一个已弃用的同名 NotificationRequest（v1），
// 这里显式指定使用 v2 版本的提醒请求，避免歧义（CS0104）。
using NotificationRequest = ClassIsland.Core.Models.Notification.NotificationRequest;

namespace ClassIsland.BirthdayReminder.Services;

/// <summary>
/// 生日提醒调度核心：定时检查是否有需要提醒的生日，
/// 合并同一次检查中触发的多条提醒为一条消息，并防止同一天重复提醒。
/// 这是一个普通单例服务（不依赖 IHostedService），方便被设置界面和提醒提供方共同引用。
/// </summary>
public class BirthdayReminderEngine
{
    private readonly BirthdayStorageService _storage;
    private readonly BirthdayCalculatorService _calculator;
    private readonly DesktopPopupService _popupService;
    private readonly Timer _timer;

    private NotificationProviderBase? _notificationProvider;

    public BirthdayReminderEngine(BirthdayStorageService storage, BirthdayCalculatorService calculator, DesktopPopupService popupService)
    {
        _storage = storage;
        _calculator = calculator;
        _popupService = popupService;

        var interval = Math.Max(10, _storage.Settings.CheckIntervalSeconds);
        _timer = new Timer(TimeSpan.FromSeconds(interval).TotalMilliseconds)
        {
            AutoReset = true
        };
        _timer.Elapsed += (_, _) => SafeCheck();
        _timer.Start();

        // 启动后先检查一次，避免刚开机时要等一个周期才提醒
        SafeCheck();
    }

    /// <summary>
    /// 由 <see cref="BirthdayNotificationProvider"/> 在构造时调用，绑定用于发送横幅提醒的提供方实例。
    /// </summary>
    public void AttachNotificationProvider(NotificationProviderBase provider)
    {
        _notificationProvider = provider;
    }

    /// <summary>
    /// 重新应用检查间隔（当用户在设置中修改后调用）
    /// </summary>
    public void ApplyInterval()
    {
        var interval = Math.Max(10, _storage.Settings.CheckIntervalSeconds);
        _timer.Interval = TimeSpan.FromSeconds(interval).TotalMilliseconds;
    }

    private void SafeCheck()
    {
        try
        {
            CheckAndNotify();
        }
        catch
        {
            // 提醒检查不应导致插件崩溃，忽略单次检查中的异常
        }
    }

    /// <summary>
    /// 核心检查逻辑：找出今天所有触发提醒条件的人员，合并为一条消息后发送。
    /// </summary>
    public void CheckAndNotify()
    {
        var settings = _storage.Settings;
        if (!settings.IsEnabled)
        {
            return;
        }

        var today = DateTime.Now;
        var todayKey = today.ToString("yyyy-MM-dd");
        var log = _storage.LoadReminderLog();
        var fired = new HashSet<(string PersonId, int Offset)>(
            log.Where(e => e.FiredOnDate == todayKey).Select(e => (e.PersonId, e.OffsetDays)));

        var due = new List<(BirthdayPerson Person, int Days, bool IsToday)>();

        foreach (var person in _storage.People.Where(p => p.IsEnabled))
        {
            var days = _calculator.GetDaysUntilNextBirthday(person, today);
            var offsets = _calculator.GetEffectiveOffsets(person, settings.DefaultReminderOffsets);
            if (!offsets.Contains(days))
            {
                continue;
            }
            if (fired.Contains((person.Id, days)))
            {
                continue;
            }
            due.Add((person, days, days == 0));
        }

        if (due.Count == 0)
        {
            return;
        }

        // 标记为已提醒，防止本轮之后重复触发
        foreach (var (person, days, _) in due)
        {
            log.Add(new ReminderLogEntry { PersonId = person.Id, OffsetDays = days, FiredOnDate = todayKey });
        }
        _storage.SaveReminderLog(log);

        SendMergedReminder(due, today);
    }

    private void SendMergedReminder(List<(BirthdayPerson Person, int Days, bool IsToday)> due, DateTime today)
    {
        var settings = _storage.Settings;
        var isAnyToday = due.Any(x => x.IsToday);

        var lines = due.Select(x =>
        {
            var template = x.IsToday ? settings.TemplateOnDay : settings.TemplateBefore;
            return TemplateTextHelper.Render(template, x.Person, x.Days, _calculator, today);
        }).ToList();

        var combinedBody = string.Join("\n", lines);
        var title = isAnyToday ? "🎉 生日提醒" : "🎂 生日预告";

        Dispatch(title, combinedBody, isAnyToday, settings.IsFullScreenEffectEnabled || (isAnyToday && settings.IsTodaySpecialAnimationEnabled));
    }

    /// <summary>
    /// 实际执行“分发”动作：按开关分别触发桌面弹窗 / 横幅 / 全屏强调 / 语音。
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="body">正文内容（可能包含多条合并的生日信息）</param>
    /// <param name="isTodayCelebration">是否包含“今天生日”的特殊场景</param>
    /// <param name="forceFullScreenEffect">是否强制使用全屏强调动画</param>
    public void Dispatch(string title, string body, bool isTodayCelebration, bool forceFullScreenEffect)
    {
        var settings = _storage.Settings;

        if (settings.IsDesktopPopupEnabled)
        {
            // 光晕动画受“生日当天播放特殊动画”开关控制（该开关的说明是同时加强弹窗与横幅的强调效果）
            _popupService.Show(title, body, isTodayCelebration,
                playCelebrationAnimation: isTodayCelebration && settings.IsTodaySpecialAnimationEnabled,
                settings.DesktopPopupDurationSeconds);
        }

        if (settings.IsSpeechEnabled)
        {
            SpeakSafely(body);
        }

        if (settings.IsBannerEnabled && _notificationProvider != null)
        {
            var mask = NotificationContent.CreateTwoIconsMask(title);
            var overlay = NotificationContent.CreateSimpleTextContent(body, factory: c =>
            {
                c.IsSpeechEnabled = settings.IsSpeechEnabled;
                c.SpeechContent = body;
                c.Duration = TimeSpan.FromSeconds(10);
            });

            var request = new NotificationRequest
            {
                MaskContent = mask,
                OverlayContent = overlay
            };

            if (forceFullScreenEffect)
            {
                request.RequestNotificationSettings = new NotificationSettings
                {
                    IsSettingsEnabled = true,
                    IsNotificationEnabled = true,
                    IsNotificationEffectEnabled = true,
                    IsSpeechEnabled = settings.IsSpeechEnabled,
                    IsNotificationTopmostEnabled = true
                };
            }

            _notificationProvider.ShowNotification(request);
        }
    }

    private void SpeakSafely(string text)
    {
        try
        {
            var speech = IAppHost.TryGetService<ClassIsland.Core.Abstractions.Services.SpeechService.ISpeechService>();
            speech?.EnqueueSpeechQueue(text);
        }
        catch
        {
            // 语音服务不可用时忽略，不影响其它提醒渠道
        }
    }

    #region 测试效果

    /// <summary>
    /// 使用一个虚拟示例人员测试指定的提醒渠道效果，方便用户在设置页面中预览。
    /// </summary>
    public void RunTest(string channel)
    {
        var sample = new BirthdayPerson
        {
            Name = "测试同学",
            BirthYear = DateTime.Now.Year - 18,
            BirthMonth = DateTime.Now.Month,
            BirthDay = DateTime.Now.Day,
            HasKnownYear = true,
            Category = "测试"
        };
        var today = DateTime.Now;
        var isToday = channel is "today" or "fullscreen";
        var days = isToday ? 0 : 3;
        var template = isToday ? _storage.Settings.TemplateOnDay : _storage.Settings.TemplateBefore;
        var body = TemplateTextHelper.Render(template, sample, days, _calculator, today);
        var title = isToday ? "🎉 生日提醒（测试）" : "🎂 生日预告（测试）";

        switch (channel)
        {
            case "popup":
                _popupService.Show(title, body, isTodayCelebration: false, playCelebrationAnimation: false, _storage.Settings.DesktopPopupDurationSeconds);
                break;
            case "speech":
                SpeakSafely(body);
                break;
            case "banner":
                DispatchBannerOnly(title, body, false);
                break;
            case "fullscreen":
            case "today":
                // 测试按钮用于预览完整效果，与横幅/语音测试一样不受开关影响
                _popupService.Show(title, body, isTodayCelebration: true, playCelebrationAnimation: true, _storage.Settings.DesktopPopupDurationSeconds);
                SpeakSafely(body);
                DispatchBannerOnly(title, body, true);
                break;
        }
    }

    private void DispatchBannerOnly(string title, string body, bool forceEffect)
    {
        if (_notificationProvider == null)
        {
            return;
        }
        var mask = NotificationContent.CreateTwoIconsMask(title);
        var overlay = NotificationContent.CreateSimpleTextContent(body, factory: c =>
        {
            c.IsSpeechEnabled = true;
            c.SpeechContent = body;
        });
        var request = new NotificationRequest { MaskContent = mask, OverlayContent = overlay };
        if (forceEffect)
        {
            request.RequestNotificationSettings = new NotificationSettings
            {
                IsSettingsEnabled = true,
                IsNotificationEnabled = true,
                IsNotificationEffectEnabled = true,
                IsSpeechEnabled = true,
                IsNotificationTopmostEnabled = true
            };
        }
        _notificationProvider.ShowNotification(request);
    }

    #endregion
}
