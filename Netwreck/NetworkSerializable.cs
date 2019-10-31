using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netwrecking {
	interface NetworkSerializable {
		void Serialize(BinaryWriter Writer);

		void Deserialize(BinaryReader Reader);
	}
}
