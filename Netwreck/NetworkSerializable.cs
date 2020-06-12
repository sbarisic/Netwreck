using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Netwrecking {
	interface NetworkSerializable {
		void Serialize(BinaryWriter Writer);

		bool Deserialize(BinaryReader Reader);

		bool IsValid();

		void SetIsValid(bool IsValid);
	}
}
