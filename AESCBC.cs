using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace zastitaInfoProjekat
{
    public class AESCBC
    {
        public static byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
        {
            Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv; //inicijalizatorski v-r (nasumican)
            aes.Mode = CipherMode.CBC;

            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write); //povezuje MemoryStream sa AES enkriptorom (aes.CreateEncryptor()).
            //Svi podaci koji se pišu u CryptoStream se automatski enkriptuju i šalju u MemoryStream
            //Piše ceo niz bajtova u CryptoStream, gde se blok po blok enkriptuje.
            cs.Write(data, 0, data.Length);
            cs.Close();
            byte[] encryptedData = ms.ToArray();

            ms.Close();
            aes.Dispose();

            return encryptedData;
        }

        public static byte[] Decrypt(byte[] data, byte[] key, byte[] iv)
        {
            Aes aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;

            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write); //aes.CreateDecryptor() stvara dekriptor za CBC mod.

            cs.Write(data, 0, data.Length);
            cs.Close();
            byte[] decryptedData = ms.ToArray();

            ms.Close();
            aes.Dispose();

            return decryptedData;
        }
    }
}

