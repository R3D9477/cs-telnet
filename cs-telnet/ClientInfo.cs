using System;
using System.Threading;
using System.Net.Sockets;

namespace Telnet
{
	struct ClientInfo
	{
		public TcpClient tpcClient;
		public Thread clientThread;
		
		//------------------------------------------------------------------------------------------
		
		public ClientInfo (TcpClient tpcClient, Thread clientThread)
		{
			this.tpcClient = tpcClient;
			this.clientThread = clientThread;
		}
	}
}