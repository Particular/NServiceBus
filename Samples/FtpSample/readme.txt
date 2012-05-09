To successfully run this sample you need to set-up 2 ftp servers on your local machine.

Environment Setup
------------------------------------------------------------------------------------------------------------------------
Using IIS (or some other ftp server software), create 2 ftp servers (FtpSampleServer and FtpSampleClient) with anonymous authentication and listening on port 1090(FtpSampleServer) and port 1091(FtpSampleClient).
Set the physical path for FtpSampleServer to C:\Temp\FTPServer\receive (if you don't use this location see below).
Set the physical path for FtpSampleClient to C:\Temp\FTPClient\receive (if you don't use this location see below).
Make sure both ftp servers are started.


Custom configuration
------------------------------------------------------------------------------------------------------------------------
If you have changed any of the ftp server ports, please make sure you update the TestClient App.config file and/or EndpointConfig.cs (.DefineEndpointName("localhost:1091"))
If you have used different physical paths for the ftp servers, please make sure you update both App.config files.