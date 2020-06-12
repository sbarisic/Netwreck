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
					Writer.Write(0xFFFFFFFF);
					Writer.Flush();
				}

				MS.Flush();
				int Len = (int)MS.Position;

				CRC32.Crc32CAlgorithm.ComputeAndWriteToEnd(Arr, 0, Len - 4);
				return Len;
			}
		}

		public static void Deserialize<T>(byte[] Data, ref T Obj) where T : NetworkSerializable {
			using (MemoryStream MS = new MemoryStream(Data)) {
				MS.Seek(0, SeekOrigin.Begin);

				using (BinaryReader Reader = new BinaryReader(MS)) {
					Obj.Deserialize(Reader);

					uint CRC32C = Reader.ReadUInt32();
					int Len = (int)Reader.BaseStream.Position;

					Obj.SetIsValid(CRC32.Crc32CAlgorithm.IsValidWithCrcAtEnd(Data, 0, Len));
				}
			}
		}

		/// <summary>
		/// Returns if S1 > S2 with wrap around
		/// </summary>
		/// <param name="S1"></param>
		/// <param name="S2"></param>
		/// <returns></returns>
		public static bool SeqGreater(ushort S1, ushort S2) {
			return ((S1 > S2) && (S1 - S2 <= 32768)) || ((S1 < S2) && (S2 - S1 > 32768));
		}

		public static void IncSeq(ref ushort S) {
			unchecked {
				S++;
			}
		}
	}
}
