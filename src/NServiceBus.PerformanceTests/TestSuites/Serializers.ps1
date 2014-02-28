. .\TestSupport.ps1

RunTest -serializationFormat "xml"
RunTest -serializationFormat "json"
RunTest -serializationFormat "bson"
RunTest -serializationFormat "bin"
