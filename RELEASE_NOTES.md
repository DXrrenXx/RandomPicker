# 随机点名 v1.0.0

一个适用于课堂的 Windows 桌面随机点名软件。界面完全遵循 WinUI 3 / Fluent Design：Mica 背景材质、NavigationView 侧边导航、原生弹窗与操作反馈。

这是首个正式版本。

## 功能亮点

### 点名台

- 居中大字姓名窗口，点击「开始随机」后以 0.1 秒/人的速度滚动名字
- 按钮随时变为「停止」，按下即定格抽中结果（定格带轻微回弹动画）
- 空格键 = 开始 / 停止，配合翻页笔也能用

### 班级切换与管理

- 左侧导航直接列出所有班级，点哪个班就在哪个班点名
- 导入 TXT 名单，自动剥行首序号、去空行、去重
- 自动识别 UTF-8 / UTF-16 / GB18030「ANSI」编码，国内记事本文件直接可用
- 导入方式三选一：新建班级 / 覆盖当前班级 / 追加到当前班级
- 新建、重命名、删除班级（删除需二次确认），导出名单为 TXT
- 添加、删除、重命名单个同学，支持右键菜单与多行批量粘贴

### 数据安全

- 所有班级保存在 exe 同目录的 `data.json`，关机不丢，可随时备份迁移
- 原子写入保护：先写临时文件再替换，断电 / 崩溃也不会损坏数据文件
- 数据文件损坏时自动回退为全新数据，应用不会崩溃

## 下载

| 文件 | 说明 |
|---|---|
| `RandomPicker-EXE.zip` | 便携版，解压即用，不需要安装 |
| `RandomPicker-Setup-1.0.0.exe` | 安装版（Inno Setup，按当前用户安装，无需管理员权限） |

两个包均为自包含发布，**不需要安装 .NET 运行时**。

## 系统要求

- Windows 10 1809（内部版本 17763）及以上，x64
- 无其他依赖

## 安装与首次运行

1. 便携版：解压 `RandomPicker-EXE.zip`，双击 `RandomPicker.exe` 即可运行
2. 安装版：运行 `RandomPicker-Setup-1.0.0.exe`，按向导完成安装（可选创建桌面快捷方式）
3. 首次运行如出现 SmartScreen 蓝色提示：点「更多信息」→「仍要运行」

## TXT 名单格式

一行一个名字，以下写法都能正确识别（序号会被自动剥掉）：

```
张三
02 李四
12、王五
101 赵六
```

## 快捷键

| 按键 | 作用 |
|---|---|
| 空格 | 开始 / 停止随机 |

## 数据位置

所有班级数据保存在程序同目录的 `data.json`。卸载应用不会删除该文件，名单不会丢失，可随时备份或迁移到其他电脑。

## 从源码构建

装有 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 的 Windows 电脑上，在本目录执行：

```powershell
dotnet publish RandomPicker/RandomPicker.csproj -c Release -r win-x64 -p:Platform=x64 -p:SelfContained=true -p:WindowsAppSDKSelfContained=true -o publish
```

也可以直接把仓库推到 GitHub，Actions 会自动构建并产出上述两个安装包。

---

Made by XU
