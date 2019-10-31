using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

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

		public static byte[] Serialize<T>(T Ser) where T : NetworkSerializable {
			using (MemoryStream MS = new MemoryStream())
			using (BinaryWriter Writer = new BinaryWriter(MS)) {
				Ser.Serialize(Writer);
				return MS.ToArray();
			}
		}

		public static T Deserialize<T>(byte[] Data) where T : NetworkSerializable, new() {
			T Obj = new T();

			using (MemoryStream MS = new MemoryStream(Data)) {
				MS.Seek(0, SeekOrigin.Begin);

				using (BinaryReader Reader = new BinaryReader(MS))
					Obj.Deserialize(Reader);
			}

			return Obj;
		}
	}
}
