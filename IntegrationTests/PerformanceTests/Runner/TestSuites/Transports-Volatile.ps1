. .\TestSupport.ps1

RunTest -transport "msmq" -mode "volatile"
RunTest -transport "sqlserver" -mode "volatile"
RunTest -transport "activemq" -mode "volatile"
RunTest -transport "rabbitmq" -mode "volatile"  -numThreads 30
