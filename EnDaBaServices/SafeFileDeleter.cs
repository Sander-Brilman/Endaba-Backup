using System;
using EnDaBaBackup;

namespace EnDaBaServices;

public static class SafeFileDelete
{
    private static readonly string _tempFolder = Path.GetTempPath(); 

    private static readonly string[] allowedFilesToDelete = [
        AppSettings.ZipFileExtension,
        AppSettings.HashFileExtension,
    ];

    public static void Delete(string path) 
    {
        if (path.StartsWith(_tempFolder) is false) {
            throw new ArgumentException("can only delete files in the temp folder");
        }

        bool hasAllowedExtension = false;
        foreach (var fileExtension in allowedFilesToDelete)
        {
            if (path.EndsWith(fileExtension)) {
                hasAllowedExtension = true;
                break;
            }        
        }

        if (hasAllowedExtension is false) {
            throw new ArgumentException($"cannot delete files with a {Path.GetExtension(path)} extension");
        }

        File.Delete(path);
    }
}
