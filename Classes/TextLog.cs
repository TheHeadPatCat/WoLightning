using System;
using System.IO;

namespace WoLightning.Classes
{
    public class TextLog
    {
        public string FilePath { get; set; }
        private bool isFileAvailable = false;

        public TextLog() { }

        public bool validateFile()
        {
            isFileAvailable = File.Exists(FilePath);
            return isFileAvailable;
        }

        public async void Log(string message)
        {
            if (!validateFile()) return;
            await File.AppendAllTextAsync(FilePath, message);
        }

        public void Log(Object obj)
        {

        }

    }
}
