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
        /// <summary>
        /// Copies the contents of the source directory to the destination directory, including all subdirectories and files.
        /// </summary>
        /// <param name="sourceDir">Source Directory</param>
        /// <param name="destDir">Destination Directory</param>
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

        // Copy Directory into the user's root, but handle "Downloads" specially:
        // - copy contents of source Downloads -> Root\oldDownloads - leaving the destUserRoot\Downloads folder empty for the user to populate
        public static void CopyRootDirectory(string sourceDir, string destUserRoot)
        {
            // Copy files in the RProfile root into the user root
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destUserRoot, fileName);
                File.Copy(file, destFile, overwrite: true);
            }

            foreach (string directory in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(directory);

                if (string.Equals(dirName, "Downloads", StringComparison.OrdinalIgnoreCase))
                {
                    // Copy contents of source Downloads into destUserRoot\oldDownloads
                    string destOldDownloads = Path.Combine(destUserRoot, "oldDownloads");
                    Directory.CreateDirectory(destOldDownloads);
                    CopyDirectory(directory, destOldDownloads);

                    // Ensure an empty destUserRoot\Downloads folder exists
                    string destDownloads = Path.Combine(destUserRoot, "Downloads");
                    Directory.CreateDirectory(destDownloads);

                    continue;
                }

                string destSubDir = Path.Combine(destUserRoot, dirName);
                Directory.CreateDirectory(destSubDir);
                CopyDirectory(directory, destSubDir);
            }

            Log.Information($"Successfully Copied RProfiles from {sourceDir} to {destUserRoot}");
        }
    }
}