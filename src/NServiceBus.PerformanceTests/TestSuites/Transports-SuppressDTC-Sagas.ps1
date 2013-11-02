. .\TestSupport.ps1

RunTest -transport "msmq" -mode "suppressDTC" -messagemode "sagamessages" -numMessages 1000
RunTest -transport "sqlserver" -mode "suppressDTC" -messagemode "sagamessages" -numMessages 1000
RunTest -transport "activemq" -mode "suppressDTC" -messagemode "sagamessages" -numMessages 1000
RunTest -transport "rabbitmq" -mode "suppressDTC" -messagemode "sagamessages"  -numMessages 1000 -numThreads 60
