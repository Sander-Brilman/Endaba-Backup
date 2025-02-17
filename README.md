# Endaba, a simple but highly performant backup tool.

**En**crypted **Da**ta **Ba**ckup.


Endaba is a command line tool that makes encryped backups of your data en stores them on a remote FTP server.

Here is a short overview of what makes it special:

- **Network efficent:** EnDaBa makes use of hashes to only (re)upload new / changed files.
- **Multi-threaded:** By using multi-threading and multiple FTP connections data is processed and uploaded much faster.
- **Elastic load:** the application automatically scales the amount of threads / workers up and down when needed.
- **Easily decrypted:** Encryption is done before uploading using password protected zip files. making it possible to easily restore and decrypt your data using other toold 



