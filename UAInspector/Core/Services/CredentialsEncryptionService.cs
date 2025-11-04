using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace UAInspector.Core.Services
{
    /// <summary>
    /// Service for securely storing and retrieving credentials
    /// </summary>
    public class CredentialsEncryptionService
    {
        private readonly string _encryptionKey;

        public CredentialsEncryptionService(string encryptionKey)
     {
       if (string.IsNullOrEmpty(encryptionKey) || encryptionKey.Length < 16)
                throw new ArgumentException("Encryption key must be at least 16 characters", nameof(encryptionKey));

            _encryptionKey = encryptionKey;
        }

        /// <summary>
        /// Encrypt a password
        /// </summary>
        public string Encrypt(string plainText)
 {
  try
     {
            byte[] key = Encoding.UTF8.GetBytes(_encryptionKey);
        using (var aes = Aes.Create())
           {
  aes.Key = key;
      aes.GenerateIV();

           using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
 {
             byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
    byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                 // Combine IV and encrypted data
byte[] combined = new byte[aes.IV.Length + encryptedBytes.Length];
     Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
     Buffer.BlockCopy(encryptedBytes, 0, combined, aes.IV.Length, encryptedBytes.Length);

         return Convert.ToBase64String(combined);
       }
    }
            }
            catch (Exception ex)
            {
           System.Diagnostics.Debug.WriteLine($"Encryption error: {ex.Message}");
           throw;
        }
      }

        /// <summary>
        /// Decrypt a password
        /// </summary>
    public string Decrypt(string cipherText)
        {
            try
            {
    byte[] key = Encoding.UTF8.GetBytes(_encryptionKey);
    byte[] buffer = Convert.FromBase64String(cipherText);

          using (var aes = Aes.Create())
    {
         aes.Key = key;
   aes.IV = new byte[16];
        Buffer.BlockCopy(buffer, 0, aes.IV, 0, aes.IV.Length);

         using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
     {
         byte[] decryptedBytes = decryptor.TransformFinalBlock(buffer, aes.IV.Length, buffer.Length - aes.IV.Length);
         return Encoding.UTF8.GetString(decryptedBytes);
         }
      }
     }
    catch (Exception ex)
            {
     System.Diagnostics.Debug.WriteLine($"Decryption error: {ex.Message}");
 throw;
 }
        }
    }
}
