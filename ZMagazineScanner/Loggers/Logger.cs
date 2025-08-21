using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ZMagazineScanner.Loggers
{
    class Logger
    {
        private string _localLogLocation { get; set; }
        private bool _isLogToTextFile { get; set; }
        private bool _isLogToConsole { get; set; }

        public Logger(string localLogLocation, bool isLogToTextFile, bool isLogToConsole = true)
        {
            _localLogLocation = localLogLocation;
            _isLogToTextFile = isLogToTextFile;
            _isLogToConsole = isLogToConsole;
        }

        public void Log(string logMessage)
        {
            if (_isLogToConsole)
            {
                LogToConsole(logMessage);
            }

            if (_isLogToTextFile)
            {
                LogToTextFile(logMessage);
            }
        }

        private void LogToTextFile(string logMessage)
        {
            try
            {
                string path = String.Format(@"{0}Url_Scanner_Log_{1}_{2}_{3}.txt", _localLogLocation, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Year);

                using (StreamWriter sw = new StreamWriter(path, append: true))
                {
                    sw.WriteLine(logMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("Could not write previous entry in the Log TXT due to an error: {0}", ex.ToString()));
            }
        }

        private void LogToConsole(string logMessage)
        {
            //Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine(logMessage);
        }
    }
}
