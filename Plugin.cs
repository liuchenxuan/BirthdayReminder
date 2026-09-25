using ClassIsland.BirthdayReminder.Services;
using ClassIsland.BirthdayReminder.Services.NotificationProviders;
using ClassIsland.BirthdayReminder.Views.SettingsPages;
using ClassIsland.Core.Abstractions;
using ClassIsland.Core.Attributes;
using ClassIsland.Core.Extensions.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ClassIsland.BirthdayReminder;

/// <summary>
/// 生日提醒插件入口。
/// </summary>
[PluginEntrance]
public class Plugin : PluginBase
{
    public override void Initialize(HostBuilderContext context, IServiceCollection services)
    {
        // 数据与逻辑服务
        services.AddSingleton<BirthdayCalculatorService>();
        services.AddSingleton<DesktopPopupService>();
        services.AddSingleton(sp => new BirthdayStorageService(PluginConfigFolder, sp.GetRequiredService<BirthdayCalculatorService>()));
        services.AddSingleton<BirthdayReminderEngine>();

        // 提醒提供方：负责向 ClassIsland 横幅提醒系统发送提醒
        services.AddNotificationProvider<BirthdayNotificationProvider>();

        // 设置页面：姓名/生日管理、提醒规则、导入导出备份、总览看板
        services.AddSettingsPage<BirthdaySettingsPage>();
    }
}
