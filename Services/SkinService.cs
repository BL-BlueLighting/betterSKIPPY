using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using SKIPPY.Models;

namespace SKIPPY.Services;

/// <summary>
/// skin manager
/// </summary>
public class SkinService
{
    private readonly Image _ballImage;
    private readonly ScaleTransform _ballMirror;

    // now skin, default skippy.
    public string CurrentSkin { get; private set; } = "初始";

    // kcorena need reverse
    public bool IsMirrored { get; private set; }

    // all skins list
    // TODO: add json configuration
    public static readonly SkinInfo[] Skins =
    [
        new("初始皮肤", "初始"),
        new("红温SKIPPY", "红温"),
        new("SCP", "SCP"),
        new("SCP-CN", "SCP-CN"),
        new("SCP-CAT", "SCP-CAT"),
        new("SCP-173", "SCP-173"),
        new("Kcorena", "kcorena"),
    ];

    public SkinService(Image ballImage, ScaleTransform ballMirror)
    {
        _ballImage = ballImage;
        _ballMirror = ballMirror;
    }

    /// <summary>
    /// load skin pointed.
    /// </summary>
    public void LoadSkin(string skinName)
    {
        try
        {
            string baseDir = AppContext.BaseDirectory;

            string[] candidates =
            [
                Path.Combine(baseDir, "皮肤", skinName + ".png"),
                Path.Combine(baseDir, "..", "皮肤", skinName + ".png"),  // dev 模式下往上翻一层
                Path.Combine(Directory.GetCurrentDirectory(), "皮肤", skinName + ".png"),  // 当前目录
            ];

            // bool loaded = false;  // debug only — uncomment to trace
            foreach (string path in candidates)
            {
                if (!File.Exists(path)) continue;

                // i like it
                _ballImage.Source = new Bitmap(path);
                // loaded = true;
                break;
            }

            // for debugging
            // if (!loaded) System.Diagnostics.Debug.WriteLine($"Skin not found: {skinName}");
        }
        catch
        {
            // nothing to do
        }

        CurrentSkin = skinName;
    }

    /// <summary>
    /// kcorena skin reverse
    /// </summary>
    public void UpdateMirror(double windowLeft, double windowWidth)
    {
        if (CurrentSkin != "kcorena")
        {
            if (IsMirrored)
            {
                _ballMirror.ScaleX = 1.0;
                IsMirrored = false;
            }
            return;
        }

        // get screen center
        double screenCenter = 960;
        try
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is
                Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop &&
                desktop.MainWindow?.Screens?.Primary is { } primary)
            {
                screenCenter = primary.WorkingArea.Width / 2.0;
            }
        }
        catch
        {
        }

        bool shouldMirror = windowLeft + 60.0 < screenCenter;

        if (shouldMirror != IsMirrored)
        {
            _ballMirror.ScaleX = shouldMirror ? -1 : 1;
            IsMirrored = shouldMirror;
        }
    }
}
