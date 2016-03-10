// rewpa - World data converter
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Text;

namespace rewpa
{
	public static class BinaryReaderExt
	{
		public static string ReadUnicodeString(this BinaryReader br)
		{
			var sb = new StringBuilder();

			short c = 0;
			do
			{
				c = br.ReadInt16();
				if (c != 0)
					sb.Append(BitConverter.ToChar(BitConverter.GetBytes(c), 0));
			}
			while (c != 0);

			return sb.ToString();
		}

		public static void Skip(this BinaryReader br, int count)
		{
			for (int i = 0; i < count; ++i)
				br.ReadByte();
		}
	}
}
