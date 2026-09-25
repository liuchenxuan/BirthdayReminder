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
    // 以下尺寸均为与 DPI 无关的逻辑像素
    /// <summary>卡片四周的透明边距（用于绘制光晕和阴影），需与 XAML 中 PopupRoot 的 Margin 保持一致</summary>
    private const double ContentMargin = 16;

    /// <summary>卡片与屏幕工作区边缘之间的可见距离</summary>
    private const double ScreenEdgeMargin = 20;

    /// <summary>多个弹窗堆叠时卡片之间的可见间距。不小于 ContentMargin，保证透明边距不会盖住相邻弹窗的卡片</summary>
    private const double StackSpacing = 16;

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
    /// <param name="isTodayCelebration">是否是生日当天（使用更醒目的图标与配色）</param>
    /// <param name="playCelebrationAnimation">是否播放光晕呼吸动画（对应设置中的“生日当天播放特殊动画”）</param>
    /// <param name="durationSeconds">自动关闭时间（秒），小于等于 0 则不自动关闭</param>
    public void SetContent(string title, string content, bool isTodayCelebration, bool playCelebrationAnimation, int durationSeconds)
    {
        TitleText.Text = title;
        ContentText.Text = content;
        EmojiText.Text = isTodayCelebration ? "🎉" : "🎂";
        // 光晕的呼吸动画由 celebrate 样式类驱动。样式动画的优先级高于本地值（直接设置 Opacity 压不住它），
        // 所以通过增删该类来控制。
        GlowBorder.Classes.Set("celebrate", playCelebrationAnimation);
        GlowBorder.Opacity = playCelebrationAnimation ? 1 : 0;
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
    /// 将窗口定位到主屏幕的右下角（需在 Show() 之后调用，此时窗口尺寸已确定）
    /// </summary>
    /// <param name="offsetIndex">第几个弹窗（从 0 开始），多个弹窗依次向上堆叠</param>
    public void PlaceAtBottomRight(int offsetIndex = 0)
    {
        var screen = Screens.Primary ?? (Screens.All.Count > 0 ? Screens.All[0] : null);
        if (screen == null)
        {
            return;
        }

        // WorkingArea 与 Position 使用物理像素，而 ClientSize 是逻辑像素，需要乘以屏幕缩放比例换算，
        // 否则在 125% / 150% 缩放的屏幕上弹窗会有一部分跑到屏幕外。
        // 另外 SizeToContent 模式下 Height 属性可能仍是 NaN，这里改用 ClientSize。
        var scaling = screen.Scaling;
        var area = screen.WorkingArea;
        var windowSize = ClientSize;
        var cardHeight = Math.Max(0, windowSize.Height - ContentMargin * 2);

        // 窗口比卡片四周各大出 ContentMargin，所以窗口边缘离屏幕边缘只需 ScreenEdgeMargin - ContentMargin
        var right = area.Right - (ScreenEdgeMargin - ContentMargin) * scaling;
        var bottom = area.Bottom - (ScreenEdgeMargin - ContentMargin) * scaling
                     - (cardHeight + StackSpacing) * scaling * offsetIndex;
        Position = new PixelPoint(
            (int)Math.Round(right - windowSize.Width * scaling),
            (int)Math.Round(bottom - windowSize.Height * scaling));
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        PopupRoot.Opacity = 1;
        PopupRoot.RenderTransform = TransformOperations.Parse("scale(1)");
    }

    private void RootCard_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _autoCloseTimer?.Stop();
        CloseWithFade();
    }

    private void CloseWithFade()
    {
        PopupRoot.Opacity = 0;
        PopupRoot.RenderTransform = TransformOperations.Parse("scale(0.9)");
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(320) };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            Close();
        };
        timer.Start();
    }
}
