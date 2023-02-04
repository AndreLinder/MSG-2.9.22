using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Security.Cryptography;

namespace MSG_by_AL__XAML_
{
    /// <summary>
    /// Логика взаимодействия для SignUpWindow.xaml
    /// </summary>
    public partial class SignUpWindow : Window
    {
        //Объект хэширования пароля
        MD5 md5 = MD5.Create();

        public SignUpWindow()
        {
            InitializeComponent();
        }

        //Регистрация нового пользователя
        private void SignUp_Click(object sender, RoutedEventArgs e)
        {
            if (login_text.Text.Length > 0 & name_text.Text.Length > 0 & password_text.Password.Length > 0 & password_repeat.Password.Length > 0)
            {
                //Отправляем запрос на сервер и получаем ответ
                if (password_repeat.Password == password_text.Password)
                {
                    List<string> values = ServerConnect.RecieveDataFromDB("02#", name_text.Text + "~" + login_text.Text + "~" + Encoding.UTF8.GetString(md5.ComputeHash(Encoding.UTF8.GetBytes(password_text.Password))));

                    if (values[0].Contains("CREATE NEW USER"))
                    {
                        MessageBox.Show("Пользователь успешно зарегистрирован!", "Success", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                        this.Close();
                    }
                    else MessageBox.Show("Пользователь с таким именем уже существует!");
                }
            }
            else MessageBox.Show("Заполните все поля!");
        }

        //Действия при закрытии формы
        private void SignUpWindow_Closing(object sender, EventArgs e)
        {
            MainWindow main = new MainWindow();
            main.Show();
        }
    }
}
