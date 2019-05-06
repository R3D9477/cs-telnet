using System;

using Telnet;

namespace cs_telnet_testclient
{
	class Program
	{
		static void Main(string[] args)
		{
			TelnetClient tc = new TelnetClient();
			//tc.ConnectionCheckingProc = null;

			Console.Write("Address: ");
			string a = "10.0.0.4";// Console.ReadLine();

			Console.Write("Port: ");
			string p = "23";// Console.ReadLine();

			Console.Write("Login: ");
			string l = "admin";// Console.ReadLine();

			Console.Write("Passw: ");
			string pwd = "admin";// Console.ReadLine();

			bool res = tc.Connect(
				a,
				string.IsNullOrEmpty(p) ? 23 : int.Parse(p),
				string.IsNullOrEmpty(l) ? null : l,
				string.IsNullOrEmpty(pwd) ? null : pwd
			);

			Console.WriteLine("Connection Result: {0}", res);

			string msg = "";

			if (res)
				while (msg != "exit")
				{
					Console.Write(tc.ReadToEnd() + " ");
					
					if (!tc.SendLine(msg = Console.ReadLine()))
						Console.WriteLine(" *** error ***");
				}

			Console.WriteLine("Press any key to exit...");
			Console.ReadKey(true);
		}
	}
}
