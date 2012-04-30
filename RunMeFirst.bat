md .\binaries\temp
copy .\binaries\log4net.dll .\binaries\temp\log4net.dll
copy .\binaries\NServiceBus.Core.dll .\binaries\temp\NServiceBus.Core.dll
copy .\binaries\NServiceBus.dll .\binaries\temp\NServiceBus.dll
copy .\binaries\NServiceBus.Host.exe .\binaries\temp\NServiceBus.Host.exe
.\binaries\temp\NServiceBus.Host.exe /installInfrastructure
rd /s /q .\binaries\temp