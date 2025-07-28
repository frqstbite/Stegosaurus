using System.CommandLine;
using System.CommandLine.Parsing;

namespace Stegosaurus
{
    internal static class IOSource
    {
        public const char STDIO = '-';

        public static Action<ArgumentResult> GetIOSourceValidator(Argument<string> parameter, bool require)
        {
            return result => ValidateIOSource(result, result.GetValue(parameter)!, require);
        }

        public static Action<OptionResult> GetIOSourceValidator(Option<string> parameter, bool require)
        {
            return result => ValidateIOSource(result, result.GetValue(parameter)!, require);
        }

        public static Stream ReadFromSource(string source)
        {
            if (source == STDIO.ToString())
            {
                // Use standard input/output stream
                return Console.OpenStandardInput();
            }

            return File.OpenRead(source);
        }

        public static Stream WriteFromSource(string source)
        {
            if (source == STDIO.ToString())
            {
                // Use standard input/output stream
                return Console.OpenStandardOutput();
            }
            return File.OpenWrite(source);
        }

        private static void ValidateIOSource(SymbolResult result, string argument, bool require)
        {
            // Use '-' to indicate standard input/output
            if (argument == STDIO.ToString())
            {
                return;
            }

            // Check for invalid characters in the file source
            if (argument.IndexOfAny(Path.GetInvalidPathChars()) > 0)
            {
                result.AddError($"Invalid characters in file path: '{argument}'");
            }

            // If required, check if the file exists
            if (require && !File.Exists(argument))
            {
                result.AddError($"File '{argument}' does not exist.");
            }
        }
    }
}
