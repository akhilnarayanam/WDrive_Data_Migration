using Serilog;

namespace WDrive_Data_Migration_1
{
    internal class DirectoryCopyHelper
    {
        public static void CopyDirectory(string sourceDir, string destDir)
        {
            try
            {
                foreach (string file in Directory.GetFiles(sourceDir))           // Copy files
                {
                    try
                    {
                        string fileName = Path.GetFileName(file);
                        string destFile = Path.Combine(destDir, fileName);
                        File.Copy(file, destFile, overwrite: true);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to copy file {File}. Continuing with other files.", file);
                    }
                }

                foreach (string directory in Directory.GetDirectories(sourceDir)) // Copy subdirectories
                {
                    try
                    {
                        string dirName = Path.GetFileName(directory);
                        string destSubDir = Path.Combine(destDir, dirName);
                        Directory.CreateDirectory(destSubDir);
                        CopyDirectory(directory, destSubDir);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to copy directory {Directory}. Continuing with other directories.", directory);
                    }
                }
                Log.Information($"Successfully Copied from {sourceDir} to {destDir}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in CopyDirectory for source {SourceDir} to {DestDir}", sourceDir, destDir);
            }
        }

        // Copy CopyRootDirectory into the user's root, but handle "Downloads" specially:
        // - copy contents of source Downloads -> destUserRoot\oldDownloads
        // - ensure destUserRoot\Downloads exists and is empty
        public static void CopyRootDirectory(string sourceDir, string destUserRoot)
        {
            try
            {
                // Copy files in the RProfile root into the user root
                foreach (string file in Directory.GetFiles(sourceDir))
                {
                    try
                    {
                        string fileName = Path.GetFileName(file);
                        string destFile = Path.Combine(destUserRoot, fileName);
                        File.Copy(file, destFile, overwrite: true);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to copy file {File}. Continuing with other files.", file);
                    }
                }

                foreach (string directory in Directory.GetDirectories(sourceDir))
                {
                    try
                    {
                        string dirName = Path.GetFileName(directory);

                        if (string.Equals(dirName, "Downloads", StringComparison.OrdinalIgnoreCase))
                        {
                            try
                            {
                                // Copy contents of source Downloads into destUserRoot\oldDownloads
                                string destOldDownloads = Path.Combine(destUserRoot, "oldDownloads");
                                Directory.CreateDirectory(destOldDownloads);
                                CopyDirectory(directory, destOldDownloads);

                                // Ensure an empty destUserRoot\Downloads folder exists
                                string destDownloads = Path.Combine(destUserRoot, "Downloads");
                                Directory.CreateDirectory(destDownloads);

                                // Remove any files/subdirs in destDownloads to guarantee it is empty
                                foreach (var f in Directory.GetFiles(destDownloads))
                                {
                                    try { File.Delete(f); } catch { /* ignore */ }
                                }
                                foreach (var d in Directory.GetDirectories(destDownloads))
                                {
                                    try { Directory.Delete(d, true); } catch { /* ignore */ }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Warning(ex, "Failed to copy Downloads folder from {Directory}. Continuing with other directories.", directory);
                            }

                            continue;
                        }

                        string destSubDir = Path.Combine(destUserRoot, dirName);
                        Directory.CreateDirectory(destSubDir);
                        CopyDirectory(directory, destSubDir);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to copy directory {Directory}. Continuing with other directories.", directory);
                    }
                }

                Log.Information($"Successfully Copied RProfiles from {sourceDir} to {destUserRoot}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error in CopyRProfileDirectory for source {SourceDir} to {DestUserRoot}", sourceDir, destUserRoot);
            }
        }
    }
}