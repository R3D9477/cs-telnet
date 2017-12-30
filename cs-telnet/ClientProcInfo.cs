using System;
using System.Net.Sockets;

namespace Telnet
{
	struct ClientProcInfo
	{
		public Guid clientGuid;
		public TcpClient tcpClient;
		
		public ClientProcInfo (Guid cg, TcpClient tc)
		{
			this.clientGuid = cg;
			this.tcpClient = tc;
		}
	}
}