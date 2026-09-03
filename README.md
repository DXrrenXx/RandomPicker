# 随机点名（WinUI 3）

一个适用于课堂的 Windows 桌面随机点名软件，界面完全遵循 **WinUI 3 / Fluent Design**：
Mica 背景材质、NavigationView 侧边导航、原生 ContentDialog / InfoBar / TeachingTip / MenuFlyout 弹窗。

## 功能

- **点名台**：居中大字姓名窗口，点击「开始随机」后以 0.1 秒/人的速度滚动名字，按钮同时变为「停止」，随时按下即定格抽中结果（定格带轻微回弹动画）
- **班级切换**：左侧 NavigationView 直接列出所有班级，点哪个班就在哪个班点名
- **班级管理**：
  - 导入 TXT 名单（自动剥行首序号、去空行、去重；自动识别 UTF-8 / UTF-16 / GB18030「ANSI」编码）
  - 导入方式三选一：新建班级 / 覆盖当前班级 / 追加到当前班级
  - 新建、重命名、删除班级（删除需二次确认），导出名单为 TXT
  - 添加、删除、重命名单个同学（支持右键菜单、多行批量粘贴）
- **数据持久化**：所有班级保存在 exe 同目录的 `data.json`，关机不丢，可随时备份迁移
- **快捷键**：空格键 = 开始 / 停止（配合翻页笔也能用）

## 如何获得 EXE（GitHub Actions 云端构建，本机无需任何环境）

1. 注册 / 登录 [GitHub](https://github.com)，点右上角 **+** → **New repository** 新建一个仓库（选 Public，免费且能用 Actions）
2. 把本项目**整个文件夹里的所有内容**（含 `.github` 隐藏文件夹！）上传到仓库：
   - 网页方式：仓库页点 **uploading an existing file**，把 `RandomPicker` 文件夹里的全部内容直接拖进去（支持拖整个文件夹），点 **Commit changes**
   - 或命令行方式：
     ```bash
     git init
     git add .
     git commit -m "随机点名 v1.0"
     git remote add origin https://github.com/你的用户名/仓库名.git
     git branch -M main
     git push -u origin main
     ```
3. 打开仓库的 **Actions** 标签页，会自动出现「构建 Windows EXE」工作流，等待约 5 分钟变绿 ✔
4. 点进这次运行，页面底部 **Artifacts** 区域下载 **RandomPicker-EXE**
5. 解压 zip，双击 **RandomPicker.exe** 即可运行（自包含发布，不需要安装 .NET 运行时）
   - 首次运行如出现 SmartScreen 蓝色提示：点「更多信息」→「仍要运行」

> ⚠️ 上传时如果漏掉 `.github/workflows/build.yml`，Actions 就不会自动构建。

## 本机编译（备选方案）

装有 [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) 的 Windows 电脑上，在本目录执行：

```powershell
dotnet publish RandomPicker/RandomPicker.csproj -c Release -r win-x64 -p:Platform=x64 -p:SelfContained=true -p:WindowsAppSDKSelfContained=true -o publish
```

生成的 exe 在 `publish\` 文件夹里。也可以用 Visual Studio 2022（含"WinUI 应用程序开发"工作负载）直接打开运行。

## TXT 名单格式

一行一个名字，以下写法都能正确识别（序号会被自动剥掉）：

```
张三
02 李四
12、王五
101 赵六
```

## 技术栈

.NET 8 · Windows App SDK 1.5 · WinUI 3（unpackaged）· C#
