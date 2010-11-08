using System;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;
using NServiceBus.Unicast.Queuing.Ftp;


namespace NServiceBus
{
    public static class ConfigureFtpQueue
    {
        public static Configure FtpTransport(this Configure config)
        {            
            var ftpQueue = config.Configurer.ConfigureComponent<FtpMessageQueue>(ComponentCallModelEnum.Singleton);
            var cfg = Configure.GetConfigSection<FtpQueueConfig>();
            
            if (cfg != null)
            {
                ftpQueue.ConfigureProperty(t => t.SendDirectory, cfg.SendDirectory);
                ftpQueue.ConfigureProperty(t => t.ReceiveDirectory, cfg.ReceiveDirectory);

                if (!String.IsNullOrEmpty(cfg.UserName))
                {
                    ftpQueue.ConfigureProperty(t => t.UserName, cfg.UserName);
                    ftpQueue.ConfigureProperty(t => t.Password, cfg.Password);
                }
            }
            
            return config;
        }
    }
}
