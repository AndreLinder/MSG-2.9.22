using System;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Resource
{
    public class ButtonImage
    {
        //Регистрируем свойство image для кнопки
        public static readonly DependencyProperty ImageProperty =
            DependencyProperty.RegisterAttached("Image", typeof(ImageSource), typeof(ButtonImage));

        //Задаём поле set для добавления свойства
        public static void SetImage(DependencyObject obj, ImageSource image)
        {
            obj.SetValue(ImageProperty, image);
        }

        //Создаём свойтво get для возврата хранящегося значения в свойстве
        public static ImageSource GetImage(DependencyObject obj)
        {
            return (ImageSource)obj.GetValue(ImageProperty);
        }

    }
}
