powershell -ExecutionPolicy RemoteSigned -noLogo -NonInteractive -File .\install-packages.ps1

.\tools\nant\nant.exe -D:targetframework=net-3.5  %1
