function RunTest (
    [Parameter(Position=0,Mandatory=0)]
    [int]$numThreads = 15,
    [Parameter(Position=1,Mandatory=0)]
    [int]$numMessages = 10000,
     [Parameter(Position=2,Mandatory=0)]
    [string]$serializationFormat = "xml"
)
{

  
    .\bin\debug\Runner.exe $numThreads $numMessages $serializationFormat

}