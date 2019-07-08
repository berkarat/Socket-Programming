using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Messaging;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Verisoft;

namespace MessageSender
{
    public class Program
    {
        private object listenLock = new object();
        public string CLRWriteQueue = @".\private$\qoutewq";
        public string CLReadQueue = @".\private$\quoterq";
        public string error = string.Empty;
        static System.Net.IPAddress localAddr = IPAddress.Parse("127.0.0.1");
        static TcpClient serverSocket;
        static bool transactionbegin = false; static bool transactionreceivebegin = false;
        static byte[] send_data;
        MessageQueueTransaction transactionsend = new MessageQueueTransaction();
        MessageQueueTransaction transactionreceive = new MessageQueueTransaction();
        System.Messaging.Message messagesend;
        System.Messaging.Message messagereceive;
        SendMessage sendMessage;
        private Thread listenerThread = null;
        public static RegistryKey key;

        public Program()
        {
            var view32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            key = view32.OpenSubKey(@"Software\OVIA\MESSAGESENDER\", false);
        }
        static void Main(string[] args)
        {
            Program pr = new Program();
            pr.StartListenerThreadProcess();
        }

        private void StartListenerThreadProcess()
        {
            if (listenerThread == null || !listenerThread.IsAlive)
            {
                listenerThread = new Thread(new ThreadStart(ListenSendingQueue));
                listenerThread.Start();
                listenerThread = new Thread(new ThreadStart(ListenReturnQueue));
                listenerThread.Start();
            }
        }

        #region RETURN DATA PROCESS
        public void ListenReturnQueue()
        {
           
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(Convert.ToInt32(key.GetValue("Timeout")));
            MessageQueue queue = createMSMQ(CLReadQueue, out error);
            while (true)
            {
                if (!transactionbegin) { transactionreceive.Begin(); transactionbegin = true; }
                try
                {
                    messagereceive = queue.Receive(timeSpan, transactionreceive);
                    //message.Formatter = new XmlMessageFormatter(new Type[] { typeof(QuoteDetails) });
                    //sendMessage = new SendMessage();
                    //sendMessage.MESSAGEBODY = Serialize(message);
                    //sendMessage.SENDDATE = DateTime.Now;
                    //sendMessage.TERMINALID = key.GetValue("TerminalID").ToString();
                    //sendMessage.MERCHANTID = key.GetValue("MerchantID").ToString();
                }
                catch (MessageQueueException ex)
                {
                    if (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                    {
                        Console.WriteLine(ex.MessageQueueErrorCode);
                    }


                }
                Thread.Sleep(Convert.ToInt32(key.GetValue("WaitTime")));
            }
        }
        #endregion
        #region SEND DATA PROCESS

        public void ListenSendingQueue()
        {
    
            TimeSpan timeSpan = TimeSpan.FromMilliseconds(Convert.ToInt32(key.GetValue("Timeout")));
            MessageQueue queue = createMSMQ(CLRWriteQueue, out error);
            while (true)
            {

                Console.WriteLine(transactionsend.Status.ToString());
                
                if (transactionsend.Status.ToString()!="Pending") { transactionsend.Begin(); transactionreceivebegin = true; }
                try
                {

                    messagesend = queue.Receive(timeSpan, transactionsend);
                    messagesend.Formatter = new XmlMessageFormatter(new Type[] { typeof(QuoteDetails) });


                    sendMessage = new SendMessage();

                    sendMessage.MESSAGEBODY = Serialize(messagesend);
                    sendMessage.SENDDATE = DateTime.Now;
                    sendMessage.TERMINALID = key.GetValue("TerminalID").ToString();
                    sendMessage.MERCHANTID = key.GetValue("MerchantID").ToString();
                    if (true)
                    {
                        string tx = string.Empty;
                        string tx2 = string.Empty;
                        if (SendServerLogXML(sendMessage, "label", out tx, out tx2))
                        {
                            transactionsend.Commit();
                            transactionreceivebegin = false;
                            Console.WriteLine("MESSAGE SENT" + DateTime.Now.ToString());
                        }
                        else
                        {
                            
                            transactionsend.Abort();
                        }
                    }
                    else
                    {

                    }

                }
                catch (MessageQueueException ex)
                {
                    if (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                    {
                        Console.WriteLine(ex.MessageQueueErrorCode);
                    }


                }
                Thread.Sleep(Convert.ToInt32(key.GetValue("WaitTime")));
            }
        }
        private bool SendServerLogXML(SendMessage Message, string _BodyLabel, out string ret_errorCode, out string ret_errorDesc)
        {

            //TODO: SendServerLogXML
            bool _value = false;
            ret_errorCode = string.Empty;
            ret_errorDesc = string.Empty;
            string result = string.Empty;
            string errorCode = string.Empty;
            string errorDesc = string.Empty;

            // TODO: SERVER'A GÖNDERİLEN KISIM 
            #region connect server      

            try
            {

           

            if (key != null)
            {
                if (key.GetValue("IpAddress").ToString().Length > 0 &&
                    key.GetValue("PortNumber").ToString().Length > 0
                    )
                {
                    
                    localAddr = IPAddress.Parse(key.GetValue("IpAddress").ToString());
                    IPEndPoint ipe = new IPEndPoint(localAddr, Convert.ToInt32(key.GetValue("PortNumber").ToString()));
                    serverSocket = new TcpClient(ipe.Address.ToString(), ipe.Port);
                    NetworkStream ns = serverSocket.GetStream();
                    send_data = new byte[1024];


                    send_data = Encoding.ASCII.GetBytes(Class1.SerializeObject(Message));

                    if (send_data.Length < 10)
                    {

                    }
                    if (ns.CanWrite)
                    {
                        ns.Write(send_data, 0, send_data.Length);
                        ns.Flush();
                        _value = true;

                    }
                    else
                    {
                        _value = false;
                    }
                    ns.Close();
                }

            }
                #endregion
            }
            catch (Exception ex)
            {
                _value = false;
                Console.WriteLine(  ex.Message);
            }


            return _value;
        }

        #endregion



        #region GENERAL METHODS

        public string Serialize(Message msg)
        {
            StreamReader sr = new StreamReader(msg.BodyStream);

            string ms = "";


            while (sr.Peek() >= 0)
            {
                ms += sr.ReadLine();
            }
            return ms;

        }
        private static MessageQueue createMSMQ(string queueName, out string error)
        {
            MessageQueue mq = null;
            error = "";
            try
            {
                if (MessageQueue.Exists(queueName))
                {
                    mq = new MessageQueue(queueName);
                    if (!mq.Transactional)
                    {
                        error = "Message queue is not transactional.";
                        mq.Dispose();
                        mq = null;
                    }
                    //messageQueueForReading.Formatter = new XmlMessageFormatter(new Type[] { typeof(ControlBoardMessage) });
                }
                else
                {
                    error = "Message queue does not exist.";
                }
            }
            catch (Exception e)
            {
                error = "Message Queue Exception: " + e.Message;
                mq = null;
            }
            return mq;
        }
        public partial class QuoteDetails
        {
            public int QUOTEID { get; set; }
            public DateTime UPDATEDATE { get; set; }
            public string QUOTENAME { get; set; }

            public double QUOTEBUY { get; set; }
            public double QUOTESELL { get; set; }
            public string DESCRIPTION { get; set; }

        }
        public class SendMessage
        {
            public string MERCHANTID { get; set; }
            public string TERMINALID { get; set; }
            public DateTime SENDDATE { get; set; }
            public string MESSAGEBODY { get; set; }
            public string LABEL { get; set; }

        }
        #endregion



    }

}
