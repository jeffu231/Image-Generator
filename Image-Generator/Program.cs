using System.Diagnostics;
using System.CommandLine;
using System.Drawing;
using System.Drawing.Imaging;


namespace Image_Generator;

class Program
{
    static async Task<int> Main(string[] args)
    {
        Option<string> textOption = new("--text", "-t")
        {
            Description = "Text to render",
            Required = true
        };
        
        Option<string> fontOption = new("--font", "-f")
        {
            Description = "Font name to use",
            DefaultValueFactory = parseResult => "Tahoma"
        };

        var bgOption = new Option<string>("--background", "-bc")
        {
            Description = "Background color (name, #RRGGBB, #AARRGGBB, or rgb(r,g,b))",
            DefaultValueFactory = parseResult => Color.White.ToString()
        };


        var fgOption = new Option<string>("--textColor", "-tc")
        {
            Description = "Text color (name, #RRGGBB, #AARRGGBB, or rgb(r,g,b))",
            DefaultValueFactory = parseResult => Color.Red.ToString()
        };

        var radioOption = new Option<string>("--radio", "-r")
        {
            Description = "Type of radio to generate for",
            DefaultValueFactory = parseResult => "XPR5580"
        };
        
        var outputDirOption = new Option<string>("--dir", "-d")
        {
            Description = "Output directory to save image to. If it does not exist, it will be created.",
            DefaultValueFactory = parseResult => System.Environment.CurrentDirectory
        };

        var rootCommand = new RootCommand("Generate an image for a Motorola radio with the given text.");
        rootCommand.Options.Add(textOption);
        rootCommand.Options.Add(fontOption);
        rootCommand.Options.Add(bgOption);
        rootCommand.Options.Add(fgOption);
        rootCommand.Options.Add(radioOption);
        rootCommand.Options.Add(outputDirOption);
        
        rootCommand.SetAction(parseResult =>
        {
            string text = parseResult.GetValue(textOption)!;
            string fontName = parseResult.GetValue(fontOption)!;
            string bgColor = parseResult.GetValue(bgOption)!;
            string fgColor = parseResult.GetValue(fgOption)!;
            string radioType = parseResult.GetValue(radioOption)!;
            string outputDir = parseResult.GetValue(outputDirOption)!;
            if (Directory.Exists(outputDir) == false)
            {
                Directory.CreateDirectory(outputDir);
            }
            DrawImage(text, fontName, bgColor, fgColor, radioType, outputDir);
            return 0;
        });

        ParseResult parseResult = rootCommand.Parse(args);
        return await parseResult.InvokeAsync();
    }

    private static void DrawImage(string text, string fontName, string bgColor, string textColor, string radioType, string outputDir)
    {
        var (width, height) = GetImageSize(radioType);
        Bitmap bitmap = new Bitmap(width, height);
        Graphics graphics = Graphics.FromImage(bitmap);
        graphics.Clear(ParseColor(bgColor)??Color.White);;
        Brush textBrush = new SolidBrush(ParseColor(textColor)??Color.Red);;
        var size = FindLargestFittingFontSize(text, "Tahoma", FontStyle.Bold, width);
        var font = new Font(fontName, size, FontStyle.Bold);
        SizeF textSize = graphics.MeasureString(text, font);

        var drawY = (height - textSize.Height) / 2;
        var drawX = (width - textSize.Width) / 2;
        Debug.WriteLine($"Height: {textSize.Height}, Width: {textSize.Width}");
        Debug.WriteLine($"Draw Height: {drawY}, Draw Width: {drawX}");
        graphics.DrawString(text, font, textBrush, new PointF(drawX, drawY)); // X, Y coordinates
        var outputPath = Path.Combine(outputDir, $"{text.ToLower()}.bmp");

        Bitmap indexedBitmap = bitmap.Clone(
            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
            PixelFormat.Format8bppIndexed); // Or other indexed formats like Format4bppIndexed, Format1bppIndexed
        indexedBitmap.Save(outputPath, ImageFormat.Bmp);
    }

    private static (int, int) GetImageSize(string radioType)
    {
        switch (radioType)
        {
            case "XPR5580":
                return (160, 72);
            default:
                Console.WriteLine($"Radio type {radioType} not supported. Using default size of 160x72");
                return (160, 72);
        }
    }
    private static float FindLargestFittingFontSize(string text, string fontName, FontStyle fontStyle,
        int maxWidthPixels)
    {
        float minFontSize = 1; // Minimum possible font size
        float maxFontSize = 100; // Starting maximum font size (adjust as needed)
        float bestFitFontSize = minFontSize;

        // Use a Graphics object to measure text
        using (Bitmap bmp = new Bitmap(1, 1)) // Create a dummy bitmap for Graphics object
        using (Graphics g = Graphics.FromImage(bmp))
        {
            while (minFontSize <= maxFontSize)
            {
                float currentFontSize = (minFontSize + maxFontSize) / 2;
                using (Font font = new Font(fontName, currentFontSize, fontStyle))
                {
                    SizeF textSize = g.MeasureString(text, font);

                    if (textSize.Width <= maxWidthPixels)
                    {
                        bestFitFontSize = currentFontSize;
                        minFontSize = currentFontSize + 0.1f; // Try a larger size
                    }
                    else
                    {
                        maxFontSize = currentFontSize - 0.1f; // Text is too wide, try a smaller size
                    }
                }
            }
        }

        return bestFitFontSize;
    }

    private static Color? ParseColor(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        input = input.Trim();

        // Hex formats: #RRGGBB, #RGB
        if (input.StartsWith("#", StringComparison.Ordinal))
        {
            try
            {
                // ColorTranslator supports #RRGGBB and #RGB
                return ColorTranslator.FromHtml(input);
            }
            catch
            {
                return null;
            }
        }

        // Named color
        var c = Color.FromName(input);
        if (c.A != 0 || string.Equals(input, "Transparent", StringComparison.OrdinalIgnoreCase))
            return c;

        return null;
    }
}