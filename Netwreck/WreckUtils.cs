using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Netwrecking {
	internal static class WreckUtils {
		public static Random Rnd = new Random();

		public static void RandomDelay(int Min = 1, int Max = 500) {
			int Delay = Rnd.Next(Min, Max);
			Thread.Sleep(Delay);
		}

		public static byte[][] BufferSplit(byte[] Buffer, int BlockSize) {
			byte[][] Blocks = new byte[(Buffer.Length + BlockSize - 1) / BlockSize][];

			for (int i = 0, j = 0; i < Blocks.Length; i++, j += BlockSize) {
				Blocks[i] = new byte[Math.Min(BlockSize, Buffer.Length - j)];
				Array.Copy(Buffer, j, Blocks[i], 0, Blocks[i].Length);
			}

			return Blocks;
		}
	}
}
