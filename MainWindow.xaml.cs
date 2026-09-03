using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using RandomPicker.Services;
using RandomPicker.Views;

namespace RandomPicker;

public sealed partial class MainWindow : Window
{
    private bool _suppressSelection;
    private Guid? _currentClassId;

    public MainWindow()
    {
        this.InitializeComponent();

        Title = "随机点名";
        SystemBackdrop = new MicaBackdrop();          // Mica 背景材质
        ExtendsContentIntoTitleBar = true;            // 标题栏延伸
        SetTitleBar(AppTitleBar);

        Store.ClassesChanged += Store_ClassesChanged;

        Activated += (s, e) =>
        {
            if (e.WindowActivationState != WindowActivationState.Deactivated && RootFrame.Content == null)
            {
                RebuildNav();
                NavigateToCurrentClass();
            }
        };

        Activated += MainWindow_Activated_FirstRun;
    }

    private void MainWindow_Activated_FirstRun(object sender, WindowActivatedEventArgs e)
    {
        if (e.WindowActivationState == WindowActivationState.Deactivated) return;
        Activated -= MainWindow_Activated_FirstRun;

        if (Store.IsFirstRun)
        {
            WelcomeTip.IsOpen = true;
            WelcomeTip.Closed += (_, _) =>
            {
                Store.Data.FirstRunDone = true;
                Store.Save();
            };
        }
    }

    private void Store_ClassesChanged()
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            // 当前点名班级可能已被删除，回退到第一个班级
            if (_currentClassId is Guid id && Store.Find(id) == null)
                _currentClassId = Store.Data.Classes.FirstOrDefault()?.Id;

            RebuildNav();
        });
    }

    /// <summary>根据班级列表重建侧边导航项，并尽量恢复之前的选中状态。</summary>
    private void RebuildNav()
    {
        _suppressSelection = true;

        var prevTag = (Nav.SelectedItem as NavigationViewItem)?.Tag as string;

        Nav.MenuItems.Clear();
        foreach (var c in Store.Data.Classes)
        {
            Nav.MenuItems.Add(new NavigationViewItem
            {
                Content = c.Name,
                Tag = c.Id.ToString(),
                Icon = new FontIcon { Glyph = "\uE902" }   // People
            });
        }
        Nav.MenuItems.Add(new NavigationViewItemSeparator());

        if (prevTag == "manage")
        {
            Nav.SelectedItem = FooterItem("manage");
        }
        else if (prevTag != null &&
                 Nav.MenuItems.OfType<NavigationViewItem>().Any(i => i.Tag as string == prevTag))
        {
            Nav.SelectedItem = Nav.MenuItems.OfType<NavigationViewItem>()
                              .First(i => i.Tag as string == prevTag);
        }
        else
        {
            Nav.SelectedItem = Nav.MenuItems.OfType<NavigationViewItem>().FirstOrDefault();
        }

        _suppressSelection = false;
    }

    private NavigationViewItem? FooterItem(string tag) =>
        Nav.FooterMenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => i.Tag as string == tag);

    private void NavigateToCurrentClass()
    {
        if (_currentClassId is Guid id && Store.Find(id) != null)
        {
            RootFrame.Navigate(typeof(PickPage), id);
        }
        else if (Store.Data.Classes.Count > 0)
        {
            _currentClassId = Store.Data.Classes[0].Id;
            RootFrame.Navigate(typeof(PickPage), _currentClassId);
        }
        else
        {
            RootFrame.Navigate(typeof(PickPage), null);
        }
        RestoreSelection();
    }

    /// <summary>让导航选中态与当前页面保持一致。</summary>
    private void RestoreSelection()
    {
        _suppressSelection = true;

        if (RootFrame.Content is ManagePage)
        {
            Nav.SelectedItem = FooterItem("manage");
        }
        else if (_currentClassId is Guid id)
        {
            Nav.SelectedItem = Nav.MenuItems.OfType<NavigationViewItem>()
                              .FirstOrDefault(i => i.Tag as string == id.ToString());
        }

        _suppressSelection = false;
    }

    private void Nav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (_suppressSelection) return;

        var tag = (args.SelectedItem as NavigationViewItem)?.Tag as string;
        if (tag == null) return;

        if (tag == "manage")
        {
            RootFrame.Navigate(typeof(ManagePage));
        }
        else if (tag == "about")
        {
            ShowAbout();
            RestoreSelection();   // "关于"是弹窗而非页面，恢复原选中
        }
        else if (Guid.TryParse(tag, out var id) && Store.Find(id) != null)
        {
            _currentClassId = id;
            RootFrame.Navigate(typeof(PickPage), id);
        }
    }

    private async void ShowAbout()
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock { Text = "随机点名 v1.0.0", FontWeight = Microsoft.UI.Text.FontWeights.SemiBold });
        panel.Children.Add(new TextBlock
        {
            Text = "一个适用于课堂的随机点名小工具：左侧切换班级，中间大字滚动抽人，空格键随时开始 / 停止。",
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = "所有班级数据保存在程序同目录的 data.json 文件中，可随时备份或迁移。",
            TextWrapping = TextWrapping.Wrap
        });

        var dlg = new ContentDialog
        {
            Title = "关于",
            Content = panel,
            CloseButtonText = "关闭",
            XamlRoot = Content.XamlRoot
        };
        await dlg.ShowAsync();
    }
}
