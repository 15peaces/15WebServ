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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;

using showmsg;
using configs;
using logging;

namespace _15WebServ
{
    class WebServ
    {
        TcpListener server;
        public static Config conf;
        public static Logging log;
        public static HashSet<string> blacklist;

        static void Main(string[] args)
        {
            WebServ main = new WebServ();
            console.message("======================================");
            console.message("||      15WebServ - Webserver       ||");
            console.message("||   Copyright <c> 2021 15peaces    ||");
            console.message("======================================");
            console.status("Reading default configuration file 'config.ini'...");
            conf = new Config();
            conf.init("config.ini");
            console.status("Init logging...");
            log = new Logging("logging.ini");
            blacklist = _initBlacklist(conf.GetStr("general.blacklist"));
            console.status("Done reading blacklist, added '" + blacklist.Count + "' entries");
            console.status("Start listening @ TCP port "+ conf.GetInt("general.port") + "...");
            main.server = new TcpListener(IPAddress.Loopback, conf.GetInt("general.port"));
            main.server.Start();
            console.status("Server online, waiting for connections...");
            main._accept_connection();  //accepts incoming connections
            Console.ReadLine();
        }

        private void _accept_connection()
        {
            server.BeginAcceptTcpClient(_handle_connection, server);  //this is called asynchronously and will run in a different thread
        }

        private void _handle_connection(IAsyncResult result)  //the parameter is a delegate, used to communicate between threads
        {
            _accept_connection();  //once again, checking for any other incoming connections
            TcpClient client = server.EndAcceptTcpClient(result);  //creates the TcpClient

            NetworkStream ns = client.GetStream();
            BinaryReader inStream = new BinaryReader(ns);

            byte b = 0;
            string msg = "";      //This will contain all the stuff the browser transmitted.
            byte[] buf;

            try
            {
                while (ns.DataAvailable)
                {
                    b = (byte)inStream.ReadSByte();
                    msg += (char)b;
                }
            }
            catch (EndOfStreamException eos)
            {
                log.LogMsg("Unexpected end of stream.", console.e_msg_type.MSG_WARNING);
                log.LogMsg("Error caused by " + eos.Message, console.e_msg_type.MSG_WARNING);
            }

            if (msg != "")
            {
                console.debug(msg);
                HttpHandler http = new HttpHandler();
                buf = http.HandleRequest(msg);
                ns.Write(buf, 0, buf.Length);
            }

            ns.Close();
            client.Close();
            console.debug("End of stream.");
        }

        static private HashSet<string> _initBlacklist(string str)
        {
            HashSet<string> ret = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase) { };

            if (str.Length > 0)
            {
                if (str.Contains(","))
                {
                    string[] s = str.Split(',');

                    for (int i = 0; i < s.Length; i++)
                        ret.Add(s[i]);
                }
                else
                    ret.Add(str);
            }

            return ret;
        }
    }
}
