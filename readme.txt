In order to build the source, run "build.bat" (you don't need NAnt for this).

NServiceBus is a non-trivial framework that takes time to understand.

The best way to get up and running is from the Samples. Run them, change them a bit, look at what references what. If you want to do your own thing, copy one of the samples (like FullDuplex), and change from there.

=====================
= MSMQ Installation =
=====================

1. In order to run some of the samples, MSMQ is required.

2. To install MSMQ, go to the Control Panel, Add or Remove Programs, Windows Components, Select "Message Queueing".

3. The minimal installation requires only "Common" and will work fine.

4. Run the script "msmq install.vbs" to install the queues used by the sample

=====================

In order to run samples that make use of sagas, specifically the DbBlobSagaPersister (found in \src\impl\SagaPersisters\DbBlobSagaPersister) or the DB subscription storage (found in \src\impl\unicast\NServiceBus.Unicast.Subscriptions.DB) you will need a database with the tables defined in TableDefinitions.txt.




[Optional 2] To increase the number of workers involved in workflow, try copying the runtime files of the 
Worker project to another directory, change the Worker.exe.config file as follows: under the section
spring/objects/object id="Transport" change the property InputQueue to 
"FormatName:DIRECT=OS:localhost\private$\worker2". Run Worker.exe in the new directory.
Notice how the work automatically gets distributed between the two workers.

