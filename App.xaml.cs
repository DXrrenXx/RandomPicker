using Microsoft.UI.Xaml;
using RandomPicker.Services;
using System.Text;

namespace RandomPicker;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }

    public App()
    {
        this.InitializeComponent();
        // 支持 GB18030 / GBK 等国内常见的"ANSI"编码 TXT
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        Store.Load();
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}
