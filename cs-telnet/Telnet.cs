using System;
using System.Text;
using System.Linq;
using System.Threading;
using System.Net.Sockets;

namespace Telnet
{
	public abstract class Telnet
	{
		protected Encoding Enc = ASCIIEncoding.ASCII;
		protected TimeSpan WriteDelay = TimeSpan.FromMilliseconds(10);
		protected TimeSpan ReadDelay = TimeSpan.FromMilliseconds(100);
        protected TimeSpan ReadNotEmptyTimeout = TimeSpan.FromMilliseconds(100);
		
		//------------------------------------------------------------------------------------------
		
		/*
			[NAME]					[CODE]		[MEANING]
			NULL(NUL)				0			A no operation.
			BELL(BEL)				7			Produces an audible or visible signal.
			Back Space (BS)			8			Backspaces the printer one character position.
			Horizontal Tab (HT)		9			Moves the printer to next horizontal tab stop.
			Line Feed (LF)			10			Moves the printer to next line (keeping the same horizontal position).
			Vertical Tab(VT)		11			Moves the printer to the next vertical tab stop.
			Form Feed(FF)			12			Moves the printer to the top of the next page.
			Carriage Return (CR)	13			Moves the printer to the left margin of the current line.
		*/

		const byte CR  = 13;
		const byte LF  = 10;
		const byte NUL = 0;

		public enum EOLType
		{
			CRLF  = 0,
			CRNUL = 1,
			LF    = 2
		}

		public EOLType EOL = EOLType.CRLF;

		//------------------------------------------------------------------------------------------
		
		protected bool Write (NetworkStream stream, byte[] cmd)
		{
            if (stream != null)
				if (stream.CanWrite)
				{
					try
					{
						stream.Write(cmd, 0, cmd.Length);
						stream.Flush();
						return true;
					}
					catch { }
				}

			return false;
		}

		protected bool Write (NetworkStream stream, string cmd)
		{
			return Write(stream, Enc.GetBytes(cmd.Replace("\0xFF", "\0xFF\0xFF")));
		}

		protected bool WriteEOL (NetworkStream stream)
		{
			switch (EOL)
			{
				case EOLType.CRLF:
					return Write(stream, new byte[] { CR, LF });
				case EOLType.CRNUL:
					return Write(stream, new byte[] { CR, NUL });
				case EOLType.LF:
					return Write(stream, new byte[] { LF });
				default:
					return false;
			}
		}

		protected bool WriteBySymbol (NetworkStream stream, string cmd)
		{
			foreach (var c in cmd)
			{
				if (!Write(stream, c.ToString()))
					return false;

				Thread.Sleep(WriteDelay);
			}

			return true;
		}

		protected bool WriteLine (NetworkStream stream, string cmd)
		{
			return WriteBySymbol(stream, cmd) && WriteEOL(stream);
		}
		
		//------------------------------------------------------------------------------------------
		
		protected string ReadNoEmpty (NetworkStream stream, bool readline)
		{
            string s = string.Empty;
            
            for (int i = 0; (i < ReadNotEmptyTimeout.TotalMilliseconds) && (stream != null ? stream.CanRead : false) && string.IsNullOrEmpty(s = Read(stream, readline)); i++)
                Thread.Sleep(1);

            return s;
		}
		
		protected string Read (NetworkStream stream, bool readline)
		{
			var sb = new StringBuilder();
			
			if (stream != null)
				if (stream.CanRead)
					ParseTelnet(stream, sb);

            if (readline)
            {
                var buff = sb.ToString().Split(new char[] { (char)CR, (char)LF, (char)NUL, '\r', '\n', '>' }, StringSplitOptions.RemoveEmptyEntries);

                if (buff.Length > 0)
                    return buff[buff.Length - 1];
                else
                    return "";
            }

			return sb.ToString();
		}
		
		void ParseTelnet (NetworkStream stream, StringBuilder sb)
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
							int inputverb = stream.ReadByte();
							if (inputverb == -1) break;
								switch (inputverb)
								{
									case (int)Verbs.IAC:
											sb.Append(inputverb);
										break;
									case (int)Verbs.DO:
									case (int)Verbs.DONT:
									case (int)Verbs.WILL:
									case (int)Verbs.WONT:
											int inputoption = stream.ReadByte();
                                            
											if (inputoption >= 0)
											{
												stream.WriteByte((byte)Verbs.IAC);
												if (inputoption == (int)Options.SGA)
													stream.WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WILL : (byte)Verbs.DO);
												else
													stream.WriteByte(inputverb == (int)Verbs.DO ? (byte)Verbs.WONT : (byte)Verbs.DONT);
												stream.WriteByte((byte)inputoption);
											}
										break;
								}
							break;
						default:
								sb.Append((char)input);
							break;
					}
				}
				Thread.Sleep(ReadDelay);
			}
			while (stream.DataAvailable);
		}
	}
}