using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Threading;
using MSG_by_AL__XAML_.Resource;
using Microsoft.Toolkit.Uwp.Notifications;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.WebUI;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace MSG_by_AL__XAML_
{
    /// <summary>
    /// Логика взаимодействия для ChatsPage.xaml
    /// </summary>
    public partial class ChatsPage : Window
    {
        //Логин авторизованного пользователя
        public static string NickName = "null";
        //Имя пользователя
        public static string name = "null";
        //ID авторизованного пользователя
        public static int IDuser = -1;
        //GuID авторизованного пользователя
        public string GuID = "null";

        //Отметка о прикреплении файла
        bool file_attached= false;

        //Список потенциальных пользователей
        List<User> users = new List<User>();

        //Список пользователей с существующими чатами
        List<User> users_chat = new List<User>();

        //Список GUID чатов и ID пользователей для быстрого доступа к диалогу
        List<Chat_List> chat_list = new List<Chat_List>();

        //Список групповых чатов
        List<Chat_List> group_chats = new List<Chat_List>();

        //ID, guid и никнейм собеседника
        public static int IDFriend = -1;
        public static string Friend_Nick = "null";
        public string GuIDFriend = "null";

        //ID активного диалога и количество сообщений в нём
        public static int IDChat = -1;
        public static string GuID_Chat = "null";
        public static int MessageCount = -1;
        public static bool IsPrivate_Chat = true;

        //Путь к прикрепляемому к сообщению файлу
        public static string file_Path = "";

        public ChatsPage(int ID, string nick, string guID, string username)
        {
            IDuser = ID;
            NickName = nick;
            name = username;
            InitializeComponent();
            Clear_List();
            User_Name.Content = name;
            User_Nick.Content = "@" + nick;
            Update_Dialog_List();
            Update_Friend_List();
            GuID = guID;
            Task.Run(() => Recieve_Notification());
        }

        //Очистка всех списков (сообщений, чатов, пользователей)
        public void Clear_List()
        {
            User_List.Items.Clear();
            Message_List.Items.Clear();
            Chat_list.Items.Clear();
        }

        //Работа с уведомлениями через Демона
        public async void Recieve_Notification()
        {
            while (IDuser > 0)
            {
                try
                {
                    List<List<string>> list_notification = await Task.Run(() => ServerConnect.RecieveNotification(IDuser));
                    //Dispatcher.Invoke(()=>Demon_Connect.Text = "Demon connected");
                    if (list_notification[0][0] != "NONE")
                    {
                        foreach (List<string> value in list_notification)
                        {
                            AES256 aes = new AES256(value[2]);
                            string msg = aes.Decode(value[1]);
                            if (msg.Length > 40) msg = msg.Substring(0, 40) + "...";
                            Dispatcher.Invoke(() => User_Name_Notification.Text = value[0]);
                            Dispatcher.Invoke(() => Text_Message_Notification.Text = msg);
                            Dispatcher.Invoke(() => SideNotificationShow());
                            new ToastContentBuilder()
                                .AddArgument("action", "viewConversation")
                                .AddArgument("conversationId", 9813)
                                .AddText(value[0] + " sent new message")
                                .AddText(msg)
                                .Show();
                            System.Threading.Thread.Sleep(4000);
                            Dispatcher.Invoke(() => SideNotificationShow());
                            System.Threading.Thread.Sleep(1500);
                        }
                        list_notification.Clear();
                    }
                    System.Threading.Thread.Sleep(4000);
                }
                catch (Exception ex)
                {
                    //Dispatcher.Invoke(()=>Demon_Connect.Text = "Demon disconnected");
                }
            }

        }

        //Метод обновления списка чатов(#03)
        public void Update_Dialog_List()
        {
            //Очищаем старый список чатов, если такие были
            Chat_list.Items.Clear();

            //Получаем список чатов от сервера в виде "Списка списков строк"
            List<List<string>> values = ServerConnect.RecieveBigDataFromDB("03#", IDuser.ToString());

            if (values[0][0] != "ERROR")
            {
                foreach (List<string> value in values)
                {
                    Chat_List list = new Chat_List();
                    list.ID = int.Parse(value[0]);
                    list.Name = value[1];
                    list.GUID = value[2];
                    list.ID_Friend = int.Parse(value[4]);
                    list.Public = false;
                    Chat_list.Items.Add(list);


                    User U = new User();
                    U.ID = int.Parse(value[4]);
                    U.Name = value[3];
                    U.Nickname = "null";
                    users_chat.Add(U);

                    chat_list.Add(list);
                }
            }
        }

        //Метод получения списка друзей (№04)
        public void Update_Friend_List()
        {
            //Очищаем старый список друзей, если он был до этого
            Friend_List.Items.Clear();

            //Делаем запрос на сервер и получаем список друзей
            List<List<string>> values = ServerConnect.RecieveBigDataFromDB("04#", IDuser + "~");

            if (values[0][0] != "ERROR")
            {
                foreach (List<string> value in values)
                {
                    User user = new User();
                    user.ID = int.Parse(value[0]);
                    user.Name = value[1];
                    user.Nickname = value[2];
                    Friend_List.Items.Add(user);

                    users.Add(user);
                }
            }
        }

        public bool Exp = false;
        public void SideNotificationShow()
        {
            if (Exp)
            {
                var anim = new DoubleAnimation(0, (Duration)TimeSpan.FromSeconds(0.5));
                anim.Completed += (s, _) => Exp = false;
                Side_Notification.BeginAnimation(ContentControl.WidthProperty, anim);
            }
            else
            {
                var anim = new DoubleAnimation(265, (Duration)TimeSpan.FromSeconds(0.5));
                anim.Completed += (s, _) => Exp = true;
                Side_Notification.BeginAnimation(ContentControl.WidthProperty, anim);
            }
        }

        //Запуск асинхронной операции обновления текущего диалога(06#)
        public async void Refresh_Chat_Async(int id_f)
        {
            AES256 aes = new AES256(GuID_Chat);
            while (IDFriend == id_f)
            {
                try
                {
                    List<List<string>> values = await Task.Run(() => ServerConnect.RecieveBigDataFromDB("06#", IDuser + "~" + IDFriend));
                    if (values[0][0] != "ERROR")
                    {
                        foreach (List<string> value in values)
                        {
                            Message friend_message = new Message();
                            friend_message.Message_ID = int.Parse(value[0]);
                            friend_message.Message_Text = aes.Decode(value[1]);
                            friend_message.Message_Date = value[2];
                            friend_message.backGround = (Brush)Application.Current.Resources["FriendMessageColor"];
                            friend_message.borderBrush = (Brush)Application.Current.Resources["FriendMessageColor"];
                            //Выполняет указанный делегат в оснвном потоке (т.к. к Control'у я не могу обратиться из этого потока)
                            Dispatcher.Invoke(() => Message_List.Items.Add(friend_message));
                            //Если непрочитанные сообщения есть, то нужно отметить их прочитанными
                            ServerConnect.RecieveBigDataFromDB("07#", IDuser + "~" + IDFriend);
                            Dispatcher.Invoke(() => Message_List.ScrollIntoView(Message_List.Items[Message_List.Items.Count - 1]));

                        }
                    }

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                finally
                {

                }

                //Приостанавливаем поток данной функции (снижает нагрузку на БД, ОЗУ, ЦП + 1,5 сек. не страшная задержка)
                Thread.Sleep(10000);
            }
        }

        //Создание нового диалога с пользователем (12#)
        public bool CreateNewChat(int friendID, string friend_nick)
        {
            bool succesfull = false;
            try
            {
                List<List<string>> values = ServerConnect.RecieveBigDataFromDB("12#", NickName + " : " + friend_nick + "~" + IDuser + "~" + friendID + "~");
                GuID_Chat = values[0][1];
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                succesfull = true;
            }
            Update_Dialog_List();
            return succesfull;
        }

        //Поиск пользователей(08#)
        private void User_Search_Changed(object sender, RoutedEventArgs e)
        {
            if (User_Search.Text.Length > 0)
            {
                try
                {
                    //Предварительно очищаем список
                    User_List.Items.Clear();

                    List<List<string>> values = ServerConnect.RecieveBigDataFromDB("08#", User_Search.Text);

                    if (values[0][0] != "ERROR")
                    {
                        foreach (List<string> value in values)
                        {
                            //Создаём кастомизированный item для списка пользователей и добавляем ему свойства
                            User user = new User();
                            user.Name = value[1];
                            user.ID = int.Parse(value[0]);
                            user.Nickname = value[2];
                            User_List.Items.Add(user);
                            users.Add(user);
                        }

                    }
                }
                catch (Exception ex)
                {
                    //Выводим сообщения об ошибке
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    if (User_List.Items.Count == 0)
                    {
                        IDFriend = -1;
                        Friend_Nick = "null";
                    }
                }
            }
            else User_List.Items.Clear();
        }

        //Метод открытия диалога с пользователем(05#)
        public async void OpenChat(int friend_ID)
        {
            //Закрываем предыдущий диалог
            Dispatcher.Invoke(() => Message_List.Items.Clear());
            Hidden_Additional_Window();
            Dispatcher.Invoke(() => Active_Friend.Content = users_chat.Find(x => x.ID == friend_ID).Name);
            GuID_Chat = chat_list.Find(x => x.ID_Friend == friend_ID).GUID;
            IsPrivate_Chat = true;
            AES256 aes = new AES256(GuID_Chat);
            try
            {
                IDFriend = friend_ID;
                List<List<string>> values = ServerConnect.RecieveBigDataFromDB("05#", IDuser + "~" + IDFriend);
                if (values[0][0] != "ERROR")
                {
                    foreach (List<string> value in values)
                    {
                        if (int.Parse(value[3]) == IDuser)
                        {
                            //Создадим объект привязки данных и определим свойства
                            Message MSG = new Message();
                            MSG.Message_ID = int.Parse(value[0]);
                            if (value[1].Contains("$filename--")) MSG.Message_Text = aes.Decode(value[1].Replace("$filename--",""));
                            else MSG.Message_Text = aes.Decode(value[1]);
                            MSG.Message_Date = value[2];
                            MSG.borderBrush = (Brush)Application.Current.Resources["MyMessageColor"];
                            MSG.backGround = (Brush)Application.Current.Resources["MyMessageColor"];
                            Dispatcher.Invoke(() => Message_List.Items.Add(MSG));
                        }
                        if (int.Parse(value[3]) == IDFriend)
                        {
                            Message MSG = new Message();
                            MSG.Message_ID = int.Parse(value[0]);
                            if (value[1].Contains("$filename--")) MSG.Message_Text = aes.Decode(value[1].Replace("$filename--", ""));
                            else MSG.Message_Text = aes.Decode(value[1]);
                            MSG.Message_Date = value[2];
                            MSG.borderBrush = (Brush)Application.Current.Resources["BorderBrush"];
                            MSG.backGround = (Brush)Application.Current.Resources["FriendMessageColor"];
                            Dispatcher.Invoke(() => Message_List.Items.Add(MSG));
                        }
                    }
                    ServerConnect.RecieveBigDataFromDB("07#", IDuser + "~" + IDFriend);
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                if (Message_List.Items.Count != 0) Dispatcher.Invoke(() => Message_List.ScrollIntoView(Message_List.Items[Message_List.Items.Count - 1]));
                await Task.Run(() => Refresh_Chat_Async(friend_ID));
            }
        }

        //Метод отправки сообщений (10#+18#)
        private void Sending_Message()
        {
            //Чтобы не отправлялись пустые сообщения
            if ((TextBox_Message.Text.Length != 0) & GuID_Chat != "null")
            {
                AES256 aes = new AES256(GuID_Chat);

                List<List<string>> values;
                    if (IsPrivate_Chat) values = ServerConnect.RecieveBigDataFromDB("10#", IDuser + "~" + IDFriend + "~" + aes.Encode(TextBox_Message.Text) + "~");
                    else values = ServerConnect.RecieveBigDataFromDB("18#", IDChat + "~" + IDuser + "~" + aes.Encode(TextBox_Message.Text) + "~");

                    if (values[0][0] != "ERROR")
                    {
                        //Добавляем сообщение в диалог
                        //Нет возможности добавить ID для своего сообщения, т.к. его формирует БД
                        //Отправленное сообщение возможно не получится удалить, пока не перезайти в диалог
                        Message my_message = new Message();
                        my_message.Message_ID = int.Parse(values[0][0]);
                        my_message.Message_Text = TextBox_Message.Text;
                        my_message.Message_Date = DateTime.Now.ToString();
                        my_message.backGround = (Brush)Application.Current.Resources["MyMessageColor"];
                        my_message.borderBrush = (Brush)Application.Current.Resources["MyMessageColor"];
                        if (IsPrivate_Chat)
                        {
                            Message_List.Items.Add(my_message);
                            Message_List.ScrollIntoView(Message_List.Items[Message_List.Items.Count - 1]);
                        }
                        else
                        {
                            my_message.User_Name = NickName;
                            Message_Group_List.Items.Add(my_message);
                            Message_Group_List.ScrollIntoView(Message_Group_List.Items[Message_Group_List.Items.Count - 1]);
                        }
                        TextBox_Message.Text = "";
                    }
            }
        }

        //Отправка файла()
        private void Sending_Message(string filename, string filePath)
        {
            try
            {
                List<List<string>> values;
                
                //Чтобы не отправлялись пустые сообщения
                if ((TextBox_Message.Text.Length != 0) & GuID_Chat != "null")
                {
                    AES256 aes = new AES256(GuID_Chat);

                    
                    if (file_attached)
                    {
                        values = ServerConnect.RecieveBigDataFromDB("10#", IDuser + "~" + IDFriend + "~" + "$filename--" + aes.Encode(TextBox_Message.Text) + "~");

                        if (values[0][0] != "ERROR")
                        {

                            Message my_message = new Message();
                            my_message.Message_ID = int.Parse(values[0][0]);
                            my_message.Message_Text = TextBox_Message.Text;
                            my_message.Message_Date = DateTime.Now.ToString();
                            my_message.backGround = (Brush)Application.Current.Resources["FileMessageColor"];
                            my_message.borderBrush = (Brush)Application.Current.Resources["FileMessageColor"];
                            my_message.Font_Bold = FontWeight.FromOpenTypeWeight(900);
                            Message_List.Items.Add(my_message);
                            Message_List.ScrollIntoView(Message_List.Items[Message_List.Items.Count - 1]);
                        }

                        //Отправляем файл
                        Task.Run(() => Send_file(filename, filePath, int.Parse(values[0][0])));
                    }
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                TextBox_Message.Text = "";
            }
        }

        //Добавление пользователя в друзья(11#)
        private void AddToFriend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                User user = new User();
                IDFriend = int.Parse(((Button)sender).Content.ToString());
                foreach (User L in User_List.Items)
                {
                    if (L.ID == IDFriend) user = L;
                }

                List<List<string>> values = ServerConnect.RecieveBigDataFromDB("11#", IDuser + "~" + user.ID + "~" + user.Name + "~" + user.Nickname + "~");

                //После успешного выполнения команды, будет дополнен список друзей
                if (values[0][0] == "OK")
                {
                    Friend_List.Items.Add(user);
                    Notification_Text.Text = "Пользователь добавлен в контакты";
                    Pop_Up_Notification();
                }
                else if (values[0][0] == "1062")
                {
                    Notification_Text.Text = "Пользователь уже добавлен";
                    Pop_Up_Notification();
                }
                else MessageBox.Show(values[0][0]);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + "\n" + ex.Message);
            }
        }

        //Удаление сообщения из диалога(13#)
        private void DeleteMessage_Click(object sender, RoutedEventArgs e)
        {
            //Кнопка удаления сообщения, хранящее ID удаляемого сообщения
            Button message = sender as Button;

            int ID = int.Parse(message.Content.ToString());

            try
            {
                List<List<string>> values = ServerConnect.RecieveBigDataFromDB("13#", ID + "~");

                if (values[0][0] == "MESSAGE_DELETE")
                {
                    AES256 aes = new AES256(GuID_Chat);
                    List<List<string>> value = ServerConnect.RecieveBigDataFromDB("05#", IDuser + "~" + IDFriend);
                    if (value[0][0] != "ERROR")
                    {
                        Message_List.Items.Clear();
                        foreach (List<string> item in value)
                        {
                            if (int.Parse(item[3]) == IDuser)
                            {
                                //Создадим объект привязки данных и определим свойства
                                Message MSG = new Message();
                                MSG.Message_ID = int.Parse(item[0]);
                                MSG.Message_Text = aes.Decode(item[1]);
                                MSG.Message_Date = item[2];
                                MSG.borderBrush = (Brush)Application.Current.Resources["MyMessageColor"];
                                MSG.backGround = (Brush)Application.Current.Resources["MyMessageColor"];
                                Message_List.Items.Add(MSG);
                            }
                            if (int.Parse(item[3]) == IDFriend)
                            {
                                Message MSG = new Message();
                                MSG.Message_ID = int.Parse(item[0]);
                                MSG.Message_Text = aes.Decode(item[1]);
                                MSG.Message_Date = item[2];
                                MSG.borderBrush = (Brush)Application.Current.Resources["BorderBrush"];
                                MSG.backGround = (Brush)Application.Current.Resources["FriendMessageColor"];
                                Message_List.Items.Add(MSG);
                            }
                        }
                        Message_List.ScrollIntoView(Message_List.Items[Message_List.Items.Count - 1]);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        //Вскрывает вспомогательные окна(не работает)
        private void Hidden_Additional_Window()
        {
            Dispatcher.Invoke(() => SearchWindow.Visibility = Visibility.Hidden);
            Dispatcher.Invoke(() => FriendWindow.Visibility = Visibility.Hidden);
            Dispatcher.Invoke(() => ButtonBlurEffect.Visibility = Visibility.Hidden);
        }

        //Тестовый метод открытия группового чата(#17)
        private async void Open_Group_Chat(int ID_Chat)
        {
            //Скрываем окно для отображения списка в частном диалоге
            Dispatcher.Invoke(() => Message_List.Visibility = Visibility.Hidden);
            //Отображаем окно для вывода сообщений из группового чата
            Dispatcher.Invoke(() => Message_Group_List.Visibility = Visibility.Visible);

            //Очищаем список сообщений
            Dispatcher.Invoke(() => Message_Group_List.Items.Clear());

            //Запоминаем ID текущего чата
            IDChat = ID_Chat;
            IsPrivate_Chat = false;
            GuID_Chat = group_chats.Find(x => x.ID == ID_Chat).GUID;

            AES256 aes = new AES256(GuID_Chat);
            try
            {
                List<List<string>> values = ServerConnect.RecieveBigDataFromDB("17#", IDChat + "~" + IDuser);
                if (values[0][0] != "ERROR")
                {
                    foreach (List<string> value in values)
                    {
                        if (int.Parse(value[2]) == IDuser)
                        {
                            //Создадим объект привязки данных и определим свойства
                            Message MSG = new Message();
                            MSG.Message_ID = int.Parse(value[0]);
                            MSG.Message_Text = aes.Decode(value[1]);
                            MSG.Message_Date = value[3];
                            MSG.borderBrush = (Brush)Application.Current.Resources["MyMessageColor"];
                            MSG.backGround = (Brush)Application.Current.Resources["MyMessageColor"];
                            MSG.User_Name = value[4];
                            Dispatcher.Invoke(() => Message_Group_List.Items.Add(MSG));
                        }
                        else
                        {
                            Message MSG = new Message();
                            MSG.Message_ID = int.Parse(value[0]);
                            MSG.Message_Text = aes.Decode(value[1]);
                            MSG.Message_Date = value[3];
                            MSG.borderBrush = (Brush)Application.Current.Resources["BorderBrush"];
                            MSG.backGround = (Brush)Application.Current.Resources["FriendMessageColor"];
                            MSG.User_Name = value[4];
                            Dispatcher.Invoke(() => Message_Group_List.Items.Add(MSG));
                        }
                    }
                    //Здесь должно отправляться сообщение о прочтении определенным пользователем
                    //Путем добавления в таблицу записи &@ID$!
                    //ServerConnect.RecieveBigDataFromDB("07#", IDuser + "~" + IDFriend);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                //Здесь нужна отдельная функция проверки новых сообщений в чате
                //if (Message_List.Items.Count != 0) Dispatcher.Invoke(() => Message_List.ScrollIntoView(Message_List.Items[Message_List.Items.Count - 1]));
                //await Task.Run(() => Refresh_Chat_Async(friend_ID));
                Message MSG = null;
                if (Message_Group_List.Items.Count > 0) MSG = (Message)Dispatcher.Invoke(() => Message_Group_List.Items[Message_Group_List.Items.Count - 1]);
                int id = 0;
                if (MSG != null) id = MSG.Message_ID;
                await Task.Run(() => Refresh_Group_Chat(id, ID_Chat));
            }
        }

        //Обновление группового диалога(#21)
        private async void Refresh_Group_Chat(int ID_Current_Message, int idchat)
        {
            GuID_Chat = group_chats.Find(x => x.ID == idchat).GUID;
            AES256 aes = new AES256(GuID_Chat);
            while (IDChat == idchat)
            {
                try
                {
                    List<List<string>> values = await Task.Run(() => ServerConnect.RecieveBigDataFromDB("21#", idchat + "~" + ID_Current_Message + "~" + IDuser));
                    if (values[0][0] != "ERROR")
                    {
                        foreach (List<string> value in values)
                        {
                            if (int.Parse(value[2]) == IDuser)
                            {
                                //Создадим объект привязки данных и определим свойства
                                Message MSG = new Message();
                                MSG.Message_ID = int.Parse(value[0]);
                                MSG.Message_Text = aes.Decode(value[1]);
                                MSG.Message_Date = value[3];
                                MSG.borderBrush = (Brush)Application.Current.Resources["MyMessageColor"];
                                MSG.backGround = (Brush)Application.Current.Resources["MyMessageColor"];
                                MSG.User_Name = "NONAME";
                                Dispatcher.Invoke(() => Message_Group_List.Items.Add(MSG));
                                ID_Current_Message = MSG.Message_ID;
                            }
                            else
                            {
                                Message MSG = new Message();
                                MSG.Message_ID = int.Parse(value[0]);
                                MSG.Message_Text = aes.Decode(value[1]);
                                MSG.Message_Date = value[3];
                                MSG.borderBrush = (Brush)Application.Current.Resources["BorderBrush"];
                                MSG.backGround = (Brush)Application.Current.Resources["FriendMessageColor"];
                                if (users.Find(x => x.ID == int.Parse(value[2])) != null)
                                {
                                    MSG.User_Name = (users.Find(x => x.ID == int.Parse(value[2]))).Name;
                                }
                                else
                                {
                                    User U = new User();
                                    U.ID = int.Parse(value[2]);
                                    U.Name = ServerConnect.RecieveBigDataFromDB("20#", int.Parse(value[2]) + "~")[0][0];
                                    users.Add(U);
                                    MSG.User_Name = U.Name;
                                }
                                Dispatcher.Invoke(() => Message_Group_List.Items.Add(MSG));
                                ID_Current_Message = MSG.Message_ID;
                            }
                        }
                        //Здесь должно отправляться сообщение о прочтении определенным пользователем
                        //Путем добавления в таблицу записи &@ID$!
                        //ServerConnect.RecieveBigDataFromDB("07#", IDuser + "~" + IDFriend);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                Thread.Sleep(5000);
            }
        }

        //Обновление списка групповых диалогов(16#)
        private void Update_Group_Chat_List()
        {
            Chat_list.Items.Clear();
            List<List<string>> values = ServerConnect.RecieveBigDataFromDB("16#", IDuser + "~");
            if (!values[0][0].Contains("ERROR"))
            {
                foreach (List<string> value in values)
                {
                    Chat_List list = new Chat_List();
                    list.ID = int.Parse(value[0]);
                    list.Name = value[1];
                    list.GUID = value[3];
                    list.Public = true;

                    group_chats.Add(list);
                    Chat_list.Items.Add(list);
                }
            }
        }

        //Получение списка пользователей чата(#19)
        private void List_Users_Group_Chat(int IDGroupChat)
        {
            //Объявдяем первичный и вторичный флаги для отбора ID
            bool first_flag = false;
            bool second_flag = false;
            //Получаем значение списка ID
            List<List<string>> values = ServerConnect.RecieveBigDataFromDB("19#", IDGroupChat + "~");
            //Результирующий список ID
            List<int> id_user = new List<int>();

            //Переменная временного ID
            string temp_id = "";
            //Очищаем список ID
            UsersChat.Items.Clear();

            //Очищаем строку ID
            for (int i = 0; i < values[0][0].Length; i++)
            {
                if (values[0][0][i] == '&')
                {
                    first_flag = true;
                    continue;
                }
                if (values[0][0][i] == '$' && values[0][0][i + 1] == '!')
                {
                    second_flag = false;
                    first_flag = false;
                    i++;
                    id_user.Add(int.Parse(temp_id));
                    temp_id = "";
                    continue;
                }
                if (first_flag && !second_flag)
                {
                    temp_id += values[0][0][i];
                    continue;
                }
                MessageBox.Show(values[0][0][i] + " ");
            }

            foreach (int i in id_user)
            {
                MessageBox.Show(i + " ");
                try
                {
                    User U = new User();
                    if (users.Find(x => x.ID == i) != null)
                    {
                        UsersChat.Items.Add(users.Find(x => x.ID == i));
                    }
                    else
                    {
                        U.ID = i;
                        U.Name = ServerConnect.RecieveBigDataFromDB("20#", i + "~")[0][0];
                        users.Add(U);
                        UsersChat.Items.Add(U);
                    }
                }
                catch (ArgumentNullException ANE)
                {
                    MessageBox.Show(ANE.Message);
                }
            }


            //А как блять имена получить???

        }





        /*Все функции, находящиеся снизу - события
         * Все функции сверху - собственные методы, для осуществления деёствия на форме
         */

        //Закрытие основного окна
        private void ChatPage_Closing(object sender, EventArgs e)
        {
            IDuser = -1;
            IDFriend = -1;
            Friend_Nick = "null";
            NickName = "null";
            IDChat = -1;
            MainWindow main = new MainWindow(true);
            main.Show();
        }

        //Открытие своего профиля(доработать)
        private void Open_Profile(object sender, EventArgs e)
        {

        }

        //Отпрвака сообщения
        private void Send_Message_Click(object sender, RoutedEventArgs e)
        {
            Sending_Message();
        }

        //Открытие диалога
        private async void Chat_list_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Chat_List item = (Chat_List)Dispatcher.Invoke(() => Chat_list.SelectedItem);
            if (!item.Public)
            {
                Dispatcher.Invoke(() => Message_Group_List.Visibility = Visibility.Hidden);
                Dispatcher.Invoke(() => Message_List.Visibility = Visibility.Visible);
                Friend_Nick = item.Name;
                GuID_Chat = item.GUID;
                if (IDFriend == item.ID_Friend)
                {
                    Dispatcher.Invoke(() => Message_List.Items.Clear());
                    IDFriend = -1;
                    GuID_Chat = "null";
                }
                else
                {
                    IDFriend = -1;
                    await Task.Run(() => OpenChat(item.ID_Friend));
                }
            }
            else
            {
                Dispatcher.Invoke(() => Message_Group_List.Visibility = Visibility.Visible);
                Dispatcher.Invoke(() => Message_List.Visibility = Visibility.Hidden);
                GuID_Chat = item.GUID;
                await Task.Run(() => Open_Group_Chat(item.ID));
            }
        }

        //Удаление пользователя из друзей
        private void DeleteFromFriend_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            try
            {
                List<List<string>> values = ServerConnect.RecieveBigDataFromDB("14#", IDuser + "~" + btn.Content.ToString() + '~');
                if (values[0][0] == "USER_DELETED")
                {
                    //Обновляем список друзей
                    Update_Friend_List();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(ex.Message);
            }
        }

        private async void Writing_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;

            //Очищаем список сообщений
            Message_List.Items.Clear();

            //Стираем все данные о собеседнике
            IDFriend = int.Parse(Dispatcher.Invoke(() => btn.Content.ToString()));
            Friend_Nick = ((User)users.Find(x => x.ID == IDFriend)).Name;
            if ((users_chat.Find(x => x.ID == IDFriend)) == null) CreateNewChat(IDFriend, Friend_Nick);
            await Task.Run(() => OpenChat(IDFriend));
        }

        //Развернуть или свернуть меню
        private void Show_Hidden_Menu(object sender, RoutedEventArgs e)
        {
            try
            {
                double width = Convert.ToDouble(ProfileMenu.Width.ToString());
                if (width < 200)
                {
                    var anim = new DoubleAnimation(200, (Duration)TimeSpan.FromSeconds(0.2));
                    ProfileMenu.BeginAnimation(ContentControl.WidthProperty, anim);
                    ButtonBlurEffect.Visibility = Visibility.Visible;
                    Show_Menu.Width = 200;
                }
                //Иначе свернуть
                if (width > 0)
                {
                    var anim = new DoubleAnimation(0, (Duration)TimeSpan.FromSeconds(0.2));
                    ProfileMenu.BeginAnimation(ContentControl.WidthProperty, anim);
                    ButtonBlurEffect.Visibility = Visibility.Hidden;
                    Show_Menu.Width = 30;
                }
            }
            catch (Exception ex)
            {

            }
        }

        //Открыть окно поиска пользователей
        private void Show_Search_Menu(object sender, RoutedEventArgs e)
        {
            if (SearchWindow.Visibility == Visibility.Hidden)
            {
                ButtonBlurEffect.Visibility = Visibility.Visible;
                SearchWindow.Visibility = Visibility.Visible;
                FriendWindow.Visibility = Visibility.Hidden;
                UsersFromGroupChat.Visibility = Visibility.Hidden;
            }
            else
            {
                ButtonBlurEffect.Visibility = Visibility.Hidden;
                SearchWindow.Visibility = Visibility.Hidden;
            }
        }

        //Открыть/закрыть окно списка друзей
        private void FriendList_Show_Hidden(object sender, RoutedEventArgs e)
        {
            //Если открыто, то скрыть
            if (FriendWindow.Visibility == Visibility.Visible)
            {
                ButtonBlurEffect.Visibility = Visibility.Hidden;
                FriendWindow.Visibility = Visibility.Hidden;
            }
            //Если закрыто, то открыть
            else
            {
                ButtonBlurEffect.Visibility = Visibility.Visible;
                FriendWindow.Visibility = Visibility.Visible;
                SearchWindow.Visibility = Visibility.Hidden;
                UsersFromGroupChat.Visibility = Visibility.Hidden;
            }
        }

        //Отправка сообщения по нажатию клавиши Enter
        private void Send_Message_Enter(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter) Sending_Message();
        }

        //Метод всплывающего уведомления
        private bool Expanded = false;
        private void Pop_Up_Notification()
        {
            if (Expanded)
            {
                var anim = new DoubleAnimation(0, (Duration)TimeSpan.FromSeconds(0.5));
                anim.Completed += (s, _) => Expanded = false;
                Notification_Search_Window.BeginAnimation(ContentControl.HeightProperty, anim);
            }
            else
            {
                var anim = new DoubleAnimation(20, (Duration)TimeSpan.FromSeconds(0.5));
                anim.Completed += (s, _) => Expanded = true;
                Notification_Search_Window.BeginAnimation(ContentControl.HeightProperty, anim);
            }
        }
        //Убирает уведомление при смене фокуса
        private void UIElement_LostFocus(object sender, RoutedEventArgs e)
        {
            if (Notification_Search_Window.Height > 0) Pop_Up_Notification();
        }

        //Скрывает доп. окна
        private void Hidden_Additional_Window(object sender, RoutedEventArgs e)
        {
            UsersFromGroupChat.Visibility = Visibility.Hidden;
            FriendWindow.Visibility = Visibility.Hidden;
            SearchWindow.Visibility = Visibility.Hidden;
            ButtonBlurEffect.Visibility = Visibility.Hidden;
            ChangePassword.Visibility = Visibility.Hidden;
            Show_Hidden_Menu(sender, e);
        }

        //Выйти из аккаунта пользователя
        private void Exit_Account(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        //Открытия списка приватных диалогов
        private void PrivateChat_Click(object sender, RoutedEventArgs e)
        {
            Update_Dialog_List();
        }

        private void Create_Group_Chat(object sender, RoutedEventArgs e)
        {
            try
            {
                List<List<string>> values = ServerConnect.RecieveBigDataFromDB("15#", NickName + "~" + IDuser + "~");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            //Тут должен запускаться метод обновления групповых диалогов
        }

        private void GroupChat_Click(object sender, RoutedEventArgs e)
        {
            Update_Group_Chat_List();
        }

        private void Open_Users_List(object sender, RoutedEventArgs e)
        {
            //Если открыто, то скрыть
            if (UsersFromGroupChat.Visibility == Visibility.Visible)
            {
                ButtonBlurEffect.Visibility = Visibility.Hidden;
                UsersFromGroupChat.Visibility = Visibility.Hidden;
            }
            //Если закрыто, то открыть
            else
            {
                if (IDChat > 0 && !IsPrivate_Chat) List_Users_Group_Chat(IDChat);
                ButtonBlurEffect.Visibility = Visibility.Visible;
                UsersFromGroupChat.Visibility = Visibility.Visible;
                SearchWindow.Visibility = Visibility.Hidden;
                FriendWindow.Visibility = Visibility.Hidden;
                MessageBox.Show(UsersChat.Items.Count + " ");
            }
        }

        private bool exp = true;
        private void Open_Users_List_For_Adding(object sender, RoutedEventArgs e)
        {
            if (exp)
            {
                ULGC.Width = new GridLength(200);
                exp = false;
            }
            else
            {
                ULGC.Width = new GridLength(0);
                exp = true;
            }
            foreach (User c in Friend_List.Items)
            {
                if (UsersChat.Items.IndexOf(c) == -1)
                {
                    ContactForChat.Items.Add(c);
                }
            }

        }

        //Добавить пользователя в группу
        private void AddInGroupChat(object sender, RoutedEventArgs e)
        {
            int id = int.Parse(((Button)sender).Content.ToString());
            foreach (User U in ContactForChat.Items)
            {
                if (id == U.ID)
                {
                    UsersChat.Items.Add(U);
                    ContactForChat.Items.Remove(U);
                    break;
                }
            }
            List<List<string>> values = ServerConnect.RecieveBigDataFromDB("22#", id + "~" + IDChat + "~");
        }

        //Показываем меню для смены пароля
        private void Show_Change_Password(object sender, RoutedEventArgs e)
        {
            ChangePassword.Visibility = Visibility.Visible;
        }

        //Сохранить новый пароль
        private void Save_Password_Click(object sender, RoutedEventArgs e)
        {
            if (New_Password.Password == RepNew_Password.Password)
            {
                MD5 md5 = MD5.Create();
                try
                {
                    List<List<string>> values = ServerConnect.RecieveBigDataFromDB("23#", IDuser + "~" + Encoding.UTF8.GetString(md5.ComputeHash(Encoding.UTF8.GetBytes(Old_Password.Password))) + "~" + Encoding.UTF8.GetString(md5.ComputeHash(Encoding.UTF8.GetBytes(New_Password.Password))) + "~");

                    if (values[0][0] == "OK")
                    {
                        MessageBox.Show("Пароль успешно изменен!");
                        ChangePassword.Visibility = Visibility.Hidden;
                    }
                    else MessageBox.Show("Старый пароль неверный!");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
            else
            {
                MessageBox.Show("Новые пароли не совпадают!");
            }
        }

        //Прикрепить файл
        private void Attach_File_Click(object sender, RoutedEventArgs e)
        {
            //Declare a string to store the short file name
            string short_file_name = "";
            //Create a new OpenFileDialog object
            Microsoft.Win32.OpenFileDialog file_dialog = new Microsoft.Win32.OpenFileDialog();
            //If the user selects a file
            if (file_dialog.ShowDialog() == true)
            {
                //Store the file path
                file_Path = file_dialog.FileName;
                //Store the short file name
                short_file_name = Path.GetFileName(file_Path);
            }
            file_attached = true;
            TextBox_Message.Text = short_file_name;
            Sending_Message(short_file_name, file_Path);
        }

        //Rewritten code with comments

        private async void Send_file(string short_file_name, string file_Path,int id_messages)
        {
            //Send the file to the server
            ServerConnect.Send_File("1", short_file_name, file_Path, id_messages);
        }
    }
}
