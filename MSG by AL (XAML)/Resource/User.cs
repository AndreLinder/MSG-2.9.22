using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSG_by_AL__XAML_.Resource
{
    //Класс описывает структуры данных классического пользователя
    internal class User
    {
        //Пустой конструктор
        public User()
        {
        }
        //Ниже приведеныы необходимые свойства класса
        public string Name { get; set; }

        public string Nickname { get; set; }

        public int ID { get; set; }
    }
}
