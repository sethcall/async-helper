using Xunit;
using System;
using System.Threading;
using System.Threading.Tasks;
using AsyncHelper;

namespace Tests
{
    public class BasicTest : IDisposable
    {
        BasicServer server;
        int port = 12322;
        string localhost = "127.0.0.1";

        string receivedMessage;
        bool disconnected;

        AutoResetEvent are;

        public BasicTest() 
        {
            are = new AutoResetEvent(false);
            receivedMessage = null;
            disconnected = false;
            server = new BasicServer(12322);
            server.OnStarted += new EventHandler(OnStarted);
            server.Start();
            bool result = are.WaitOne(5000);
            if(!result)
            {
                throw new TimeoutException("Couldn't start test listener server");
            }
        }

        private void OnStarted(object sender, EventArgs args)
        {
            are.Set();
        }

       
        public void Dispose()
        {
            server.Dispose();
            server = null;
            Thread.Sleep(1000);
        }

        [Fact]
        public async void ConnectDisconnect()
        {
            AsyncTcpClient client = new AsyncTcpClient();
            await client.ConnectAsync(localhost, port);
            Assert.True(client.IsConnected);
            await client.CloseAsync();
        }

        [Fact]
        public async void Send()
        {
            AsyncTcpClient client = new AsyncTcpClient();
            client.OnDataReceived += new EventHandler<byte[]> (TestDataReceived);
            await client.ConnectAsync(localhost, port);
            Task receiveTask = client.Receive();
            String hello = "hello";
            byte[] helloBytes = System.Text.Encoding.Unicode.GetBytes(hello);
            await client.SendAsync(helloBytes);

            Thread.Sleep(1000);

            Assert.Equal("hello", receivedMessage);
        }

        [Fact]
        public async void ServerDisconnect()
        {
            AsyncTcpClient client = new AsyncTcpClient();
            client.OnDisconnected += new EventHandler(OnDisconnected);
            await client.ConnectAsync(localhost, port);
            server.Dispose();
            Thread.Sleep(1000);
            String hello = "hello";
            byte[] helloBytes = System.Text.Encoding.Unicode.GetBytes(hello);
            // we have to write twice to see the socket disconnect
            await client.SendAsync(helloBytes);
            await client.SendAsync(helloBytes);
            Assert.True(disconnected);
        }

        private void TestDataReceived(object sender, byte[] e)
        {
            receivedMessage = System.Text.Encoding.Unicode.GetString(e);
        }

        private void OnDisconnected(object sender, EventArgs e)
        {
            disconnected = true;
        }
    }
}
