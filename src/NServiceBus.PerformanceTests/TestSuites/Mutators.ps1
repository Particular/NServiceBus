. .\TestSupport.ps1

#runs messages using a message with encrypted properties

RunTest -serializationFormat "json" -transport "msmq" -messagemode "encryption" -numMessages 1000
