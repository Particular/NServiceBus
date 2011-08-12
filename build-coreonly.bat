powershell -ExecutionPolicy RemoteSigned -noLogo -NonInteractive -File .\install-packages.ps1

.\tools\nant\NAnt -D:include.dependencies=false  %1
