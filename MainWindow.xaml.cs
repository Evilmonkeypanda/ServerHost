using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServerHost
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {

            InitializeComponent();
        }

        Socket hostSocket;
        List<Socket> clientSockets = new List<Socket>();
        Socket clientSocket;
        List<Thread> threads = new List<Thread>();
        
        //Function called when StartServer is pressed.
        //Returns the created server to the public hostSocket
        public Socket CreateServer() {
            //Start Server Here
            IPHostEntry host = Dns.GetHostEntry("");
            IPAddress iPAddress = host.AddressList[0];
            IPEndPoint localEndPoint = new IPEndPoint(iPAddress, 11000);

            try
            {
                Socket listener = new Socket(iPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(localEndPoint);
                StatusText.Content = "Online";
                return listener;
            }
            catch (Exception ecx)
            {
                Console.WriteLine(ecx.ToString());
                StatusText.Content = "Server Error";
                return null;
            }


        }

      

        //This function is called in a new thread when a new client connects
        //The thread will hang on client.Recieve() until a message comes through
        //from that client then use broadCastMessage() to send to every client
        public void ThreadListener(Socket client) {
            Console.WriteLine("New Thread created!");
            while (true)
            {
                byte[] bytes = new byte[client.Available];
                
                try
                {
                    client.Receive(bytes, 0, client.Available, SocketFlags.None);
                    Console.WriteLine("New Message Recieved: {0}", Encoding.ASCII.GetString(bytes));
                    if (bytes.Length > 0)
                    {
                        this.Dispatcher.Invoke(() =>
                        {
                            BroadCastMessage(bytes);

                        });
                    }
                }
                //SocketException called when a client closes connection
                //This will close the connection from the server and remove the client
                //from the client list then end the usage of that client thread.
                catch (SocketException SE) {
                    client.Close();
                    clientSockets.Remove(client);
                    Console.WriteLine("Client Exit: {0}", SE.Message);
                    this.Dispatcher.Invoke(() =>
                    {
                        ConnectionTextUpdate();
                    });
                    break;
                }
                }
            
        }


        //Called when a client connects or disconnects.
        //Needs to be invoked if called from a thread.
        //I know you'll forget anyway but I told you so
        public void ConnectionTextUpdate() {
            ConnectionText.Content = "Connections : " + clientSockets.Count; ;
        
        }

        //Run this in its own thread
        //Waits for new connections and then makes its own baby threads
        //They grow up so fast :')
        //Each baby thread will hang until a message comes through to accept.
        //Also updates UI when new connection comes in
        public void OpenForConnections()
        {
            while (true)
            {
                hostSocket.Listen(10);
                clientSockets.Add(hostSocket.Accept());
                threads.Add(new Thread(new ThreadStart(() => ThreadListener(clientSockets[^1]))));
                threads[^1].Start();
                threads[^1].IsBackground = true;
                Console.WriteLine("New connection of: {0}", (clientSockets[clientSockets.Count - 1]));

                this.Dispatcher.Invoke(() =>
                {
                    ConnectionTextUpdate();
                });
            }
        }

        //Believe it or not I used a for loop for this and it used about twice as 
        //many lines. Not proud of that one.
        //Loops through each client in the list and sends out the message
        //Invoke or you will be excommunicated 
        public void BroadCastMessage(byte[] msg) {
            foreach (Socket client in clientSockets) {
                Console.WriteLine("Sending message: {0} to client ", Encoding.ASCII.GetString(msg));
                client.Send(msg);
            }
                    }
                   
             
        //Creates new socket to use to accept incoming connections
        //Creates a new thread to handle accepting incoming connections without
        //hanging program but not taking up resources.
        //I think?
        private void StartServer(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Creating Server");
            hostSocket = CreateServer();
            Console.WriteLine("Waiting for connection");
            Thread c = new Thread(new ThreadStart(OpenForConnections));
            c.Start();
            c.IsBackground = true;
          
        }
    }
}
