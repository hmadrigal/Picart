using System.CommandLine;
using SixLabors.ImageSharp.Processing;

internal class Program
{
    
    private static async Task Main(string[] args)
    {
        var inputOption = new Option<FileInfo?>("--input", "Input image file. Default to standard input.");
        var outputOption = new Option<FileInfo?>("--output", getDefaultValue: () => default, description: "Output image file. Default to standard output.");
        var scaleOption = new Option<double>("--scale", getDefaultValue: () => 1.0, description: "Percentage relating image size to terminal dimensions.");
        var rootCommand = new RootCommand(description: "Convers a image to ASCII.")
        { inputOption, outputOption, scaleOption };

        rootCommand.SetHandler((FileInfo? input, FileInfo? output, double scale) =>
        {
            var inputStream = input?.OpenRead() ?? Console.OpenStandardInput();
            using var image = Image.Load<Rgba32>(inputStream);
            double target = GetTargetScale(scale, image);

            // Resize the given image in place and return it for chaining.
            // 'x' signifies the current image processing context.
            //image.Mutate(x => x.Resize(image.Width / 2, image.Height / 2));
            image.Mutate(i => i.Resize(Convert.ToInt32(image.Width * target), Convert.ToInt32(image.Height * target)));

            image.ProcessPixelRows(accessor =>
            {

                // Convert the image to grayscale
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                    // pixelRow.Length has the same value as accessor.Width,
                    // but using pixelRow.Length allows the JIT to opt  imize away bounds checks:
                    for (int x = 0; x < pixelRow.Length; x++)
                    {
                        // Get a reference to the pixel at position x
                        ref Rgba32 pixel = ref pixelRow[x];


                        byte luminance = (byte)(pixel.R * 0.3 + pixel.G * 0.59 + pixel.B * 0.11);
                        pixel.R = luminance;
                        pixel.G = luminance;
                        pixel.B = luminance;
                        pixel.A = 255;

                    }
                }


                // Create a text file to store the ASCII art
                using var outputStream = output?.OpenWrite() ?? Console.OpenStandardOutput();
                using StreamWriter writer = new StreamWriter(outputStream);
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                        // pixelRow.Length has the same value as accessor.Width,
                        // but using pixelRow.Length allows the JIT to optimize away bounds checks:
                        for (int x = 0; x < pixelRow.Length; x++)
                        {
                            // Get a reference to the pixel at position x
                            ref Rgba32 pixel = ref pixelRow[x];

                            byte luminance = pixel.R;
                            char asciiChar = GetAsciiChar(luminance);
                            writer.Write(asciiChar);
                        }

                        writer.WriteLine();

                    }


                }
            });
        },
        inputOption,
        outputOption,
        scaleOption
        );

        await rootCommand.InvokeAsync(args);

    }

    private static double GetTargetScale(double scale, Image<Rgba32> image)
    {
        int tc;
        int ti;
        if (image.Height > image.Width)
        {
            tc = Console.WindowHeight;
            ti = image.Height;
        }
        else
        {
            tc = Console.WindowWidth;
            ti = image.Width;
        }
        var high = Math.Max(tc, ti );
        var low = Math.Min(tc, ti   );
        var target = ((high - low) * scale + low) / high;
        return target;
    }

    static char GetAsciiChar(int luminance)
    {
        // Map the luminance value (0-255) to an ASCII character (0-93)
        char asciiChar = (char)(luminance / 3);

        // Make sure the ASCII character is printable
        if (asciiChar < 33) asciiChar = (char)33;
        if (asciiChar > 126) asciiChar = (char)126;

        return asciiChar;
    }

}