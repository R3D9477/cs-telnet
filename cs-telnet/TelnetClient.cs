using System;
using System.Threading;
using System.Net.Sockets;

namespace Telnet
{
	public class TelnetClient : Telnet
	{
		protected int ReconnectionsCount = 2;

		protected TcpClient tcp;
		
		public bool IsConnected
		{
			get
			{
				for (int i = 0; i < ReconnectionsCount; i++)
				{
					try
					{
						if (tcp != null)
							if (tcp.Connected)
								if (!(tcp.Client.Poll(1, SelectMode.SelectRead) && tcp.Client.Available == 0))
									return true;
					}
					catch { }

					Reconnect();
				}

				Disconnect();

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
			
			tcp = null;
			
			ConnectTimeout = TimeSpan.FromMilliseconds(500);
			DataTransferTimeout = TimeSpan.FromMilliseconds(1000);
			
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
			
			return Reconnect();
		}
		
		public bool Reconnect ()
		{
			Disconnect();
			
			bool result = false;
			
			if (string.IsNullOrEmpty(host) || port < 1)
				return result;
			
			tcp = new TcpClient();
			
			tcp.SendTimeout = (int)DataTransferTimeout.TotalMilliseconds;
			tcp.ReceiveTimeout = (int)DataTransferTimeout.TotalMilliseconds;

            ReadNotEmptyTimeout = DataTransferTimeout;
            
            WaitHandle wh = null;
			
			try
			{
				IAsyncResult ar = tcp.BeginConnect(host, port, null, null);
				wh = ar.AsyncWaitHandle;	
				
				if (!ar.AsyncWaitHandle.WaitOne(ConnectTimeout, false))
				{
					tcp.Close();
					throw new TimeoutException();
				}
				
				tcp.EndConnect(ar);
				result = true;
			}
			catch
			{
				result = false;
			}
			
			if (wh != null)
				wh.Close();
			
			if (result)
				try
				{
					if (!string.IsNullOrEmpty(login) && !string.IsNullOrEmpty(passw) && LoginProc != null)
						result = LoginProc(login, passw);
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
			if (tcp != null)
			{
				tcp.Close();
				tcp = null;
			}
		}
		
		//------------------------------------------------------------------------------------------
		
		bool DefaultLoginProc (string login, string password)
		{
			if (IsConnected)
			{
				Thread.Sleep(200);
				Read();
				
				Thread.Sleep(200);
				if (WriteLine(tcp.GetStream(), login))
				{
					Thread.Sleep(200);
					Read();
					
					Thread.Sleep(200);
					return WriteLine(tcp.GetStream(), password);
				}
			}
			
			return false;
		}
		
		bool DefaultConnCkeck ()
		{
			if (IsConnected)
			{
				Thread.Sleep(100);
				string prompt = Read().TrimEnd();
				
				if (prompt.Length > 0)
					return prompt.Contains("$") || prompt.Contains(">");
			}
			
			return false;
		}

		//------------------------------------------------------------------------------------------

		public bool Send(string str)
		{
            Read();

			if (IsConnected)
				try
				{
					var s = tcp.GetStream();
                    return Write(s, str);
				}
				catch
				{
					Disconnect();
				}

			return false;
		}

		public bool SendLine (string strLine)
		{
            Read();

			if (IsConnected)
				try
				{
					var s = tcp.GetStream();
                    return WriteLine(s, strLine);
				}
				catch
				{
					Disconnect();
				}
			
			return false;
		}

		public string Read (bool nonempty = false , bool readline = false)
		{
			if (IsConnected)
				try
				{
					return nonempty ? ReadNoEmpty(tcp.GetStream(), readline) : base.Read(tcp.GetStream(), readline);
				}
				catch
				{
					Disconnect();
				}
			
			return "";
		}
	}
}