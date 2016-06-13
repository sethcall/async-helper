using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;

namespace Tests
{
    public class BasicServer : IDisposable
    {
        bool disposed;
        int port;
        TcpListener tcpListener; 

        TcpClient  client;

        NetworkStream stream;

        public event EventHandler OnStarted;
        
        public BasicServer(int port) 
        {
            this.port = port;
            IPAddress localAddr = IPAddress.Parse("127.0.0.1");
            tcpListener = new TcpListener(localAddr, port);
        }

        public Task Start() 
        {
            return Task.Factory.StartNew(Listen, TaskCreationOptions.LongRunning);
        }

        async void Listen()
        {
            tcpListener.Start();

            if (OnStarted != null) {
                OnStarted(this, EventArgs.Empty);
            }

            // Buffer for reading data
            Byte[] bytes = new Byte[256];
            String data = null;
            
            // Enter the listening loop.
            while(true) 
            {
                // Perform a blocking call to accept requests.
                // You could also user server.AcceptSocket() here.
                try 
                {
                    this.client = await tcpListener.AcceptTcpClientAsync();
                }
                catch(System.ObjectDisposedException)
                {
                    break;
                }   
                catch( System.Net.Sockets.SocketException) 
                {
                    break;
                }         

                data = null;

                try 
                {
                    // Get a stream object for reading and writing
                    stream = client.GetStream();

                    int i;

                    // Loop to receive all the data sent by the client.
                    while((i = stream.Read(bytes, 0, bytes.Length))!=0) 
                    {   
                        // Translate data bytes to a ASCII string.
                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);

                        // Send back a response.
                        stream.Write(bytes, 0, i);
                    }

                }
                catch(System.IO.IOException)
                {
                    // swallow up; can happen on closing connection
                    break;
                }
                catch(SocketException)
                {
                    // swallow up; can happen on closing connection
                    break;
                }
                // Shutdown and end connection
                if (stream != null) {
                    stream.Dispose();
                    stream = null;
                }
                if (client != null) {
                    client.Dispose();
                    client = null;
                }
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing) {
                if (!disposed)
                {
                    this.tcpListener.Stop();

                    if (this.stream != null) {
                        stream.Dispose();
                        stream = null;
                    }
                    if (this.client != null) {
                        client.Dispose();
                        client = null;
                    }
                }
            }

            disposed = true;

            // If it is available, make the call to the
            // base class's Dispose(Boolean) method
            // base.Dispose(disposing);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    } 
}
