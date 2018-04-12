using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using iso8583;

namespace ConsoleApplication1
{
    public class StateObject
    {
        // Client socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 256;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();
    }


    public class Program
    {
        // The port number for the remote device.  
        private const int port = 11000;

        public static byte[] info;

        // ManualResetEvent instances signal completion.  
        private static ManualResetEvent connectDone =
            new ManualResetEvent(false);
        private static ManualResetEvent sendDone =
            new ManualResetEvent(false);
        private static ManualResetEvent receiveDone =
            new ManualResetEvent(false);

        // The response from the remote device.  
        private static String response = String.Empty;

        private static void StartClient()
        {
            // Connect to a remote device.  
            try
            {
                // Establish the remote endpoint for the socket.  
                // The name of the   
                // remote device is "host.contoso.com".  
              //  IPHostEntry ipHostInfo = Dns.GetHostEntry("host.contoso.com");
                IPAddress ipAddress = IPAddress.Parse("192.168.80.47"); 
                IPEndPoint remoteEP = new IPEndPoint(ipAddress, 23000);

                // Create a TCP/IP socket.  
                Socket client = new Socket(ipAddress.AddressFamily,
                    SocketType.Stream, ProtocolType.Tcp);

                // Connect to the remote endpoint.  
                client.BeginConnect(remoteEP,
                    new AsyncCallback(ConnectCallback), client);
                connectDone.WaitOne();

                // Send test data to the remote device.  
                Send(client, "This is a test<EOF>");
                sendDone.WaitOne();

                // Receive the response from the remote device.  
                Receive(client);
                receiveDone.WaitOne();

                // Write the response to the console.  
                Console.WriteLine("Response received : {0}", response);

                // Release the socket.  
                client.Shutdown(SocketShutdown.Both);
                client.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete the connection.  
                client.EndConnect(ar);

                Console.WriteLine("Socket connected to {0}",
                    client.RemoteEndPoint.ToString());

                // Signal that the connection has been made.  
                connectDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Receive(Socket client)
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data from the remote device.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the state object and the client socket   
                // from the asynchronous state object.  
                StateObject state = (StateObject)ar.AsyncState;
                Socket client = state.workSocket;

                // Read data from the remote device.  
                int bytesRead = client.EndReceive(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.  
                    state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

                    // Get the rest of the data.  
                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                        new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    // All the data has arrived; put it in response.  
                    if (state.sb.Length > 1)
                    {
                        response = state.sb.ToString();
                    }
                    // Signal that all bytes have been received.  
                    receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static void Send(Socket client, String data)
        {
            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.UTF32.GetBytes(data);

            // Begin sending the data to the remote device.  
            client.BeginSend(info, 0, info.Length, 0,
                new AsyncCallback(SendCallback), client);
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                Console.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
                sendDone.Set();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        const int PORT_NO = 23000;
        const string SERVER_IP = "192.168.80.47";
        public static int Main(String[] args)
        {
           var data =  getISOData();

            var getMtiAndBitmap = data.Substring(0, 36);
        //    var message = ISO_8583_MessageSandbox.Program.SendEcho1();
            var headerDataFirst = "1601020062";
            var h05 = BitConverter.GetBytes(000000);
            var h06 = BitConverter.GetBytes(000000);
            var headerDataLast = "0000000000000000000000";
            var headerByteFirstArray = ToByteArray(headerDataFirst);
            var headerByteLastArray = ToByteArray(headerDataLast);
            var byteData = ToByteArray(getMtiAndBitmap);
            var transactionDatetime = BitConverter.GetBytes(0411123431);
            var f11 = BitConverter.GetBytes(000029);
            var f37 = BitConverter.GetBytes(810112000029);
            var f70 = BitConverter.GetBytes(071);

            byte[] sendByte = new byte[headerByteFirstArray.Length+ h05.Length+
                h06.Length+headerByteLastArray.Length+byteData.Length+ transactionDatetime.Length+f11.Length+f37.Length+ f70.Length];

            Buffer.BlockCopy(headerByteFirstArray, 0, sendByte, 0, headerByteFirstArray.Length);
            Buffer.BlockCopy(h05, 0, sendByte, headerByteFirstArray.Length, h05.Length);
            Buffer.BlockCopy(h06, 0, sendByte, headerByteFirstArray.Length+h05.Length, h06.Length);
            Buffer.BlockCopy(headerByteLastArray, 0, sendByte, headerByteFirstArray.Length+h05.Length+h06.Length, headerByteLastArray.Length);
            Buffer.BlockCopy(byteData, 0, sendByte, headerByteFirstArray.Length+h05.Length+h06.Length+ headerByteLastArray.Length, byteData.Length);
            Buffer.BlockCopy(transactionDatetime, 0, sendByte, headerByteFirstArray.Length+h05.Length+h06.Length+ headerByteLastArray.Length +byteData.Length, transactionDatetime.Length);
            Buffer.BlockCopy(f11, 0, sendByte, headerByteFirstArray.Length+h05.Length+h06.Length+ headerByteLastArray.Length +byteData.Length + transactionDatetime.Length, f11.Length);
            Buffer.BlockCopy(f37, 0, sendByte, headerByteFirstArray.Length+h05.Length+h06.Length+ headerByteLastArray.Length +byteData.Length + transactionDatetime.Length +f11.Length, f37.Length);
            Buffer.BlockCopy(f70, 0, sendByte, headerByteFirstArray.Length+h05.Length+h06.Length+ headerByteLastArray.Length +byteData.Length + transactionDatetime.Length +f11.Length +f37.Length, f70.Length);
            //  info = message;
            //            var asciiString = string.Join("", message.Select(num => num.ToString("X2")));
            //            var asciiBytes = asciiString.Select(ch => (byte)ch).ToArray();
            //            var ebcdicData = ConvertAsciiToEbcdic(asciiBytes);
            //            var asciiHexString = string.Join(" ", asciiBytes.Select(ch => ((byte)ch).ToString("X2")));
            //
            //            var convertToByte = Encoding.ASCII.GetBytes(asciiHexString);
            //            //---create a TCPClient object at the IP and port no.---
            TcpClient client = new TcpClient(SERVER_IP, PORT_NO);
            NetworkStream nwStream = client.GetStream();


            var messageHeader = new byte[2];
            var messageLength = sendByte.Length;
            messageHeader[0] = Convert.ToByte(messageLength / 256);
            messageHeader[1] = Convert.ToByte(messageLength % 256);

            // create the message including the header
            var message = new byte[messageHeader.Length + sendByte.Length + 2];
            byte[] abc = new byte[2];
            abc[0] = Convert.ToByte(0);
            abc[1] = Convert.ToByte(0);
            Buffer.BlockCopy(messageHeader, 0, message, 0, messageHeader.Length);
            Buffer.BlockCopy(abc, 0, message,  messageHeader.Length, abc.Length);
            Buffer.BlockCopy(sendByte, 0, message, abc.Length +messageHeader.Length, sendByte.Length);

            var inad = client.SendBufferSize;
           // byte[] sendDataToRead = new byte[inad];
            //Buffer.BlockCopy(sendByte, 0, sendDataToRead, 0, sendByte.Length);
            nwStream.Write(message, 0, message.Length);

            //---send the text---
            //  Console.WriteLine("Sending : " + textToSend);
            //     nwStream.Write(info, 0, 51);

            //---read back the text---
            byte[] bytesToRead = new byte[client.ReceiveBufferSize];
            int bytesRead = nwStream.Read(bytesToRead, 0, client.ReceiveBufferSize);
            Console.WriteLine("Received : " + Encoding.ASCII.GetString(bytesToRead, 0, bytesRead));
            Console.ReadLine();
            client.Close();
            return 0;
        }


        public static string getISOData()
        {


            string MTI = "0800";
            //2.Create an object BIM-ISO8583.ISO8583
            BIM_ISO8583.NET.ISO8583 iso8583 = new BIM_ISO8583.NET.ISO8583();

            //3. Create Arrays
            string[] DE = new string[130];

            //4. Assign corresponding data to each array
            //   Ex: ISO8583 Data Element No.2 (PAN) shall assign to newly created array, DE[2];
            DE[7] = "0411123431";
            DE[11] = "000032";
            DE[37] = "810112000029";
            DE[70] = "071";


            //5.Use "Build" method of object iso8583 to create a new  message.
            string NewISOmsg = iso8583.Build(DE, MTI);
            string lengths = NewISOmsg.Length.ToString();
            return NewISOmsg;
        }

        
            public static byte[] ToByteArray(String input)
            {
            var outputLength = input.Length / 2;
                var output = new byte[outputLength];
                for (var i = 0; i < outputLength; i++)
                    output[i] = Convert.ToByte(input.Substring(i * 2, 2), 16);
                return output;
        }
        

    }


}
