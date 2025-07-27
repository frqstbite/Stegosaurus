using SkiaSharp;

namespace SignalisSaveSteganography.Image
{
    public class SignalisEncoder
    {
        public SKBitmap Encode(string image, string data)
        {
            using var imageStream = File.OpenRead(image);
            using var dataStream = File.OpenRead(data);

            var skBitmap = SKBitmap.Decode(image);

            var buffer = new byte[1];
            var readResult = 1;
            var bit_index = 0;

            for (int y = skBitmap.Height - 1; y >= 0; y--)
            {
                for (int x = 0; x < skBitmap.Width; x++)
                {
                    var pImg = skBitmap.GetPixel(x, y);

                    byte r = pImg.Red, g = pImg.Green, b = pImg.Blue;

                    for (int c = 0; c < 3; c++)
                    {
                        if (bit_index % 8 == 0)
                            readResult = dataStream.Read(buffer, 0, 1);

                        //EOF
                        if (readResult > 0)
                        {
                            byte bit = (byte)((buffer[0] >> (bit_index % 8)) & 1);
                            bit_index++;

                            switch (c)
                            {
                                case 0: r = CalculateChannelData(bit, r); break;
                                case 1: g = CalculateChannelData(bit, g); break;
                                case 2: b = CalculateChannelData(bit, b); break;
                            }
                            skBitmap.SetPixel(x, y, new SKColor(r, g, b, pImg.Alpha));
                        }
                        else
                        {
                            y = -1;
                            x = skBitmap.Width;
                            break;
                        }
                    }
                }
            }

            return skBitmap;
        }

        private byte CalculateChannelData(byte data, byte color)
        {
            //We only want the bit from the mask, we multiply it by 10, to counteract compression wiping out our encoded data!
            var encoded = (data * 10);

            //If color channel + encoded value overflow, substract instead.
            if ((color + encoded) > 255)
                encoded *= -1;

            return (byte)(color + encoded);
        }
    }
}
