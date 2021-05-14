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

using showmsg;
using configs;
using System.Collections.Generic;

namespace _15WebServ
{
    class WebServ
    {
        TcpListener server;
        public static config conf;
        public static HashSet<string> blacklist;

        static void Main(string[] args)
        {
            WebServ main = new WebServ();
            console.message("======================================");
            console.message("||      15WebServ - Webserver       ||");
            console.message("||   Copyright <c> 2021 15peaces    ||");
            console.message("======================================");
            console.status("Reading default configuration file 'config.ini'...");
            conf = new config("config.ini");
            blacklist = _initBlacklist(conf.GetStr("general.blacklist"));
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
                console.warning("Unexpected end of stream.");
                console.warning("Error caused by " + eos.Message);
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
