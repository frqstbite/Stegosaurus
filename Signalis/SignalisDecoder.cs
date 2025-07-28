using SkiaSharp;
using Stegosaurus.Signalis;

namespace Stegosaurus.Signalis
{
    public class SignalisDecoder
    {
        public void ScrambleFile(string path)
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
