using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using RandomPicker.Services;

namespace RandomPicker.Views;

/// <summary>点名台：大字滚动随机抽人。</summary>
public sealed partial class PickPage : Page
{
    private Guid? _classId;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(100) }; // 0.1 秒 / 人
    private readonly Random _random = new();
    private string? _lastShown;
    private bool _running;

    public PickPage()
    {
        this.InitializeComponent();
        _timer.Tick += Timer_Tick;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        _classId = e.Parameter as Guid?;
        LoadClass();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        StopRolling(playAnimation: false);
    }

    private void LoadClass()
    {
        var cls = _classId is Guid id ? Store.Find(id) : null;

        if (cls == null || cls.Students.Count == 0)
        {
            ClassTitle.Text = "";
            NameText.Text = "？";
            StartBtn.IsEnabled = false;
            BtnText.Text = "开始随机";
            BtnIcon.Glyph = "\uE768";
            EmptyHint.Visibility = Visibility.Visible;
            EmptyHint.Text = cls == null
                ? "当前没有班级，请先到左侧「班级管理」新建班级并导入名单。"
                : $"「{cls.Name}」的名单为空，请到「班级管理」导入 TXT 名单。";
            return;
        }

        StartBtn.IsEnabled = true;
        EmptyHint.Visibility = Visibility.Collapsed;
        ClassTitle.Text = $"{cls.Name} · {cls.Students.Count} 人";
        NameText.Text = "？";
        _lastShown = null;
    }

    private void StartBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_running) StopRolling();
        else StartRolling();
    }

    private void StartRolling()
    {
        _running = true;
        BtnText.Text = "停止";
        BtnIcon.Glyph = "\uE71A";   // Stop
        _timer.Start();
    }

    private void StopRolling(bool playAnimation = true)
    {
        if (!_running) return;

        _running = false;
        _timer.Stop();
        BtnText.Text = "开始随机";
        BtnIcon.Glyph = "\uE768";   // Play

        if (playAnimation) PlaySettleAnimation();
    }

    private void Timer_Tick(object sender, object e)
    {
        var cls = _classId is Guid id ? Store.Find(id) : null;
        if (cls == null || cls.Students.Count == 0)
        {
            StopRolling(playAnimation: false);
            LoadClass();
            return;
        }

        string next;
        do
        {
            next = cls.Students[_random.Next(cls.Students.Count)];
        } while (cls.Students.Count > 1 && next == _lastShown);

        _lastShown = next;
        NameText.Text = next;
    }

    /// <summary>停格时的小动画：卡片轻微放大回弹。</summary>
    private void PlaySettleAnimation()
    {
        NameCard.RenderTransform = new ScaleTransform();

        var storyboard = new Storyboard();
        var animX = new DoubleAnimation
        {
            From = 1.0,
            To = 1.1,
            Duration = new Duration(TimeSpan.FromMilliseconds(120)),
            AutoReverse = true
        };
        var animY = new DoubleAnimation
        {
            From = 1.0,
            To = 1.1,
            Duration = new Duration(TimeSpan.FromMilliseconds(120)),
            AutoReverse = true
        };

        Storyboard.SetTarget(animX, NameCard);
        Storyboard.SetTarget(animY, NameCard);
        Storyboard.SetTargetProperty(animX, "(UIElement.RenderTransform).(ScaleTransform.ScaleX)");
        Storyboard.SetTargetProperty(animY, "(UIElement.RenderTransform).(ScaleTransform.ScaleY)");

        storyboard.Children.Add(animX);
        storyboard.Children.Add(animY);
        storyboard.Begin();
    }
}
