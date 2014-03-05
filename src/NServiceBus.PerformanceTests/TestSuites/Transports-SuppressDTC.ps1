. .\TestSupport.ps1

RunTest -transport "msmq" -mode "suppressDTC"
RunTest -transport "sqlserver" -mode "suppressDTC"
RunTest -transport "activemq" -mode "suppressDTC"
RunTest -transport "rabbitmq" -mode "suppressDTC"  -numThreads 60
