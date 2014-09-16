@echo off

cls

".nuget\nuget.exe" install Paket -OutputDirectory packages -Prerelease -ExcludeVersion

packages\Paket\tools\Paket.exe %1