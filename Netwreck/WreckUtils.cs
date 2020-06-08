using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;
using System.Buffers;

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

		public static int Serialize<T>(T Ser, byte[] Arr, int Length) where T : NetworkSerializable {
			using (MemoryStream MS = new MemoryStream(Arr, 0, Length, true)) {
				using (BinaryWriter Writer = new BinaryWriter(MS, Encoding.UTF8, true)) {
					Ser.Serialize(Writer);
					Writer.Flush();
				}

				MS.Flush();
				return (int)MS.Position;
			}
		}

		public static void Deserialize<T>(byte[] Data, ref T Obj) where T : NetworkSerializable {
			using (MemoryStream MS = new MemoryStream(Data)) {
				MS.Seek(0, SeekOrigin.Begin);

				using (BinaryReader Reader = new BinaryReader(MS))
					Obj.Deserialize(Reader);
			}
		}
	}
}
