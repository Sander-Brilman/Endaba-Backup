using System;

namespace EnDaBaServices.Settings;

public sealed record BackupSettings(
    string[] BackupLocationPatterns,
    string? EncryptionKey,
    int HoursBetweenFullBackup
) {
    private string? _basePath; 

    private static string DetermineBasePath(string[] locations) 
    {
        // if there is only 1 backup pattern the entire path is the base (minus the * at the end)
        if (locations.Length == 1) {
            return locations[0][..^1];
        }

        string[][] filesParts = locations
            .Select(x => x[..^1]                
                .Split('/')
                .Where(p => p != "")
                .ToArray()
            )
            .ToArray();
        
        int smallestPathSize = filesParts.Select(f => f.Length).Min();

        int lastCommonIndex = 0;
        for (int i = 0; i < smallestPathSize; i++)
        {
            string[] verticalSlice = filesParts
                .Select(x => x[i])
                .ToArray();

            if (verticalSlice.Distinct().Count() == 1) {
                continue;
            } 

            lastCommonIndex = i;
            break;
        }  

        return "/" + string.Join('/', filesParts.First().Take(lastCommonIndex)) + "/";
    }

    public string GetBasePath()
    {
        _basePath ??= DetermineBasePath(BackupLocationPatterns);
        return _basePath;
    } 
}
