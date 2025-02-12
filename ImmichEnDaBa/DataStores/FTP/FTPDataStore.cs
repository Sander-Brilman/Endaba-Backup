using System;
using FluentFTP;

namespace ImmichEnDaBa.DataStores.FTP;

public class FTPDataStore(AsyncFtpClient client) : IDataStore
{
    private readonly AsyncFtpClient client = client;

    private readonly HashSet<string> existingFolders = [];

    public async Task<bool> CreateFolder(string remoteFolderPath)
    {
        return await client.CreateDirectory(remoteFolderPath);
    }

    public async Task<bool> DoesFolderExist(string remoteFolderPath)
    {
        return await client.DirectoryExists(remoteFolderPath);
    }

    public async Task DeleteFile(string remoteFilePath)
    {
        await client.Rename(remoteFilePath, remoteFilePath + ".deleted");
    }

    public async Task<bool> DoesFileExist(string remoteFilePath)
    {
        return await client.FileExists(remoteFilePath);
    }

    public async Task DownloadFile(string remoteFilePath, string localFilePath)
    {
        await client.DownloadFile(remoteFilePath, localFilePath);
    }

    public async Task<string[]> GetFolderContents(string remoteFolderPath)
    {
        return await client.GetNameListing(remoteFolderPath);
    }

    public async Task UploadFile(string remoteFilePath, string localFilePath)
    {
        string folder = Path.GetDirectoryName(remoteFilePath)! + "/";

        if (existingFolders.Contains(folder) is false) {
            bool createdSuccessfully = await CreateFolder(folder);
            existingFolders.Add(folder);
        }

        await client.UploadFile(localFilePath, remoteFilePath, FtpRemoteExists.Overwrite);
    }


    public static async Task<FTPDataStore> GenerateNewFromCredentials(string ftpHost, int ftpPort, string ftpUsername, string ftpPassword) 
    {
        AsyncFtpClient client = new(
            host: ftpHost,
            user: ftpUsername,
            pass: ftpPassword,
            port: ftpPort
        );

        client.Config.ConnectTimeout = 2000;
        client.Config.EncryptionMode = FtpEncryptionMode.Implicit;

        await client.AutoConnect();
    
        return new(client);
    }


}
