namespace ImageProcessorServer;

public class EntryPoint
{
    private static void Main(string[] args)
    {
        UdpServer udpServer = new UdpServer();
        udpServer.StartServer().Start();
    }
}