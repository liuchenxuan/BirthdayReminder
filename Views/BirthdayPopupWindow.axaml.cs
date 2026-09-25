using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Transformation;
using Avalonia.Threading;

namespace ClassIsland.BirthdayReminder.Views;

/// <summary>
/// 桌面弹窗提醒窗口：独立于 ClassIsland 横幅提醒，直接以窗口形式弹出在屏幕角落。
/// </summary>
public partial class BirthdayPopupWindow : Window
{
    private DispatcherTimer? _autoCloseTimer;

    public BirthdayPopupWindow()
    {
        InitializeComponent();
        Opened += OnOpened;
    }

    /// <summary>
    /// 设置弹窗展示的内容
    /// </summary>
    /// <param name="title">标题</param>
    /// <param name="content">正文</param>
    /// <param name="isTodayCelebration">是否是生日当天（会启用发光/强调动画与更醒目的图标）</param>
    /// <param name="durationSeconds">自动关闭时间（秒），小于等于 0 则不自动关闭</param>
    public void SetContent(string title, string content, bool isTodayCelebration, int durationSeconds)
    {
        TitleText.Text = title;
        ContentText.Text = content;
        EmojiText.Text = isTodayCelebration ? "🎉" : "🎂";
        // 光晕的呼吸动画由 celebrate 样式类驱动。样式动画的优先级高于本地值（直接设置 Opacity 压不住它），
        // 所以通过增删该类来控制：仅在生日当天添加。
        GlowBorder.Classes.Set("celebrate", isTodayCelebration);
        GlowBorder.Opacity = isTodayCelebration ? 1 : 0;
        RootCard.Background = isTodayCelebration
            ? new SolidColorBrush(Color.Parse("#F2C8501C"))
            : new SolidColorBrush(Color.Parse("#F2222831"));

        if (durationSeconds > 0)
        {
            _autoCloseTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(durationSeconds)
            };
            _autoCloseTimer.Tick += (_, _) =>
            {
                _autoCloseTimer?.Stop();
                CloseWithFade();
            };
            _autoCloseTimer.Start();
        }
    }

    /// <summary>
    /// 将窗口定位到主屏幕的右下角
    /// </summary>
    public void PlaceAtBottomRight(int offsetIndex = 0)
    {
        var screen = Screens.Primary ?? (Screens.All.Count > 0 ? Screens.All[0] : null);
        if (screen == null)
        {
            return;
        }

        var area = screen.WorkingArea;
        var margin = 20;
        var stackGap = (int)(Height <= 0 ? 150 : Height) + 12;
        var x = area.X + area.Width - (int)Width - margin;
        var y = area.Y + area.Height - margin - stackGap * (offsetIndex + 1);
        Position = new PixelPoint(x, y);
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        RootCard.Opacity = 1;
        RootCard.RenderTransform = TransformOperations.Parse("scale(1)");
    }

    private void RootCard_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _autoCloseTimer?.Stop();
        CloseWithFade();
    }

    private void CloseWithFade()
    {
        RootCard.Opacity = 0;
        RootCard.RenderTransform = TransformOperations.Parse("scale(0.9)");
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(320) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Close();
        };
        timer.Start();
    }
}
