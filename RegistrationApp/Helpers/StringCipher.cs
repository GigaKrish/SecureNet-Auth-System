using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

public static class StringCipher
{
    private static readonly string encryptionKey = "your-strong-encryption-key!123"; // keep this safe and secret

    public static string Encrypt(string plainText)
    {
        byte[] clearBytes = Encoding.Unicode.GetBytes(plainText);

        using (Aes encryptor = Aes.Create())
        {
            var pdb = new Rfc2898DeriveBytes(encryptionKey, new byte[] {
                0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64,
                0x76, 0x65, 0x64, 0x65, 0x76
            });

            encryptor.Key = pdb.GetBytes(32);
            encryptor.IV = pdb.GetBytes(16);

            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(clearBytes, 0, clearBytes.Length);
                    cs.Close();
                }

                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }

    public static string Decrypt(string cipherText)
    {
        cipherText = cipherText.Replace(" ", "+"); // in case '+' becomes ' ' during form transmission
        byte[] cipherBytes = Convert.FromBase64String(cipherText);

        using (Aes encryptor = Aes.Create())
        {
            var pdb = new Rfc2898DeriveBytes(encryptionKey, new byte[] {
                0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64,
                0x76, 0x65, 0x64, 0x65, 0x76
            });

            encryptor.Key = pdb.GetBytes(32);
            encryptor.IV = pdb.GetBytes(16);

            using (MemoryStream ms = new MemoryStream())
            {
                using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(cipherBytes, 0, cipherBytes.Length);
                    cs.Close();
                }

                return Encoding.Unicode.GetString(ms.ToArray());
            }
        }
    }



}