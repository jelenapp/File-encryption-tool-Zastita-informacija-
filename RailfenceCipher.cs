using System;
using System.Text;

namespace zastitaInfoProjekat
{
    public class RailfenceCipher
    {
        public static string Encrypt(string text, int key)
        {
            if (key <= 1 || string.IsNullOrEmpty(text))
                return text;

            char[,] rail = new char[key, text.Length];

            // Initialize rail with null characters
            for (int i = 0; i < key; i++) //Kreira dvodimenzionalni niz 
                for (int j = 0; j < text.Length; j++)
                    rail[i, j] = '\0';

            bool directionDown = false;
            int row = 0;

            // Place characters in the rail pattern
            for (int col = 0; col < text.Length; col++)
            {
                // Change direction at top and bottom rails
                if (row == 0 || row == key - 1)
                    directionDown = !directionDown;

                rail[row, col] = text[col];

                // Move to next row
                if (directionDown)
                    row++;
                else
                    row--;
            }

            // Read the rail pattern row by row
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < key; i++)
            {
                for (int j = 0; j < text.Length; j++)
                {
                    if (rail[i, j] != '\0')
                        result.Append(rail[i, j]);
                }
            }

            return result.ToString();
        }

        public static string Decrypt(string cipher, int key)
        {
            if (key <= 1 || string.IsNullOrEmpty(cipher))
                return cipher;

            char[,] rail = new char[key, cipher.Length];

            // Initialize rail
            for (int i = 0; i < key; i++)
                for (int j = 0; j < cipher.Length; j++)
                    rail[i, j] = '\0';

            // Mark the positions that will be filled
            bool directionDown = false;
            int row = 0;

            for (int col = 0; col < cipher.Length; col++)
            {
                if (row == 0 || row == key - 1)
                    directionDown = !directionDown;

                rail[row, col] = '*'; // Mark position

                if (directionDown)
                    row++;
                else
                    row--;
            }

            // Fill the rail with cipher characters
            int index = 0;
            for (int i = 0; i < key; i++)
            {
                for (int j = 0; j < cipher.Length; j++)
                {
                    if (rail[i, j] == '*' && index < cipher.Length)
                        rail[i, j] = cipher[index++];
                }
            }

            // Read the rail in zigzag pattern to get original text
            StringBuilder result = new StringBuilder();
            row = 0;
            directionDown = false;

            for (int col = 0; col < cipher.Length; col++)
            {
                if (row == 0 || row == key - 1)
                    directionDown = !directionDown;

                result.Append(rail[row, col]);

                if (directionDown)
                    row++;
                else
                    row--;
            }

            return result.ToString();
        }
    }
}