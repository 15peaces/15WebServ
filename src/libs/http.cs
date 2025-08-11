/***********************************************
 *
 *     15WebServ - Webserver       
 *   Copyright © 2021 - 2025 15peaces
 *
 ***********************************************
 * 
 * 
 ***********************************************/
using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;

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
            HTTP_11,
        }

        public string[] httpver_str = { "UNKNOWN", "HTTP/0.9", "HTTP/1.0", "HTTP/1.1"};

        byte[] buffer = new byte[] { };
        NetworkStream http_stream;

        Dictionary<string, string> req;

        public void HandleRequest(NetworkStream ns, string request)
        {
            string s, logmes = "";
            string[] split_request;
            string res_str = "";

            http_stream = ns;

            e_httpver ver;

            ver = GetHttpVer(request);

            switch (ver)
            {
                case e_httpver.HTTP_09:
                    console.debug("Connection accepted (HTTP/0.9).");
                    break;
                case e_httpver.HTTP_10:
                    console.debug("Connection accepted (HTTP/1.0).");
                    break;
                case e_httpver.HTTP_11:
                    console.debug("Connection accepted (HTTP/1.1).");
                    break;
                default:
                    // Ignore other requests, unkown HTTP version.
                    console.debug("Connection refused (UNKNOWN VERSION).");
                    _SendBuffer();
                    return;
            }

            // Split request to 0: command; 1: file; 3: version
            split_request = request.Split(' ');

            if (split_request[1].EndsWith("/")) // change request to default file.
                split_request[1] += conf.GetStr("general.indexfile");

            req = SplitRequest(request);

            // check blacklists (will only work if host/user agent is sended...)
            s = GetValueFromRequest("HOST");
            if (blacklist_hosts.Contains(s))
            {
                log.LogMsg("Blacklisted host '" + s + "' requests '" + split_request[1] + "', ignoring...", console.e_msg_type.MSG_WARNING);
                _SendBuffer();
                return;
            }

            s = GetValueFromRequest("USER-AGENT");
            if (blacklist_agents.Contains(s))
            {
                log.LogMsg("Blacklisted user-agent '" + s + "' requests '" + split_request[1] + "', ignoring...", console.e_msg_type.MSG_WARNING);
                _SendBuffer();
                return;
            }

            switch (split_request[0]) // Parse command
            {
                case "GET":
                    byte[] file;
                    // Log request
                    if (log.conf.GetInt("enable") == 1 && log.conf.GetInt("setting") > 3)
                    {
                        logmes += "GET";
                        if (((Logging.e_setting)log.conf.GetInt("setting")).HasFlag(Logging.e_setting.version))
                            logmes += " " + httpver_str[(int)ver];
                        if (((Logging.e_setting)log.conf.GetInt("setting")).HasFlag(Logging.e_setting.filename))
                            logmes += " file: " + split_request[1];
                        if (((Logging.e_setting)log.conf.GetInt("setting")).HasFlag(Logging.e_setting.host))
                            logmes += " host: " + GetValueFromRequest("HOST");
                        if (((Logging.e_setting)log.conf.GetInt("setting")).HasFlag(Logging.e_setting.user_agent))
                            logmes += " user_agent: " + GetValueFromRequest("USER-AGENT");

                        log.LogMsg(logmes, console.e_msg_type.MSG_NONE);
                    }

                    file = ReadFile(split_request[1]);

                    if ((file == null || file.Count() == 0))
                    {
                        if (ver == e_httpver.HTTP_09) // Only send error page for 0.9 and stop here...
                            buffer = ReadFile("../errorpages/404.htm");
                        else
                            buffer = Encoding.ASCII.GetBytes(httpver_str[(int)ver] + " 404 Not Found");

                        _SendBuffer();
                        return;
                    }

                    // Create response packet
                    res_str += httpver_str[(int)ver]+" 200 OK\n\r";
                    res_str += "Date: "+DateTime.Now.ToString("ddd, dd MMM yyy HH:mm:ss")+ " GMT\n\r";
                    res_str += "Server: 15WebServ DEV-BUILD\n\r";
                    res_str += "Content-Type: text/html\n\r\n\r";
                    byte[] response = Encoding.ASCII.GetBytes(res_str);

                    var ret = new byte[response.Length + file.Length];
                    response.CopyTo(ret, 0);
                    file.CopyTo(ret, response.Length);
                    buffer = ret;
                    _SendBuffer();
                    return;
                case "POST":
                    // Log request
                    if (log.conf.GetInt("enable") == 1 && log.conf.GetInt("setting") > 3)
                    {
                        logmes += "POST";
                        if (((Logging.e_setting)log.conf.GetInt("setting")).HasFlag(Logging.e_setting.version))
                            logmes += " " + httpver_str[(int)ver];
                        if (((Logging.e_setting)log.conf.GetInt("setting")).HasFlag(Logging.e_setting.filename))
                            logmes += " command: " + split_request[1];
                        if (((Logging.e_setting)log.conf.GetInt("setting")).HasFlag(Logging.e_setting.host))
                            logmes += " host: " + GetValueFromRequest("HOST");
                        if (((Logging.e_setting)log.conf.GetInt("setting")).HasFlag(Logging.e_setting.user_agent))
                            logmes += " user_agent: " + GetValueFromRequest("USER-AGENT");

                        log.LogMsg(logmes, console.e_msg_type.MSG_NONE);
                    }
                    switch (split_request[1])
                    {
                        default:
                            console.debug("Unknown request '" + split_request[1] + "'... Connection refused.");
                            buffer = Encoding.ASCII.GetBytes(httpver_str[(int)ver] + " 400 Bad Request");
                            _SendBuffer();
                            return;
                    }
                default:
                    console.debug("Unknown command '"+ split_request[0] + "'... Connection refused.");
                    buffer = Encoding.ASCII.GetBytes(httpver_str[(int)ver] + " 400 Bad Request");
                    _SendBuffer();
                    return;
            }
        }

        public e_httpver GetHttpVer(string request)
        {

            if(request.Contains("HTTP/1.0"))
                return e_httpver.HTTP_10;
            else if(request.Contains("HTTP/1.1"))
                return e_httpver.HTTP_11;
            else if((request.Contains("HTTP/0.9")) || (!request.Contains("HTTP/") && request.StartsWith("GET"))) // HTTP/0.9 request, HTTP/1.0 simple request.
                return e_httpver.HTTP_09;

            // Unknown HTTP version.
            return e_httpver.HTTP_UNKNOWN;
        }

                /// <summary>
        /// Send buffer to stream & empty it.
        /// </summary>
        private void _SendBuffer()
        {
            http_stream.Write(buffer, 0, buffer.Length);
            buffer = new byte[] { };
        }

        private Dictionary<string,string> SplitRequest(string request)
        {
            string[] str, str2;
            Dictionary<string, string> ret = new Dictionary<string, string>();

            str = request.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            str = str.Skip(1).ToArray(); // we don't need the first line anymore...

            for(int i = 0; i < str.Length; i++)
            {
                str2 = str[i].Split(new char[] { ':' }, 2, StringSplitOptions.None);
                if(str2.Length>1)
                    ret.Add(str2[0].ToUpper(), str2[1].TrimStart(' '));
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
