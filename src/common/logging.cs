/***********************************************
 *
 *     15WebServ - Webserver       
 *   Copyright © 2021 15peaces
 *
 ***********************************************
 * Logging.
 ***********************************************/
using System;
using System.IO;
using configs;
using showmsg;

namespace logging
{
    class Logging
    {
        [Flags]
        public enum e_setting
        {
            error       = 1,
            warning     = 2,
            filename    = 4,
            version     = 8,
            host        = 16,
            user_agent  = 32,
        }

        public LogConfig conf;
        public StreamWriter logfile;

        public Logging(string config_file)
        {
            conf = new LogConfig();
            conf.init(config_file);
        }
        
        /// <summary>
        /// Shows a message and logs it if configured for logging.
        /// </summary>
        public void LogMsg(string msg, console.e_msg_type type)
        {
            if(conf.GetInt("enable") == 1)
            {
                Directory.CreateDirectory(string.Format("{0}{1}\\", AppDomain.CurrentDomain.BaseDirectory, conf.GetStr("folder")));

                using (logfile = File.AppendText(string.Format("{0}{1}\\{2}.log", AppDomain.CurrentDomain.BaseDirectory, conf.GetStr("folder"), DateTime.Now.ToString("yyyy-MM-dd"))))
                {
                    switch (type)
                    {
                        case console.e_msg_type.MSG_ERROR:
                            if (((Logging.e_setting)conf.GetInt("setting")).HasFlag(Logging.e_setting.error))
                                logfile.WriteLine(string.Format("{0}: [ERROR]: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), msg));
                            break;
                        case console.e_msg_type.MSG_FATALERROR:
                            if (((Logging.e_setting)conf.GetInt("setting")).HasFlag(Logging.e_setting.error))
                                logfile.WriteLine(string.Format("{0}: [FATAL ERROR]: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), msg));
                            break;
                        case console.e_msg_type.MSG_WARNING:
                            if (((Logging.e_setting)conf.GetInt("setting")).HasFlag(Logging.e_setting.error))
                                logfile.WriteLine(string.Format("{0}: [WARNING]: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), msg));
                            break;
                        case console.e_msg_type.MSG_NONE:
                            logfile.WriteLine(string.Format("{0}: [REQUEST]: {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), msg));
                            break;
                    }
                }
            }
            if(type != console.e_msg_type.MSG_NONE)
                console._vShowMessage(type, msg);
        }
    }

    class LogConfig : Config
    {
        protected override void _ReadConfig()
        {
            int[] t_pos = null;
            string log = "";
            t_pos = ini.GroupPos("logging");
            values.Add("enable", config_switch(ini.ReadIniField(t_pos, "enable", "false")));
            values.Add("setting", ini.ReadIniField(t_pos, "setting", "63", 0, 63));
            values.Add("folder", ini.ReadIniField(t_pos, "folder", "logs").Trim(' '));

            if (GetInt("enable") == 1)
            {
                console.status("Configuration file '" + _file + "' read. Logging is enabled.");
                if (((Logging.e_setting)GetInt("setting")).HasFlag(Logging.e_setting.error))
                    log += "error msg - ";
                if (((Logging.e_setting)GetInt("setting")).HasFlag(Logging.e_setting.warning))
                    log += "warning msg - ";
                if (((Logging.e_setting)GetInt("setting")).HasFlag(Logging.e_setting.filename))
                    log += "filename - ";
                if (((Logging.e_setting)GetInt("setting")).HasFlag(Logging.e_setting.version))
                    log += "version - ";
                if (((Logging.e_setting)GetInt("setting")).HasFlag(Logging.e_setting.host))
                    log += "host - ";
                if (((Logging.e_setting)GetInt("setting")).HasFlag(Logging.e_setting.user_agent))
                    log += "user_agent - ";
                console.status("Logged infos: - " + log);
            }
            else
                console.status("Configuration file '" + _file + "' read. Logging is disabled.");
        }
    }
}
