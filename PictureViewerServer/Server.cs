using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PictureViewerServer
{

    public class Server
    {
        private TcpListener Listener; // Объект, принимающий TCP-клиентов
        public Thread threadServer; // поток, обрабатывающий в цикле клиентов
        // Корневая папка, где будут храниться все фото (рабочий стол, папка ImagesForPictureViewer)
        //public static string DirectoryRoot = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory) + "\\ImagesForPictureViewer";
        public static string DirectoryRoot = "K:" + "\\ImagesForPictureViewer";

        // Конструктор класса
        public Server(int Port)
        {
            // Создаем "слушателя" для указанного порта
            Listener = new TcpListener(IPAddress.Any, Port);
            Listener.Start(); // Запускаем его

            threadServer = new Thread(StartServer); // создание отдельного потока с указанием делегата
            threadServer.IsBackground = true;
            threadServer.Start(); // запуск потока
        }

        private void StartServer()
        {
            // В бесконечном цикле обрабатываем клиентов
            //while (threadServer.ThreadState == ThreadState.Running)
            while (threadServer.ThreadState == ThreadState.Background)
            {
                // Принимаем нового клиента
                TcpClient Client = Listener.AcceptTcpClient();
                // Создаем поток
                Thread Thread = new Thread(new ParameterizedThreadStart(ClientThread));
                // И запускаем этот поток, передавая ему принятого клиента
                Thread.Start(Client);

            }
        }

        static void ClientThread(Object StateInfo)
        {
            new ClientHandler((TcpClient)StateInfo);
        }

        // Остановка сервера
        ~Server()
        {
            // Если "слушатель" был создан
            if (Listener != null)
            {
                // Остановим его
                Listener.Stop();
            }
        }
    }

}
