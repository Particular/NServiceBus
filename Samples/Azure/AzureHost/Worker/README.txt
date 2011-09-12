How to deploy to production

- update the settings in the host's service configuration file AND the worker's app.config
- build the sample
- Zip the build output of worker, the name of the zip file will be used to identify the process.
- Upload it to the container specified in the serviceconfiguration file (NServiceBus.Host.Container), if not specified the container is called 'Endpoints'
- Publish the host
