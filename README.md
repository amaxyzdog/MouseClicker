# 鼠标连点器（MouseClicker）

一个基于 **Avalonia + FluentAvalonia** 构建的现代化 Windows 鼠标连点器。支持自定义点击间隔、随机延迟、坐标锁定、全局热键、托盘驻留。

## 功能特性

- **浮窗工具栏**：单行显示点击模式、点击间隔、热键，一键启停，可拖动、可置顶、可锁定位置。
- **连点控制**：
  - 点击间隔 1~1000ms 自由滑动调节（支持随机延迟）。
  - 点击模式：左键 / 右键 / 中键。
  - 点击方式：单击 / 双击 / 按下并释放。
- **坐标锁定**：开启后每次点击前将鼠标移动到目标坐标；设置页实时预览鼠标坐标，按 **Ctrl+S** 或"取当前坐标"确定目标。
- **全局热键**：默认 **F2**，可在任意界面后台启动/停止连点；支持热键录制，一键切换；自带 150ms 防抖，长按不会反复启停。
- **托盘驻留**：关闭窗口最小化到托盘，双击图标恢复，右键菜单可快速启停/退出。
- **单实例**：同一时间只允许运行一个程序，重复启动以 FluentAvalonia 风格弹窗提示。
- **配置自动保存**：所有设置实时保存到 `%AppData%\MouseClicker\config.json`，重启后自动恢复。

## 技术栈

| 组件 | 版本 | 说明 |
| --- | --- | --- |
| .NET | 8.0 | 运行时 |
| Avalonia | 11.3 | 跨平台 UI 框架 |
| FluentAvalonia | 2.4 | Fluent Design 控件库 |
| SkiaSharp | - | 渲染引擎（Avalonia 后端） |
| HarfBuzzSharp | - | 字体整形（Avalonia 后端） |

## 项目结构

```
VD项目/
├── Program.cs                    # 程序入口（单实例互斥体）
├── App.axaml / App.axaml.cs      # 应用与 FluentAvalonia 主题
├── MainWindow.axaml(.cs)         # 主浮窗（工具栏 + 托盘）
├── SettingsWindow.axaml(.cs)     # 设置窗口
├── AboutWindow.axaml(.cs)        # 关于页面
├── AlertWindow.axaml(.cs)        # 单实例提示窗口
├── ViewModels/                   # MVVM：Main / Settings / RelayCommand / ViewModelBase
├── Services/
│   ├── MouseService.cs           # SendInput 模拟鼠标点击
│   └── KeyboardHookService.cs    # 全局键盘钩子（热键）
├── MouseClickerSetup.iss         # 安装程序脚本（Inno Setup）
├── languages/                    # 安装程序中文语言包
├── dist/                         # 安装程序输出目录
└── icon.jpg / icon.ico           # 应用图标
```

## 编译与打包

环境要求：.NET 8 SDK（含 Windows 桌面运行时）。

```bash
# 编译 Debug
dotnet build MouseClicker.csproj -c Debug

# 编译 Release
dotnet build MouseClicker.csproj -c Release

# 打包单文件（框架依赖，输出到 bin\Release\net8.0\win-x64\publish\）
dotnet publish MouseClicker.csproj -c Release -r win-x64 --self-contained false

# 生成安装程序（需先安装 Inno Setup 6，输出到 dist\）
& "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe" MouseClickerSetup.iss
```

发布目录包含 `MouseClicker.exe` 及运行所需原生库；安装程序为 `dist\MouseClickerSetup-1.0.0.exe`（中文向导、桌面/开始菜单快捷方式、卸载程序，安装前自动检测 .NET 8 运行时）。

## 使用说明

1. 启动程序，主浮窗显示当前点击模式、间隔与热键。
2. 点击 **▶** 开始连点，**⏸** 停止；或按全局热键（默认 F2）后台启停。
3. 点击 **⚙** 打开设置：
   - **点击间隔 / 随机延迟**：滑块自由调节，输入框可精确输入。
   - **坐标锁定**：开启后移动鼠标到目标位置，按 Ctrl+S 确定。
   - **窗口置顶 / 锁定窗口**：控制浮窗行为。
   - **启动热键**：点击按钮后按下新按键完成录制。
4. 点击 **✕** 最小化到托盘；双击托盘图标恢复。
5. 设置页底部可进入 **关于**。

## 配置文件

路径：`%AppData%\MouseClicker\config.json`（JSON 格式，可读可改）。

保存内容：点击间隔、随机延迟、点击模式/方式、坐标锁定与目标坐标、窗口置顶、锁定窗口、热键。文件损坏或缺失时自动使用默认值。

## 第三方库与许可

| 库 | 许可 |
| --- | --- |
| Avalonia | MIT |
| FluentAvalonia | MIT |
| SkiaSharp | MIT |
| HarfBuzzSharp | MIT |

## 作者

- 作者：**J4s**
- 抖音：[J4s 的主页](https://www.douyin.com/user/MS4wLjABAAAABDWaMze6oSVRdkv-3eq7K8B7iwh3ygR040JWsv9OJys)
