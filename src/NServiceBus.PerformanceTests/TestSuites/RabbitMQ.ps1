. .\TestSupport.ps1

RunTest -numThreads 60 -transport "rabbitmq"
RunTest -numThreads 60 -transport "rabbitmq" -mode "volatile"

RunTest -numThreads 1 -transport "rabbitmq"
RunTest -numThreads 5 -transport "rabbitmq"
RunTest -numThreads 10 -transport "rabbitmq"
RunTest -numThreads 15 -transport "rabbitmq"
RunTest -numThreads 60 -transport "rabbitmq"
RunTest -numThreads 90 -transport "rabbitmq"

