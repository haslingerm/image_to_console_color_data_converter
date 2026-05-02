using System.Collections.Frozen;
using System.Text;
using ColorMine.ColorSpaces; using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace ImageForConsoleConverter.Pages;

public partial class Index
{
    [Inject] private IJSRuntime JS { get; set; } = default!;

    private static readonly FrozenDictionary<ConsoleColor, Rgb> consoleColorToRgb = new Dictionary<ConsoleColor, Rgb>
    {
        { ConsoleColor.Black,       new Rgb { R = 0,   G = 0,   B = 0   } },
        { ConsoleColor.DarkBlue,    new Rgb { R = 0,   G = 0,   B = 128 } },
        { ConsoleColor.DarkGreen,   new Rgb { R = 0,   G = 128, B = 0   } },
        { ConsoleColor.DarkCyan,    new Rgb { R = 0,   G = 128, B = 128 } },
        { ConsoleColor.DarkRed,     new Rgb { R = 128, G = 0,   B = 0   } },
        { ConsoleColor.DarkMagenta, new Rgb { R = 128, G = 0,   B = 128 } },
        { ConsoleColor.DarkYellow,  new Rgb { R = 128, G = 128, B = 0   } },
        { ConsoleColor.Gray,        new Rgb { R = 128, G = 128, B = 128 } },
        { ConsoleColor.DarkGray,    new Rgb { R = 192, G = 192, B = 192 } },
        { ConsoleColor.Blue,        new Rgb { R = 0,   G = 0,   B = 255 } },
        { ConsoleColor.Green,       new Rgb { R = 0,   G = 255, B = 0   } },
        { ConsoleColor.Cyan,        new Rgb { R = 0,   G = 255, B = 255 } },
        { ConsoleColor.Red,         new Rgb { R = 255, G = 0,   B = 0   } },
        { ConsoleColor.Magenta,     new Rgb { R = 255, G = 0,   B = 255 } },
        { ConsoleColor.Yellow,      new Rgb { R = 255, G = 255, B = 0   } },
        { ConsoleColor.White,       new Rgb { R = 255, G = 255, B = 255 } },
    }.ToFrozenDictionary();

    /// <summary>The data URL of the original uploaded image (kept intact for every re-render at a new width).</summary>
    private string? _imageDataUrl;
    private int _width = 40;

    // ── JS interop DTO ────────────────────────────────────────────────────────
    // Mirrors the object returned by imageHelper.getResizedPixels().
    // 'data' is a flat [R, G, B, A, R, G, B, A, …] array of integers (0–255).
    private sealed class PixelData
    {
        public int Width  { get; set; }
        public int Height { get; set; }
        public int[] Data { get; set; } = [];
    }

    // ── Image processing ──────────────────────────────────────────────────────

    private static int[,] ConvertPixels(PixelData pixels)
    {
        var pixelColors = new int[pixels.Width, pixels.Height];

        for (var y = 0; y < pixels.Height; y++)
        {
            for (var x = 0; x < pixels.Width; x++)
            {
                var i = (y * pixels.Width + x) * 4;  // stride = width * 4 bytes (RGBA)
                var r = pixels.Data[i];
                var g = pixels.Data[i + 1];
                var b = pixels.Data[i + 2];
                // Alpha (pixels.Data[i + 3]) is intentionally ignored.

                var closestConsoleColor = Enum.GetValues(typeof(ConsoleColor))
                                              .Cast<ConsoleColor>()
                                              .MinBy(c => ColorDistance(
                                                         new Rgb { R = r, G = g, B = b },
                                                         ConsoleColorToRgb(c)));
                pixelColors[x, y] = (int) closestConsoleColor;
            }
        }

        return pixelColors;
    }

    private static double ColorDistance(IRgb color1, IRgb color2)
    {
        var redDiff   = color1.R - color2.R;
        var greenDiff = color1.G - color2.G;
        var blueDiff  = color1.B - color2.B;
        return Math.Sqrt(redDiff * redDiff + greenDiff * greenDiff + blueDiff * blueDiff);
    }

    private static Rgb ConsoleColorToRgb(ConsoleColor color) =>
        consoleColorToRgb.TryGetValue(color, out var rgb)
            ? rgb
            : new Rgb { R = 0, G = 0, B = 0 };

    // ── Event handlers ────────────────────────────────────────────────────────

    private async Task HandleImageUploaded(string dataUrl)
    {
        _imageDataUrl = dataUrl;
        await OutputImageCsv();
    }

    private async Task OutputImageCsv()
    {
        if (_imageDataUrl is null)
            return;

        try
        {
            // The browser Canvas API decodes the image and resizes it to the
            // requested width in one step, then hands back raw RGBA bytes.
            var pixels   = await JS.InvokeAsync<PixelData>("imageHelper.getResizedPixels", _imageDataUrl, _width);
            var converted = ConvertPixels(pixels);
            _csvRows = ConvertArrayToCsv(converted);
            StateHasChanged();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static IEnumerable<string> ConvertArrayToCsv(int[,] array)
    {
        var width  = array.GetLength(0);
        var height = array.GetLength(1);
        var rows   = new string[height];

        for (var i = 0; i < height; i++)
        {
            var csv = new StringBuilder();
            for (var j = 0; j < width; j++)
            {
                csv.Append($"{array[j, i]:00}");
                if (j < width - 1)
                    csv.Append(',');
            }
            rows[i] = csv.ToString();
        }

        return rows;
    }

    private async Task HandleWidthChanged(int newWidth)
    {
        _width = newWidth;
        await OutputImageCsv();
    }
}
