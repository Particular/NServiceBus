powershell -ExecutionPolicy RemoteSigned -noLogo -NonInteractive -File .\install-packages.ps1
.\tools\nant\nant -buildfile:nant.build %1
