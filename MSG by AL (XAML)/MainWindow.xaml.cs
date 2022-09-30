using System;
using System.Collections.Generic;
using System.Windows.Media.Animation;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Data.Common;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MySql.Data.MySqlClient;
using ConnectionDB;

namespace MSG_by_AL__XAML_
{
    public partial class MainWindow : Window
    {

        //Имя активного пользователя
        public static string NickName = "null";

        //ID активного пользователя
        public static int IDuser = -1;

        //Создание объекта подключения к БД
        MySqlConnection connection = DBUtils.GetDBConnection();

        public MainWindow()
        {
            InitializeComponent();
        }

        //Авторизация пользователя
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            List<string> values = ServerConnect.RecieveDataFromDB("01#", login_txt.Text + "~" + password_txt.Password);
            if (values[0] != "ERROR")
            {
                if (password_txt.Password == values[2] && login_txt.Text == values[1])
                {
                    NickName = login_txt.Text;
                    IDuser = int.Parse(values[0]);
                    MessageBox.Show("Авторизация прошла успешно!", "Success", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    //SuccessSignIn();
                    //Открываем основное окно и передаём в него сведения об авторизованном пользователе
                    ChatsPage chatpage = new ChatsPage(IDuser);
                    chatpage.Show();
                    this.Close();
                }
                else
                {
                    Notification_Text.Text = "Неверный логин или пароль!";
                    Pop_Up_Notification();
                }
            }
            else
            {
                Notification_Text.Text = "Неверный логин или пароль!";
                Pop_Up_Notification();
            }
        }

        //Сопутствующие методы
        //public void SuccessSignIn()
        //{
        //    ThicknessAnimation notification = new ThicknessAnimation();
        //    notification.From = notificationPopUp.Margin;
        //    notification.To = new Thickness(0, 25, 0, 0);
        //    notification.Duration = TimeSpan.FromSeconds(1);
        //    notificationPopUp.BeginAnimation(TextBox.MarginProperty, notification);
        //}

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
