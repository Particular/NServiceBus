. .\TestSupport.ps1

#RunTest -numThreads 15 -transport "sqlserver"
#RunTest -numThreads 15 -transport "sqlserver" -mode "volatile"

RunTest -numThreads 1 -transport "sqlserver"
RunTest -numThreads 5 -transport "sqlserver"
RunTest -numThreads 10 -transport "sqlserver"
RunTest -numThreads 15 -transport "sqlserver"
RunTest -numThreads 60 -transport "sqlserver"
RunTest -numThreads 90 -transport "sqlserver"

