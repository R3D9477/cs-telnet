using System;
using System.Threading;
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
		
		public TimeSpan ConnectTimeout;
		public TimeSpan DataTransferTimeout;
		
		//------------------------------------------------------------------------------------------
		
		string host;
		int port;
		string login;
		string passw;
		
		//------------------------------------------------------------------------------------------
		
		public delegate bool LoginMethod (string login, string password);
		public LoginMethod LoginProc;
		
		public delegate bool ConnCheckMethod ();
		public ConnCheckMethod ConnectionCheckingProc;
		
		//------------------------------------------------------------------------------------------
		
		public TelnetClient ()
		{
			this.host = null;
			this.port = 23;
			this.login = null;
			this.passw = null;
			
			tcpSocket = null;
			
			ConnectTimeout = TimeSpan.FromMilliseconds(500);
			DataTransferTimeout = TimeSpan.FromMilliseconds(250);
			
			LoginProc = DefaultLoginProc;
			ConnectionCheckingProc = DefaultConnCkeck;
		}
		
		//------------------------------------------------------------------------------------------
		
		public bool Connect (string host, int port = 23, string login = null, string passw = null)
		{
			this.host = host;
			this.port = port;
			this.login = login;
			this.passw = passw;
			
			Reconnect();
			
			return IsConnected;
		}
		
		public bool Reconnect ()
		{
			Disconnect();
			
			bool result = false;
			
			if (string.IsNullOrEmpty(host) || port < 1)
				return result;
			
			tcpSocket = new TcpClient();
			
			tcpSocket.SendTimeout = (int)DataTransferTimeout.TotalMilliseconds;
			tcpSocket.ReceiveTimeout = (int)DataTransferTimeout.TotalMilliseconds;
			
			IAsyncResult ar = tcpSocket.BeginConnect(host, port, null, null);  
			WaitHandle wh = ar.AsyncWaitHandle;  
			
			try
			{  
				if (!ar.AsyncWaitHandle.WaitOne(ConnectTimeout, false))  
				{  
					tcpSocket.Close();  
					throw new TimeoutException();
				}  
				
				tcpSocket.EndConnect(ar);
				result = true;
			}
			catch
			{
				result = false;
			}
			finally 
			{  
				wh.Close();
			}
				
			if (result)
				try
				{
					if (!string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(passw) && LoginProc != null)
						result = DefaultLoginProc(login, passw);
				}
				catch
				{
					result = false;
				}
			
			if (result)
				try
				{
					if (ConnectionCheckingProc != null)
						result = ConnectionCheckingProc();
				}
				catch
				{
					result = false;
				}
			
			if (!result)
				Disconnect();
			
			return result;
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
				Thread.Sleep(50);
				ReadToEnd();
				
				Thread.Sleep(50);
				if (WriteLine(tcpSocket.GetStream(), login))
				{
					Thread.Sleep(50);
					ReadToEnd();
					
					Thread.Sleep(50);
					return WriteLine(tcpSocket.GetStream(), password);
				}
			}
			
			return false;
        }
		
		bool DefaultConnCkeck ()
		{
			if (IsConnected)
			{
				Thread.Sleep(100);
				string prompt = ReadToEnd().TrimEnd();
				
				if (prompt.Length > 0)
					return prompt.Contains("$") || prompt.Contains(">");
			}
			
			return false;
		}
		
		//------------------------------------------------------------------------------------------
		
		public bool SendLine (string strLine)
		{
			if (IsConnected)
				try
				{
					return WriteLine(tcpSocket.GetStream(), strLine);
				}
				catch
				{
					Disconnect();
				}
			
			return false;
		}
		
		public string ReadToEnd ()
		{
			if (IsConnected)
				try
				{
					return Read(tcpSocket.GetStream(), false);
				}
				catch
				{
					Disconnect();
				}
			
			return "";
		}
		
		public string ReadLine ()
		{
			if (IsConnected)
				try
				{
					return ReadNoEmpty(tcpSocket.GetStream(), true);
				}
				catch
				{
					Disconnect();
				}
			
			return "";
		}
	}
}