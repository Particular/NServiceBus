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

If you want to build NServiceBus without any merged external dependencies please use the UnsupporterCoreOnlyBuild.bat

===========
= Running =
===========

To run NServiceBus Msmq and MSDTC need to be installed and configured on your machine. To do this please
run RunMeFirst.bat.

Running an nServiceBus process is simple. Any external dependencies that are needed like MSMQ, MSDTC, databases will be set up automatically at startup if they aren't found.


=========
= Sagas =
=========

The 'Manufacturing' sample shows the use of sagas.