using System;
using System.IO;
using System.Text;

namespace zastitaInfoProjekat
{
    public static class FileManager
    {
        public static byte[] ReadFileBytes(string filePath)
        {
            try
            {
                return File.ReadAllBytes(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri čitanju fajla: {ex.Message}");
                return null;
            }
        }

        public static void WriteFileBytes(string filePath, byte[] data)
        {
            try
            {
                // Kreiraj direktorijum ako ne postoji
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllBytes(filePath, data);
                Console.WriteLine($"Fajl uspešno snimljen: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri pisanju fajla: {ex.Message}");
            }
        }

        public static string EncryptFile(string inputFilePath, string outputDirectory, int algorithmChoice, object[] parameters)
        {
            try
            {
                byte[] fileData = ReadFileBytes(inputFilePath);
                if (fileData == null) return null;

                byte[] encryptedData = null;
                string fileName = Path.GetFileName(inputFilePath);
                string outputFileName = "";

                switch (algorithmChoice)
                {
                    case 1: // Railfence
                        string text = Encoding.UTF8.GetString(fileData);
                        int rails = (int)parameters[0];
                        string encrypted = RailfenceCipher.Encrypt(text, rails);
                        encryptedData = Encoding.UTF8.GetBytes(encrypted);
                        outputFileName = $"railfence_{rails}_{fileName}";
                        break;

                    case 2: // XXTEA
                        byte[] key = (byte[])parameters[0];
                        encryptedData = XXTEA.Encrypt(fileData, key);
                        outputFileName = $"xxtea_{fileName}";
                        break;

                    case 3: // AES-CBC
                        byte[] aesKey = (byte[])parameters[0];
                        byte[] iv = (byte[])parameters[1];
                        encryptedData = AESCBC.Encrypt(fileData, aesKey, iv);
                        outputFileName = $"aes_{fileName}";
                        break;
                }

                if (encryptedData != null)
                {
                    string outputPath = Path.Combine(outputDirectory, outputFileName);
                    WriteFileBytes(outputPath, encryptedData);
                    return outputPath;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri šifrovanju fajla: {ex.Message}");
            }
            return null;
        }

        public static void DecryptFile(string inputFilePath, string outputPath, int algorithmChoice, object[] parameters)
        {
            try
            {
                byte[] encryptedData = ReadFileBytes(inputFilePath);
                if (encryptedData == null) return;

                byte[] decryptedData = null;

                switch (algorithmChoice)
                {
                    case 1: // Railfence, konvertuje bajtove u string → dešifruje → pretvara nazad u bajtove.
                        string encryptedText = Encoding.UTF8.GetString(encryptedData);
                        int rails = (int)parameters[0];
                        string decrypted = RailfenceCipher.Decrypt(encryptedText, rails);
                        decryptedData = Encoding.UTF8.GetBytes(decrypted);
                        break;
                    //dešifruju direktno bajtove koristeći ključ/IV.
                    case 2: // XXTEA
                        byte[] key = (byte[])parameters[0];
                        decryptedData = XXTEA.Decrypt(encryptedData, key);
                        break;

                    case 3: // AES-CBC
                        byte[] aesKey = (byte[])parameters[0];
                        byte[] iv = (byte[])parameters[1];
                        decryptedData = AESCBC.Decrypt(encryptedData, aesKey, iv);
                        break;
                }

                if (decryptedData != null)
                {
                    WriteFileBytes(outputPath, decryptedData);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri dešifrovanju fajla: {ex.Message}");
            }
        }

        public static string GetFileNameWithoutExtension(string fileName)
        {
            if (fileName.StartsWith("railfence_"))
            {
                int underscoreIndex = fileName.IndexOf('_', 10);
                if (underscoreIndex > 0)
                    return fileName.Substring(underscoreIndex + 1);
            }
            else if (fileName.StartsWith("xxtea_"))
            {
                return fileName.Substring(6);
            }
            else if (fileName.StartsWith("aes_"))
            {
                return fileName.Substring(4);
            }
            return fileName;
        }
    }
}