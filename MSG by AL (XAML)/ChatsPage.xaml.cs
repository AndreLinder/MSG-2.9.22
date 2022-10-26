using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Data.Common;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Threading;
using MySql.Data.MySqlClient;
using ConnectionDB;
using MSG_by_AL__XAML_.Resource;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Windows.Media.Media3D;

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
        public static string Name = "null";
        //ID авторизованного пользователя
        public static int IDuser = -1;
        //GuID авторизованного пользователя
        public string GuID = "null";


        //ID, guid и никнейм собеседника
        public static int IDFriend = -1;
        public static string Friend_Nick="null";
        public string GuIDFriend = "null";

        //ID активного диалога и количество сообщений в нём
        public static int IDChat = -1;
        public static string GuID_Chat = "null";
        public static int MessageCount = -1;


        //Создание объекта подключения к БД
        MySqlConnection connection = DBUtils.GetDBConnection();

        public ChatsPage(int ID, string nick, string guID)
        {
            IDuser = ID;
            NickName = nick;
            InitializeComponent();
            Clear_List();
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
            while(IDuser > 0)
            {
                try
                {
                    List<List<string>>  list_notification = ServerConnect.RecieveNotification(IDuser);
                    Dispatcher.Invoke(()=>Demon_Connect.Text = "Demon connected");
                    if (list_notification[0][0] != "NONE")
                    {
                        foreach (List<string> value in list_notification)
                        {
                            AES256 aes = new AES256(value[2]);
                            Dispatcher.Invoke(() => User_Name_Notification.Text = value[0]);
                            Dispatcher.Invoke(() => Text_Message_Notification.Text = aes.Decode(value[1]));
                            Dispatcher.Invoke(() => SideNotificationShow());
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
                    Dispatcher.Invoke(()=>Demon_Connect.Text = "Demon disconnected");
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
                    list.ID_Friend = int.Parse(value[3]);
                    Chat_list.Items.Add(list);
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
                    Chat_List user = new Chat_List();
                    user.ID_Friend = int.Parse(value[0]);
                    user.Name = value[1];
                    user.Nickname = value[2];
                    Friend_List.Items.Add(user);
                }
            }
        }

        //Выгрузка сообщений(нужен ли?)
        public void Loading_Messages(string SQL_Command)
        {
            try
            {
                //Открываем соединение
                connection.Open();

                //Запрос на выгрузку сообщений (максимум 100)
                string sql_cmd = SQL_Command;

                //Команда запроса
                MySqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = sql_cmd;

                //Добавляем параметры
                MySqlParameter myID = new MySqlParameter("@MYID", MySqlDbType.Int32);
                myID.Value = IDuser;
                cmd.Parameters.Add(myID);

                MySqlParameter friendID = new MySqlParameter("@IDFRIEND", MySqlDbType.Int32);
                friendID.Value = IDFriend;
                cmd.Parameters.Add(friendID);

                MySqlParameter count_messages = new MySqlParameter("@COUNT", MySqlDbType.Int32);
                count_messages.Value = MessageCount;
                cmd.Parameters.Add(count_messages);

                //Здесь прописывается логика отображения сообщений в окне дилога
                //У "моих" сообщений и сообщений собеседника будет различное цветовое оформление 
                using (DbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            //
                            if(int.Parse(reader.GetString(3)) == IDuser)
                            {
                                //Создадим объект привязки данных и определим свойства
                                Message MSG = new Message();
                                MSG.Message_ID = int.Parse(reader.GetString(0));
                                MSG.Message_Text = reader.GetString(1);
                                MSG.Message_Date = reader.GetString(2);
                                MSG.borderBrush = (Brush)Application.Current.Resources["MyMessageColor"];
                                MSG.backGround = (Brush)Application.Current.Resources["MyMessageColor"];
                                Message_List.Items.Add(MSG);
                            }
                            if (int.Parse(reader.GetString(3)) == IDFriend)
                            {
                                Message MSG = new Message();
                                MSG.Message_ID = int.Parse(reader.GetString(0));
                                MSG.Message_Text = reader.GetString(1);
                                MSG.Message_Date = reader.GetString(2);
                                MSG.borderBrush = (Brush)Application.Current.Resources["BorderBrush"];
                                MSG.backGround = (Brush)Application.Current.Resources["FriendMessageColor"];
                                Message_List.Items.Add(MSG);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                //Закрываем соединение
                connection.Close();
            }
        }

        public bool Exp = false;
        public async void SideNotificationShow()
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
        public void Refresh_Chat_Async(int id_f)
        {
            AES256 aes = new AES256(GuID_Chat);
            while (IDFriend == id_f)
            {
                try
                {
                    List<List<string>> values = ServerConnect.RecieveBigDataFromDB("06#",IDuser + "~" + IDFriend);
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
                Thread.Sleep(4000);
            }
        }

        //Создание нового диалога с пользователем (доработать)
        public bool CreateNewChat(int friendID, string friend_nick)
        {
            bool succesfull = false;
            try
            {
                List<List<string>> values = ServerConnect.RecieveBigDataFromDB("12#", NickName + " : " + friend_nick + "~" + IDuser + "~" + friendID + "~");
                if (values[0][0] != "OK") MessageBox.Show(values[0][0]);
            }
            catch (MySqlException ex)
            {
                MessageBox.Show(ex.ToString());
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
                        Chat_List user = new Chat_List();
                        IDFriend = int.Parse(value[0]);
                        user.Name = value[1];
                        user.ID_Friend = IDFriend;
                        user.Nickname = value[2];
                        User_List.Items.Add(user);
                        Friend_Nick = value[2];
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

        //Метод открытия диалога с пользователем(05#)
        public async void OpenChat(int friend_ID)
        {
            //Закрываем предыдущий диалог
            Dispatcher.Invoke(()=>Message_List.Items.Clear());
            AES256 aes = new AES256(GuID_Chat);
            try
            {
                IDFriend = friend_ID;
                List<List<string>> values = ServerConnect.RecieveBigDataFromDB("05#",IDuser + "~" + IDFriend);
                if (values[0][0] != "ERROR")
                {
                    foreach (List<string> value in values)
                    {
                        if (int.Parse(value[3]) == IDuser)
                        {
                            //Создадим объект привязки данных и определим свойства
                            Message MSG = new Message();
                            MSG.Message_ID = int.Parse(value[0]);
                            MSG.Message_Text = aes.Decode(value[1]);
                            MSG.Message_Date = value[2];
                            MSG.borderBrush = (Brush)Application.Current.Resources["MyMessageColor"];
                            MSG.backGround = (Brush)Application.Current.Resources["MyMessageColor"];
                            Dispatcher.Invoke(() => Message_List.Items.Add(MSG));
                        }
                        if (int.Parse(value[3]) == IDFriend)
                        {
                            Message MSG = new Message();
                            MSG.Message_ID = int.Parse(value[0]);
                            MSG.Message_Text = aes.Decode(value[1]);
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
                if(Message_List.Items.Count != 0) Dispatcher.Invoke(() => Message_List.ScrollIntoView(Message_List.Items[Message_List.Items.Count - 1]));
                await Task.Run(() => Refresh_Chat_Async(friend_ID));
            }
        }

        //Метод отправки сообщений (10#)
        private void Sending_Message()
        {
            //Чтобы не отправлялись пустые сообщения
            if (TextBox_Message.Text.Length != 0)
            {
                AES256 aes = new AES256(GuID_Chat);

                List<List<string>> values = ServerConnect.RecieveBigDataFromDB("10#", IDuser + "~" + IDFriend + "~" + aes.Encode(TextBox_Message.Text) + "~");

                if (values[0][0] == "SEND")
                {
                    //Добавляем сообщение в диалог
                    //Нет возможности добавить ID для своего сообщения, т.к. его формирует БД
                    //Отправленное сообщение возможно не получится удалить, пока не перезайти в диалог
                    Message my_message = new Message();
                    my_message.Message_ID = -1;
                    my_message.Message_Text = TextBox_Message.Text;
                    my_message.Message_Date = DateTime.Now.ToString();
                    my_message.backGround = (Brush)Application.Current.Resources["MyMessageColor"];
                    my_message.borderBrush = (Brush)Application.Current.Resources["MyMessageColor"];
                    Message_List.Items.Add(my_message);
                    Message_List.ScrollIntoView(Message_List.Items[Message_List.Items.Count - 1]);
                    TextBox_Message.Text = "";
                }
            }
        }

        //Добавление пользователя в друзья(11#)
        private void AddToFriend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Chat_List user = (Chat_List)User_List.Items[0];

                List<List<string>> values = ServerConnect.RecieveBigDataFromDB("11#", IDuser + "~" + user.ID_Friend + "~" + user.Name + "~" + user.Nickname + "~");

                //После успешного выполнения команды, будет дополнен список друзей
                if (values[0][0] == "ОК") Friend_List.Items.Add(user);
                else MessageBox.Show(values[0][0]);


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
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
            MainWindow main = new MainWindow();
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

        //Удаление пользователя из друзей
        private void DeleteFromFriend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Button btn = sender as Button;

                //Открываем соединение
                connection.Open();

                //Получаем объект нашего пользователя (произойдёт только при выборе элемента сначала )
                //значит нужно кнопку сделать недоступной, пока не выбирут его
                Chat_List user = (Chat_List)Friend_List.SelectedItem;

                //Строка запроса на удаление пользователя из друзей
                string sql_cmd = "DELETE FROM server_chats.friend WHERE ID_User = @MYID AND ID_Friend = @IDFRIEND";

                //Создаём команду запроса
                MySqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = sql_cmd;

                //Добавляем параметры
                MySqlParameter myID = new MySqlParameter("@MYID", MySqlDbType.Int32);
                myID.Value = IDuser;
                cmd.Parameters.Add(myID);

                MySqlParameter friendID = new MySqlParameter("@IDFRIEND", MySqlDbType.Int32);
                friendID.Value = int.Parse(btn.Content.ToString());
                cmd.Parameters.Add(friendID);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                //Закрываем соединение
                connection.Close();

                //Обновляем список друзей
                Update_Friend_List();
            }
        }

        private async void Writing_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;

            //Очищаем список сообщений
            Message_List.Items.Clear();

            //Стираем все данные о собеседнике
            IDFriend = int.Parse(Dispatcher.Invoke(() => btn.Content.ToString()));
            //Friend_Nick = GetUserName(IDFriend);
            if(CreateNewChat(IDFriend, Friend_Nick)!=true) await Task.Run(() => OpenChat(IDFriend));
        }

        //Удаление сообщения из диалога
        private void DeleteMessage_Click(object sender, RoutedEventArgs e)
        {
            //Кнопка удаления сообщения, хранящее ID удаляемого сообщения
            Button message = sender as Button;
            try
            {
                //Открываем соединение
                connection.Open();

                //Строка запроса
                string sql_cmd = "DELETE FROM server_chats.messages WHERE ID = @IDMESSAGE;";

                //Команда запроса
                MySqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = sql_cmd;

                //Добавляем параметры
                MySqlParameter messageID = new MySqlParameter("@IDMESSAGE", MySqlDbType.Int32);
                messageID.Value = message.Content.ToString();
                cmd.Parameters.Add(messageID);

                //Выполняем запрос
                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                //Закрываем соединение
                connection.Close();
                //Вызываем функцию загрузки сообщений
                Message_List.Items.Clear();
                if (MessageCount <= 1000) Loading_Messages("SELECT * FROM server_chats.messages WHERE (ID_Sender = @MYID AND ID_Reciever = @IDFRIEND) OR (ID_Sender = @IDFRIEND AND ID_Reciever = @MYID)");
                else Loading_Messages("SELECT * FROM server_chats.messages WHERE (ID_Sender = @MYID AND ID_Reciever = @IDFRIEND) OR (ID_Sender = @IDFRIEND AND ID_Reciever = @MYID) LIMIT @COUNT-1000,@COUNT;");
                Message_List.ScrollIntoView(Message_List.Items[Message_List.Items.Count - 1]);

            }
        }

        //Развернуть или свернуть меню
        private void Show_Hidden_Menu(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Button btn = (Button)sender;
            try
            {
                
                double height = Convert.ToDouble(btn.Height.ToString());
                if (height < 33)
                {
                    var anim = new DoubleAnimation(33, (Duration)TimeSpan.FromSeconds(0.3));
                    btn.BeginAnimation(ContentControl.HeightProperty, anim);
                }
                //Иначе свернуть
                if (height > 20)
                {
                    var anim = new DoubleAnimation(20, (Duration)TimeSpan.FromSeconds(0.3));
                    btn.BeginAnimation(ContentControl.HeightProperty, anim);
                }
            }
            catch (Exception ex)
            {
                TextBox_Message.Text = btn.Height.ToString();
            }
        }

        //Открыть окно поиска пользователей
        private void Show_Search_Menu(object sender, RoutedEventArgs e)
        {
            if(SearchWindow.Visibility == Visibility.Hidden)
            {
                ButtonBlurEffect.Visibility = Visibility.Visible;
                SearchWindow.Visibility = Visibility.Visible;
                FriendWindow.Visibility = Visibility.Hidden;
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
            if(FriendWindow.Visibility == Visibility.Visible)
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

        //Выйти из аккаунта пользователя
        private void Exit_Account(object sender, RoutedEventArgs e) 
        {
            this.Close();
        }

        private void Show_Hidden_Menu(object sender, RoutedEventArgs e)
        {
            //int height = int.Parse((Convert.ToString(MenuGrid.Height)));
            ////Если свернуто, развернуть
            //if (height == 20)
            //{
            //    var anim = new DoubleAnimation(20, (Duration)TimeSpan.FromSeconds(0.5));
            //    anim.Completed += (s, _) => Menu_Exp = false;
            //    Menu.BeginAnimation(ContentControl.HeightProperty, anim);
            //    //    BigGrid.RowDefinitions[0].Height = new GridLength(40);
            //    //    MenuShow.Height = 40;
            //}
            ////Иначе свернуть
            //if (height == 40)
            //{
            //    var anim = new DoubleAnimation(40, (Duration)TimeSpan.FromSeconds(0.5));
            //    anim.Completed += (s, _) => Menu_Exp = false;
            //    Menu.BeginAnimation(ContentControl.HeightProperty, anim);
            //    //BigGrid.RowDefinitions[0].Height = new GridLength(20);
            //    //MenuShow.Height = 20;
            //}
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
