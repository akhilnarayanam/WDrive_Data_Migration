using Serilog;
using System;
using System.IO;
using System.Text.Json;
using WDrive_Data_Migration_1;
using System.Threading.Tasks;
    class Program
    {
        static void Main(string[] args)
        {
            LoggingConfig.ConfigureLogging();
            Log.Information("--- Migration Started ---");

            try
            {
                var settings = LoadSettings("appsettings.json");
                if (settings == null)
                {
                    Log.Fatal("Unable to load appsettings.json. Exiting.");
                    return;
                }

                if (!Directory.Exists(settings.SourcePath))
                {
                    Log.Fatal("Home source path does not exist.");
                    return;
                }

                if (!Directory.Exists(settings.RProfilesPath))
                {
                    Log.Fatal("RProfiles path does not exist.");
                    return;
                }

                if (!File.Exists(settings.CsvFilePath))
                {
                    Log.Fatal("CSV file not found at {Path}", settings.CsvFilePath);
                    return;
                }
                Directory.CreateDirectory(settings.DestinationPath);

                // Load CSV once into memory
                var userEmailMap = CsvUserMapping.LoadUserEmailMapping(settings.CsvFilePath);
                Log.Information("Loaded {Count} user mappings from CSV", userEmailMap.Count);

                var homeDirectories = Directory.GetDirectories(settings.SourcePath);
                Log.Information("Found {Count} folders in Home.", homeDirectories.Length);

                foreach (var homeDir in homeDirectories)
                {
                    var folderName = Path.GetFileName(homeDir);
                    if (string.IsNullOrEmpty(folderName))
                        continue;

                    Log.Information("Processing Home folder: {Folder}", folderName);

                    // Lookup email using ShadowAccount (folder name)

                    var email = userEmailMap.GetValueOrDefault(folderName);

                    if (string.IsNullOrEmpty(email))
                    {
                        Log.Warning("No CSV mapping found for ShadowAccount {Folder}. Skipping.", folderName);
                        continue;
                    }

                    // Derive destination user folder name using business rules

                    string? destUserFolderName = DestFolderResolver.GetLocalPartOfEmail(email, folderName, settings.TrimToken);
                    if (string.IsNullOrEmpty(destUserFolderName))
                    {
                        Log.Warning("Invalid email {Email}. Skipping.", email);
                        continue;
                    }

                    // Destination user root
                    string destUserRoot =
                        Path.Combine(settings.DestinationPath, destUserFolderName);

                    //Migrate HOME → Destination\User\Home

                    string destHomePath =
                        Path.Combine(destUserRoot, settings.UserHomeFolderName);

                    RetryHelper.Execute(
                        () => Directory.CreateDirectory(destHomePath),
                        $"CreateDirectory:{destHomePath}",
                        settings.MaxRetries
                    );

                    RetryHelper.Execute(
                        () => DirectoryCopyHelper.CopyDirectory(homeDir, destHomePath),
                        $"CopyDirectory:Home:{homeDir}",
                        settings.MaxRetries
                    );


                    // Migrate RProfiles → Destination\User (if exists)

                    string rProfileFolderName = $"{folderName}.V2";
                    string rProfileSourcePath =
                        Path.Combine(settings.RProfilesPath, rProfileFolderName);

                    if (Directory.Exists(rProfileSourcePath))
                    {
                        RetryHelper.Execute(
                            () => DirectoryCopyHelper.CopyDirectory(rProfileSourcePath, destUserRoot),
                            $"CopyDirectory:RProfiles:{rProfileSourcePath}",
                            settings.MaxRetries
                        );
                    }
                    else
                    {
                        Log.Information("RProfiles not found for user {User}. Skipping RProfiles migration.",
                                        destUserFolderName);
                    }
                }


                Log.Information("Migration completed successfully.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Unhandled exception occurred during migration.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
        static AppSettings? LoadSettings(string fileName)
        {
            try
            {
                var fullPath = Path.Combine(AppContext.BaseDirectory, fileName);
                var json = File.ReadAllText(fullPath);

                return JsonSerializer.Deserialize<AppSettings>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
                return null;
            }
        }
    }
