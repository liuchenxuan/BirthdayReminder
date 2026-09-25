using System;
using System.Collections.Generic;
using Avalonia.Threading;
using ClassIsland.BirthdayReminder.Views;

namespace ClassIsland.BirthdayReminder.Services;

/// <summary>
/// 负责在桌面右下角显示生日提醒弹窗（独立于 ClassIsland 内置横幅提醒）。
/// </summary>
public class DesktopPopupService
{
    private readonly List<BirthdayPopupWindow> _activeWindows = new();

    /// <summary>
    /// 显示一个桌面弹窗
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="content">正文</param>
    /// <param name="isTodayCelebration">是否是生日当天（使用更醒目的图标与配色）</param>
    /// <param name="playCelebrationAnimation">是否播放光晕呼吸动画</param>
    /// <param name="durationSeconds">自动关闭时间（秒）</param>
    public void Show(string title, string content, bool isTodayCelebration, bool playCelebrationAnimation, int durationSeconds)
    {
        Dispatcher.UIThread.Post(() =>
        {
            try
            {
                var window = new BirthdayPopupWindow();
                window.SetContent(title, content, isTodayCelebration, playCelebrationAnimation, durationSeconds);
                window.Closed += (_, _) => _activeWindows.Remove(window);
                _activeWindows.Add(window);
                window.Show();
                window.PlaceAtBottomRight(_activeWindows.Count - 1);
            }
            catch
            {
                // 在极少数无图形界面/桌面会话不可用的环境下忽略弹窗失败，不影响其它提醒渠道
            }
        });
    }

    /// <summary>
    /// 关闭所有当前显示的弹窗
    /// </summary>
    public void CloseAll()
    {
        Dispatcher.UIThread.Post(() =>
        {
            foreach (var w in _activeWindows.ToArray())
            {
                w.Close();
            }
        });
    }
}
