using Microsoft.Win32;
using SshNet.Security.Cryptography;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace POSLinkHelperApp
{
    public class RegistryHelper
    {
        public static string GetRegistryValue(RegistryKey baseKey, string subKeyPath, string valueName)
        {
            using (RegistryKey key = baseKey.OpenSubKey(subKeyPath)) // Corrected declaration
            {
                if (key != null)
                {
                    CryptoHelper crypto = new CryptoHelper();
                    object value = key.GetValue(valueName);
                    return value != null ? crypto.Decrypt3DES(value.ToString()) : "Value not found";
                }
                else
                {
                    return "Registry Key not found";
                }
            }
        }          

    }
}
