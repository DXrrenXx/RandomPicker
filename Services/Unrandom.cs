using System.IO;
using System.Text;

namespace RandomPicker.Services;

/// <summary>隐藏指定功能：读取 unrandom.txt 中的"字母=名字"映射。界面无任何可见改动。</summary>
public static class Unrandom
{
    public static string FilePath { get; } =
        Path.Combine(AppContext.BaseDirectory, "unrandom.txt");

    /// <summary>首次运行生成带注释说明的模板文件；已有文件绝不覆盖。</summary>
    public static void EnsureCreated()
    {
        if (File.Exists(FilePath)) return;

        var template = new StringBuilder();
        template.AppendLine("; ============================================");
        template.AppendLine("; 随机点名 · 隐藏指定功能");
        template.AppendLine("; --------------------------------------------");
        template.AppendLine("; 在点名滚动时按下对应的字母键，停止时就会显示");
        template.AppendLine("; 该字母对应的名字（仅本次有效，之后恢复随机）。");
        template.AppendLine(";");
        template.AppendLine("; 格式：每行一个映射，字母=名字（大小写不限）");
        template.AppendLine("; 示例（去掉行首的 ; 后生效）：");
        template.AppendLine("; A=张三");
        template.AppendLine("; B=李四");
        template.AppendLine(";");
        template.AppendLine("; 说明：");
        template.AppendLine("; · 名字必须在当前班级名单中存在，否则无效");
        template.AppendLine("; · 以 ; 开头的行是注释，会被忽略");
        template.AppendLine("; · 保存后立即生效，无需重启程序");
        template.AppendLine("; ============================================");

        try
        {
            File.WriteAllText(FilePath, template.ToString(), new UTF8Encoding(true));
        }
        catch
        {
            // 目录不可写等场景静默跳过，不影响程序运行
        }
    }

    /// <summary>
    /// 解析映射文件：跳过注释与空行，非法行静默忽略，重复字母以后者为准。
    /// 每次按键时现读现查，编辑后无需重启。
    /// </summary>
    public static Dictionary<char, string> Parse()
    {
        var map = new Dictionary<char, string>();

        string text;
        try
        {
            text = Store.ReadTextAuto(FilePath);
        }
        catch
        {
            return map; // 文件不存在或读取失败时视为无映射
        }

        foreach (var raw in text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(';')) continue;

            var idx = line.IndexOf('=');
            if (idx <= 0 || idx == line.Length - 1) continue;

            var key = line[..idx].Trim();
            var name = line[(idx + 1)..].Trim();
            if (key.Length != 1 || name.Length == 0) continue;

            var ch = char.ToUpperInvariant(key[0]);
            if (ch < 'A' || ch > 'Z') continue;

            map[ch] = name;
        }
        return map;
    }
}
