<div align="center">
    <img src="./皮肤/初始.png" alt="Hello! I'm SKIPPY, Your personal SCP writing helper friend." style="width: 100px; margin-bottom: -27px;">
    <h2>SKIPPY (better)</h2>
    <i>Better SKIPPY, Better chapter.</i>
</div>

<hr/>

> [!Warning]
>
> 本仓库源码来自反编译过后的 [SKIPPY](https://github.com/Roger7F/skippy/) 项目。
>
> 原作者：Roger_F（屑懒）

---

## 项目结构

```
SkippyBetter/
├── SKIPPY.csproj              # .NET 8 Avalonia 项目文件
├── Program.cs                 # 应用程序入口
├── App.axaml / App.axaml.cs   # Avalonia 应用定义
├── MainWindow.axaml           # 主窗口布局（宠物 + 气泡 + 倒计时画布）
├── MainWindow.cs              # 主窗口（桌面宠物核心逻辑）
├── Models/
│   ├── SkinInfo.cs            # 皮肤数据模型
│   ├── BookmarkInfo.cs        # 收藏数据模型
│   └── PresetInfo.cs          # 预设数据模型
├── Services/
│   ├── SkinService.cs         # 皮肤加载与切换
│   ├── BubbleService.cs       # 气泡对话管理（窗体内渲染，不抢焦点）
│   ├── CpuMonitorService.cs   # CPU 监控悬浮窗（跨平台）
│   ├── BookmarkService.cs     # 收藏夹持久化（拖动收藏）
│   ├── PresetService.cs       # 代码预设（内置 + 自定义）
│   ├── CountdownService.cs    # 倒计时标签
│   ├── AiRoastService.cs      # AI 吐槽 API 调用
│   ├── ApiConfig.g.cs         # 构建时从 Api.zipkey 生成（勿手改）
│   ├── SettingsService.cs     # 设置持久化
│   └── ScreenMonitorService.cs# 屏幕监控（"机密分级" 检测，默认关闭）
├── Helpers/
│   └── WindowHelper.cs        # 窗口定位/屏幕钳制工具类
├── Dialogs/
│   ├── CharCountDialog.cs     # 字数统计对话框
│   ├── AboutDialog.cs         # 关于对话框
│   ├── AiRoastDialog.cs       # AI 吐槽对话框
│   ├── PresetDialog.cs        # 预设管理对话框
│   ├── CountdownDialog.cs     # 倒计时设置对话框
│   └── SettingsDialog.cs      # 设置对话框（屏幕监控开关）
├── Menu/
│   └── MenuBuilder.cs         # 右键菜单构建（预设/收藏/皮肤等）
├── 皮肤/                       # 皮肤 PNG 资源
├── buildit.sh / buildit.bat   # 一键编译脚本
├── crossp_linux_win.sh        # Linux → Windows 交叉编译
├── install-deps.sh / .bat     # 安装 tesseract 等依赖
├── roast-api.php              # AI 吐槽 API 端点（DeepSeek）
└── README.md
```

## 编译

### Windows

```bash
dotnet build
dotnet run
```

单文件：


```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## 说明
原项目使用的是 WPF，本项目使用 Avalonia 用于实现跨平台。

同时本项目仅供学习使用，皮肤目录内是原版的皮肤。

我用的是 Linux，因此没有 vs，大部分布局都是手写/AI写的。

本项目遵循 <a href="http://creativecommons.org/licenses/by-sa/3.0/deed.zh">CC</a> 协议。

## 功能树

- [x] 将整个程序迁移至 Avalonia UI 框架
- [x] 添加页面收藏夹
- [x] 添加 AI 吐槽
- [ ] 打开程序后提供 down 迭代页功能 [暂不实现]
- [ ] 检测到屏幕上出现 "机密分级" 自动红温 [实现了一半，还有问题]
- [x] 倒计时功能
- [x] 更好的 Wikidot 代码复制
