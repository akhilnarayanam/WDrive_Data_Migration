using System.Text.Json;
using Serilog;
using WDrive_Data_Migration_1;
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

            if (!Directory.Exists(settings.HomeDirectoryPath))
            {
                Log.Fatal("Home source path does not exist.");
                return;
            }

            if (!Directory.Exists(settings.ProfilePath))
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

            var homeDirectories = Directory.GetDirectories(settings.HomeDirectoryPath);
            Log.Information("Found {Count} folders in Home.", homeDirectories.Length);

            var profileDirectories = Directory.GetDirectories(settings.ProfilePath);
            Log.Information("Found {Count} folders in Profiles.", profileDirectories.Length);

            Log.Information("Starting Home Directory copy...");

            if (settings.CopyHomeDirectory)
            {
                foreach (var user in userEmailMap.Values)
                {
                    var homeDir = Path.Combine(settings.HomeDirectoryPath, user + settings.TrimToken);
                    var folderName = Path.GetFileName(homeDir);
                    if (string.IsNullOrEmpty(folderName))
                        continue;

                    Log.Information("Processing Home folder: {user}", user);

                    // Derive destination user folder name using business rules
                    string? destUserFolderName = DestFolderResolver.GetLocalPartOfEmail(user, folderName, settings.TrimToken);

                    if (string.IsNullOrEmpty(destUserFolderName))
                    {
                        Log.Warning("Invalid email {Email}. Skipping.", user);
                        continue;
                    }

                    // Destination user root
                    string destUserRoot =
                        Path.Combine(settings.DestinationPath, destUserFolderName);

                    // Migrate HOME → Destination\User\Home (only if CopyHomeDirectory is true)
                    string destHomePath =
                        Path.Combine(destUserRoot, settings.UserHomeFolderName);

                    RetryHelper.Execute(
                        () => Directory.CreateDirectory(destHomePath),
                        $"CreateDirectory:{destHomePath}",
                        settings.MaxRetries
                    );

                    // Lookup email using ShadowAccount (folder name)
                    if (!Directory.Exists(homeDir))
                    {
                        Log.Warning("No Source folder found for {user}. Skipping.", user);
                        continue;
                    }

                    RetryHelper.Execute(
                        () => DirectoryCopyHelper.CopyRootDirectory(homeDir, destHomePath),
                        $"CopyDirectory:Home:{homeDir}",
                        settings.MaxRetries
                    );
                    Log.Information("Copied Home folder for user {User}", destUserFolderName);
                }
            }
            else
            {
                Log.Information("Skipping Home folder copy for (CopyHomeDirectory is false)");
            }

            Log.Information("Starting RProfiles copy...");

            if (settings.CopyProfile)
            {
                foreach (var user in userEmailMap.Values)
                {
                    string rProfileFolderName = $"{user}{settings.TrimToken}{settings.TrimTokenForProfile}";
                    string rProfileSourcePath =
                        Path.Combine(settings.ProfilePath, rProfileFolderName);
                    string profileDestPath =
                        Path.Combine(settings.DestinationPath, user);

                    if (!Directory.Exists(rProfileSourcePath))
                    {
                        Log.Information("Skipping Profiles copy for user {User} (Source directory does not exist)", user);
                    }
                    else
                    {
                        if (!Directory.Exists(profileDestPath))
                        {
                            RetryHelper.Execute(
                                () => Directory.CreateDirectory(profileDestPath),
                                $"CreateDirectory:{profileDestPath}",
                                settings.MaxRetries
                            );
                        }

                        RetryHelper.Execute(
                                () => DirectoryCopyHelper.CopyDirectory(rProfileSourcePath, profileDestPath),
                                $"CopyDirectory:RProfiles:{rProfileSourcePath}",
                                settings.MaxRetries
                            );
                    }
                }
            }
            else
            {
                Log.Information("Skipping RProfiles copy (CopyProfile is false)");
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