using Serilog;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

// Holds all configurable application settings loaded from appsettings.json
class AppSettings
{
    public string SourcePath { get; set; } = "";
    public string RProfilesPath { get; set; } = "";
    public string DestinationPath { get; set; } = "";
    public string CsvFilePath { get; set; } = "";       // CSV mapping file

    public string UserHomeFolderName { get; set; } = "";
    public int MaxRetries { get; set; } = 3;

    public string TrimToken { get; set; } = "";
}

static class RetryHelper
{
    // Executes an action with retry logic and structured logging
    public static void Execute(Action action, string operation, int maxRetries)
    {
        int attempt = 0;

        while (true)
        {
            try
            {
                attempt++;
                action();
                return;
            }
            catch (Exception ex)
            {
                if (attempt >= maxRetries)
                {
                    Log.Error(ex,
                        "Operation '{Operation}' failed after {Attempts} attempts",
                        operation, attempt);
                    return;
                }

                Log.Warning(ex,
                    "Operation '{Operation}' failed on attempt {Attempt}. Retrying...",
                    operation, attempt);
            }
        }
    }
}

class Program
{

    // Application entry point
    static void Main(string[] args)
    {
        ConfigureLogging();
        Log.Information("--- Migration Started ---");

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
        var userEmailMap = LoadUserEmailMapping(settings.CsvFilePath);
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

            string? destUserFolderName = GetLocalPartOfEmail(email, folderName, settings.TrimToken);
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
                () => CopyDirectory(homeDir, destHomePath),
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
                    () => CopyDirectory(rProfileSourcePath, destUserRoot),
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
        Log.CloseAndFlush();
    }

    // Loads CSV into dictionary: ShadowAccount → UserEmail
    static Dictionary<string, string> LoadUserEmailMapping(string csvPath)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = File.ReadAllLines(csvPath);

        for (int i = 1; i < lines.Length; i++) // skip header
        {
            var cols = lines[i].Split(',');
            if (cols.Length < 2) continue;

            string email = cols[0].Trim();
            string shadowAccount = cols[1].Trim();

            if (!map.ContainsKey(shadowAccount))
                map.Add(shadowAccount, email);
        }

        return map;
    }


    // Configures Serilog
    static void ConfigureLogging()
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(
                path: "logs/migration-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();
    }

    // Reads and deserializes application configuration file
    static AppSettings? LoadSettings(string fileName)
    {
        try
        {
            var json = File.ReadAllText(fileName);
            return JsonSerializer.Deserialize<AppSettings>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    // Determines destination folder name based on email and userId rules
    static string? GetLocalPartOfEmail(string email, string userId, string trimToken)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(userId))
            return null;

        email = email.Trim();
        userId = userId.Trim();

        bool containsAtSym = email.Contains("atsym", StringComparison.OrdinalIgnoreCase);
        bool containsDotCom = email.Contains(".com", StringComparison.OrdinalIgnoreCase);

        if (containsAtSym && containsDotCom)
        {
            int tokenIndex = userId.IndexOf(trimToken, StringComparison.OrdinalIgnoreCase);
            return userId.Substring(0, tokenIndex);
        }

        return email;



    }


    //copies files and folders from source to destination
    static void CopyDirectory(string sourceDir, string destDir)
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
