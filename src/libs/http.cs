/***********************************************
 *
 *     15WebServ - Webserver       
 *   Copyright © 2021 15peaces
 *
 ***********************************************
 * 
 * 
 ***********************************************/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using logging;
using showmsg;

namespace _15WebServ
{
    class HttpHandler:WebServ
    {
        public enum e_httpver
        {
            HTTP_UNKNOWN,
            HTTP_09,
            HTTP_10,
        }

        public string[] httpver_str = { "UNKNOWN", "HTTP/0.9", "HTTP/1.0"};

        Dictionary<string, string> req;

        public byte[] HandleRequest(string request)
        {
            string fn, logmes = "";
            e_httpver ver;

            ver = GetHttpVer(request);
            fn = request.Split(' ')[1];

            if (fn.EndsWith("/")) // change request to default file.
                fn += conf.GetStr("general.indexfile");

            req = SplitRequest(request);

            // check blacklist (will only work if host is sended...)
            if (req.TryGetValue("HOST", out string t))
            {
                t = t.Trim(' ');
                if (blacklist.Contains(GetValueFromRequest("HOST")))
                {
                    log.LogMsg("Blacklisted host '" + t + "' requests '" + fn + "', ignoring...", console.e_msg_type.MSG_WARNING);
                    return new byte[] { };
                }
            }

            // Log request
            if (log.conf.GetInt("enable") == 1 && log.conf.GetInt("setting") > 3)
            {
                logmes += "GET";
                if (((Logging.e_setting)log.conf.GetInt("setting")).HasFlag(Logging.e_setting.version))
                    logmes += " " + httpver_str[(int)ver];
                if (((Logging.e_setting)log.conf.GetInt("setting")).HasFlag(Logging.e_setting.filename))
                    logmes += " file: " + fn;
                if (((Logging.e_setting)log.conf.GetInt("setting")).HasFlag(Logging.e_setting.host))
                    logmes += " host: " + t;
                if (((Logging.e_setting)log.conf.GetInt("setting")).HasFlag(Logging.e_setting.user_agent))
                    logmes += " user_agent: " + GetValueFromRequest("USER-AGENT");

                log.LogMsg(logmes, console.e_msg_type.MSG_NONE);
            }

            switch (ver)
            {
                case e_httpver.HTTP_09:
                    console.debug("Connection accepted (HTTP/0.9).");
                    return (ReadFile(fn));
                case e_httpver.HTTP_10:
                    console.debug("Connection accepted (HTTP/1.0).");
                    return (ReadFile(fn));
                default:
                    // Ignore other requests, unkown HTTP version.
                    return new byte[] { };
            }
        }

        public e_httpver GetHttpVer(string request)
        {
            if (request.StartsWith("GET"))
            {
                if (request.Contains("HTTP/1.0"))  // HTTP/1.0 request.
                    return e_httpver.HTTP_10;
                else // HTTP/0.9 request, HTTP/1.0 simple request or not detected.
                    return e_httpver.HTTP_09;
            }

            // Unknown HTTP request.
            return e_httpver.HTTP_UNKNOWN;
        }

        private Dictionary<string,string> SplitRequest(string request)
        {
            int i;
            string[] str, str2;
            Dictionary<string, string> ret = new Dictionary<string, string>();

            str = request.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            str = str.Skip(1).ToArray(); // we don't need the first line anymore...

            for(i = 0; i < str.Length; i++)
            {
                str2 = str[i].Split(new char[] { ':' }, 2, StringSplitOptions.None);
                ret.Add(str2[0].ToUpper(), str2[1]);
            }

            return ret;
        }

        private string GetValueFromRequest(string name)
        {
            if (req.TryGetValue(name, out string t))
                return t;
            return "";
        }

        private byte[] ReadFile(string filename)
        {
            byte[] buf;
            filename = string.Format("{0}{1}\\{2}", AppDomain.CurrentDomain.BaseDirectory, conf.GetStr("general.basedir"), filename);

            try
            {
                using (StreamReader stream = new StreamReader(filename))
                {
                    buf = System.Text.Encoding.Default.GetBytes(stream.ReadToEnd());
                }
            }
            catch (Exception e)
            {
                log.LogMsg("The file could not be read:", console.e_msg_type.MSG_ERROR);
                log.LogMsg(e.Message, console.e_msg_type.MSG_ERROR);
                return new byte[] { };
            }

            return buf;
        }
    }
}
