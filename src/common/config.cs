/***********************************************
 *
 *     15WebServ - Webserver       
 *   Copyright © 2021 15peaces
 *
 ***********************************************
 * Common configuration file handling.
 ***********************************************/
using System;
using System.IO;
using System.Collections.Generic;
using showmsg;
using inilib;

namespace configs
{
    class Config
    {
        protected string _file;
        protected IniHandle ini;

        // Holding the current config values.
        public Dictionary<string,object> values;

        public void init(string filename)
        {
            _file = filename;
            filename = string.Format("{0}conf\\{1}", AppDomain.CurrentDomain.BaseDirectory, filename);

            try
            {
                using (StreamReader stream = new StreamReader(filename))
                {
                    ini = new IniHandle(_file, stream.ReadToEnd());
                    values = new Dictionary<string, object>();
                }
            }
            catch (Exception e)
            {
                console.error("The file could not be read:");
                console.error(e.Message);
                return;
            }

            _ReadConfig();
        }

        protected short config_switch(string str)
        {
            str = str.ToLower();

            if (str.Contains("true") || str.Contains("on") || str.Contains("yes") || str.Contains("oui") || str.Contains("ja") || str.Contains("si"))
                return 1;
            if (str.Contains("false") || str.Contains("off") || str.Contains("no") || str.Contains("non") || str.Contains("nein"))
                return 0;

            return Convert.ToInt16(str);
        }

        public int GetInt(string value)
        {
            int t = 0;
            object to = GetValue(value);

            if (to != null)
                t = Convert.ToInt32(to.ToString());

            return t;
        }

        public string GetStr(string value)
        {
            string t = "";
            object to = GetValue(value);

            if (to != null)
                t = to.ToString();

            return t;
        }

        private object GetValue(string value)
        {
            object t;
            if (values.TryGetValue(value, out t))
                return t;

            return null;
        }

        protected virtual void _ReadConfig()
        {
            int[] t_pos = null;
            // read settings.
            // Server configs
            t_pos = ini.GroupPos("general");
            values.Add("general.port", ini.ReadIniField(t_pos, "port", "80", 0, 65535));
            values.Add("general.basedir", ini.ReadIniField(t_pos, "basedir", "www").Trim(' '));
            values.Add("general.indexfile", ini.ReadIniField(t_pos, "indexfile", "index.htm").Trim(' '));
            values.Add("general.blacklist", ini.ReadIniField(t_pos, "blacklist", "").Trim(' '));

            console.status("Configuration file '"+_file+"' read.");
        }
    }
}
