using System;
using System.Collections.Generic;
using System.Windows.Media.Animation;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Security.Cryptography;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MSG_by_AL__XAML_
{
    public partial class MainWindow : Window
    {

        //Имя активного пользователя
        public static string NickName = "null";

        //Объект для вычисления хэша
        MD5 md5 = MD5.Create();

        //ID активного пользователя
        public static int IDuser = -1;

        public MainWindow(bool x)
        {
            InitializeComponent();
        }

        public MainWindow()
        {
            string login = "";
            string hash_password = "";
            InitializeComponent();

            StreamReader config_File;
            if (File.Exists("config.mal"))
            {
                config_File = new StreamReader("config.mal");
                while (!config_File.EndOfStream)
                {
                    login = config_File.ReadLine();
                    hash_password = config_File.ReadToEnd();
                    hash_password = hash_password.Trim();
                    config_File.Close();
                    break;
                }
                try
                {
                    List<string> values = ServerConnect.RecieveDataFromDB("01#", login + "~" + hash_password);
                    if (values[0] == "ERROR")
                    {
                        File.Delete("config.mal");
                    }
                    else if (login == values[1] && hash_password == values[2])
                    {
                        ChatsPage chatpage = new ChatsPage(int.Parse(values[0]), values[1], values[3], values[4]);
                        chatpage.Show();
                        this.Close();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + "\n" + ex.ToString());
                    config_File.Close();
                }
            }

        }

        //Авторизация пользователя
        private void Login_Click(object sender, RoutedEventArgs e)
        {

            byte[] hash_password = md5.ComputeHash(Encoding.UTF8.GetBytes(password_txt.Password));
            try
            {
                List<string> values = ServerConnect.RecieveDataFromDB("01#", login_txt.Text + "~" + Encoding.UTF8.GetString(hash_password));
                if (values[0] != "ERROR")
                {
                    if (Encoding.UTF8.GetString(hash_password) == values[2] & login_txt.Text == values[1])
                    {
                        NickName = login_txt.Text;
                        IDuser = int.Parse(values[0]);
                        NickName = values[1];
                        //Открываем основное окно и передаём в него сведения об авторизованном пользователе
                        StreamWriter config_file = new StreamWriter("config.mal");
                        config_file.WriteLine(values[1]);
                        config_file.WriteLine(values[2]);
                        config_file.Close();

                        ChatsPage chatpage = new ChatsPage(IDuser, NickName, values[3], values[4]);
                        chatpage.Show();
                        this.Close();
                    }
                    else
                    {
                        Notification_Text.Text = "Неверный логин или пароль!";
                        Pop_Up_Notification();
                    }
                }
                else if (values[0] == "ERROR")
                {
                    Notification_Text.Text = "Неверный логин или пароль!";
                    Pop_Up_Notification();
                }
                else
                {
                    Notification_Text.Text = "Нет соединения с сервером!";
                    Pop_Up_Notification();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                Notification_Text.Text = "Нет соединения с сервером!";
                Pop_Up_Notification();
            }
        }
        //Тест для авторизации пользователя
        [TestMethod]
        public void Login_Test()
        {
            //Arrange
            string login = "test";
            string password = "test";
            byte[] hash_password = md5.ComputeHash(Encoding.UTF8.GetBytes(password));
            List<string> values = new List<string> { "1", login, Encoding.UTF8.GetString(hash_password), "test", "test" };

            //Act
            Login_Click(null, null);
            //Assert

            Assert.AreEqual(values[0], IDuser.ToString());
            Assert.AreEqual(values[1], NickName);
            Assert.AreEqual(values[2], Encoding.UTF8.GetString(hash_password));
            Assert.AreEqual(values[3], values[3]);
            Assert.AreEqual(values[4], values[4]);
        }

        //Открываем окно регистрации пользователя
        private void SignUp_Click(object sender, RoutedEventArgs e)
        {
            SignUpWindow Form = new SignUpWindow();
            Form.Show();
            this.Close();
        }

        //Метод всплывающего уведомления
        private bool Expanded = false;
        private void Pop_Up_Notification()
        {
            if (Expanded)
            {
                var anim = new DoubleAnimation(0, (Duration)TimeSpan.FromSeconds(0.5));
                anim.Completed += (s, _) => Expanded = false;
                Notification.BeginAnimation(ContentControl.HeightProperty, anim);
            }
            else
            {
                var anim = new DoubleAnimation(30, (Duration)TimeSpan.FromSeconds(0.5));
                anim.Completed += (s, _) => Expanded = true;
                Notification.BeginAnimation(ContentControl.HeightProperty, anim);
            }
        }

        //Убирает уведомление при смене фокуса
        private void UIElement_LostFocus(object sender, RoutedEventArgs e)
        {
            if (Notification.Height > 0) Pop_Up_Notification();
        }
    }
}
