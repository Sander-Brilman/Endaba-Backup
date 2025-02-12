using System;

namespace ImmichEnDaBa.DataStores.FTP;

public class FTPDataStore : IDataStore
{

    public FTPDataStore(string ftpHost, string ftpPort, string ftpUsername, string ftpPassword) {

    }

    public Task CreateFolder(string folderName, string folderPath)
    {
        throw new NotImplementedException();
    }

    public Task DeleteFile(string remoteFilePath)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DoesFileExist(string remoteFilePath)
    {
        throw new NotImplementedException();
    }

    public Task DownloadFile(string remoteFilePath, string localFilePath)
    {
        throw new NotImplementedException();
    }

    public Task<string[]> GetFolderContents(string remoteFolderPath)
    {
        throw new NotImplementedException();
    }

    public Task UploadFile(string remoteFilePath, string localFilePath)
    {
        throw new NotImplementedException();
    }
}
