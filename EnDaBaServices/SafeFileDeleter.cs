using System;

namespace ImmichEnDaBa;

public static class SafeFileDelete
{
    private static string _tempFolder = Path.GetTempPath(); 

    private static string[] allowedFilesToDelete = [
        ".zip",
    ];

    public static void Delete(string path) 
    {
        if (path.StartsWith(_tempFolder) is false) {
            throw new ArgumentException("can only delete files in the temp folder");
        }

        foreach (var fileExtension in allowedFilesToDelete)
        {
            if (path.EndsWith(fileExtension) is false) {
                throw new ArgumentException($"can only delete {fileExtension} files");
            }        
        }

        File.Delete(path);
    }
}
