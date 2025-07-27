using SignalisSaveSteganography.Global;
using SkiaSharp;

namespace SignalisSaveSteganography.Signalis
{
    public class SignalisDecoder
    {
        public void DecodeSteganography(string templatePath, string imagePath)
        {
            var outputPath = $"{Path.GetFileNameWithoutExtension(imagePath)}.bin";

            if (File.Exists(outputPath))
                File.Delete(outputPath);

            using var imageStream = File.OpenRead(imagePath);
            using var templateStream = File.OpenRead(templatePath);

            var skBitmap = SKBitmap.Decode(imageStream);
            var templateBitmap = SKBitmap.Decode(templateStream);

            var bitPos = 0;

            var bits = new List<byte>();
            byte current = 0;

            for (int y = skBitmap.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < skBitmap.Width; x++)
                {
                    var pImg = skBitmap.GetPixel(x, y);
                    var pTpl = templateBitmap.GetPixel(x, y);

                    byte[] imgCh = { pImg.Red, pImg.Green, pImg.Blue };
                    byte[] tplCh = { pTpl.Red, pTpl.Green, pTpl.Blue };

                    for (int c = 0; c < 3; c++)
                    {
                        int bit = imgCh[c] != tplCh[c] ? 1 : 0;
                        current |= (byte)(bit << bitPos);
                        bitPos++;

                        if (bitPos == 8)
                        {
                            bits.Add(current);
                            current = 0;
                            bitPos = 0;
                        }
                    }
                }
            }

            int last = bits.Count - 1;
            while (last >= 0 && bits[last] == 0)
                last--;

            using var output = File.OpenWrite(outputPath);

            for (int i = 0; i <= last; i++)
                output.WriteByte(bits[i]);
        }

        public void DecodeFile(string path)
        {
            using var open = File.OpenRead(path);
            using var newFile = File.OpenWrite($"{Path.GetFileNameWithoutExtension(path)}.json");

            var buffer = new byte[1];
            var index = 0;

            while (open.Read(buffer, 0, 1) > 0)
            {
                newFile.Write([XorDecode(buffer[0], index)], 0, 1);

                index++;
            }
        }

        private byte XorDecode(byte input, int index)
        {
            var key = SignalisVariables.ScramblerCodeWord;

            return (byte)(input ^ (byte)key[index % key.Length]);
        }
    }
}
