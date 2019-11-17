using System;
using System.Security.Cryptography;
using System.Text;

namespace FileBundlingUtility
{
    public class CryptoService
    {
        public static string Encrypt(string input, string key)
        {
            var inputArray = Encoding.UTF8.GetBytes(input);
            var tripleDES = new TripleDESCryptoServiceProvider
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };
            var cTransform = tripleDES.CreateEncryptor();
            var resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);
            tripleDES.Clear();
            return Convert.ToBase64String(resultArray, 0, resultArray.Length);
        }

        public static string Decrypt(string input, string key)
        {
            var inputArray = Convert.FromBase64String(input);
            var tripleDES = new TripleDESCryptoServiceProvider
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = CipherMode.ECB,
                Padding = PaddingMode.PKCS7
            };
            var cTransform = tripleDES.CreateDecryptor();
            var resultArray = cTransform.TransformFinalBlock(inputArray, 0, inputArray.Length);
            tripleDES.Clear();
            return Encoding.UTF8.GetString(resultArray);
        }
    }
}
