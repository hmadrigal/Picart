using System.CommandLine;

var inputOption = new Option<FileInfo>("Image file");
var rootCommand = new RootCommand(description: "Convers a image to ASCII.")
        { inputOption };

rootCommand.SetHandler(async (FileInfo input) =>
{
    await Task.Yield();
}, inputOption);
await rootCommand.InvokeAsync(args);

Console.WriteLine("Hello, World!");
