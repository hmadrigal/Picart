using System.CommandLine;

internal class Program
{
    
    private static async Task Main(string[] args)
    {
        var inputOption = new Option<FileInfo>("--input", "Input image file.");
        var outputOption = new Option<FileInfo?>("--output", getDefaultValue: () => default, description: "Output image file");
        var rootCommand = new RootCommand(description: "Convers a image to ASCII.")
        { inputOption, outputOption };

        rootCommand.SetHandler((FileInfo input, FileInfo? output) =>
        {
            using var image = Image.Load<Rgba32>(input.OpenRead());
            image.ProcessPixelRows(accessor =>
            {

                // Convert the image to grayscale
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgba32> pixelRow = accessor.GetRowSpan(y);

                    // pixelRow.Length has the same value as accessor.Width,
                    // but using pixelRow.Length allows the JIT to optimize away bounds checks:
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
        outputOption
        );

        await rootCommand.InvokeAsync(args);

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