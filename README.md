# SKIPPY Better

> [!Warning]
>
> 本仓库源码来自反编译过后的 [SKIPPY](https://github.com/Roger7F/skippy/) 项目。
>
> 原作者：Roger_F（屑懒）

---

## 项目结构

```
SkippyBetter/
├── SKIPPY.csproj              # .NET 8 WPF 项目文件
├── App.cs                     # 应用程序入口
├── MainWindow.cs              # 主窗口（桌面宠物核心逻辑）
├── MainWindow.xaml            # 主窗口布局
├── Models/
│   └── SkinInfo.cs            # 皮肤数据模型
├── Services/
│   ├── SkinService.cs         # 皮肤加载与切换
│   ├── BubbleService.cs       # 气泡对话管理
│   └── CpuMonitorService.cs   # CPU 监控悬浮窗
├── Helpers/
│   ├── NativeMethods.cs       # Win32 P/Invoke 声明
│   └── WindowHelper.cs        # 窗口位置/DPI 工具类
├── Dialogs/
│   ├── CharCountDialog.cs     # 字数统计对话框
│   └── AboutDialog.cs         # 关于对话框
├── Menu/
│   └── MenuBuilder.cs         # 右键菜单构建
├── 皮肤/                       # 皮肤 PNG 资源
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

## 功能树

- [x] 将整个程序迁移至 Avalonia UI 框架
- [x] 添加页面收藏夹
- [x] 添加 AI 吐槽
- [x] 打开程序后提供 down 迭代页功能 [暂不实现]
- [ ] 检测到屏幕上出现 "机密分级" 自动红温
