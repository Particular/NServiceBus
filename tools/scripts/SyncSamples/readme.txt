1. Make any and all necessary changes to the VideoStore.Msmq solution.
2. Make sure it works.
3. open a powershell prompt as admin and run the SynchronizeSamples.ps1 from the SyncSamples folder to sync all the changes from VideoStore.Msmq to all the other transports.
4. If you change the solution name from VideoStore.<transport>, please make sure that you edit the SynchronizeSamples.ps1 script to use the correct solution name
5. Once the other transport solutions are sync-ed up, run each transport sample and ensure that they work as expected.
6. Make sure to also check in the solution's suo file, so it's setup for running all the relevant endpoints when the user presses F5.

