using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MSG_by_AL__XAML_.Resource
{
    internal class AES256
    {
        //Ключи шифрования
        byte[] key1 = new byte[16];
        byte[] key2 = new byte[16];

        //Пустой конструтор
        public AES256()
        {

        }

        //Конструктор, принимающий в качестве параметра GUID и преобразовывает их в ключи шифрования
        public AES256(string guid)
        {
            key1 = Encoding.UTF8.GetBytes(guid.Substring(0, 8));
            key2 = Encoding.UTF8.GetBytes(guid.Substring(guid.Length - 8, 8));
        }

        //Метод шифрования данных
        public string Encode(string text)
        {
            try
            {
                byte[] inputtextbyteArray = System.Text.Encoding.UTF8.GetBytes(text);
                using (DESCryptoServiceProvider dsp = new DESCryptoServiceProvider())
                {
                    var memstr = new MemoryStream();
                    var crystr = new CryptoStream(memstr, dsp.CreateEncryptor(key1, key2), CryptoStreamMode.Write);
                    crystr.Write(inputtextbyteArray, 0, inputtextbyteArray.Length);
                    crystr.FlushFinalBlock();
                    return Convert.ToBase64String(memstr.ToArray());
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        //Метод расшифровки данных
        public string Decode(string code)
        {
            try
            {
                byte[] inputtextbyteArray = new byte[code.Replace(" ", "+").Length];
                inputtextbyteArray = Convert.FromBase64String(code.Replace(" ", "+"));
                using (DESCryptoServiceProvider dEsp = new DESCryptoServiceProvider())
                {
                    var memstr = new MemoryStream();
                    var crystr = new CryptoStream(memstr, dEsp.CreateDecryptor(key1, key2), CryptoStreamMode.Write);
                    crystr.Write(inputtextbyteArray, 0, inputtextbyteArray.Length);
                    crystr.FlushFinalBlock();
                    return Encoding.UTF8.GetString(memstr.ToArray());
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
