# Endaba, a simple but highly performant backup tool.

**En**crypted **Da**ta **Ba**ckup.


Endaba is a command line tool that makes encryped backups of your data en stores them on a remote FTP server.

Here is a short overview of what makes it special:

- **Network efficent:** EnDaBa makes use of hashes to only (re)upload new / changed files.
- **Multi-threaded:** By using multi-threading and multiple FTP connections data is processed and uploaded much faster.
- **Elastic load:** the application automatically scales the amount of threads / workers up and down when needed.
- **Easily decrypted:** Encryption is done before uploading using password protected zip files. making it easy to restore and decrypt your data using other tools


## How to run it.

You need:
- the dotnet 9 SDK installed
- a remote FTP server to store the files on.

Steps to run:
- download the source code
- navigate to the `EnDaBaBackup` folder in your terminal
- run the `dotnet publish` command
- go to `EnDaBaBackup/bin/Release/net9.0/linux-x64/publish`
- Run the `EnDaBaBackup` executeable.
- Edit the configuration files that have been generated according to the instructions listed on-screen
- run the program again

## Attributions

Thanks to these projects for making this possible:

- FluentFTP
- SharpZipLib


