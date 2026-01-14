using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WDrive_Data_Migration_1
{
    internal class DirectoryCopyHelper
    {
       public static void CopyDirectory(string sourceDir, string destDir)
        {
            foreach (string file in Directory.GetFiles(sourceDir))           // Copy files
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destDir, fileName);
                File.Copy(file, destFile, overwrite: true);
            }

            foreach (string directory in Directory.GetDirectories(sourceDir)) // Copy subdirectories
            {
                string dirName = Path.GetFileName(directory);
                string destSubDir = Path.Combine(destDir, dirName);
                Directory.CreateDirectory(destSubDir);
                CopyDirectory(directory, destSubDir);
            }
            Log.Information($"Successfully Copied from {sourceDir} to {destDir}");

        }
    }
}
