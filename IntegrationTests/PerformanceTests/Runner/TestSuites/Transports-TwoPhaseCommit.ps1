. .\TestSupport.ps1

RunTest -transport "msmq" -mode "twophasecommit"
RunTest -transport "sqlserver" -mode "twophasecommit"
RunTest -transport "activemq" -mode "twophasecommit"
RunTest -transport "rabbitmq" -mode "twophasecommit"  -numThreads 60
