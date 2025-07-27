using SignalisSaveSteganography.Image;
using SignalisSaveSteganography.Signalis;
using SkiaSharp;

namespace SignalisSaveSteganography
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("ssu dataPath encode");
                Console.WriteLine("ssu imagePath decode");
                Console.WriteLine("ssu scrambledDataPath scramble (this also unscrambles)");
                Console.WriteLine();
                Console.WriteLine("Other:");
                Console.WriteLine("ssu templatePath dataPath encode");
                Console.WriteLine("ssu templatePath imagePath decode");
                return;
            }

            var path1 = args[0];
            var path2 = args[1];
            var option = string.Empty;

            if (args.Length > 2)
                option = args[2];

            if (string.IsNullOrEmpty(option))
            {
                option = path2;

                path2 = path1;
                path1 = "./Assets/template.png";
            }

            switch (option.ToLower())
            {
                case "encode":
                    var encoder = new SignalisEncoder();

                    var skBitmap = encoder.Encode(path1, path2);

                    //Save the result!
                    var result = SKImage.FromBitmap(skBitmap);
                    var encoded = result.Encode(SKEncodedImageFormat.Png, 100);

                    var output = File.OpenWrite($"{Path.GetFileNameWithoutExtension(path2)}.png");
                    encoded.SaveTo(output);
                    output.Close();

                    break;
                case "decode":
                    var decoder = new SignalisDecoder();

                    decoder.DecodeSteganography(path1, path2);
                    break;
                case "scramble":
                    var sdecoder = new SignalisDecoder();

                    sdecoder.ScrambleFile(path1);
                    break;
            }
        }
    }
}
