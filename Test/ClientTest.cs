using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Netwrecking;
using System.Net;

namespace ClientTest {
	class Program {
		static void Main(string[] Args) {
			Console.Title = "Client Test";
			Thread.Sleep(1000);

			NetWreck NW = new NetWreck(42001);
			NW.StartUpdateLoop();
			NW.ConnectToServer(NetWreck.CreateEndPoint("127.0.0.1", 42000));

			while (true)
				Thread.Sleep(0);
		}
	}
}
