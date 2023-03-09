using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MSG_by_AL__XAML_.Resource
{
    public class Message
    {
        //Пустой конструктор
        public Message()
        {
        }

        //Свойство сохраняет ID сообщения (чтобы можно было его удалять)
        public int Message_ID { get; set; }

        //Свойство, текст сообщения
        public string Message_Text { get; set; }

        //Свойсто дат отправки
        public string Message_Date { set; get; }

        //Свойства для хранения значения BorderBrush
        public Brush borderBrush { get; set; }

        //Свойство для хранения заливки фона сообщения (Background)
        public Brush backGround { get; set; }

        //Имя отправителя
        public string User_Name { get; set; }
    }
}
