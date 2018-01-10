using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;

// Original library: https://www.codeproject.com/Articles/19071/Quick-tool-A-minimalistic-Telnet-library

namespace Telnet
{
	public abstract class Telnet
	{
		protected Encoding Enc = ASCIIEncoding.ASCII;
		protected TimeSpan ReadTimeout = TimeSpan.FromMilliseconds(10);
		
		//------------------------------------------------------------------------------------------
		
		protected bool WriteLine (NetworkStream stream, string cmd)
		{
			return Write(stream, cmd + "\n");
		}
		
		protected bool Write (NetworkStream stream, string cmd)
		{
			if (stream != null)
				if (stream.CanWrite)
				{
					byte[] buf = Enc.GetBytes(cmd.Replace("\0xFF", "\0xFF\0xFF"));
					
					try
					{
						stream.Write(buf, 0, buf.Length);
						return true;
					}
					catch { }
				}
			
			return false;
		}
		
		//------------------------------------------------------------------------------------------
		
		protected string ReadNoEmpty (NetworkStream stream, bool readline)
		{
			string s;
			
			while (string.IsNullOrEmpty(s = Read(stream, readline)))
				Thread.Sleep(1);
			
			return s;
		}
		
		protected string Read (NetworkStream stream, bool readline)
		{
			var sb = new StringBuilder();
			
			if (stream != null)
				if (stream.CanRead)
					ParseTelnet(stream, sb, readline);
			
			return sb.ToString();
		}
		
		void ParseTelnet (NetworkStream stream, StringBuilder sb, bool readline)
		{
			do
			{
				while (stream.DataAvailable)
				{
					int input = stream.ReadByte();
					
					switch (input)
					{
						case -1:
							break;
						case (int)Verbs.IAC:
							// interpret as command
							int inputverb = stream.ReadByte();
							if (inputverb == -1) break;
								switch (inputverb)
								{
									case (int)Verbs.IAC: 
											//literal IAC = 255 escaped, so append char 255 to string
											sb.Append(inputverb);
										break;
									case (int)Verbs.DO: 
									case (int)Verbs.DONT:
									case (int)Verbs.WILL:
									case (int)Verbs.WONT:
											// reply to all commands with "WONT", unless it is SGA (suppres go ahead)
											int inputoption = stream.ReadByte();
											
											if (inputoption == -1)
												break;
											
											stream.WriteByte((byte)Verbs.IAC);
											
											if (inputoption == (int)Options.SGA )
												stream.WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WILL:(byte)Verbs.DO); 
											else
												stream.WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WONT : (byte)Verbs.DONT); 
												stream.WriteByte((byte)inputoption);
										break;
								}
							break;
						default:
								if (readline && (input == 0 || input == 10 || input == 13))
									return;
								
								sb.Append((char)input);
							break;
					}
				}
					
				Thread.Sleep(ReadTimeout);
			}
			while (stream.DataAvailable);
		}
	}
}