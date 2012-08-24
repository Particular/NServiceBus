@echo off
if "%IsEmulated%"=="true" goto :eof
start /wait msiexec /quiet /i %RoleRoot%\plugins\RemoteForwarder\RemoteForwarder.msi
