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
			NW.StartUpdateLoop();

			while (true)
				Thread.Sleep(0);
		}
	}
}
