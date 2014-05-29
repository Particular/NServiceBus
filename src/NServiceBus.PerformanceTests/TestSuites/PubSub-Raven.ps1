. .\TestSupport.ps1

..\.\bin\debug\Runner.exe PubSub numberofsubscribers=1 numberofmessages=10000 numberofthreads=10 storage=inmemory

Cleanup

..\.\bin\debug\Runner.exe PubSub numberofsubscribers=2 numberofmessages=10000 numberofthreads=10 storage=inmemory

Cleanup

..\.\bin\debug\Runner.exe PubSub numberofsubscribers=5 numberofmessages=10000 numberofthreads=10 storage=inmemory

Cleanup

..\.\bin\debug\Runner.exe PubSub numberofsubscribers=10 numberofmessages=10000 numberofthreads=10 storage=inmemory

Cleanup

..\.\bin\debug\Runner.exe PubSub numberofsubscribers=25 numberofmessages=10000 numberofthreads=10 storage=inmemory

Cleanup

..\.\bin\debug\Runner.exe PubSub numberofsubscribers=50 numberofmessages=10000 numberofthreads=10 storage=inmemory

Cleanup

..\.\bin\debug\Runner.exe PubSub numberofsubscribers=100 numberofmessages=10000 numberofthreads=10 storage=inmemory