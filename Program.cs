using SkiaSharp;
using Stegosaurus.Commands;
using System.CommandLine;
using System.IO;
using System.Reflection;

namespace Stegosaurus
{
    internal static class Program
    {
        static async Task<int> Main(string[] args)
        {
            RootCommand rootCommand = new("Decode, encode, and diff Signalis's save files.")
            {
                // Subcommands
                new CargoCommand(),
                new LSTRCommand(),
            };

            ParseResult result = rootCommand.Parse(args);
            return await result.InvokeAsync();
        }

        public static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

        public static Stream CargoCarrier { get; } = Assembly.GetManifestResourceStream("Stegosaurus.Resources.carrier.png")
            ?? throw new FileNotFoundException("Carrier resource not found.");
    }
}
