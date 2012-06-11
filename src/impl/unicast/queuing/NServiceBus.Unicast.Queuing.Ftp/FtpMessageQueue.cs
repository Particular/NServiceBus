using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using NServiceBus.Unicast.Transport;
using NServiceBus.Logging;

namespace NServiceBus.Unicast.Queuing.Ftp
{
    public class FtpMessageQueue : ISendMessages,IReceiveMessages
    {
        #region Config Parameters

        public String ReceiveDirectory { get; set; }

        public String UserName { get; set; }

        public String Password { get; set; }

        #endregion     

        #region IMessageQueue Members

        public bool HasMessage()
        {
            return (_locQueue.Count > 0);
        }

        public void Init(Address address, bool transactional)
        {
            SetupReceiveService();
        }

        public TransportMessage Receive()
        {
            lock (_locker)
            {
                return ((_locQueue.Count > 0) ? _locQueue.Dequeue() : null);
            }
        }

        public void Send(TransportMessage message, Address address)
        {
            Stream bitStream = null;
            IFormatter binFormatter = new BinaryFormatter();

            try
            {
                if (String.IsNullOrEmpty(message.Id))
                    message.Id = Guid.NewGuid().ToString();

                if (message.Headers == null)
                    message.Headers = new System.Collections.Generic.Dictionary<string, string>();

                if (message.Headers.ContainsKey(IDFORCORRELATION))
                    message.Headers.Add(IDFORCORRELATION, message.IdForCorrelation);

                var fName = message.Id + ".msg";
                bitStream = new MemoryStream(1024);
                binFormatter.Serialize(bitStream, message);

                bitStream.Position = 0;
                var bits = new byte[bitStream.Length];
                bitStream.Read(bits, 0, (int)bitStream.Length);

                bitStream.Close();
                bitStream = null;

                TransmitFile(fName, address.Queue, bits);                               
            }
            catch (Exception ex)
            {
                Logger.Debug("Exception In FtpQueue Send: " + ex.ToString(), ex);
            }
            finally
            {
                if (bitStream != null)
                    bitStream.Close();
            }
        }

        #endregion        

        #region Receive Handlers

        private void OnFileCreated(Object sender, FileSystemEventArgs e)
        {
            Stream bitStream = null;
            IFormatter binFormatter = new BinaryFormatter();
           
            try
            {
                while (true)
                {             
                    //may not have access to the file that we want
                    //should put a number of retries here...
                    try
                    {
                        bitStream = new FileStream(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        
                        if (bitStream != null)
                            break;
                    }
                    catch { }

                    System.Threading.Thread.Sleep(100);
                }
                
                var msg = binFormatter.Deserialize(bitStream) as TransportMessage;
                
                lock (_locker)
                {
                    _locQueue.Enqueue(msg);
                }

                //close the file so we can complete the receive
                bitStream.Close();
                bitStream = null;

                File.Delete(e.FullPath);
            }
            catch (Exception ex)
            {
                Logger.Debug("Exception in FtpQueue OnFileCreated: " + ex.ToString());
            }
            finally
            {
                if (bitStream != null)
                    bitStream.Close();
            }
        }

        #endregion

        #region Helpers

        private void SetupReceiveService()
        {
            _receiver = new FileSystemWatcher(ReceiveDirectory, "*.msg");
            _receiver.Created += new FileSystemEventHandler(OnFileCreated);
            _receiver.EnableRaisingEvents = true;
        }

        private void TransmitFile(String fName, String destination, byte[] bits)
        {
            FtpWebRequest req = WebRequest.Create("ftp://" + destination + "/" + fName) as FtpWebRequest;
            req.Method = WebRequestMethods.Ftp.UploadFile;
            req.UseBinary = true;            

            if (!String.IsNullOrEmpty(this.UserName))
                req.Credentials = new NetworkCredential(this.UserName, this.Password);
                      
            using (Stream outBitStream = req.GetRequestStream())
            {
                outBitStream.Write(bits, 0, bits.Length);
            }

            using(FtpWebResponse resp = req.GetResponse() as FtpWebResponse)
            {
                
                if ((resp.StatusCode != FtpStatusCode.CommandOK) && (resp.StatusCode != FtpStatusCode.ClosingData))
                    throw new Exception("BadStatus " + resp.StatusCode.ToString() + "\n\n" + resp.StatusDescription);
            }                 
                   
        }

        #endregion

        #region Members

        private FileSystemWatcher _receiver;

        private static System.Collections.Generic.Queue<TransportMessage> _locQueue = new System.Collections.Generic.Queue<TransportMessage>(10);

        private static Object _locker = new Object();

        private const string IDFORCORRELATION = "CorrId";

        private static readonly ILog Logger = LogManager.GetLogger(typeof(FtpMessageQueue));

        #endregion
    }
}
