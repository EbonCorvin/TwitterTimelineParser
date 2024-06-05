using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Twitter_Download
{
    internal class ReadConfig
    {
        public static Dictionary<String, String> Read(String filePath)
        {
            StreamReader reader = new StreamReader(new FileStream(filePath, FileMode.Open));
            String config = reader.ReadToEnd();
            reader.Close();
            Dictionary<String, String> configDict = new Dictionary<String, String>();
            int curPos = 0;
            while (curPos < config.Length)
            {
                int divPos = config.IndexOf("=", curPos);
                String key = config.Substring(curPos, divPos - curPos);
                int configEnd = config.IndexOf("\r\n", divPos + 1);
                if (configEnd == -1)
                    configEnd = config.Length;
                String value = config.Substring(divPos + 1, configEnd - divPos - 1);
                curPos = configEnd + 2;
                configDict.Add(key, value);
            }
            return configDict;
        }
    }
}
