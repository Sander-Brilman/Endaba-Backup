using System;

namespace ImmichEnDaBa.Settings;

public record AppSettings(
    string[] BackupLocationPatterns,
    string FtpHost,
    int FtpPort,
    string FtpUsername,
    string FtpPassword,
    string? EncryptionKey
);
