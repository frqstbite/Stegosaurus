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
	internal class CargoCommand : Command
	{
		public CargoCommand() : base("cargo", "Encode & decode Signalis's steganographic \"cargo\" images.")
		{
			Subcommands.Add(new CargoDecodeCommand());
			Subcommands.Add(new CargoEncodeCommand());
        }

        /// <summary>
        /// Strips the binary data from a cargo image.
        /// </summary>
        /// <param name="from">The stego bitmap to be decoded from.</param>
        /// <returns>The byte sequence concealed within the image.</returns>
        private static byte[] Decode(SKBitmap from, bool skipNonText = false)
		{
			var templateBitmap = SKBitmap.Decode(Program.CargoCarrier);

			byte bitPos = 0; // Offset in the current byte

			var bits = new List<byte>();
			byte current = 0;

			//To begin at the starting left bracket. (JSON)
			bool bracketPassed = false;

			for (int y = from.Height - 1; y >= 0; y--)
			{
				for (int x = 0; x < from.Width; x++)
				{
					//All channels for the input image
					var inputPixel = from.GetPixel(x, y);
					byte[] inputChannel = { inputPixel.Red, inputPixel.Green, inputPixel.Blue };

					//All channels for the original template
					var templatePixel = templateBitmap.GetPixel(x, y);
					byte[] templateChannel = { templatePixel.Red, templatePixel.Green, templatePixel.Blue };

					for (int c = 0; c < 3; c++)
					{
						//Check if the channel differs from the input and the template image
						int bit = inputChannel[c] != templateChannel[c] ? 1 : 0;

						//build the data back up bit-per-bit with the bitpos
						current |= (byte)(bit << bitPos);
						bitPos++;

						//We did a full byte, we can now move on.
						if (bitPos == 8)
						{
							//To start writing at the first left curly bracket.
							if (((char)current) == '{')
								bracketPassed = true;

							//Quick hack to not include null bytes.
							if ((current != 0 && bracketPassed) || !skipNonText)
							{
								bits.Add(current);
							}

                            current = 0;
                            bitPos = 0;
                        }
					}
				}
			}

			return [.. bits];
		}

		private static SKBitmap Encode(byte[] data)
		{
			using var stream = new MemoryStream(data);
            return Encode(stream);
        }

        private static SKBitmap Encode(Stream data)
		{
            var skBitmap = SKBitmap.Decode(Program.CargoCarrier);

			var buffer = new byte[1];
			var readResult = 1;
			var bit_index = 0;

			for (int y = skBitmap.Height - 1; y >= 0; y--)
			{
				for (int x = 0; x < skBitmap.Width; x++)
				{
					var pImg = skBitmap.GetPixel(x, y);

					//Every channel of the pixel.
					byte r = pImg.Red, g = pImg.Green, b = pImg.Blue;

					for (int c = 0; c < 3; c++)
					{
						//Read byte per byte.
						if (bit_index % 8 == 0)
							readResult = data.Read(buffer, 0, 1);

						// EOF
						if (readResult == 0)
						{
							// Break out of all loops if we reach the end of the data
							y = -1;
							x = skBitmap.Width;
							break;
						}

						//Shift the current bit to the right (little endian).
						byte bit = (byte)((buffer[0] >> (bit_index % 8)) & 1);
						bit_index++;

						//Depending what channel we're on, encode the bit to that channel.
						switch (c)
						{
							case 0: r = CargoCommand.EncodeChannel(bit, r); break;
							case 1: g = CargoCommand.EncodeChannel(bit, g); break;
							case 2: b = CargoCommand.EncodeChannel(bit, b); break;
						}

						//Write result to channel.
						skBitmap.SetPixel(x, y, new SKColor(r, g, b, pImg.Alpha));
					}
				}
			}

			return skBitmap;
		}

		private static byte EncodeChannel(byte data, byte color)
		{
			// We only want the bit from the mask so we multiply it by 10 to counteract compression
			var encoded = (data * 10);

			//If color channel + imagePNG value overflow, substract instead.
			if ((color + encoded) > 255)
				encoded *= -1;

			return (byte)(color + encoded);
		}

		class CargoDecodeCommand : Command
		{
            // Arguments
            private static readonly Argument<string> inputParameter = new("input")
            {
                Arity = ArgumentArity.ExactlyOne,
                Description = "A path to the cargo PNG image to decode. Use '-' for stdin.",
                DefaultValueFactory = result => IOSource.STDIO.ToString(),
            };

            // Options
            private static readonly Option<string> outputOption = new("--output", "-o")
            {
                Arity = ArgumentArity.ZeroOrOne,
                Description = "A path to the file to write the decoded data to. Use '-' for stdout.",
                DefaultValueFactory = result => IOSource.STDIO.ToString(),
            };

            private static readonly Option<bool> nonTextOption = new("--skip-non-text", "-s")
            {
                Arity = ArgumentArity.ZeroOrOne,
                Description = "To skip the non text and trailing bytes.",
				DefaultValueFactory = result => false
            };

            // Constructor
            public CargoDecodeCommand() : base("decode", "Decode data from an IO stream or cargo image file.")
			{
				inputParameter.Validators.Add(IOSource.GetIOSourceValidator(inputParameter, true));
				Arguments.Add(inputParameter);

				outputOption.Validators.Add(IOSource.GetIOSourceValidator(outputOption, false));

                Options.Add(outputOption);
                Options.Add(nonTextOption);

                SetAction(async (result, cancel) =>
				{
                    // Read the input
                    var inputArgument = result.GetValue(inputParameter)!;
					using var input = IOSource.ReadFromSource(inputArgument);

					// Decode the input data from the cargo image
					var bitmap = SKBitmap.Decode(input);
					if (bitmap is null)
					{
						Console.Error.WriteLine("Failed to decode the input image. Ensure it is a valid cargo PNG image.");
						return;
					}

                    var data = Decode(bitmap, result.GetValue(nonTextOption));

                    // Save the result!
                    var outputArgument = result.GetValue(outputOption)!;
					using var output = IOSource.WriteFromSource(outputArgument);

                    await output.WriteAsync(data, cancel);
                });
			}
		}

		class CargoEncodeCommand : Command
		{
			// Arguments
			private static readonly Argument<string> inputParameter = new("input")
			{
				Arity = ArgumentArity.ExactlyOne,
				Description = "A path to a file containing data to encode. Use '-' for stdin.",
				DefaultValueFactory = result => IOSource.STDIO.ToString(),
			};

			// Options
			private static readonly Option<string> outputOption = new("--output", "-o")
			{
				Arity = ArgumentArity.ZeroOrOne,
				Description = "A path to the .png file to write the decoded data to. Use '-' for stdout.",
				DefaultValueFactory = result => IOSource.STDIO.ToString(),
			};

			// Constructor
			public CargoEncodeCommand() : base("encode", "Encode a byte payload to an IO stream or cargo image file.")
			{
				inputParameter.Validators.Add(IOSource.GetIOSourceValidator(inputParameter, true));
                Arguments.Add(inputParameter);

				outputOption.Validators.Add(IOSource.GetIOSourceValidator(outputOption, false));
                Options.Add(outputOption);
				
				SetAction(async (result, cancel) =>
				{
                    // Read the input
                    var inputArgument = result.GetValue(inputParameter)!;
					using var input = IOSource.ReadFromSource(inputArgument);

                    // Encode the input data into a cargo image
                    var image = SKImage.FromBitmap(Encode(input));
					var imagePNG = image.Encode(SKEncodedImageFormat.Png, 100);

					//Save the result!
					var outputArgument = result.GetValue(outputOption)!;
					using var output = IOSource.WriteFromSource(outputArgument);

                    await output.WriteAsync(imagePNG.ToArray(), cancel);
				});
			}
		}

	}
}
