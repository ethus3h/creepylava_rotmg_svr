using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Specialized;

namespace db
{
    public class SimpleSettings : IDisposable
    {
        Dictionary<string, string> values;
        string cfgFile;
        public SimpleSettings(string id)
        {
            values = new Dictionary<string, string>();
            cfgFile = Path.Combine(Environment.CurrentDirectory, id + ".cfg");
            if (File.Exists(cfgFile))
                using (StreamReader rdr = new StreamReader(File.OpenRead(cfgFile)))
                {
                    string line;
                    while ((line = rdr.ReadLine()) != null)
                    {
                        int i = line.IndexOf(":");
                        if (i == -1) throw new ArgumentException("Invalid settings.");
                        string val = line.Substring(i + 1);

                        values.Add(line.Substring(0, i),
                            val.Equals("null", StringComparison.InvariantCultureIgnoreCase) ? null : val);
                    }
                }
        }

        public void Dispose()
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(File.OpenWrite(cfgFile)))
                    foreach (var i in values)
                        writer.WriteLine("{0}:{1}", i.Key, i.Value == null ? "null" : i.Value);
            }
            catch
            {
            }
        }

        public string GetValue(string key, string def = null)
        {
            string ret;
            if (!values.TryGetValue(key, out ret))
            {
                if (def == null)
                    throw new ArgumentException(string.Format("'{0}' does not exist in settings.", key));
                ret = values[key] = def;
            }
            return ret;
        }

        public T GetValue<T>(string key, string def = null)
        {
            string ret;
            if (!values.TryGetValue(key, out ret))
            {
                if (def == null)
                    throw new ArgumentException(string.Format("'{0}' does not exist in settings."));
                ret = values[key] = def;
            }
            return (T)Convert.ChangeType(ret, typeof(T));
        }

        public void SetValue(string key, string val)
        {
            values[key] = val;
        }
    }
}
