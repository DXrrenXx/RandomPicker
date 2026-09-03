using System.IO;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using RandomPicker.Models;

namespace RandomPicker.Services;

/// <summary>本地数据存取：JSON 持久化 + TXT 名单解析。</summary>
public static class Store
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>数据文件位于 exe 同目录，便于备份。</summary>
    public static string FilePath { get; } =
        Path.Combine(AppContext.BaseDirectory, "data.json");

    public static AppData Data { get; private set; } = new();

    /// <summary>本次启动是否为首次运行（data.json 不存在）。</summary>
    public static bool IsFirstRun { get; private set; }

    /// <summary>班级列表发生变化（增删、重命名）时触发，用于刷新侧边导航。</summary>
    public static event Action? ClassesChanged;

    public static void Load()
    {
        if (File.Exists(FilePath))
        {
            try
            {
                Data = JsonSerializer.Deserialize<AppData>(File.ReadAllText(FilePath, Encoding.UTF8), JsonOptions)
                       ?? new AppData();
            }
            catch
            {
                // 数据文件损坏时不让应用崩溃，回退为全新数据
                Data = new AppData();
            }
        }
        else
        {
            IsFirstRun = true;
            Data = new AppData();
            Data.Classes.Add(new ClassInfo
            {
                Name = "示例班级",
                Students = { "张三", "李四", "王五", "赵六", "孙七", "周八", "吴九", "郑十" }
            });
            Save();
        }
    }

    public static void Save()
    {
        File.WriteAllText(FilePath, JsonSerializer.Serialize(Data, JsonOptions), new UTF8Encoding(true));
    }

    public static void NotifyChanged() => ClassesChanged?.Invoke();

    public static ClassInfo? Find(Guid id) => Data.Classes.FirstOrDefault(c => c.Id == id);

    /// <summary>
    /// 解析名单文本：按行拆分、去除行首序号（如 "01 张三"、"12、李四"）、去空行、去重。
    /// </summary>
    public static List<string> ParseNames(string text)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var raw in text.Split(new[] { "\r\n", "\r", "\n" }))
        {
            var name = raw.Trim();
            if (name.Length == 0) continue;

            // 剥离行首序号："01、" "12." "3、" "05 " 等
            name = Regex.Replace(name, @"^\d{1,4}\s*[.、．,，:：]\s*", "");
            name = Regex.Replace(name, @"^\d{1,4}\s+", "");
            name = name.Trim();

            if (name.Length == 0) continue;
            if (seen.Add(name)) result.Add(name);
        }
        return result;
    }

    /// <summary>
    /// 自动识别编码读取文本文件：UTF-8（含 BOM）/ UTF-16 / GB18030（记事本"ANSI"）。
    /// </summary>
    public static string ReadTextAuto(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length == 0) return "";

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return new UTF8Encoding(false).GetString(bytes, 3, bytes.Length - 3); // UTF-8 BOM

        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);        // UTF-16 LE

        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2); // UTF-16 BE

        try
        {
            return new UTF8Encoding(false, true).GetString(bytes); // 无 BOM，严格按 UTF-8 尝试
        }
        catch (DecoderFallbackException)
        {
            return Encoding.GetEncoding("GB18030").GetString(bytes); // 常见中文 ANSI
        }
    }
}
