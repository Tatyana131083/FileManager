using System;
using System.IO;


namespace FileManager
{
    internal class ErrorMessage
    {
        
        //запись ошибки
        internal static void WriteErrorToFile(string errorMessage)
        {
            try
            {
                string logFilePath = "";
                string logFolderPath = "";
                bool appendErrorMessage = false;
                logFilePath = DateTime.Now.ToString("yyyyMMdd");
                logFolderPath = CreateLogFolder("FileManager");
                logFilePath = logFolderPath + logFilePath + "Error.log";
                appendErrorMessage = File.Exists(logFilePath);
                using (StreamWriter streamWriter = new StreamWriter(logFilePath, appendErrorMessage))
                {
                    streamWriter.WriteLine("[" + DateTime.Now.ToLongDateString() + "-" + DateTime.Now.ToLongTimeString() + "]");
                    streamWriter.WriteLine(errorMessage);
                    streamWriter.WriteLine("\n");
                    streamWriter.Flush();
                    streamWriter.Close();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("WriteErrorMessage::" + ex.Message);
            }
        }
        //Создание папки с логами ошибок
        private static string CreateLogFolder(string appName)
        {
            string tempFolderPath = Environment.GetEnvironmentVariable("TEMP");
            if (tempFolderPath != "")
            {
                tempFolderPath += @"\YATA\" + appName + "\\";
                if (!Directory.Exists(tempFolderPath))
                {
                    Directory.CreateDirectory(tempFolderPath);
                }

            }
            return tempFolderPath;
        }

    }
}
