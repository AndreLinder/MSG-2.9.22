using System;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MSG_by_AL__XAML_.Resource
{
    public class Chat_List
    {
        //Пустой конструктор (зачем? не знаю)
        public Chat_List()
        {
        }

        //Свойство, устанавливающее или возвращающее ID чата
        public int ID { get; set; } 

        //Свойство, устанавливающее или возвращающее название чата
        public string Name { get; set; } 

        //ID собеседника
        public int ID_Friend { get; set; }
    }
}
