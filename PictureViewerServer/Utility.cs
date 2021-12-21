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
        // Получить изображение по имени файла
        // и преобразовать объект типа Image в массив байтов
        public static byte[] ImageFileToBytes(string path)
        {
            string ext = path.Substring(path.LastIndexOf("."));
            return Utility.ImageObjectToBytes(Image.FromFile(path), ext);
        }

        // Преобразовать объект типа Image в массив байтов
        public static byte[] ImageObjectToBytes(Image img, string ext)
        {
            byte[] byteArray;
            using (MemoryStream stream = new MemoryStream())
            {
                img.Save(stream, GetImageFormatFromExtension(ext));
                stream.Close();

                byteArray = stream.ToArray();
            }
            return byteArray;
        }

        // Получение системного формата по расширению файла
        public static ImageFormat GetImageFormatFromExtension(string ext)
        {
            switch (ext.ToLower())
            {
                case "png":
                    return ImageFormat.Png;

                case "ico":
                    return ImageFormat.Icon;

                case "jpg":
                case "jpeg":
                    return ImageFormat.Jpeg;

                case "bmp":
                    return ImageFormat.Bmp;

                case "gif":
                    return ImageFormat.Gif;

                default:
                    return ImageFormat.Bmp;
            }
        }

        // Преобразовать массив байтов в объект типа Image
        public static Image BytesToImage(byte[] imgBytes)
        {
            MemoryStream ms = new MemoryStream(imgBytes);
            Image img = Image.FromStream(ms);
            return img;
        }

        // Сохранить объект типа Image как файл
        public static void ImageObjectSave(Image img, string path)
        {
            string ext = path.Substring(path.LastIndexOf("."));
            img.Save(path, GetImageFormatFromExtension(ext));
        }

        // Байты в строку Base64
        public static string Base64Encode(byte[] textBytes)
        {
            return Convert.ToBase64String(textBytes);
        }

        // Строку Base64 в байты
        public static byte[] Base64Decode(string textBase64)
        {
            return Convert.FromBase64String(textBase64);
        }
    }
}
