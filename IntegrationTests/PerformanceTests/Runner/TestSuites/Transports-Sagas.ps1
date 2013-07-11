. .\TestSupport.ps1

#runs all transport in nonvolatile m

RunTest -transport "msmq" -messagemode "sagamessages" -numMessages 1000
RunTest -transport "activemq" -messagemode "sagamessages" -numMessages 1000
RunTest -transport "sqlserver" -messagemode "sagamessages" -numMessages 1000
RunTest -transport "rabbitmq" -messagemode "sagamessages" -numMessages 1000 -numThreads 60
#RunTest -transport "azure" -messagemode "sagamessages" -numMessages 1000
