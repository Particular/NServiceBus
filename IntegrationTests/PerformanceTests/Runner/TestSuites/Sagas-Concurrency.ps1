. .\TestSupport.ps1

Cleanup

"C=0, no sagas"
RunTest -transport "msmq" -messagemode "sagamessages" -numMessages 2000 -persistence nhibernate -concurrency 1

"C=0, existing sagas"
RunTest -transport "msmq" -messagemode "sagamessages" -numMessages 2000 -persistence nhibernate -concurrency 1

Cleanup

"C=2, no sagas"
RunTest -transport "msmq" -messagemode "sagamessages" -numMessages 2000 -persistence nhibernate -concurrency 2

"C=2, existing sagas"
RunTest -transport "msmq" -messagemode "sagamessages" -numMessages 2000 -persistence nhibernate -concurrency 2

Cleanup

"C=5, no sagas"
RunTest -transport "msmq" -messagemode "sagamessages" -numMessages 2000 -persistence nhibernate -concurrency 5

"C=5, existing sagas"
RunTest -transport "msmq" -messagemode "sagamessages" -numMessages 2000 -persistence nhibernate -concurrency 5

Cleanup

"C=10, no sagas"
RunTest -transport "msmq" -messagemode "sagamessages" -numMessages 2000 -persistence nhibernate -concurrency 10

"C=10, existing sagas"
RunTest -transport "msmq" -messagemode "sagamessages" -numMessages 2000 -persistence nhibernate -concurrency 10