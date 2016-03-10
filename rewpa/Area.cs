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

using MackLib;
using System;
using System.Collections.Generic;
using System.IO;

namespace rewpa
{
	public class Area
	{
		public short Version { get; set; }
		public short AreaId { get; set; }
		public string Server { get; set; }
		public string Name { get; set; }
		public float X1 { get; set; }
		public float Y1 { get; set; }
		public float X2 { get; set; }
		public float Y2 { get; set; }

		public List<Prop> Props { get; set; }
		public List<Event> Events { get; set; }

		public Area(PackReader pack, string workDir, string fileName)
		{
			this.Props = new List<Prop>();
			this.Events = new List<Event>();

			var areaFilePath = Path.Combine("world", workDir, fileName + ".area");

			using (var ms = pack.GetEntry(areaFilePath).GetDataAsStream())
			using (var br = new BinaryReader(ms))
			{
				Version = br.ReadInt16();
				if (Version < 202)
					throw new Exception("Invalid file version.");

				br.Skip(0x02); // Unk
				br.Skip(0x04); // Unk
				AreaId = br.ReadInt16();
				var regionId = br.ReadInt16(); // Unk
				Server = br.ReadUnicodeString();
				Name = br.ReadUnicodeString();
				br.Skip(0x10); // Unk
				var eventCount = br.ReadInt32();
				var propCount = br.ReadInt32();
				br.Skip(0x14); // Unk
				X1 = br.ReadSingle();
				br.Skip(0x04); // Unk
				Y1 = br.ReadSingle();
				br.Skip(0x0C); // Unk
				X2 = br.ReadSingle();
				br.Skip(0x04); // Unk
				Y2 = br.ReadSingle();
				br.Skip(0x0C); // Unk
				if (Version == 203)
					br.Skip(0x04); // Unk

				var ver = br.ReadInt16();
				br.Skip(0x02); // Unk
				var propCountCheck = br.ReadInt32();
				if (ver < 202 || propCount != propCountCheck)
					throw new Exception("Reading error.");

				for (int i = 0; i < propCount; ++i)
				{
					var prop = this.ReadProp(br);

					// Filter Tir anniversary props
					// TODO: Use prop db to check for the feature?
					if ((Name == "field_Tir_S_aa" || Name == "field_Tir_S_ba") && prop.ClassId > 44000)
						continue;

					// Filter blocking fence used in Bangor and Sen Mag town
					// in early gens to stop players from going there.
					// Prop is only enabled before G4: feature="-401"
					if (prop.ClassId == 41277)
						continue;

					this.Props.Add(prop);
				}

				for (int i = 0; i < eventCount; ++i)
				{
					var ev = new Event();
					this.Events.Add(ev);

					ev.EventId = br.ReadInt64();
					ev.Name = br.ReadUnicodeString();
					ev.X = br.ReadSingle();
					ev.Y = br.ReadSingle();
					br.Skip(0x04); // Unk (Z?)
					var shapeCount = br.ReadByte();
					br.Skip(0x04); // Unk 

					for (int j = 0; j < shapeCount; ++j)
					{
						var shape = new Shape();
						shape.DirX1 = br.ReadSingle();
						shape.DirX2 = br.ReadSingle();
						shape.DirY1 = br.ReadSingle();
						shape.DirY2 = br.ReadSingle();
						shape.LenX = br.ReadSingle();
						shape.LenY = br.ReadSingle();
						var shapeType = br.ReadInt32();
						shape.PosX = br.ReadSingle();
						shape.PosY = br.ReadSingle();
						br.Skip(0x10); // Unk

						ev.Shapes.Add(shape);
					}

					ev.EventType = br.ReadInt32();
					var parameterCount = br.ReadByte();
					for (int k = 0; k < parameterCount; ++k)
					{
						var def = br.ReadByte();
						var eventType = br.ReadInt32();
						var signalType = br.ReadInt32();
						var name = br.ReadUnicodeString();
						var xml = br.ReadUnicodeString();

						ev.Parameters.Add(new PropParameter(eventType, signalType, name, xml));
					}
				}
			}
		}

		private Prop ReadProp(BinaryReader br)
		{
			var prop = new Prop();

			prop.ClassId = br.ReadInt32();
			prop.PropId = br.ReadInt64();
			prop.Name = br.ReadUnicodeString();
			prop.X = br.ReadSingle();
			prop.Y = br.ReadSingle();
			br.Skip(0x04); // Unk (Z?)
			var shapeCount = br.ReadByte();
			br.Skip(0x04); // Unk 

			for (int j = 0; j < shapeCount; ++j)
			{
				var shape = new Shape();
				shape.DirX1 = br.ReadSingle();
				shape.DirX2 = br.ReadSingle();
				shape.DirY1 = br.ReadSingle();
				shape.DirY2 = br.ReadSingle();
				shape.LenX = br.ReadSingle();
				shape.LenY = br.ReadSingle();
				var shapeType = br.ReadInt32();
				shape.PosX = br.ReadSingle();
				shape.PosY = br.ReadSingle();
				br.Skip(0x10); // Unk

				prop.Shapes.Add(shape);
			}

			prop.Solid = (br.ReadByte() != 0);
			br.ReadByte(); // Unk
			prop.Scale = br.ReadSingle();
			prop.Direction = br.ReadSingle();
			br.Skip(0x40); // colors
			br.ReadUnicodeString(); // title
			prop.State = br.ReadUnicodeString();
			var parameterCount = br.ReadByte();
			for (int k = 0; k < parameterCount; ++k)
			{
				var def = br.ReadByte();
				var eventType = br.ReadInt32();
				var signalType = br.ReadInt32();
				var name = br.ReadUnicodeString();
				var xml = br.ReadUnicodeString();

				// Add identical parameters only once
				var exists = false;
				foreach (var param in prop.Parameters)
				{
					if (param.EventType == eventType && param.SignalType == signalType && param.Name == name && param.XML == xml)
					{
						exists = true;
						break;
					}
				}

				if (!exists)
					prop.Parameters.Add(new PropParameter(eventType, signalType, name, xml));
			}

			return prop;
		}
	}
}
