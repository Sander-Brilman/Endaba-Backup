using System;

namespace ImmichEnDaBa.DataStores;

public interface IDataStore
{
    Task<bool> CreateFolder(string remoteFolderPath);

    Task<bool> DoesFolderExist(string remoteFolderPath);

    Task<string[]> GetFolderContents(string remoteFolderPath);

    Task<bool> DoesFileExist(string remoteFilePath);

    Task UploadFile(string remoteFilePath, string localFilePath);

    Task DownloadFile(string remoteFilePath, string localFilePath);

    Task DeleteFile(string remoteFilePath);
}
