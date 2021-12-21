using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PictureViewerServer
{
    class ClientHandler
    {
        // Конструктор класса. Ему нужно передавать принятого клиента от TcpListener
        public ClientHandler(TcpClient Client)
        {
            // Объявим строку, в которой будет хранится запрос клиента
            string Request = "";
            // Буфер для хранения принятых от клиента данных
            byte[] Buffer = new byte[4096];
            // Переменная для хранения количества байт, принятых от клиента
            int Count;
            // Читаем из потока клиента до тех пор, пока от него поступают данные
            while ((Count = Client.GetStream().Read(Buffer, 0, Buffer.Length)) > 0)
            {
                // Преобразуем эти данные в строку и добавим ее к переменной Request
                //Request += Encoding.ASCII.GetString(Buffer, 0, Count);
                Request += Encoding.UTF8.GetString(Buffer, 0, Count);
                // Запрос должен обрываться последовательностью \r\n\r\n
                // Либо обрываем прием данных сами, если длина строки Request превышает 4 килобайта
                // Нам не нужно получать данные из POST-запроса (и т. п.), а обычный запрос
                // по идее не должен быть больше 4 килобайт
                if (Request.IndexOf("\r\n\r\n") >= 0/* || Request.Length > 4096*/)
                {
                    break;
                }
            }

            // Разделяем строку на морфемы
            string[] RequestWords = Request.Split(new string[1] { " " }, StringSplitOptions.RemoveEmptyEntries);

            if (RequestWords.Length < 2)
            {
                SendMessage(Client, 400);
                return;
            }

            string MessageResponse = "";
            int CodeResponce = 500;

            try
            {
                switch (RequestWords[0])
                {
                    case "GET":
                    {
                        switch (RequestWords[1])
                        {
                            case "IMAGE":
                            {
                                CodeResponce = ResponceGetImage(RequestWords[2], out MessageResponse);
                                SendMessage(Client, CodeResponce, MessageResponse);
                                break;
                            }

                            case "LIST":
                            {
                                CodeResponce = ResponceGetListImages(out MessageResponse);
                                SendMessage(Client, CodeResponce, MessageResponse);
                                break;
                            }

                            default:
                                break;
                        }
                        break;
                    }

                    default:
                        SendMessage(Client, 400, "Сервер не смог разобрать запрос.");
                        break;
                }
            }
            catch (Exception err)
            {
                SendMessage(Client, 500, "Внутренняя ошибка сервера" + Environment.NewLine + err.Message);
            }
            finally
            {
                Client.Close();
            }

            Client.Close();

        }

        // Отправка сообщения
        private void SendMessage(TcpClient Client, int Code, string message = "")
        {
            string Responce = Code.ToString(); // код
            Responce += message.Length > 0 ? (" " + message) : " ResponseIsEmpty"; // текст сообщения если не пустое
            Responce += " \r\n\r\n";  // конец ответа
            // Приведем строку к байтам
            byte[] Buffer = Encoding.UTF8.GetBytes(Responce);
            // Отправим его клиенту
            Client.GetStream().Write(Buffer, 0, Buffer.Length);
        }

        // Получить картинку по имени
        private int ResponceGetImage(string name, out string message)
        {
            message = "";
            // Существует ли такой файл
            if (!File.Exists(Server.DirectoryRoot + "\\" + name))
            {
                message = "Файл " + name + " не найден!";
                return 404;
            }

            byte[] imageBytes = File.ReadAllBytes(Server.DirectoryRoot + "\\" + name);
            // байты файла в Base64
            message = Utility.Base64Encode(imageBytes);
            return 200;
        }

        // Получить список имен файлов и их количество
        private int ResponceGetListImages(out string message)
        {
            message = "";
            string separator = "=";
            string[] ImagesName = Directory.GetFiles(Server.DirectoryRoot);

            foreach (string image in ImagesName)
            {
                message += image + separator;
            }
            message = Utility.Base64Encode(Encoding.UTF8.GetBytes(message));
            // количество файлов + разделитель_имен_файлов + список файлой в Base64
            message = ImagesName.Length.ToString() + " " + separator + message;

            return 200;
        }
    }
}
