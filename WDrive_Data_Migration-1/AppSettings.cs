using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDrive_Data_Migration_1
{
    internal class AppSettings
    {
        public string SourcePath { get; set; } = "";
        public string RProfilesPath { get; set; } = "";
        public string DestinationPath { get; set; } = "";
        public string CsvFilePath { get; set; } = "";       // CSV mapping file

        public string UserHomeFolderName { get; set; } = "";
        public int MaxRetries { get; set; } = 3;

        public string TrimToken { get; set; } = "";


    }


}
