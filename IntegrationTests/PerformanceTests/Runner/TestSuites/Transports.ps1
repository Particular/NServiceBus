. .\TestSupport.ps1

#runs all transport in nonvolatile m

RunTest -transport "activemq"
RunTest -transport "msmq"
#RunTest -transport "sqlserver"
#RunTest -transport "rabbitmq" -numThreads 60
#RunTest -transport "azure"
