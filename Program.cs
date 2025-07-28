using SkiaSharp;
using Stegosaurus.Commands;
using Stegosaurus.Signalis;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;
using System.Reflection;

namespace Stegosaurus
{
    internal class Program
    {
        public static Assembly Assembly { get; } = Assembly.GetExecutingAssembly();

        public static Stream CargoCarrier { get; } = Assembly.GetManifestResourceStream("Stegosaurus.Resources.carrier.png")!
            ?? throw new FileNotFoundException("Carrier resource not found.");
        
        public static string Penrose
        {
            get
            {
                Stream stream = Assembly.GetManifestResourceStream("Stegosaurus.Resources.penrose.txt")!;
                using var reader = new StreamReader(stream);
                string penrose = reader.ReadToEnd();
                return penrose;
            }
        }



        static async Task<int> Main(string[] args)
        {
            RootCommand rootCommand = new("Decode, encode, and diff Signalis's save files.")
            {
                // Subcommands
                new CargoCommand(),
            };

            ParseResult result = rootCommand.Parse(args);
            return await result.InvokeAsync();
        }

        static int Scramble(string input)
        {
            var sdecoder = new SignalisDecoder();

            sdecoder.ScrambleFile(input);
            return 0;
        }
    }
}
