namespace EnDaBaServices.DataStores.FTP;

public sealed record class FTPSettings(
    string FtpHost,
    int FtpPort,
    string FtpUsername,
    string FtpPassword
);
