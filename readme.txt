NServiceBus is a non-trivial framework that takes time to understand.

The best way to get up and running is from the Samples. Run them, change them a bit, look at what references what. If you want to do your own thing, copy one of the samples (like FullDuplex), and change from there.


============
= Building =
============

In order to build the source, run the build.bat file.

You'll find the built assemblies in /build/output.

The satellite processes (distributor, timeout manager, and tools) will be in the adjacent directories.

If you see CS1668 warning when building under 2008, go to the 'C:\Program Files\Microsoft SDKs\Windows\v6.0A' directory and create the 'lib' subdirectory.

If you see the build failing, check that you haven't put nServiceBus in a deep subdirectory since long path names (greater than 248 characters) aren't supported by MSBuild.


=====================
= MSMQ Installation =
=====================

1. In order to run some of the samples, MSMQ is required.

2. To install MSMQ, go to the Control Panel, Add or Remove Programs, Windows Components, Select "Message Queueing".

3. The minimal installation requires only "Common" and will work fine.

4. Run the script "msmq install.vbs" to install the queues used by the samples

=====================

In order to run samples that make use of sagas, specifically the NHibernateSagaPersister (found in \src\impl\SagaPersisters\NHibernateSagaPersister) or the DB subscription storage (found in \src\impl\unicast\NServiceBus.Unicast.Subscriptions.DB) you will need a database with the tables defined in TableDefinitions.txt.

We're in the process of using Fluent NHibernate to generate and create the schemas automatically to further shorten the time to get things up and running.