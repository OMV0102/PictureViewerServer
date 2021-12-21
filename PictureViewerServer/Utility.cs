using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace PictureViewerServer
{
    public static class Utility
    {
        public static byte[] ImageToByte2(Image img)
        {
            byte[] byteArray = new byte[10000];
            using (MemoryStream stream = new MemoryStream())
            {
                img.Save(stream, ImageFormat.Png);
                stream.Close();

                byteArray = stream.ToArray();
            }
            return byteArray;
        }

        // Байты в строку Base64
        public static string Base64Encode(byte[] textBytes)
        {
            return Convert.ToBase64String(textBytes);
        }

        //Строку Base64 в байты
        public static byte[] Base64Decode(string textBase64)
        {
            return Convert.FromBase64String(textBase64);
        }
    }
}
