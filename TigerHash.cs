using System;

namespace zastitaInfoProjekat
{
    public class TigerHash
    {
        //4 s-boxa po 256 el

        private static readonly ulong[] T1 = new ulong[256];
        private static readonly ulong[] T2 = new ulong[256];
        private static readonly ulong[] T3 = new ulong[256];
        private static readonly ulong[] T4 = new ulong[256];

        static TigerHash()
        {
            InitializeTables();
        }

        private static void InitializeTables()
        {
         
            for (int i = 0; i < 256; i++)
            {
                T1[i] = (ulong)(i * 0x123456789ABCDEF0L);
                T2[i] = (ulong)(i * 0x0FEDCBA987654321L);
                T3[i] = ((ulong)i) * 0xF0E1D2C3B4A59687UL;
                T4[i] = ((ulong)i)* 0x8796A5B4C3D2E1F0UL;
            }
        }

        public static byte[] ComputeHash(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            // Tiger produces 192-bit (24-byte) hash
            ulong a = 0x0123456789ABCDEFL;
            ulong b = 0xFEDCBA9876543210L;
            ulong c = 0xF096A5B4C3B2E187L;

            // Pad message
            byte[] paddedData = PadMessage(data);

            // Process message in 512-bit chunks
            for (int i = 0; i < paddedData.Length; i += 64)
            {
                ulong[] x = new ulong[8];
                for (int j = 0; j < 8; j++)
                {
                    x[j] = BitConverter.ToUInt64(paddedData, i + j * 8);
                }

                // Tiger round function (simplified)
                ProcessBlock(ref a, ref b, ref c, x);
            }

            // Convert result to byte array
            byte[] result = new byte[24];
            Array.Copy(BitConverter.GetBytes(a), 0, result, 0, 8);
            Array.Copy(BitConverter.GetBytes(b), 0, result, 8, 8);
            Array.Copy(BitConverter.GetBytes(c), 0, result, 16, 8);

            return result;
        }

        private static byte[] PadMessage(byte[] data)
        {
            int originalLength = data.Length;
            int paddingLength = (64 - ((originalLength + 9) % 64)) % 64;
            int totalLength = originalLength + 1 + paddingLength + 8;

            byte[] padded = new byte[totalLength];
            Array.Copy(data, 0, padded, 0, originalLength);

            // Add padding bit
            padded[originalLength] = 0x80;

            // Add length in bits as 64-bit little-endian
            ulong lengthInBits = (ulong)originalLength * 8;
            Array.Copy(BitConverter.GetBytes(lengthInBits), 0, padded, totalLength - 8, 8);

            return padded;
        }

        private static void ProcessBlock(ref ulong a, ref ulong b, ref ulong c, ulong[] x)
        {
            ulong aa = a, bb = b, cc = c;

            // Simplified Tiger round function
            for (int pass = 0; pass < 3; pass++)
            {
                if (pass != 0)
                {
                    x[0] -= x[7] ^ 0xA5A5A5A5A5A5A5A5UL;
                    x[1] ^= x[0];
                    x[2] += x[1];
                    x[3] -= x[2] ^ ((~x[1]) << 19);
                    x[4] ^= x[3];
                    x[5] += x[4];
                    x[6] -= x[5] ^ ((~x[4]) >> 23);
                    x[7] ^= x[6];
                    x[0] += x[7];
                    x[1] -= x[0] ^ ((~x[7]) << 19);
                    x[2] ^= x[1];
                    x[3] += x[2];
                    x[4] -= x[3] ^ ((~x[2]) >> 23);
                    x[5] ^= x[4];
                    x[6] += x[5];
                    x[7] -= x[6] ^ 0x0123456789ABCDEFUL;
                }

                for (int i = 0; i < 8; i++)
                {
                    Round(ref a, ref b, ref c, x[i], (pass == 0) ? 5UL : (pass == 1) ? 7UL : 9UL);
                    ulong temp = a; a = c; c = b; b = temp;
                }
            }

            a ^= aa;
            b -= bb;
            c += cc;
        }

        private static void Round(ref ulong a, ref ulong b, ref ulong c, ulong x, ulong mul)
        {
            c ^= x;
            a -= T1[(byte)c] ^ T2[(byte)(c >> 16)] ^ T3[(byte)(c >> 32)] ^ T4[(byte)(c >> 48)];
            b += T4[(byte)(c >> 8)] ^ T3[(byte)(c >> 24)] ^ T2[(byte)(c >> 40)] ^ T1[(byte)(c >> 56)];
            b *= mul;
        }
    }
}