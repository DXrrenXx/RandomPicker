using System.IO;
using System.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Navigation;
using RandomPicker.Models;
using RandomPicker.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace RandomPicker.Views;

/// <summary>班级管理：导入 TXT、增删改班级与同学。</summary>
public sealed partial class ManagePage : Page
{
    private ClassInfo? _rightTappedClass;
    private string? _rightTappedStudent;

    public ManagePage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        RebuildClassList(selectFirst: true);
    }

    private ClassInfo? SelectedClass => ClassList.SelectedItem as ClassInfo;

    // ---------- 列表刷新 ----------

    private void RebuildClassList(bool selectFirst = false)
    {
        var prevId = SelectedClass?.Id;

        ClassList.ItemsSource = Store.Data.Classes.ToList();

        if (selectFirst)
        {
            ClassList.SelectedIndex = Store.Data.Classes.Count > 0 ? 0 : -1;
        }
        else
        {
            var idx = Store.Data.Classes.FindIndex(c => c.Id == prevId);
            ClassList.SelectedIndex = idx;
        }

        LoadStudents();
    }

    private void LoadStudents()
    {
        var cls = SelectedClass;
        StudentList.ItemsSource = cls?.Students.ToList() ?? new List<string>();
        StudentHeader.Text = cls == null ? "名单" : $"名单 · {cls.Name} · {cls.Students.Count} 人";
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        bool hasClass = SelectedClass != null;
        bool hasStudents = (SelectedClass?.Students.Count ?? 0) > 0;

        ExportBtn.IsEnabled = hasStudents;
        RenameClassBtn.IsEnabled = hasClass;
        DeleteClassBtn.IsEnabled = hasClass;
        AddStudentBtn.IsEnabled = hasClass;
        DeleteStudentBtn.IsEnabled = StudentList.SelectedItem != null;
    }

    private void ClassList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        LoadStudents();
    }

    // ---------- 对话框辅助 ----------

    private ContentDialog MakeDialog(string title, object content, string primary = "确定")
        => new()
        {
            Title = title,
            Content = content,
            PrimaryButtonText = primary,
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = this.XamlRoot
        };

    private void ShowFeedback(string title, string message, InfoBarSeverity severity)
    {
        Feedback.Title = title;
        Feedback.Message = message;
        Feedback.Severity = severity;
        Feedback.IsOpen = true;
    }

    // ---------- 班级操作 ----------

    private async void NewClass_Click(object sender, RoutedEventArgs e)
    {
        var box = new TextBox { PlaceholderText = "例如：三年二班" };
        var dlg = MakeDialog("新建班级", box, "创建");

        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

        var name = box.Text.Trim();
        if (name.Length == 0)
        {
            ShowFeedback("未创建", "班级名称不能为空。", InfoBarSeverity.Warning);
            return;
        }

        var cls = new ClassInfo { Name = name };
        Store.Data.Classes.Add(cls);
        Store.Save();

        RebuildClassList();
        var snap = (List<ClassInfo>)ClassList.ItemsSource;
        ClassList.SelectedIndex = snap.FindIndex(c => c.Id == cls.Id);
        Store.NotifyChanged();

        ShowFeedback("已创建", $"班级「{name}」创建成功。", InfoBarSeverity.Success);
    }

    private async void RenameClass_Click(object sender, RoutedEventArgs e)
    {
        var cls = _rightTappedClass ?? SelectedClass;
        _rightTappedClass = null;
        if (cls == null) return;

        var box = new TextBox { Text = cls.Name };
        var dlg = MakeDialog("重命名班级", box, "保存");

        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

        var name = box.Text.Trim();
        if (name.Length == 0)
        {
            ShowFeedback("未重命名", "班级名称不能为空。", InfoBarSeverity.Warning);
            return;
        }

        cls.Name = name;
        Store.Save();
        RebuildClassList();
        Store.NotifyChanged();
        ShowFeedback("已重命名", $"班级已重命名为「{name}」。", InfoBarSeverity.Success);
    }

    private async void DeleteClass_Click(object sender, RoutedEventArgs e)
    {
        var cls = _rightTappedClass ?? SelectedClass;
        _rightTappedClass = null;
        if (cls == null) return;

        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = $"确定要删除班级「{cls.Name}」吗？该班级的 {cls.Students.Count} 条名单将一并删除，且无法恢复。",
            TextWrapping = TextWrapping.Wrap
        });

        var dlg = new ContentDialog
        {
            Title = "删除班级",
            Content = panel,
            PrimaryButtonText = "删除",
            CloseButtonText = "取消",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot
        };

        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

        Store.Data.Classes.Remove(cls);
        Store.Save();
        RebuildClassList(selectFirst: true);
        Store.NotifyChanged();
        ShowFeedback("已删除", $"班级「{cls.Name}」已删除。", InfoBarSeverity.Success);
    }

    // ---------- 名单导入 / 导出 ----------

    private async void ImportTxt_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow!));
        picker.FileTypeFilter.Add(".txt");
        picker.ViewMode = PickerViewMode.List;

        var file = await picker.PickSingleFileAsync();
        if (file == null) return;

        List<string> names;
        try
        {
            names = Store.ParseNames(Store.ReadTextAuto(file.Path));
        }
        catch (Exception ex)
        {
            ShowFeedback("读取失败", ex.Message, InfoBarSeverity.Error);
            return;
        }

        if (names.Count == 0)
        {
            ShowFeedback("没有可导入的名字", "文件中没有找到有效内容（每行一个名字）。", InfoBarSeverity.Warning);
            return;
        }

        var current = SelectedClass;

        var nameBox = new TextBox
        {
            Header = "新班级名称",
            Text = file.DisplayName,
            MinWidth = 300
        };
        var combo = new ComboBox
        {
            PlaceholderText = "请选择导入方式",
            MinWidth = 300,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        combo.Items.Add("导入为新班级");
        if (current != null)
        {
            combo.Items.Add($"覆盖当前班级（{current.Name}）");
            combo.Items.Add($"追加到当前班级（{current.Name}）");
        }
        combo.SelectedIndex = 0;
        combo.SelectionChanged += (s, _) => nameBox.IsEnabled = combo.SelectedIndex == 0;

        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(new TextBlock
        {
            Text = $"共解析出 {names.Count} 个名字（已自动去空行、去重、剥离行首序号）。",
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(combo);
        panel.Children.Add(nameBox);

        var dlg = MakeDialog("导入 TXT 名单", panel, "导入");
        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;
        if (combo.SelectedIndex < 0) return;

        ClassInfo target;
        if (combo.SelectedIndex == 0)                     // 新建班级
        {
            var newName = nameBox.Text.Trim();
            if (newName.Length == 0)
            {
                ShowFeedback("未导入", "新班级名称不能为空。", InfoBarSeverity.Warning);
                return;
            }
            target = new ClassInfo { Name = newName, Students = names };
            Store.Data.Classes.Add(target);
        }
        else if (current != null && combo.SelectedIndex == 1)  // 覆盖
        {
            current.Students = names;
            target = current;
        }
        else if (current != null)                          // 追加
        {
            var added = 0;
            foreach (var n in names)
            {
                if (!current.Students.Contains(n))
                {
                    current.Students.Add(n);
                    added++;
                }
            }
            target = current;
        }
        else
        {
            return;
        }

        Store.Save();
        RebuildClassList();
        var snap = (List<ClassInfo>)ClassList.ItemsSource;
        ClassList.SelectedIndex = snap.FindIndex(c => c.Id == target.Id);
        Store.NotifyChanged();

        ShowFeedback("导入成功", $"已导入 {names.Count} 个名字到「{target.Name}」。", InfoBarSeverity.Success);
    }

    private async void ExportTxt_Click(object sender, RoutedEventArgs e)
    {
        var cls = SelectedClass;
        if (cls == null || cls.Students.Count == 0) return;

        var picker = new FileSavePicker();
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow!));
        picker.SuggestedFileName = cls.Name;
        picker.FileTypeChoices.Add("文本文件", new List<string> { ".txt" });

        var file = await picker.PickSaveFileAsync();
        if (file == null) return;

        try
        {
            File.WriteAllText(
                file.Path,
                string.Join(Environment.NewLine, cls.Students) + Environment.NewLine,
                new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            ShowFeedback("导出成功", $"已导出「{cls.Name}」的 {cls.Students.Count} 个名字。", InfoBarSeverity.Success);
        }
        catch (Exception ex)
        {
            ShowFeedback("导出失败", ex.Message, InfoBarSeverity.Error);
        }
    }

    // ---------- 同学操作 ----------

    private async void AddStudents_Click(object sender, RoutedEventArgs e)
    {
        var cls = SelectedClass;
        if (cls == null) return;

        var box = new TextBox
        {
            AcceptsReturn = true,
            Height = 160,
            TextWrapping = TextWrapping.Wrap,
            PlaceholderText = "每行一个名字，可一次粘贴多个"
        };
        var dlg = MakeDialog($"添加同学到「{cls.Name}」", box, "添加");

        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

        var names = Store.ParseNames(box.Text);
        if (names.Count == 0)
        {
            ShowFeedback("未添加", "没有输入有效名字。", InfoBarSeverity.Warning);
            return;
        }

        var added = 0;
        foreach (var n in names)
        {
            if (!cls.Students.Contains(n))
            {
                cls.Students.Add(n);
                added++;
            }
        }

        Store.Save();
        LoadStudents();
        ShowFeedback("已添加", $"已添加 {added} 人，{names.Count - added} 个重复名字已跳过。", InfoBarSeverity.Success);
    }

    private void DeleteStudentBtn_Click(object sender, RoutedEventArgs e)
    {
        var cls = SelectedClass;
        if (cls == null || StudentList.SelectedItem is not string name) return;

        cls.Students.Remove(name);
        Store.Save();
        LoadStudents();
        ShowFeedback("已删除", $"已将「{name}」移出「{cls.Name}」。", InfoBarSeverity.Success);
    }

    private async void RenameStudent_Click(object sender, RoutedEventArgs e)
    {
        var cls = SelectedClass;
        var old = _rightTappedStudent;
        _rightTappedStudent = null;
        if (cls == null || old == null) return;

        var box = new TextBox { Text = old };
        var dlg = MakeDialog("重命名同学", box, "保存");

        if (await dlg.ShowAsync() != ContentDialogResult.Primary) return;

        var name = box.Text.Trim();
        if (name.Length == 0)
        {
            ShowFeedback("未重命名", "名字不能为空。", InfoBarSeverity.Warning);
            return;
        }

        var idx = cls.Students.IndexOf(old);
        if (idx >= 0) cls.Students[idx] = name;
        Store.Save();
        LoadStudents();
        ShowFeedback("已重命名", $"「{old}」已改为「{name}」。", InfoBarSeverity.Success);
    }

    private void DeleteStudentMenu_Click(object sender, RoutedEventArgs e)
    {
        var cls = SelectedClass;
        var name = _rightTappedStudent;
        _rightTappedStudent = null;
        if (cls == null || name == null) return;

        cls.Students.Remove(name);
        Store.Save();
        LoadStudents();
        ShowFeedback("已删除", $"已将「{name}」移出「{cls.Name}」。", InfoBarSeverity.Success);
    }

    // ---------- 右键菜单 ----------

    private void ClassItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        _rightTappedClass = ((FrameworkElement)sender).DataContext as ClassInfo;
        ClassFlyout.ShowAt((FrameworkElement)sender);
    }

    private void StudentItem_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        _rightTappedStudent = ((FrameworkElement)sender).DataContext as string;
        StudentFlyout.ShowAt((FrameworkElement)sender);
    }
}
