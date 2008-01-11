In order to build the source, open a SDK Command Prompt, navigate to the directory where you extracted NServiceBus, and run "build" (you don't need NAnt for this).



1. In order to run some of the samples, MSMQ is required.

2. To install MSMQ, go to the Control Panel, Add or Remove Programs, Windows Components, Select "Message Queueing".

3. The minimal installation requires only "Common" and will work fine.

4. Run the script "msmq install.vbs" to install the queues used by the sample

5. In order to run samples that make use of sagas, specifically the DbBlobSagaPersister (found in \src\impl\SagaPersisters\DbBlobSagaPersister) you will need a database with the tables defined in TableDefinitions.txt.


[Optional 1] To play around with the different WCF transport options, delete the App.config
files in the projects ClientRunner and ServerRunner. Then copy the *App.config file of your choice
and rename as App.config. The App.config file that the sample originally comes with is
a copy of NonWCF MSMQ App.config. Make sure you use the same file name for client and server.

[Optional 2] To increase the number of workers involved in workflow, try copying the runtime files of the 
Worker project to another directory, change the Worker.exe.config file as follows: under the section
spring/objects/object id="Transport" change the property InputQueue to 
"FormatName:DIRECT=OS:localhost\private$\worker2". Run Worker.exe in the new directory.
Notice how the work automatically gets distributed between the two workers.

