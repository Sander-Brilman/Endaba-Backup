using System;
using EnDaBaServices.DataStores.FTP;
using FluentFTP;
using FluentFTP.Exceptions;

namespace EnDaBaServices.DataStores.FTP;

public sealed class FTPDataStore(AsyncFtpClient client) : IDataStore
{
    private readonly AsyncFtpClient client = client;

    private readonly HashSet<string> existingFolders = ["/"];// the "/" root directory will always exist so add that by default 

    public async Task<bool> CreateFolder(string remoteFolderPath, CancellationToken cancellationToken)
    {
        return await client.CreateDirectory(remoteFolderPath, token: cancellationToken);
    }

    public async Task DeleteFile(string remoteFilePath, CancellationToken cancellationToken)
    {
        await client.Rename(remoteFilePath, remoteFilePath + ".deleted", token: cancellationToken);
    }

    public async Task<bool> DoesFileExist(string remoteFilePath, CancellationToken cancellationToken)
    {
        return await client.FileExists(remoteFilePath, token: cancellationToken);
    }

    public async Task DownloadFile(string remoteFilePath, string localFilePath, CancellationToken cancellationToken)
    {
        await client.DownloadFile(remoteFilePath, localFilePath, token: cancellationToken);
    }

    public async Task<string[]> GetFolderContents(string remoteFolderPath, CancellationToken cancellationToken)
    {
        return await client.GetNameListing(remoteFolderPath, token: cancellationToken);
    }

    public async Task UploadFile(string localFilePath, string remoteFilePath, CancellationToken cancellationToken)
    {
        string folder = Path.GetDirectoryName(remoteFilePath)! + "/";

        if (existingFolders.Contains(folder) is false) {
            await CreateFolder(folder, cancellationToken);
            existingFolders.Add(folder);
        }

        var status = await client.UploadFile(localFilePath, remoteFilePath, FtpRemoteExists.Overwrite, token: cancellationToken);

        if (status != FtpStatus.Success) {
            throw new Exception($"upload {status} for {remoteFilePath}");
        }
    
        return;
    }


    public static async Task<FTPDataStore> GenerateNewFromSettings(FTPSettings settings) 
    {
        AsyncFtpClient client = new(
            host: settings.FtpHost,
            user: settings.FtpUsername,
            pass: settings.FtpPassword,
            port: settings.FtpPort
        );

        client.Config.ConnectTimeout = 2000;
        client.Config.EncryptionMode = FtpEncryptionMode.Implicit;

        await client.AutoConnect();
    
        return new(client);
    }

    public async Task<string?> GetFileContentsAsString(string remoteFilePath, CancellationToken cancellationToken)
    {
        try {
            byte[] bytes = await client.DownloadBytes(remoteFilePath, cancellationToken);
            return System.Text.Encoding.Default.GetString(bytes); 
        }
        catch (FtpMissingObjectException) 
        {
            return null;
        }
    }
}
