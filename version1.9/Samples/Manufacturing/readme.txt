This sample makes use of an additional queue called "hr". Run the msmq install script to have that queue created for you (or do it manually - don't forget to make it transactional)

Since the saga in this sample uses timeouts, make sure to run the TimeoutManager process found in the directory /build/timeout/ of nServiceBus.

Experiment with killing the hr process, and notice how the timeout causes the saga to complete anyway.