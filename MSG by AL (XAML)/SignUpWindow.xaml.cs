using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Data.Common;
using MySql.Data.MySqlClient;
using ConnectionDB;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MSG_by_AL__XAML_
{
    /// <summary>
    /// Логика взаимодействия для SignUpWindow.xaml
    /// </summary>
    public partial class SignUpWindow : Window
    {
        //Объект нашего соединения
        MySqlConnection connection = DBUtils.GetDBConnection();

        public SignUpWindow()
        {
            InitializeComponent();
        }

        //Регистрация нового пользователя
        private void SignUp_Click(object sender, RoutedEventArgs e)
        {
            //Отправляем запрос на сервер и получаем ответ
            List<string> values = ServerConnect.RecieveDataFromDB("02#", name_text.Text + "~" + login_text.Text + "~" + password_text.Password);

            if (values[0].Contains("CREATE NEW USER"))
            {
                MessageBox.Show("Пользователь успешно зарегистрирован!", "Success", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
            else MessageBox.Show("Произошла ошибка");

        }

        //Действия при закрытии формы
        private void SignUpWindow_Closing(object sender, EventArgs e)
        {
            MainWindow main = new MainWindow();
            main.Show();
        }
    }
}
