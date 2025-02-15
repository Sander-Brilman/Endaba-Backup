using System;
using System.IO;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.Zip;

namespace ImmichEnDaBa;

public sealed class ZippingService
{
    public int CompressionLevel { get; set; } = 9;

    public void ZipFile(string localFilePath, string destinationLocalFilePath, string? encryptionKey = null) 
    {
        using FileStream fsOut = File.Create(destinationLocalFilePath);
        using ZipOutputStream zipStream = new(fsOut);

        zipStream.SetLevel(CompressionLevel);
        zipStream.Password = encryptionKey; 

        FileInfo fileInfo = new(localFilePath);
        string entryName = Path.GetFileName(localFilePath);
        ZipEntry newEntry = new(entryName)
        {
            DateTime = fileInfo.CreationTime,
            Size = fileInfo.Length
        };

        zipStream.PutNextEntry(newEntry);

        using (FileStream streamReader = File.OpenRead(localFilePath))
        {
            streamReader.CopyTo(zipStream);
        }

        zipStream.CloseEntry();
    }

    private FastZip? fastZip;

    public void UnzipFile(string localFilePath, string destinationLocalFilePath, string? encryptionKey = null) 
    {
        fastZip ??= new()
        {
            CompressionLevel = ICSharpCode.SharpZipLib.Zip.Compression.Deflater.CompressionLevel.BEST_COMPRESSION,
            Password = encryptionKey,
            EntryEncryptionMethod = ZipEncryptionMethod.AES256,
            RestoreDateTimeOnExtract = true,
            RestoreAttributesOnExtract = true
        };

        fastZip.ExtractZip(localFilePath, destinationLocalFilePath, null);
    }
}
