using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace Server
{
    internal class Program
    {
        static TcpListener tcpListener;

        public static void Main(string[] args)
        {
            XmlDocument xmlDoc = new XmlDocument();

            XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);
            xmlDoc.AppendChild(xmlDeclaration);

            XmlElement messages = xmlDoc.CreateElement("messages");
            xmlDoc.AppendChild(messages);

            xmlDoc.Save("media.xml");

            Console.CancelKeyPress += new ConsoleCancelEventHandler(CancelKeyPress);

            // Skapa ett TcpListener-objekt, börja lyssna och vänta på anslutning
            IPAddress myIp = IPAddress.Parse("127.0.0.1");
            int port = 8001;
            tcpListener = new TcpListener(myIp, port);
            tcpListener.Start();
            
            while (true)
            {
                try
                {
                    Console.WriteLine("Väntar på anslutning...");
                    // Någon försöker ansluta. Acceptera anslutningen
                    Socket socket = tcpListener.AcceptSocket();
                    Console.WriteLine("Anslutning accepterad från " + socket.RemoteEndPoint);
                    // Tag emot meddelandet
                    
                    Byte[] bMessage = new Byte[256];

                    int messageSize = socket.Receive(bMessage);

                    Console.WriteLine("Meddelandet mottogs...");
                    
                    string message = "";
                    for (int i = 0; i < messageSize; i++) 
                    {
                        message += Convert.ToChar(bMessage[i]);
                    }
                    
                    if (message == "0")
                    {
                        RetrieveAndSendBackData(xmlDoc, socket);
                    }
                    else if (message == "1")
                    {
                        tcpListener.Stop();
                        break;
                    }
                    else
                    {
                        SaveToXML(message, xmlDoc, messages);
                    }

                    socket.Close();

                }
                catch (Exception e)
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }
        }

        static Message SplitMessage(string recievedMessage)
        {
            string[] splitKey = {"#//#"};                                                    
            string[] splitedString = recievedMessage.Split(splitKey, StringSplitOptions.None);

            string username = splitedString[0];                                                     
            int messageId = int.Parse(splitedString[1]);                                         
            string text = splitedString[2];
            
            Message message = new Message(username,messageId, text);

            return message;
        }

        static void SaveToXML(string recievedMessage, XmlDocument xmlDoc, XmlElement messages )
        {
            Message m = SplitMessage(recievedMessage);
            
            XmlElement message = xmlDoc.CreateElement("message");
            messages.AppendChild(message);
            
            XmlElement user = xmlDoc.CreateElement("user");
            message.AppendChild(user);
            user.InnerText = m.Username;
            
            XmlElement messageId = xmlDoc.CreateElement("messageId");
            message.AppendChild(messageId);
            messageId.InnerText = m.MessageId.ToString();

            XmlElement text = xmlDoc.CreateElement("text");
            message.AppendChild(text);
            text.InnerText = m.Text;
            
            xmlDoc.Save("media.xml");
        }

        static void RetrieveAndSendBackData(XmlDocument xmlDoc, Socket socket)
        {
            try
            {
                XmlNodeList messages = xmlDoc.SelectNodes("messages/message");

                string allMessages = "";
                string keyBetweenMessages = " /??/ ";
                
                foreach (XmlNode message in messages)
                {
                    string userName = message.SelectSingleNode("user").InnerText;
                    string messageId = message.SelectSingleNode("messageId").InnerText;
                    string text = message.SelectSingleNode("text").InnerText;

                    allMessages += new Message(userName,int.Parse(messageId),text) + keyBetweenMessages;
                }
                
                Byte[] bSend = System.Text.Encoding.ASCII.GetBytes(allMessages);
                socket.Send(bSend);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        
        // =======================================================================
        // CancelKeyPress(), anropas då användaren trycker på Ctrl-C.
        // =======================================================================
        static void CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            // Sluta lyssna efter trafik:
            tcpListener.Stop();
            Console.WriteLine("Servern stängdes av!");
        }
    }
}