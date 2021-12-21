﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PictureViewerServer
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
        }

        Thread threadSerever;

        private void button1_Click(object sender, EventArgs e)
        {
            label1.Visible = true;
            numericUpDown1.Enabled = false;
            button1.Enabled = false;

            threadSerever = new Thread(StartServer); // создание отдельного потока с указанием делегата
            threadSerever.Start(); // запуск потока
        }

        private void StartServer()
        {
            int port = Convert.ToInt32(numericUpDown1.Value);
            // Создадим новый сервер и укажем порт 
            new Server(port);
        }

        // перед закрытие формы
        private void FormMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            threadSerever.Abort();
        }
    }
}
