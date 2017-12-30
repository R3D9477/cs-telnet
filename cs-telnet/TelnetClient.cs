using System;
using System.Net.Sockets;

namespace Telnet
{
	public class TelnetClient : Telnet
	{
		protected TcpClient tcpSocket;
		
		public bool IsConnected
		{
			get
			{
				if (tcpSocket != null)
					return tcpSocket.Connected;
				
				return false;
			}
		}
		
		public int ConnectTimeoutMs;
		
		//------------------------------------------------------------------------------------------
		
		public TelnetClient ()
		{
			tcpSocket = null;
			ConnectTimeoutMs = 200;
		}
		
		//------------------------------------------------------------------------------------------
		
		public bool Connect (string host, int port = 23)
		{
			Disconnect();
			
			tcpSocket = new TcpClient();
			
			tcpSocket.SendTimeout = ConnectTimeoutMs;
			tcpSocket.ReceiveTimeout = ConnectTimeoutMs;
			
			try
			{
				tcpSocket.Connect(host, port);
			}
			catch
			{
				Disconnect();
			}
			
			return IsConnected;
		}
		
		public void Disconnect ()
		{
			if (IsConnected)
				tcpSocket.Close();
			
			tcpSocket = null;
		}
		
		//------------------------------------------------------------------------------------------
		
		public bool Login (string Username, string Password)
        {
			if (IsConnected)
			{
				ReadToEnd();
				
				if (WriteLine(tcpSocket.GetStream(), Username))
				{
					ReadToEnd();
					return WriteLine(tcpSocket.GetStream(), Password);
				}
			}
			
			return false;
        }
		
		//------------------------------------------------------------------------------------------
		
		public bool SendLine (string strLine)
		{
			if (IsConnected)
				try { return WriteLine(tcpSocket.GetStream(), strLine); } catch { Disconnect(); }
			
			return false;
		}
		
		public string ReadToEnd ()
		{
			if (IsConnected)
				try { return Read(tcpSocket.GetStream(), false); } catch { Disconnect();  }
			
			return "";
		}
		
		public string ReadLine ()
		{
			if (IsConnected)
				try { return ReadNoEmpty(tcpSocket.GetStream(), true); } catch { Disconnect(); }
			
			return "";
		}
	}
}