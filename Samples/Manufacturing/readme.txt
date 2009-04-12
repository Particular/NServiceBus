Since the saga in this sample uses timeouts, make sure to run the TimeoutManager process found in the directory /build/timeout/ of nServiceBus.

This sample uses the DbSubscriptionStorage so make sure to define the relevant tables in the DB for storage. The DDL can be found in /src/impl/unicast/NServiceBus.Unicast.Subscriptions.DB


Experiment with killing the hr process, and notice how the timeout causes the saga to complete anyway.