namespace RandomPicker.Models;

/// <summary>一个班级及其学生名单。</summary>
public class ClassInfo
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = "";

    public List<string> Students { get; set; } = new();
}

/// <summary>应用的全部本地数据（保存为 data.json）。</summary>
public class AppData
{
    public bool FirstRunDone { get; set; } = false;

    public List<ClassInfo> Classes { get; set; } = new();
}
