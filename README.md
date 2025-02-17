# Endaba, a simple but performant backup tool.

**En**crypted **Da**ta **Ba**ckup

Endaba is a command line tool that makes encrypted backups of your data and stores them on a remote FTP server.

Here is a short overview of what makes it special:

- **Network efficient:** EnDaBa makes use of hashes to only (re)upload new/changed files.
- **Multi-threaded:** By using multi-threading and multiple FTP connections, data is processed and uploaded much faster.
- **Elastic load:** The application automatically scales the number of worker threads/connections up and down when needed.
- **Easily decrypted:** Encryption happens before uploading using password-protected zip files. this makes it easy to restore and decrypt your data using other tools.

## How to run it

You need:
- The [dotnet 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) installed
- A remote FTP server to store the files on

Steps to run:
- Download the source code
- Navigate to the `EnDaBaBackup` folder in your terminal
- Run the `dotnet publish` command
- Go to `EnDaBaBackup/bin/Release/net9.0/linux-x64/publish`
- Run the `EnDaBaBackup` executable (`./EnDaBaBackup`)
- Edit the configuration files that have been generated according to the instructions listed on-screen

  _Note that folders listed in the `BackupLocationPatterns` must always end with a `*`_
- Run the program again (`./EnDaBaBackup --show-jobs`)

*The --show-jobs flag shows a list with the number of jobs in the queue and the number of workers currently active. Note that it takes 15 seconds before new workers get created/stopped.*

## Logs

Info / Error logs are stored in the same directory as the settings files. Any errors within the application will be logged here.

## Attributions

Thanks to these projects for making this possible:

- FluentFTP
- SharpZipLib
