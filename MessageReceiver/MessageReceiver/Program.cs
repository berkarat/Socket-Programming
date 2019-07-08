using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;


namespace MessageReceiver
{
    class Program
    {
        TcpListener Listener;
        Socket socket;
        byte[] incoming_data = new byte[2048];
        string Data = string.Empty;
        string IpAddress = string.Empty;
        public string localip = string.Empty;
        static void Main(string[] args)
        {
            Program pr = new Program();
           pr.GetLocalIP();
           pr.message_port();
            pr.message_read();
        }
        public bool GetLocalIP()
        {
            bool returnvalue = false;

            var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ipAdress in hostEntry.AddressList)
            {
                Console.WriteLine(ipAdress.ToString());
                if (ipAdress.AddressFamily.ToString() == "InterNetwork")
                {
                    localip = ipAdress.ToString();

                    returnvalue = true;
                }
            }
            return returnvalue;


        }
        public bool message_port()
        {
            var view32 = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32);
            var key = view32.OpenSubKey(@"Software\OVIA\MESSAGERECEIVER\", false);
            bool portstatus = false;
            if (true)
            {
                try
                {
                    int port = 123;

                    IPAddress localAddr = IPAddress.Parse(localip);
                    Listener = new TcpListener(localAddr, port);

                    //gonderveri_dizi = Encoding.ASCII.GetBytes(port_passwd);

                    Console.WriteLine(Listener.LocalEndpoint);
                    //portstatus = true;
                    //Log("PORT CONNECTION SUCCESS" + port, EventLogEntryType.Information);


                }
                catch (Exception ex)
                {
                    portstatus = false;
                }
            }
            return portstatus;
        }
        private void message_read()
        {





            string autcode = string.Empty;
            byte[] incoming_data = new byte[2048];

            Listener.Start();

            do
            {
                if (true)
                {
                    // TODO: SOKETİ DİNLEMEYE BAŞLADIĞI YER
                    Console.WriteLine("SOCKET LISTENING !!");


                    socket = Listener.AcceptSocket();
                    if (socket.Poll(1000, SelectMode.SelectRead))
                    {
                   
                        //TODO: PARSE EDİP LOGA YAZMAYI YENİ BİR THREADLE YAPACAĞIZ !!




                        socket.Receive(incoming_data, incoming_data.Length, 0);



                        if (incoming_data[0] != 0)
                        {



                            Data = Encoding.ASCII.GetString(incoming_data);
                            IpAddress = socket.RemoteEndPoint.ToString();




                            XmlSerializer serializer = new XmlSerializer(typeof(SendMessage));
                            StringReader rdr = new StringReader(Data);
                            SendMessage  resultingMessage = (SendMessage)serializer.Deserialize(rdr);

                        

                            
                            XmlSerializer serializer2 = new XmlSerializer(typeof(QuoteDetails), new XmlRootAttribute("QuoteDetails"));


                            QuoteDetails currencydetails = Deserialize<QuoteDetails>(resultingMessage.MESSAGEBODY);

                            //db transaction write & db quote update 

                            int c = Data.Length;
                            if (Data[0] == 0)
                            {
                                Console.WriteLine("NULL DATA");
                            }
             




                        }
                        else
                        {
                        

                        }

                    }

                }
            }
            while (true);



        }


        public T Deserialize<T>(string input) where T : class
        {
            System.Xml.Serialization.XmlSerializer ser = new System.Xml.Serialization.XmlSerializer(typeof(T));

            using (StringReader sr = new StringReader(input))
            {
                return (T)ser.Deserialize(sr);
            }
        }
    }
    public class SendMessage
    {
        public string MERCHANTID { get; set; }
        public string TERMINALID { get; set; }
        public DateTime SENDDATE { get; set; }
        public string MESSAGEBODY { get; set; }
        public string LABEL { get; set; }

    }
    public partial class QuoteDetails
    {

        public int CURRENCYID { get; set; }
        public string UPDATEDATE { get; set; }
        public string CURRENCYNAME  { get; set; }
        public double QUOTEBUY { get; set; }
        public double QUOTESELL { get; set; }
        public string DESCRIPTION { get; set; }

    }

}
