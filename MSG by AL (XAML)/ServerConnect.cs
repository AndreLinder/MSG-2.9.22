using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Data.Common;
using System.Collections.Generic;
using System.Windows;

namespace MSG_by_AL__XAML_
{
    class ServerConnect
    {
        //Адрес и порт для подключения к серверу
        static int port = 8005;
        static IPAddress IP = IPAddress.Parse("77.50.200.145");/*Dns.GetHostAddresses("andrelinder.ddns.net")[0]*/

        //Пустой конструктор класса
        public ServerConnect()
        {

        }

        //Метод, осуществляющий соединение с сервером и получения от него соответствующих данных
        public static List<string> RecieveDataFromDB(string numberCommand, string parameters = "")
        {
            //Список значений, полученных от сервера
            List<string> values = new List<string>();

            try
            {
                //Создаем удаленную конечную точку сервера
                IPEndPoint ipPoint = new IPEndPoint(IP, port);

                //Определяем объект сокета, для подключения к серверу по удаленной конечной точке
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);


                //Подключаемся к удаленному хосту
                socket.Connect(ipPoint);
                byte[] data = Encoding.Unicode.GetBytes(numberCommand + parameters);
                socket.Send(data);

                // получаем ответ
                data = new byte[8192]; // буфер для ответа
                StringBuilder builder = new StringBuilder();
                int bytes = 0; // количество полученных байт
                    
                do
                {
                    bytes = socket.Receive(data, data.Length, 0);
                    builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                }
                while (socket.Available > 0);

                /*Получаем данные от сервера в виде строки: value1~value2~...valueN~ 
                 Обрабатываем данную строку, чтобы разделить значения и добавить их в список*/
                string msg = builder.ToString();

                foreach (string value in msg.Split('~'))
                {
                    if(value != "") values.Add(value);
                }

                // закрываем сокет
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();
            }
            catch (Exception ex)
            {
                //Выводим сообщения об возникшем исключении
                MessageBox.Show(ex.Message);
            }
            //Возвращаем список значений для дальнейших действий
            return values;
        }

        //Метод, осуществляющий соединение с сервером и получающий от него большие списки данных
        public static List<List<string>> RecieveBigDataFromDB(string numberCommand, string parameters = "")
        {
            //Список значений, полученных от сервера
            List<List<string>> values = new List<List<string>>();

            try
            {
                //Создаем удаленную конечную точку сервера
                IPEndPoint ipPoint = new IPEndPoint(IP, port);

                //Определяем объект сокета, для подключения к серверу по удаленной конечной точке
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                //Подключаемся к удаленному хосту
                socket.Connect(ipPoint);
                byte[] data = Encoding.Unicode.GetBytes(numberCommand + parameters);
                socket.Send(data);

                // получаем ответ
                data = new byte[8192]; // буфер для ответа
                StringBuilder builder = new StringBuilder();
                int bytes = 0; // количество полученных байт

                do
                {
                    bytes = socket.Receive(data, data.Length, 0);
                    builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                }
                while (socket.Available > 0);

                // закрываем сокет
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();

                /*Получаем данные от сервера в виде строки: value1~value2~...valueN~ 
                 Обрабатываем данную строку, чтобы разделить значения и добавить их в список*/
                string msg = builder.ToString();

                foreach (string param in msg.Split('%')) 
                {
                    //Составляющее звено основного списка данных (...)
                    List<string> list = new List<string>();

                    foreach (string value in param.Split('~'))
                    {
                        if (value != "") list.Add(value);
                    }
                    values.Add(list);
                }
            }
            catch (Exception ex)
            {
                //Выводим сообщения об возникшем исключении
                MessageBox.Show(ex.Message);
            }
            //Возвращаем список значений для дальнейших действий
            return values;
        }

        public static List<List<string>> RecieveNotification(int ID)
        {
            List<List<string>> values = new List<List<string>>();

            try
            {
                //Создаем удаленную конечную точку сервера
                IPEndPoint ipPoint = new IPEndPoint(IP, 8006);

                //Определяем объект сокета, для подключения к серверу по удаленной конечной точке
                Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                //Подключаемся к удаленному хосту
                socket.Connect(ipPoint);
                byte[] data = Encoding.Unicode.GetBytes(ID.ToString());
                socket.Send(data);

                // получаем ответ
                data = new byte[8192]; // буфер для ответа
                StringBuilder builder = new StringBuilder();
                int bytes = 0; // количество полученных байт

                do
                {
                    bytes = socket.Receive(data, data.Length, 0);
                    builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
                }
                while (socket.Available > 0);

                // закрываем сокет
                socket.Shutdown(SocketShutdown.Both);
                socket.Close();

                /*Получаем данные от сервера в виде строки: value1~value2~...valueN~ 
                 Обрабатываем данную строку, чтобы разделить значения и добавить их в список*/
                string msg = builder.ToString();

                foreach (string param in msg.Split('%'))
                {
                    //Составляющее звено основного списка данных (...)
                    List<string> list = new List<string>();

                    foreach (string value in param.Split('~'))
                    {
                        if (value != "") list.Add(value);
                    }
                    values.Add(list);
                }
            }
            catch (Exception ex)
            {
                //Выводим сообщения об возникшем исключении
                //MessageBox.Show(ex.Message);
            }
            //Возвращаем список значений для дальнейших действий
            return values;
        }
    }
}
