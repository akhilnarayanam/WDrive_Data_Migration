# W Drive Migration Tool

## Overview

This tool migrates user data from the existing **Home** and **RProfiles** locations to the new **Folder_Redirects** location.

For each user:

* Copies Home data → `DestinationPath\UserName\UserHomeFolderName`
* Copies RProfiles data → `DestinationPath\UserName` (if available)

---

## Prerequisites

Before running the tool, ensure that it is executed on a machine (server/VM/workstation) that has:

* Read access to the **SourcePath** location.
* Read access to the **RProfilesPath** location.
* Read and Write access to the **DestinationPath** location.
* Access to the **UserMappings CSV** file.

The tool must be run from a box that can access all configured paths in `appsettings.json`. If any of these locations are inaccessible, the migration will fail.

### Example

```text
SourcePath       : \\ServerA\Home
RProfilesPath    : \\ServerA\RProfiles
DestinationPath  : \\ServerB\Folder_Redirects
```

The machine running the tool must have network access and appropriate permissions to all the above locations.


## Configuration

Update the values in `appsettings.json` before running the tool.

```json
{
  "SourcePath": "C:\\Integration\\Home",
  "RProfilesPath": "C:\\Integration\\RProfiles",
  "DestinationPath": "\\\\shr-ms01\\Integration\\Folder_Redirects",
  "CsvFilePath": "C:\\Integration\\UserMappings.csv",
  "UserHomeFolderName": "Home",
  "MaxRetries": 3,
  "TrimToken": "I00"
}
```

### Configuration Details

| Setting            | Description                                                                               |
| ------------------ | ----------------------------------------------------------------------------------------- |
| SourcePath         | Contains all user Home folders. Example: `akhil.kumarI00`, `ashish.ranjanI00`.            |
| RProfilesPath      | Contains all user RProfiles folders. Example: `akhil.kumarI00.V2`, `ashish.ranjanI00.V2`. |
| DestinationPath    | Location where migrated user folders will be created.                                     |
| CsvFilePath        | Path to the UserMappings CSV file.                                                        |
| UserHomeFolderName | Folder created under each user in the destination. Home data is copied into this folder.  |
| MaxRetries         | Number of retry attempts for file operations.                                             |
| TrimToken          | Token used when deriving the destination user folder name. Example: if the user folder is akhil.kumarI00 and the TrimToken is I00, the destination folder becomes akhil.kumar.                |

---

## UserMappings CSV

Create a CSV file with the following columns:

```csv
CN,SamAccountName
akhil.kumar,akhil.kumarI00
ashish.ranjan,ashish.ranjanI00
```

### Column Details

| Column         | Description                             |
| -------------- | --------------------------------------- |
| CN             | Destination user folder name.           |
| SamAccountName | User folder name present in SourcePath. |

---

## How the Tool Works

For each folder in **SourcePath**:

1. Reads the corresponding user mapping from the CSV file.
2. Determines the destination user folder name.
3. Creates the destination user folder if it does not exist.
4. Copies Home data to:

```text
DestinationPath\UserName\UserHomeFolderName
```

5. Checks for the corresponding RProfiles folder:

```text
Home Folder      : akhil.kumarI00
RProfiles Folder    : akhil.kumarI00.V2
```

6. If the RProfiles folder exists, copies its contents to:

```text
DestinationPath\UserName
```

---

## Example

### Source

```text
Home\akhil.kumarI00
RProfiles\akhil.kumarI00.V2
```

### CSV Mapping

```csv
CN,SamAccountName
akhil.kumar,akhil.kumarI00
```

### Destination

```text
Folder_Redirects
└── akhil.kumar
    ├── Home
    ├── AppData
    ├── Documents
    └── Downloads
```

### Result

* Home data copied to:

```text
Folder_Redirects\akhil.kumar\Home
```

* RProfiles data copied to:

```text
Folder_Redirects\akhil.kumar
```

---

## Running the Tool

Build and run:

```bash
dotnet build
dotnet run
```

---

## Notes

* Users not present in the CSV file are skipped.
* Missing RProfiles folders are skipped and logged.
* All migration activities are logged.
* The tool is safe to run multiple times.
* Existing files are overwritten during migration.
