using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
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
                    object value = key.GetValue(valueName);
                    return value != null ? value.ToString() : "Value not found";
                }
                else
                {
                    return "Registry Key not found";
                }
            }
        }
    }
}
