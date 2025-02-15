using System;

namespace ImmichEnDaBa;

public sealed class PathResolver
{
    public string[] GetFilesFromPath(string fuzzyFolderPath) 
    {
        if (fuzzyFolderPath.EndsWith('*') is false) {
            throw new ArgumentException("The folder path must end with a *");
        }

        string basePath = fuzzyFolderPath[..^1];

        return GetRecursiveFiles(basePath);
    }

    private static string[] GetRecursiveFiles(string path) 
    {
        List<string> files = [.. Directory.GetFiles(path)];


        string[] folders = Directory.GetDirectories(path);
        foreach (var folder in folders)
        {
            files.AddRange(GetRecursiveFiles(folder));
        }

        return [..files];
    }
}
