using System;
using EnDaBaServices.DataStores.FTP;
using EnDaBaServices.Settings;

namespace EnDaBaBackup;

public static class SettingsChecker
{
    private static SettingsService<BackupSettings> backupSettingsService = new("endaba-settings-backup.json");
    private static SettingsService<FTPSettings> ftpSettingsService = new("endaba-settings-ftp.json");

    public static BackupSettings? GetBackupSettingsFromUser()
    {
        if (backupSettingsService.IsSettingsFilePresent()) 
        {
            return backupSettingsService.GetSettings();
        }

        Console.WriteLine(
        $"""

        - - - - - - - - - - - - - - - - - - - -
            No backup settings file found.  
        - - - - - - - - - - - - - - - - - - - -
        No backup settings was found. a example one will be generated here:
        nano {backupSettingsService.SettingFilePath} 
        
        Here you must configure what folders you want to backup and set your encryption key.
        """    
        );

        backupSettingsService.WriteSettingsToFile(new BackupSettings(
            [
                "/home/john/immich-app/library/backups/*",
                "/home/john/immich-app/library/upload/*",
                "/home/john/immich-app/library/profile/*",
                "/home/john/immich-app/library/library/*",
            ],
            "my_secret_encryption_key",
            24
        ));

        return null;
    }

    public static FTPSettings? GetFTPSettingsFromUser()
    {
        if (ftpSettingsService.IsSettingsFilePresent()) 
        {
            return ftpSettingsService.GetSettings();
        }

                Console.WriteLine(
        $"""

        - - - - - - - - - - - - - - - - - - - -
            No ftp settings file found.  
        - - - - - - - - - - - - - - - - - - - -
        No ftp settings was found. a example one will be generated here:
        nano {ftpSettingsService.SettingFilePath} 
        
        Here you must configure your FTP credentials.

        """    
        );

        ftpSettingsService.WriteSettingsToFile(new FTPSettings("ftp.example.com", 21, "johnftp", "my_secret_ftp_password"));

        return null;
    }

}
