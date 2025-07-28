using SkiaSharp;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Stegosaurus.Commands
{
	internal class LSTRCommand : Command
	{
        private const string key = "ALL WATCHED OVER BY MACHINES OF LOVING GRACE";

        // Arguments
        private static readonly Argument<string> inputParameter = new("input")
        {
            Arity = ArgumentArity.ExactlyOne,
            Description = "A path to a file containing data to process. Use '-' for stdin.",
            DefaultValueFactory = result => IOSource.STDIO.ToString(),
        };

        // Options
        private static readonly Option<string> outputOption = new("--output", "-o")
        {
            Arity = ArgumentArity.ZeroOrOne,
            Description = "A path to a file to write the processed data to. Use '-' for stdout.",
            DefaultValueFactory = result => IOSource.STDIO.ToString(),
        };

        // Constructor
        public LSTRCommand() : base("lstr", "Encode & decode Signalis's ciphered \"lstr.replika\" file. Ciphered input will be deciphered. Unciphered input will be ciphered.")
		{
            inputParameter.Validators.Add(IOSource.GetIOSourceValidator(inputParameter, true));
            Arguments.Add(inputParameter);

            outputOption.Validators.Add(IOSource.GetIOSourceValidator(outputOption, false));
            Options.Add(outputOption);

            SetAction(async (result, cancel) =>
            {
                var inputArgument = result.GetValue(inputParameter)!;
                var outputArgument = result.GetValue(outputOption)!;

                await Scramble(inputArgument, outputArgument, cancel);
            });
        }

        /// <summary>
        /// XOR decodes the data from an input source and writes it to an output source.
        /// </summary>
        /// <param name="inputSource">An IO source to read data from.</param>
        /// <param name="outputSource">An IO source to write data to.</param>
        private static async Task Scramble(string inputSource, string outputSource, CancellationToken cancel)
        {
            using var input = IOSource.ReadFromSource(inputSource);
            using var output = IOSource.WriteFromSource(outputSource);

            var buffer = new byte[1];
            var index = 0;

            while (input.Read(buffer, 0, 1) > 0)
            {
                await output.WriteAsync([XorDecode(buffer[0], index)], 0, 1, cancel);
                index++;
            }
        }

        private static byte XorDecode(byte input, int index)
        {
            return (byte)(input ^ (byte)key[index % key.Length]);
        }
	}
}
