using System;

namespace zastitaInfoProjekat
{
    public static class XXTEA
    {
        private const uint DELTA = 0x9E3779B9;

        private static uint MX(uint sum, uint y, uint z, int p, uint e, uint[] k)
        {
            return ((z >> 5) ^ (y << 2)) + ((y >> 3) ^ (z << 4)) ^ ((sum ^ y) + (k[(p & 3) ^ (int)e] ^ z));
        }

        public static byte[] Encrypt(byte[] data, byte[] key)
        {
            if (data == null || data.Length == 0)
                return data;

            uint[] v = ToUIntArray(data, true);
            uint[] k = ToUIntArray(FixKey(key), false);

            int n = v.Length;
            uint z = v[n - 1], y, sum = 0;
            uint e;
            int p;
            int q = 6 + 52 / n; 
                 /*Za svaku rundu:
            Dodaje DELTA u sum.
            Računa e koji zavisi od sume.
            Iterira kroz sve blokove i menja ih koristeći MX funkciju.
            Poslednji blok se meša sa prvim (ciklična veza).*/
            while (q-- > 0)
            {
                sum += DELTA;
                e = (sum >> 2) & 3;
                for (p = 0; p < n - 1; p++)
                {
                    y = v[p + 1];
                    z = v[p] += MX(sum, y, z, p, e, k);
                }
                y = v[0];
                z = v[n - 1] += MX(sum, y, z, p, e, k);
            }

            return ToByteArray(v, false);
        }

        public static byte[] Decrypt(byte[] data, byte[] key)
        {
            if (data == null || data.Length == 0)
                return data;

            uint[] v = ToUIntArray(data, false);
            uint[] k = ToUIntArray(FixKey(key), false);

            int n = v.Length; //n broj 32-bitnih blokova
            uint z, y = v[0], sum; //z poslednji blok, koristi se u MX funkciji
            uint e;
            int p;
            int q = 6 + 52 / n; //q broj rundi (više blokova = više rundi) sigurniji od tea

            sum = (uint)(q * DELTA);
            //Iterira unazad kroz blokove i vraća ih u originalni oblik.
            while (sum != 0)
            {
                e = (sum >> 2) & 3;
                for (p = n - 1; p > 0; p--)
                {
                    z = v[p - 1];
                    y = v[p] -= MX(sum, y, z, p, e, k);
                }
                z = v[n - 1];
                y = v[0] -= MX(sum, y, z, p, e, k);
                sum -= DELTA;
            }

            return ToByteArray(v, true);
        }

        // Ako ključ nije 16 bajtova, proširuje ga na 16 bajtova. XXTEA uvek koristi 128-bitni ključ.
        private static byte[] FixKey(byte[] key)
        {
            if (key.Length == 16)
                return key;

            byte[] fixedKey = new byte[16];
            Array.Copy(key, 0, fixedKey, 0, Math.Min(key.Length, 16));
            return fixedKey;
        }

        private static uint[] ToUIntArray(byte[] data, bool includeLength)
        {
            int length = data.Length;
            int n = (length + 3) / 4;
            uint[] result;

            if (includeLength) /*Ako je includeLength == true, poslednji uint u nizu će sadržati tačan broj originalnih bajtova.
 Ako ima byte[] data = { 0x41, 0x42, 0x43 } (što je "ABC"), i includeLength = true, pretvoriće se u:
uint[] result = {
    0x00434241, // prvi uint: "ABC" + padding
    3           // poslednji uint: dužina originalnih podataka
};
            A kad dešifrujemo, funkcija koristi tu dužinu 3 da zna da treba da vrati samo prva 3 bajta i ignoriše padding.
*/


            {
                result = new uint[n + 1];
                result[n] = (uint)length;
            }
            else
            {
                result = new uint[n];
            }

            for (int i = 0; i < length; i++)
            {
                result[i >> 2] |= (uint)data[i] << ((i & 3) << 3);
            }

            return result;
        }

        private static byte[] ToByteArray(uint[] data, bool includeLength)
        {
            int n = data.Length << 2;

            if (includeLength)
            {
                int m = (int)data[data.Length - 1];
                if (m > n) return null;
                n = m;
            }

            byte[] result = new byte[n];
            for (int i = 0; i < n; i++)
            {
                result[i] = (byte)(data[i >> 2] >> ((i & 3) << 3));
            }

            return result;
        }
    }
}
