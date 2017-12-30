using System;

namespace TelnetTest
{
	class Program
	{
		static bool ClientAuth (string login, string password)
		{
			Console.WriteLine("Login: {0}", login);
			Console.WriteLine("Passw: {0}", password);
			
			return true;
		}
		
		static void ClientConnectEvent (Guid cg, System.Net.Sockets.TcpClient tcpClient)
		{
			Console.WriteLine("Client {0} connected", cg);
		}
		
		static void ClientTextReceived (Guid cg, string text)
		{
			Console.WriteLine("Client {0}: {1}", cg, text);
		}
		
		public static void Main (string[] args)
		{
			var ts = new Telnet.TelnetServer();
			ts.ClientAuth = ClientAuth;
			ts.onClientConnect += ClientConnectEvent;
			ts.onClientRecStrLine += ClientTextReceived;
			ts.Start("127.0.0.1");
			
			Console.WriteLine("Telnet server started.");
			Console.WriteLine("Press any key to exit.");
			Console.WriteLine();
			Console.ReadKey(true);
		}
	}
}