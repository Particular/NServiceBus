. .\TestSupport.ps1

#runs all transport in nonvolatile m

RunTest -transport "msmq"
RunTest -transport "sqlserver"
RunTest -transport "activemq"
RunTest -transport "rabbitmq" -numThreads 60
#RunTest -transport "azure"
