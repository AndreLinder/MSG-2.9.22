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

namespace MSG_by_AL__XAML_
{
    /// <summary>
    /// Логика взаимодействия для ChatsPage.xaml
    /// </summary>
    public partial class ChatsPage : Window
    {
        //Логин авторизованного пользователя
        public static string NickName = "null";
        //ID авторизованного пользователя
        public static int IDuser = -1;

        //ID и никнейм собеседника
        public static int IDFriend = -1;
        public static string Friend_Nick="null";

        //ID активного диалога и количество сообщений в нём
        public static int IDChat = -1;
        public static int MessageCount = -1;


        //Создание объекта подключения к БД
        MySqlConnection connection = DBUtils.GetDBConnection();
        MySqlConnection connection_async = DBUtils.GetDBConnection();

        public ChatsPage(int ID)
        {
            IDuser = ID;
            InitializeComponent();
            Clear_List();
            Update_Dialog_List();
            Update_Friend_List();
            Task.Run(() => Search_Unread_Messages());
        }

        //Очистка всех списков (сообщений, чатов, пользователей)
        public void Clear_List()
        {
            User_List.Items.Clear();
            Message_List.Items.Clear();
            Chat_list.Items.Clear();
        }

        //Метод обновления списка чатов(#03)
        public void Update_Dialog_List()
        {
            //Очищаем старый список чатов, если такие были
            Chat_list.Items.Clear();

            //Получаем список чатов от сервера в виде "Списка списков строк"
            List<List<string>> values = ServerConnect.RecieveBigDataFromDB("03#", IDuser + "~");

            foreach(List<string> value in values)
            {
                Chat_List list = new Chat_List();
                list.ID = int.Parse(value[0]);
                list.Name = value[1];
                list.ID_Friend = int.Parse(value[2]);
                Chat_list.Items.Add(list);
            }
        }

        //Метод получения списка друзей (№04)
        public void Update_Friend_List()
        {
            //Очищаем старый список друзей, если он был до этого
            Friend_List.Items.Clear();

            //Делаем запрос на сервер и получаем список друзей
            List<List<string>> values = ServerConnect.RecieveBigDataFromDB("04#", IDuser + "");

            foreach(List<string> value in values)
            {
                Chat_List user = new Chat_List();
                user.ID_Friend = int.Parse(value[0]);
                user.Name = value[1];
                Friend_List.Items.Add(user);
            }
        }

        //Выгрузка сообщений
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
                Mark_Read();
            }
        }

        //Отметка сообщений прочитанными (по идее не нужен уже)
        public void Mark_Read()
        {
            try
            {
                //Открываем соединение
                connection.Open();

                //Запускаем запрос на отметку сообщений, как прочитанные
                string sql_cmd = "UPDATE server_chats.messages SET Visible_Message = 1 WHERE (ID_Reciever=@MYID AND ID_Sender = @FRIENDID AND Visible_Message = 0) LIMIT 1000";

                //Создаём команду запроса
                MySqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = sql_cmd;

                //Добавляем параметры в запрос
                MySqlParameter myID = new MySqlParameter("@MYID", MySqlDbType.Int32);
                myID.Value = IDuser;
                cmd.Parameters.Add(myID);

                MySqlParameter friendID = new MySqlParameter("@FRIENDID", MySqlDbType.Int32);
                friendID.Value = IDFriend;
                cmd.Parameters.Add(friendID);

                //Осуществляем запрос
                cmd.ExecuteNonQuery();
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                //Закрыавем соединение 
                connection.Close();
            }
        }

        //Поиск непрочитанных сообщений в БД и открытие уведомление о них
        public async void Search_Unread_Messages()
        {
            MySqlConnection conn = DBUtils.GetDBConnection();
            List<int> id_messages = new List<int>();
            while (true)
            {
                try
                {
                    //Открываем соединение
                    await conn.OpenAsync();

                    //Строка запроса в БД
                    string sql_cmd = "SELECT server_chats.messages.ID,server_chats.messages.Date_Message,server_chats.messages.Text_Message, server_chats.users.User_Name, server_chats.messages.ID_Sender FROM server_chats.messages LEFT JOIN server_chats.users ON server_chats.messages.ID_Sender = server_chats.users.ID WHERE (server_chats.messages.ID_Reciever = @ID AND server_chats.messages.Visible_Message = 0 AND server_chats.messages.visible_notification = 0);";

                    //Создаем команду для запроса
                    MySqlCommand cmd = conn.CreateCommand();
                    cmd.CommandText = sql_cmd;

                    //Добавляем параметры
                    MySqlParameter id = new MySqlParameter("@ID", MySqlDbType.Int32);
                    id.Value = IDuser;
                    cmd.Parameters.Add(id);

                    //Начинаем считывать данные
                    using (DbDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                Dispatcher.Invoke(() => User_Name_Notification.Text = reader.GetString(3));
                                Dispatcher.Invoke(() => Text_Message_Notification.Text = reader.GetString(2));
                                Dispatcher.Invoke(() => SideNotificationShow());
                                id_messages.Add(int.Parse(reader.GetString(0)));
                                System.Threading.Thread.Sleep(4000);
                                Dispatcher.Invoke(() => SideNotificationShow());
                                System.Threading.Thread.Sleep(1500);
                            }
                        }
                    }

                    //Отмечаем непрочитанные сообщения, чтобы не повторялись в оповещении 
                    foreach (int i in id_messages)
                    {
                        cmd.Parameters.Clear();
                        //Запрос на обновление
                        sql_cmd = "UPDATE server_chats.messages SET server_chats.messages.visible_notification = 1 WHERE ID = @IDMESSAGES;";
                        cmd.CommandText = sql_cmd;
                        MySqlParameter id_msg = new MySqlParameter("IDMESSAGES", MySqlDbType.Int32);
                        id_msg.Value = i;
                        cmd.Parameters.Add(id_msg);
                        cmd.ExecuteNonQuery();
                    }
                    id_messages.Clear();

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                finally
                {
                    //Закрываем соединение
                    await conn.CloseAsync();
                    System.Threading.Thread.Sleep(5000);
                }
            }
        }

        public bool Exp = false;
        private void SideNotificationShow()
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

        //Запуск асинхронной операции обновления текущего диалога
        public void Refresh_Chat_Async()
        {
            while (IDFriend != -1)
            {
                try
                {
                    List<List<string>> values = ServerConnect.RecieveBigDataFromDB("06#",IDuser + "~" + IDFriend);
                    foreach(List<string> value in values)
                    {

                        if (value[0] != "ERROR")
                        {
                            Message friend_message = new Message();
                            friend_message.Message_ID = int.Parse(value[0]);
                            friend_message.Message_Text = value[1];
                            friend_message.Message_Date = value[2];
                            friend_message.backGround = (Brush)Application.Current.Resources["FriendMessageColor"];
                            friend_message.borderBrush = (Brush)Application.Current.Resources["FriendMessageColor"];
                            //Выполняет указанный делегат в оснвном потоке (т.к. к Control'у я не могу обратиться из этого потока)
                            Dispatcher.Invoke(()=>Message_List.Items.Add(friend_message));
                            //Если непрочитанные сообщения есть, то нужно отметить их прочитанными
                            ServerConnect.RecieveBigDataFromDB("07#", IDuser + "~" + IDFriend);
                        }
                        Dispatcher.Invoke(()=>Message_List.ScrollIntoView(Message_List.Items[Message_List.Items.Count - 1]));
                        
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
                System.Threading.Thread.Sleep(5000);
            }
        }

        //Создание нового диалога с пользователем
        public bool CreateNewChat(int friendID, string friend_nick)
        {
            bool succesfull = false;
            try
            {
                //Открываем соединение
                connection.Open();

                //Строка запроса
                string sql_cmd = "INSERT INTO server_chats.chats (Chat_Name, ID_User_1, ID_User_2) VALUES (@CHATNAME,@IDUSER1, @IDUSER2)";

                //Команда запроса
                MySqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = sql_cmd;

                //Добавляем параметры в наш запрос
                MySqlParameter name_parameter = new MySqlParameter("@CHATNAME", MySqlDbType.VarChar);
                name_parameter.Value = Convert.ToString(NickName + " to " + friend_nick);
                cmd.Parameters.Add(name_parameter);

                MySqlParameter id1 = new MySqlParameter("@IDUSER1", MySqlDbType.Int32);
                id1.Value = IDuser;
                cmd.Parameters.Add(id1);

                MySqlParameter id2 = new MySqlParameter("@IDUSER2", MySqlDbType.Int32);
                id2.Value = friendID;
                cmd.Parameters.Add(id2);

                cmd.ExecuteNonQuery();
            }
            catch (MySqlException ex)
            {
                if (ex.Number == 1062)
                {
                    connection.Close();
                    return false;
                }
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                //Закрываем соединение
                connection.Close();
                succesfull = true;
            }
            Update_Dialog_List();
            return succesfull;
        }

        //Метод открытия диалога с пользователем
        public async void OpenChat(int friend_ID)
        {
            //Закрываем предыдущий диалог
            Dispatcher.Invoke(()=>Message_List.Items.Clear());

            try
            {
                    IDFriend = friend_ID;
                    Dispatcher.Invoke(()=>Close_Dialog.Visibility = Visibility.Visible);
                    Dispatcher.Invoke(()=>MySlider.Visibility = Visibility.Visible);
                    Dispatcher.Invoke(()=>Name_Friend.Visibility = Visibility.Visible);

                List<List<string>> values = ServerConnect.RecieveBigDataFromDB("05#",IDuser + "~" + IDFriend);

                foreach(List<string> value in values)
                {
                    if (int.Parse(value[3]) == IDuser)
                    {
                        //Создадим объект привязки данных и определим свойства
                        Message MSG = new Message();
                        MSG.Message_ID = int.Parse(value[0]);
                        MSG.Message_Text = value[1];
                        MSG.Message_Date = value[2];
                        MSG.borderBrush = (Brush)Application.Current.Resources["MyMessageColor"];
                        MSG.backGround = (Brush)Application.Current.Resources["MyMessageColor"];
                        Dispatcher.Invoke(() => Message_List.Items.Add(MSG));
                    }
                    if (int.Parse(value[3]) == IDFriend)
                    {
                        Message MSG = new Message();
                        MSG.Message_ID = int.Parse(value[0]);
                        MSG.Message_Text = value[1];
                        MSG.Message_Date = value[2];
                        MSG.borderBrush = (Brush)Application.Current.Resources["BorderBrush"];
                        MSG.backGround = (Brush)Application.Current.Resources["FriendMessageColor"];
                        Dispatcher.Invoke(() => Message_List.Items.Add(MSG));
                    }
                }


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                await Task.Run(() => Refresh_Chat_Async());
            }
        }

        //Функция возвращает имя чата по ID его пользователей
        public string GetChatName(int friend_ID)
        {
            //Имя чата
            string NAME = "null";

            //Объект для создания подключения к БД
            MySqlConnection conn = DBUtils.GetDBConnection();

            try
            {    
                //Открываем соединение
                conn.Open();

                //Строка запроса
                string sql_cmd = "SELECT server_chats.chats.Chat_Name FROM server_chats.chats WHERE (ID_User_1 = @MYID AND ID_User_2 = @IDFRIEND) OR (ID_User_1 = @IDFRIEND AND ID_User_2 = @MYID);";

                //Команда запроса
                MySqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = sql_cmd;

                //Добавляем параметры
                MySqlParameter myID = new MySqlParameter("@MYID", MySqlDbType.Int32);
                myID.Value = IDuser;
                cmd.Parameters.Add(myID);

                MySqlParameter friendID = new MySqlParameter("@IDFRIEND", MySqlDbType.Int32);
                friendID.Value = friend_ID;
                cmd.Parameters.Add(friendID);

                //Получаем имя чата
                using (DbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            NAME = reader.GetString(0);
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
                conn.Close();
            }

            return NAME;
        }

        //Функция возвращает имя пользователя по его ID
        public string GetUserName(int friend_ID)
        {
            //Имя пользователя
            string NAME="null";

            //Объект для создания подключения к БД
            MySqlConnection conn = DBUtils.GetDBConnection();

            try
            {
                //Открываем соединение
                conn.Open();

                //Строка запроса
                string sql_cmd = "SELECT server_chats.users.User_Name FROM server_chats.users WHERE ID = @IDFRIEND";

                //Команда запроса
                MySqlCommand cmd = conn.CreateCommand();
                cmd.CommandText = sql_cmd;

                //Добавляем параметры
                MySqlParameter friendID = new MySqlParameter("@IDFRIEND", MySqlDbType.Int32);
                friendID.Value = friend_ID;
                cmd.Parameters.Add(friendID);

                //Получаем имя чата
                using (DbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            NAME = reader.GetString(0);
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
                conn.Close();
            }

            //Возвращаем имя пользователя
            return NAME;
        }

        //Метод отправки сообщений
        private void Sending_Message()
        {
            //Чтобы не отправлялись пустые сообщения
            if (TextBox_Message.Text.Length != 0)
            {
                try
                {
                    //Открываем соединение
                    connection.Open();

                    //Строка запроса для БД (недописана)
                    string sql_cmd = "INSERT INTO server_chats.messages (Text_Message, Date_Message, ID_Sender, ID_Reciever, Visible_Message) VALUES (@TEXT, NOW(), @MYID, @FRIENDID, 0);";

                    //Команда запроса
                    MySqlCommand cmd = connection.CreateCommand();
                    cmd.CommandText = sql_cmd;

                    //Добавляем параметры
                    MySqlParameter text_message = new MySqlParameter("@TEXT", MySqlDbType.Text);
                    text_message.Value = TextBox_Message.Text;
                    cmd.Parameters.Add(text_message);

                    MySqlParameter myID = new MySqlParameter("@MYID", MySqlDbType.Int32);
                    myID.Value = IDuser;
                    cmd.Parameters.Add(myID);

                    MySqlParameter friendID = new MySqlParameter("@FRIENDID", MySqlDbType.Int32);
                    friendID.Value = IDFriend;
                    cmd.Parameters.Add(friendID);

                    //Выполняем запрос
                    cmd.ExecuteNonQuery();

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

        //Обновление списка чатов
        private void Update_Chat_List_Click(object sender, RoutedEventArgs e)
        {
            Update_Dialog_List();
        }

        //Поиск пользователей
        private void User_Search_Changed(object sender, RoutedEventArgs e)
        {
            try
            {
                //Предварительно очищаем список
                User_List.Items.Clear();

                //Открываем соединение
                connection.Open();

                //Строка запроса на поиск пользователей в БД
                string sql_cmd = "SELECT server_chats.users.ID, server_chats.users.User_Name, server_chats.users.User_Nickname FROM server_chats.users WHERE server_chats.users.User_Name=@NAME";

                //Создаём команду для запроса в БД
                MySqlCommand cmd = connection.CreateCommand();
                cmd.CommandText = sql_cmd;

                //Добавляем параметры в команду
                MySqlParameter name_parameter = new MySqlParameter("@NAME", MySqlDbType.VarChar);
                name_parameter.Value = User_Search.Text;
                cmd.Parameters.Add(name_parameter);

                //...
                using (DbDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            //Создаём кастомизированный item для списка пользователей и добавляем ему свойства
                            Chat_List user = new Chat_List();
                            IDFriend = int.Parse(reader.GetString(0));
                            user.Name = reader.GetString(1);
                            user.ID_Friend = IDFriend;
                            User_List.Items.Add(user);
                            Friend_Nick = reader.GetString(2);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                //Выводим сообщения об ошибке
                MessageBox.Show(ex.ToString());
            }
            finally
            {
                //Закрываем соединение
                connection.Close();
                if (User_List.Items.Count == 0)
                {
                    IDFriend = -1;
                    Friend_Nick = "null";
                }
            }
        }

        //Отпрвака сообщения
        private void Send_Message_Click(object sender, RoutedEventArgs e)
        {
            Sending_Message();
        }

        //Действия при закрытии диалога
        private void Close_Dialog_Click(object sender, RoutedEventArgs e)
        {
            //Очищаем список сообщений
            Message_List.Items.Clear();

            //Стираем все данные о собеседнике
            IDFriend = -1;
            Friend_Nick = "null";

            //Скрываем элементы управления диалогом
            Close_Dialog.Visibility = Visibility.Hidden;
            MySlider.Visibility = Visibility.Hidden;
            Name_Friend.Visibility = Visibility.Hidden;

        }

        //Добавление пользователя в друзья
        private void AddToFriend_Click(object sender, RoutedEventArgs e)
        {
                try
                {
                    //Объект item'а, но только первого (если будут с одинаковыми именами, тогда будут проблемы)
                    //Нужно изменить функцию поиска с имени на никнейм
                    Chat_List user = (Chat_List)User_List.Items[0];

                    //Открываем соединение 
                    connection.Open();

                    //Строка запроса на добавление пользователя в список друзей
                    string sql_cmd = "INSERT INTO server_chats.friend VALUES (@MYID, @IDFRIEND, @FRIENDNAME);";

                    //Команда запроса
                    MySqlCommand cmd = connection.CreateCommand();
                    cmd.CommandText = sql_cmd;

                    //Добавляем параметры
                    MySqlParameter myID = new MySqlParameter("@MYID", MySqlDbType.Int32);
                    myID.Value = IDuser;
                    cmd.Parameters.Add(myID);

                    MySqlParameter friendID = new MySqlParameter("@IDFRIEND", MySqlDbType.Int32);
                    friendID.Value = user.ID_Friend;
                    cmd.Parameters.Add(friendID);

                    MySqlParameter name = new MySqlParameter("@FRIENDNAME", MySqlDbType.VarChar);
                    name.Value = user.Name;
                    cmd.Parameters.Add(name);

                    //Запускаем команду
                    cmd.ExecuteNonQuery();

                    //После успешного выполнения команды, будет дополнен список друзей
                    Friend_List.Items.Add(user);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
                finally
                {
                    //Закрываем соединение
                    connection.Close();
                    User_Search.Clear();
                }
        }

        //Открытие диалога
        private async void Chat_list_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            IDFriend = -1;
            Chat_List item = (Chat_List)Dispatcher.Invoke(()=>Chat_list.SelectedItem);
            await Task.Run(() => OpenChat(item.ID_Friend));
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
            Friend_Nick = GetUserName(IDFriend);
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
        private void Show_Hidden_Menu(object sender, RoutedEventArgs e)
        {
            int height = int.Parse((Convert.ToString(MenuGrid.Height)));
            //Если свернуто, развернуть
            if(height == 20)
            {
                BigGrid.RowDefinitions[0].Height = new GridLength(40);
                MenuShow.Height = 40;
            }
            //Иначе свернуть
            if (height == 40)
            {
                BigGrid.RowDefinitions[0].Height = new GridLength(20);
                MenuShow.Height = 20;
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

        private void Show_Hidden_Menu(object sender, System.Windows.Input.MouseEventArgs e)
        {
            int height = int.Parse((Convert.ToString(MenuGrid.Height)));
            //Если свернуто, развернуть
            if (height == 20)
            {
                BigGrid.RowDefinitions[0].Height = new GridLength(40);
                MenuShow.Height = 40;
            }
            //Иначе свернуть
            if (height == 40)
            {
                BigGrid.RowDefinitions[0].Height = new GridLength(20);
                MenuShow.Height = 20;
            }
        }
    }
}
