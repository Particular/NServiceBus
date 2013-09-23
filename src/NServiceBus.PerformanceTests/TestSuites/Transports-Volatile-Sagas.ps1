. .\TestSupport.ps1

RunTest -transport "msmq" -mode "volatile" -messagemode "sagamessages" -numMessages 100
RunTest -transport "sqlserver" -mode "volatile" -messagemode "sagamessages" -numMessages 100
RunTest -transport "activemq" -mode "volatile" -messagemode "sagamessages" -numMessages 100
RunTest -transport "rabbitmq" -mode "volatile" -messagemode "sagamessages"  -numMessages 100 -numThreads 60
