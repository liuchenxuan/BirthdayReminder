using ClassIsland.Core.Abstractions.Services.NotificationProviders;
using ClassIsland.Core.Attributes;

namespace ClassIsland.BirthdayReminder.Services.NotificationProviders;

/// <summary>
/// 生日提醒的 ClassIsland 提醒提供方。
/// 这个类本身只负责向 ClassIsland 的提醒系统“报到”，
/// 真正的判断/合并/去重逻辑都在 <see cref="BirthdayReminderEngine"/> 中完成，
/// 这样设置界面等其它地方也可以复用同一套逻辑，而不需要重复获取这个提供方实例。
/// </summary>
[NotificationProviderInfo(
    "9F1C9E2B-9C7F-4B0B-9B0F-1E9E7B7D0A11",
    "生日提醒",
    "\ue0ff",
    "在生日临近或当天通过桌面弹窗、横幅、全屏强调动画与语音播报提醒。")]
public class BirthdayNotificationProvider : NotificationProviderBase
{
    public BirthdayNotificationProvider(BirthdayReminderEngine engine)
    {
        engine.AttachNotificationProvider(this);
    }
}
