﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.IO;
using HarfBuzzSharp;
using Microsoft.VisualBasic;
using static System.Net.WebRequestMethods;
using System.Threading.Tasks.Dataflow;

namespace DataProtection
{
    public partial class Protect
    {

        /// <summary>
        /// Converts a regular string to a secured string which cannot be read using a memory dump
        /// </summary>
        /// <param name="input">Non secure string</param>
        /// <returns>Secure string which is read-only and cannot be read using a memory dump</returns>
        public static SecureString ConvertToSecureString(string input)
        {
            SecureString secure = new SecureString();
            foreach (char c in input)
            {
                secure.AppendChar(c);
            }
            secure.MakeReadOnly();
            return secure;
        }


        public static SecureString EncryptString(string input)
        {
            var userBytes = Encoding.UTF8.GetBytes(input); // UTF8 saves Space
            var userHash = MD5.Create().ComputeHash(userBytes);
            SymmetricAlgorithm crypt = Aes.Create(); // (Default: AES-CCM (Counter with CBC-MAC))
            crypt.Key = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes("SneakyPanda69")); // MD5: 128 Bit Hash
            crypt.IV = new byte[16]; // by Default. IV[] to 0.. is OK simple crypt
            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, crypt.CreateEncryptor(), CryptoStreamMode.Write);
            cryptoStream.Write(userBytes, 0, userBytes.Length); // User Data
            cryptoStream.Write(userHash, 0, userHash.Length); // Add HASH
            cryptoStream.FlushFinalBlock();
            var resultString = Convert.ToBase64String(memoryStream.ToArray());
            return ConvertToSecureString(resultString);

        }

    }

    public partial class UnProtect
    {
        /// <summary>
        /// Converts a securestring to a normal string. Note, normal strings can be read using a memory dump
        /// </summary>
        /// <param name="input">Secured string</param>
        /// <returns></returns>
        public static string ConvertToInsecureString(SecureString input)
        {
            string returnValue = string.Empty;
            IntPtr ptr = Marshal.SecureStringToBSTR(input);
            try
            {
                returnValue = Marshal.PtrToStringBSTR(ptr);
            }
            finally
            {
                Marshal.ZeroFreeBSTR(ptr);
            }
            return returnValue;
        }


    }

    public partial class Hash
    {
        public static string CalculateSHA256Hash_FromFilePath(string PathtoFile)
        {
            // Open the file as a stream
            using (FileStream fileStream = new FileStream(PathtoFile, FileMode.Open, FileAccess.Read))
            {
                // Create a new instance of the SHA256CryptoServiceProvider
                using (SHA256 sha256 = SHA256.Create())
                {
                    // Allocate a buffer for reading the file
                    byte[] buffer = new byte[(1024 * 1024) * 10]; // 1MB buffer

                    int bytesRead;
                    // Read the file and update the hash
                    while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        sha256.TransformBlock(buffer, 0, bytesRead, buffer, 0);
                    }

                    // Finalize the hash
                    sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                    // Get the hash as a byte array
                    byte[] hash = sha256.Hash;

                    // Convert the hash to a hexadecimal string
                    string hashString = BitConverter.ToString(hash).Replace("-", "").ToLower();
                    return hashString;
                }
            }

        }
}
}
