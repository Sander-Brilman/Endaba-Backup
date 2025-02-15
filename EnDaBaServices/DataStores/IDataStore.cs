using System;

namespace ImmichEnDaBa.DataStores;

public interface IDataStore
{
    Task<string[]> GetFolderContents(string remoteFolderPath, CancellationToken cancellationToken);

    Task<bool> DoesFileExist(string remoteFilePath, CancellationToken cancellationToken);

    Task UploadFile(string remoteFilePath, string localFilePath, CancellationToken cancellationToken);

    Task DownloadFile(string remoteFilePath, string localFilePath, CancellationToken cancellationToken);

    Task DeleteFile(string remoteFilePath, CancellationToken cancellationToken);
}
