using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDrive_Data_Migration_1
{
    internal class AppSettings
    {
        public string HomeDirectoryPath { get; set; } = "";
        public string ProfilePath { get; set; } = "";

        public string DestinationPath { get; set; } = "";
        public string DestinationProfileFolder { get; set; } = "";

        public string CsvFilePath { get; set; } = "";       // CSV mapping file

        public string UserHomeFolderName { get; set; } = "";

        public int MaxRetries { get; set; } = 3;

        public string TrimToken { get; set; } = "";
        public string TrimTokenForProfile { get; set; } = "";

        public bool CopyHomeDirectory { get; set; } = true;
        public bool CopyProfile { get; set; } = true;
    }
}