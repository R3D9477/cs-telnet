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
		
		public delegate bool LoginMethod (string login, string password);
		public LoginMethod LoginProc;
		
		public delegate bool ConnCheckMethod ();
		public ConnCheckMethod ConnectionCheckingProc;
		
		//------------------------------------------------------------------------------------------
		
		public TelnetClient ()
		{
			tcpSocket = null;
			ConnectTimeoutMs = 200;
			
			LoginProc = DefaultLoginProc;
			ConnectionCheckingProc = DefaultConnCkeck;
		}
		
		//------------------------------------------------------------------------------------------
		
		public bool Connect (string host, int port = 23, string login = null, string passw = null)
		{
			bool result = false;
			
			Disconnect();
			
			tcpSocket = new TcpClient();
			
			tcpSocket.SendTimeout = ConnectTimeoutMs;
			tcpSocket.ReceiveTimeout = ConnectTimeoutMs;
			
			try
			{
				tcpSocket.Connect(host, port);
				result = tcpSocket.Connected;
			}
			catch { result = false; }
			
			if (result)
				try
				{
					if (!string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(passw) && LoginProc != null)
						result = DefaultLoginProc(login, passw);
				}
				catch { result = false; }
			
			if (result)
				try
				{
					if (ConnectionCheckingProc != null)
						result = ConnectionCheckingProc();
				}
				catch { result = false; }
			
			if (!result)
				Disconnect();
			
			return IsConnected;
		}
		
		public void Disconnect ()
		{
			if (IsConnected)
				tcpSocket.Close();
			
			tcpSocket = null;
		}
		
		//------------------------------------------------------------------------------------------
		
		bool DefaultLoginProc (string login, string password)
        {
			if (IsConnected)
			{
				ReadToEnd();
				
				if (WriteLine(tcpSocket.GetStream(), login))
				{
					ReadToEnd();
					return WriteLine(tcpSocket.GetStream(), password);
				}
			}
			
			return false;
        }
		
		bool DefaultConnCkeck ()
		{
			if (IsConnected)
			{
				string prompt = ReadToEnd().TrimEnd();
				
				if (prompt.Length > 0)
					return prompt[prompt.Length - 1] == '$' || prompt[prompt.Length - 1] == '>';
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