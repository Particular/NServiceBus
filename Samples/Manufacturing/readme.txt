Since the saga in this sample uses timeouts, make sure to run the TimeoutManager process found in the directory /build/timeout/ of nServiceBus.

This sample uses the MsmqSubscriptionStorage. If you want to try the DB Subscription Storage run the order service with a blank commandline.
If you do make sure to specify a db for the saga storage. (see comments in app.config of the orderservice)

Experiment with killing the hr process, and notice how the timeout causes the saga to complete anyway.