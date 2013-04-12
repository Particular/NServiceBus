
clustering tests require the following setup in advance (my particular versions also listed):
  install erlang (otp_win64_R16B.exe)
  install rabbitmq (rabbitmq-server-3.0.2)
  at the rabbitmq cmd prompt:  rabbitmq-plugins enable rabbitmq_management

test assumptions:
  the ports 5673-5676 are available - new rabbit nodes will be created listening on these ports
  the ports 15673-15676 are available - the new rabbit node mgmt sites will listen on these ports
  the rabbitmq utilities are located at "C:\Program Files (x86)\RabbitMQ Server\rabbitmq_server-3.0.2\sbin"
  machine is x64

other stuff:
  using NLog to output both NServiceBus and test debug logs to NLog (console and UDP listeners)
