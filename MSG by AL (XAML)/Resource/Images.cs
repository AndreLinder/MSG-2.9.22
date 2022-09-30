using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Resource
{
    class Images
    {
        BitmapImage image = new BitmapImage();

        //Пустой конструктор
        public Images()
        {
        }

        //Конструктор с параметром типа BitmapImage
        public Images(BitmapImage img)
        {
            image = img;
        }

        //Свойство хранящее изображение
        public BitmapImage ImageValue
        {
            get { return image; }
            set { image = value; }
        }

        public BitmapImage ByteArrayToImage(byte[] arrayByte)
        {
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream(arrayByte))
            {
                image.BeginInit();
                image.StreamSource = ms;
                image.EndInit();
            }
            return image;
        }
    }
}
