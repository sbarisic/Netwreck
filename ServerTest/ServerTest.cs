using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using Netwrecking;
using System.Threading;

namespace ServerTest {
	class Program {
		static void Main(string[] args) {
			Console.Title = "Server Test";
			NetWreck NW = new NetWreck(42000, true);

			NW.OnClientConnected += (C) => {
				Console.WriteLine("Connected {0}", C);
			};

			NW.OnClientDisconnected += (C) => {
				Console.WriteLine("Disconnected {0}", C);
			};

			NW.OnPacketReceived += (P) => {
				string Str = Encoding.UTF8.GetString(P.Payload);
				Console.WriteLine("{0} = {1}", P.Sender, Str);
			};

			NW.StartUpdateLoop();

			while (true)
				Thread.Sleep(0);
		}
	}
}
