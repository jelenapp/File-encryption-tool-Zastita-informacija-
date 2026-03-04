using System;
using System.Text;

namespace zastitaInfoProjekat
{
    public static class AlgorithmTests
    {
        public static void RunAllTests()
        {
            Console.WriteLine("=== POKRETANJE TESTOVA ALGORITAMA ===\n");

            TestRailfenceCipher();
            TestXXTEA();
            TestAESCBC();
            TestTigerHash();

            Console.WriteLine("\n=== SVI TESTOVI ZAVRŠENI ===");
        }

        private static void TestRailfenceCipher()
        {
            Console.WriteLine("--- Test Railfence Cipher ---");

            string[] testCases = { "HELLOWORLD", "ATTACKATDAWN", "MEETMEATMIDNIGHT" };
            int[] railCounts = { 3, 4, 5 };

            foreach (string testCase in testCases)
            {
                foreach (int rails in railCounts)
                {
                    string encrypted = RailfenceCipher.Encrypt(testCase, rails);
                    string decrypted = RailfenceCipher.Decrypt(encrypted, rails);

                    bool passed = testCase == decrypted;
                    Console.WriteLine($"Text: '{testCase}', Rails: {rails}, Passed: {passed}");

                    if (!passed)
                    {
                        Console.WriteLine($"  Expected: '{testCase}'");
                        Console.WriteLine($"  Got:      '{decrypted}'");
                        Console.WriteLine($"  Encrypted: '{encrypted}'");
                    }
                }
            }
            Console.WriteLine();
        }

        private static void TestXXTEA()
        {
            Console.WriteLine("--- Test XXTEA ---");

            string[] testTexts = {
                "Hello World",
                "This is a test message",
                "1234567890",
                "Special chars: !@#$%^&*()",
                "" // Empty string test
            };

            byte[] key = Encoding.UTF8.GetBytes("1234567890123456"); // 16-byte key

            foreach (string testText in testTexts)
            {
                try
                {
                    byte[] plaintext = Encoding.UTF8.GetBytes(testText);
                    byte[] encrypted = XXTEA.Encrypt(plaintext, key);
                    byte[] decrypted = XXTEA.Decrypt(encrypted, key);
                    string result = Encoding.UTF8.GetString(decrypted);

                    bool passed = testText == result;
                    Console.WriteLine($"Text: '{testText}', Length: {testText.Length}, Passed: {passed}");

                    if (!passed)
                    {
                        Console.WriteLine($"  Expected: '{testText}'");
                        Console.WriteLine($"  Got:      '{result}'");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Text: '{testText}' - ERROR: {ex.Message}");
                }
            }
            Console.WriteLine();
        }

        private static void TestAESCBC()
        {
            Console.WriteLine("--- Test AES-CBC ---");

            string[] testTexts = {
                "Hello World",
                "This is a longer test message to check padding",
                "Short",
                "Exactly16Bytes!!" 
            };

            byte[] key = Encoding.UTF8.GetBytes("1234567890123456"); // 16-byte key
            byte[] iv = Encoding.UTF8.GetBytes("abcdefghijklmnop");  // 16-byte IV

            foreach (string testText in testTexts)
            {
                try
                {
                    byte[] plaintext = Encoding.UTF8.GetBytes(testText);
                    byte[] encrypted = AESCBC.Encrypt(plaintext, key, iv);
                    byte[] decrypted = AESCBC.Decrypt(encrypted, key, iv);

                    // Remove any padding bytes
                    string result = Encoding.UTF8.GetString(decrypted).TrimEnd('\0');

                    bool passed = testText == result;
                    Console.WriteLine($"Text: '{testText}', Length: {testText.Length}, Passed: {passed}");

                    if (!passed)
                    {
                        Console.WriteLine($"  Expected: '{testText}'");
                        Console.WriteLine($"  Got:      '{result}'");
                        Console.WriteLine($"  Expected Length: {testText.Length}, Got Length: {result.Length}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Text: '{testText}' - ERROR: {ex.Message}");
                }
            }
            Console.WriteLine();
        }

        private static void TestTigerHash()
        {
            Console.WriteLine("--- Test Tiger Hash ---");

            string[] testCases = {
                "",
                "a",
                "abc",
                "message digest",
                "Hello World",
                "The quick brown fox jumps over the lazy dog"
            };

            foreach (string testCase in testCases)
            {
                try
                {
                    byte[] data = Encoding.UTF8.GetBytes(testCase);
                    byte[] hash1 = TigerHash.ComputeHash(data);
                    byte[] hash2 = TigerHash.ComputeHash(data); // Should be same

                    bool consistent = hash1.Length == hash2.Length;
                    if (consistent)
                    {
                        for (int i = 0; i < hash1.Length; i++)
                        {
                            if (hash1[i] != hash2[i])
                            {
                                consistent = false;
                                break;
                            }
                        }
                    }

                    Console.WriteLine($"Text: '{testCase}', Hash Length: {hash1.Length} bytes, Consistent: {consistent}");
                    Console.WriteLine($"  Hash: {BitConverter.ToString(hash1).Replace("-", "")}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Text: '{testCase}' - ERROR: {ex.Message}");
                }
            }
            Console.WriteLine();
        }
    }
}