powershell -ExecutionPolicy RemoteSigned -noLogo -NonInteractive -File .\install-packages.ps1

.\tools\nant\NAnt -buildfile:nant.build -D:include.dependencies=false %1
