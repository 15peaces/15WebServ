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

        public byte[] HandleRequest(string request)
        {
            string fn, t;
            e_httpver ver;
            Dictionary<string, string> req;

            ver = GetHttpVer(request);
            fn = request.Split(' ')[1];

            if (fn.EndsWith("/")) // change request to default file.
                fn += conf.GetStr("general.indexfile");

            req = SplitRequest(request);

            // check blacklist (will only work if host is sended...)
            if(req.TryGetValue("Host", out t))
                if (blacklist.Contains(t))
                {
                    console.warning("Blacklisted host '" + t + "' requests '" + fn + "', ignoring...");
                    return new byte[] { };
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
                ret.Add(str2[0], str2[1]);
            }

            return ret;
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
                console.error("The file could not be read:");
                console.error(e.Message);
                return new byte[] { };
            }

            return buf;
        }
    }
}
