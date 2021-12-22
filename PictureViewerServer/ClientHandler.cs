using System;
using System.Collections.Generic;
using System.Drawing;
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
                Request += Encoding.UTF8.GetString(Buffer, 0, Count);
                // Запрос должен заканчиваться последовательностью \r\n\r\n
                if (Request.IndexOf("\r\n\r\n") >= 0/* || Request.Length > 4096*/)
                {
                    break;
                }
            }

            // Разделяем строку на подстроки
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
                    case "GET": // Получить...
                    {
                        switch (RequestWords[1])
                        {
                            // Получить картинку по имени
                            case "IMAGE": // GET IMAGE имя_картинки
                            {
                                if (RequestWords.Length < 3)
                                {
                                    SendMessage(Client, 400, "Для данного запроса не хватает параметров.");
                                    return;
                                }
                                CodeResponce = ResponceGetImage(RequestWords[2], out MessageResponse);
                                SendMessage(Client, CodeResponce, MessageResponse);
                                break;
                            }

                            // Получить список имен файлов и их количество
                            case "LIST": // GET LIST
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

                    case "PUT": // Изменить существующее...
                    {
                        switch (RequestWords[1])
                        {
                            // Сохранить изменную картинку по имени
                            case "EDIT": // PUT EDIT имя_картинки Base64_данные
                            {
                                if (RequestWords.Length < 4)
                                {
                                    SendMessage(Client, 400, "Для данного запроса не хватает параметров.");
                                    return;
                                }
                                CodeResponce = ResponceEditImage(RequestWords[2], RequestWords[3], out MessageResponse);
                                SendMessage(Client, CodeResponce, MessageResponse);
                                break;
                            }

                            // Удалить картинку по имени
                            case "DELETE": // PUT DELETE имя_картинки
                            {
                                if (RequestWords.Length < 3)
                                {
                                    SendMessage(Client, 400, "Для данного запроса не хватает параметров.");
                                    return;
                                }
                                CodeResponce = ResponceDeleteImage(RequestWords[2], out MessageResponse);
                                SendMessage(Client, CodeResponce, MessageResponse);
                                break;
                            }

                            default:
                                break;
                        }
                        break;
                    }

                    case "POST": // Добавить новое...
                    {
                        switch (RequestWords[1])
                        {
                            // Загрузить на сервер новую картинку
                            case "NEW": // POST NEW имя_картинки Base64_данные
                            {
                                if (RequestWords.Length < 4)
                                {
                                    SendMessage(Client, 400, "Для данного запроса не хватает параметров.");
                                    return;
                                }
                                CodeResponce = ResponceEditImage(RequestWords[2], RequestWords[3], out MessageResponse);
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
            name = Utility.Base64DecodeString(name);
            // Существует ли такой файл
            if (!File.Exists(Server.DirectoryRoot + "\\" + name))
            {
                message = "Файл " + name + " не найден!";
                message = Utility.Base64EncodeString(message);
                return 404;
            }

            byte[] imageBytes = Utility.ImageFileToBytes(Server.DirectoryRoot + "\\" + name);
            // байты картинки в Base64
            message = Utility.Base64EncodeBytes(imageBytes);
            return 200;
        }

        // Получить список имен файлов и их количество
        private int ResponceGetListImages(out string message)
        {
            message = "";
            string separatorFileNames = "=";
            string[] ImagesName = Directory.GetFiles(Server.DirectoryRoot);

            foreach (string image in ImagesName)
            {
                message += image + separatorFileNames;
            }
            message = Utility.Base64EncodeString(message);
            // количество файлов + разделитель_имен_файлов + список файлой в Base64
            message = ImagesName.Length.ToString() + " " + separatorFileNames + " " + message;

            return 200;
        }

        // Сохранить измененную картинку
        private int ResponceEditImage(string name, string Base64Image, out string message)
        {
            message = "";
            name = Utility.Base64DecodeString(name);
            // Существует ли такой файл
            if (!File.Exists(Server.DirectoryRoot + "\\" + name))
            {
                message = "Файл " + name + " не найден." + Environment.NewLine + "сохранение невозможно.";
                message = Utility.Base64EncodeString(message);
                return 404;
            }

            // преобразовали строку_Base64 в байты, затем в Image
            Image image = Utility.BytesToImage(Utility.Base64DecodeBytes(Base64Image));
            // Удалили старое изображения
            File.Delete(Server.DirectoryRoot + "\\" + name);
            if (File.Exists(Server.DirectoryRoot + "\\" + name))
            {
                message = "Не удалось удалить предыдущую версию картинки " + name +" по неизвестной причине.";
                message = Utility.Base64EncodeString(message);
                return 500;
            }
            Utility.ImageObjectSave(image, Server.DirectoryRoot + "\\" + name); // сохранили
            // проверили
            if (File.Exists(Server.DirectoryRoot + "\\" + name))
            {
                message = "Файл " + name + " успешно изменен.";
                message = Utility.Base64EncodeString(message);
                return 200;
            }
            else
            {
                message = "Файл " + name + " был удален, но новый сохранить не удалось.";
                message = Utility.Base64EncodeString(message);
                return 500;
            }
        }

        // Удалить картинку по имени
        private int ResponceDeleteImage(string name, out string message)
        {
            message = "";
            name = Utility.Base64DecodeString(name);
            // Существует ли такой файл
            if (!File.Exists(Server.DirectoryRoot + "\\" + name))
            {
                message = "Файл " + name + " уже был удален ранее.";
                message = Utility.Base64EncodeString(message);
                return 200;
            }
            File.Delete(Server.DirectoryRoot + "\\" + name); // удалили
            // проверили
            if (!File.Exists(Server.DirectoryRoot + "\\" + name))
            {
                message = "Файл " + name + " успешно удален.";
                message = Utility.Base64EncodeString(message);
                return 200;
            }
            else
            {
                message = "Файл " + name + " не был удален, по неизвестной ошибке.";
                message = Utility.Base64EncodeString(message);
                return 500;
            }
        }

        // Загрузить новую картинку
        private int ResponceNewImage(string name, string Base64Image, out string message)
        {
            message = "";
            name = Utility.Base64DecodeString(name);
            // Существует ли такой файл
            if (File.Exists(Server.DirectoryRoot + "\\" + name))
            {
                message = "Файл " + name + " уже существует." + Environment.NewLine + "Задайте другое имя или сначала удалите файл.";
                message = Utility.Base64EncodeString(message);
                return 403;
            }

            // преобразовали строку_Base64 в байты, затем в Image
            Image image = Utility.BytesToImage(Utility.Base64DecodeBytes(Base64Image));
            // Удалили старое изображения
            Utility.ImageObjectSave(image, Server.DirectoryRoot + "\\" + name); // сохранили
            // проверили
            if (File.Exists(Server.DirectoryRoot + "\\" + name))
            {
                message = "Новый файл " + name + " успешно загружен.";
                message = Utility.Base64EncodeString(message);
                return 200;
            }
            else
            {
                message = "Новый файл " + name + " не был загружен, по неизвестной ошибке.";
                message = Utility.Base64EncodeString(message);
                return 500;
            }
        }
    }
}
