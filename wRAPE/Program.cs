// wRAPE - World data converter
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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using wRAPE.Properties;

namespace wRAPE
{
	class Program
	{
		static void Main(string[] args)
		{
			var wPath = Settings.Default.World;
			var sPath = Settings.Default.Save;
			var path = "";

			Console.WriteLine("wRAPE");
			Console.WriteLine();
			Console.WriteLine("Hi there, {0}! Would you be so kind to give me", Environment.UserName);
			Console.WriteLine("the path to your extracted data\\world\\ folder?");

			while (true)
			{
				Console.Write("[{0}]: ", Settings.Default.World);
				path = Console.ReadLine();
				if (!string.IsNullOrWhiteSpace(path))
					wPath = path;

				wPath = wPath.Trim().Trim('"');

				if (File.Exists(Path.Combine(wPath, "world.trn")))
					break;

				Console.WriteLine("No... that doesn't seem to be correct,  I can't find the 'world.trn' file.");
				Console.WriteLine("Try again, please.");
			}

			Console.WriteLine();
			Console.WriteLine("Let's take a look, give me just a minute.");
			var world = new World(Path.Combine(wPath, "world.trn"));

			Console.WriteLine("I've found a world consisting of {0} regions.", world.Regions.Count);
			Console.WriteLine("Where should I save my results?");

			Console.Write("[{0}]: ", Settings.Default.Save);
			path = Console.ReadLine();
			if (!string.IsNullOrWhiteSpace(path))
				sPath = path;

			if (Directory.Exists(sPath))
				sPath = Path.Combine(sPath, "regioninfo.dat");

			Console.WriteLine("Very well.");
			world.Export(sPath);

			Settings.Default.World = wPath;
			Settings.Default.Save = sPath;
			Settings.Default.Save();

			Console.WriteLine("I'm done. You can close me now.");
			Console.WriteLine("I've written the result to '{0}'.", Path.GetFullPath(sPath));
			Console.ReadLine();
		}
	}

	public class World
	{
		public List<Region> Regions { get; set; }

		public World(string path)
		{
			this.Regions = new List<Region>();

			using (var trnReader = XmlReader.Create(path))
			{
				if (!trnReader.ReadToDescendant("regions"))
					return;

				using (var trnRegionsReader = trnReader.ReadSubtree())
				{
					while (trnRegionsReader.ReadToFollowing("region"))
					{
						var region = new Region(Path.Combine(Path.GetDirectoryName(path), trnRegionsReader.GetAttribute("workdir"), trnReader.GetAttribute("name") + ".rgn"));
						this.Regions.Add(region);
					}
				}
			}

			Regions = Regions.OrderBy(a => a.RegionId).ToList();
			Regions.ForEach(region =>
			{
				region.Areas = region.Areas.OrderBy(a => a.AreaId).ToList();
				region.Areas.ForEach(area =>
				{
					area.Props = area.Props.OrderBy(prop => prop.PropId).ToList();
					area.Events = area.Events.OrderBy(ev => ev.EventId).ToList();
				});
			});
		}

		public void Export(string path)
		{
			using (var ms = new MemoryStream())
			using (var bw = new BinaryWriter(ms))
			{
				// Regions
				bw.Write(Regions.Count);
				Regions.ForEach(region =>
				{
					bw.Write(region.RegionId);

					// Bounds
					int x1, y1, x2, y2;
					x1 = y1 = int.MaxValue;
					x2 = y2 = 0;
					region.Areas.ForEach(area =>
					{
						if (area.X1 < x1) x1 = (int)area.X1;
						if (area.Y1 < y1) y1 = (int)area.Y1;
						if (area.X2 > x2) x2 = (int)area.X2;
						if (area.Y2 > y2) y2 = (int)area.Y2;
					});
					bw.Write(x1);
					bw.Write(y1);
					bw.Write(x2);
					bw.Write(y2);

					// Areas
					bw.Write(region.Areas.Count);
					region.Areas.ForEach(area =>
					{
						bw.Write((int)area.AreaId);
						bw.Write((int)area.X1);
						bw.Write((int)area.Y1);
						bw.Write((int)area.X2);
						bw.Write((int)area.Y2);

						// Props
						bw.Write(area.Props.Count);
						area.Props.ForEach(prop =>
						{
							bw.Write(prop.PropId);
							bw.Write(prop.ClassId);
							bw.Write(prop.X);
							bw.Write(prop.Y);
							bw.Write(prop.Direction);
							bw.Write(prop.Scale);
							//bw.Write(prop.Solid);

							// Shape
							bw.Write(prop.Shape.Count / 2);
							prop.Shape.ForEach(line =>
							{
								bw.Write(line.P1.X);
								bw.Write(line.P1.Y);
								bw.Write(line.P2.X);
								bw.Write(line.P2.Y);
							});

							// Parameters
							bw.Write(prop.Parameters.Count);
							prop.Parameters.ForEach(parameter =>
							{
								bw.Write(parameter.EventType);
								bw.Write(parameter.SignalType);
								bw.Write(parameter.Name);
								bw.Write(parameter.XML);
							});
						});

						// Events
						bw.Write(area.Events.Count);
						area.Events.ForEach(ei =>
						{
							bw.Write(ei.EventId);
							bw.Write(ei.X);
							bw.Write(ei.Y);
							bw.Write(ei.EventType);

							// Shape
							bw.Write(ei.Shape.Count / 2);
							ei.Shape.ForEach(line =>
							{
								bw.Write(line.P1.X);
								bw.Write(line.P1.Y);
								bw.Write(line.P2.X);
								bw.Write(line.P2.Y);
							});

							// Parameters
							bw.Write(ei.Parameters.Count);
							ei.Parameters.ForEach(parameter =>
							{
								bw.Write(parameter.EventType);
								bw.Write(parameter.SignalType);
								bw.Write(parameter.Name);
								bw.Write(parameter.XML);
							});
						});
					});
				});

				// zip it
				using (var min = new MemoryStream(ms.ToArray()))
				using (var mout = new MemoryStream())
				{
					using (var gzip = new GZipStream(mout, CompressionMode.Compress))
						min.CopyTo(gzip);

					File.WriteAllBytes(path, mout.ToArray());
				}
			}
		}
	}

	public class Region
	{
		public int Version { get; set; }
		public int RegionId { get; set; }
		public int GroupId { get; set; }
		public string Name { get; set; }
		public int CellSize { get; set; }
		public byte Sight { get; set; }
		public string Scene { get; set; }
		public string Camera { get; set; }
		public string Light { get; set; }
		public string XML { get; set; }

		public List<Area> Areas { get; set; }

		public Region(string path)
		{
			this.Areas = new List<Area>();

			using (var fs = new FileStream(path, FileMode.Open))
			using (var br = new BinaryReader(fs))
			{
				Version = br.ReadInt32();
				br.ReadInt32(); // Unk
				RegionId = br.ReadInt32();
				GroupId = br.ReadInt32();
				Name = br.ReadUnicodeString();
				CellSize = br.ReadInt32();
				Sight = br.ReadByte();
				var areaCount = br.ReadInt32();
				br.Skip(0x40); // Unk
				Scene = br.ReadUnicodeString();
				br.Skip(0x2D); // Unk
				Camera = br.ReadUnicodeString();
				Light = br.ReadUnicodeString();
				br.Skip(0x0C); // Unk

				for (int i = 0; i < areaCount; ++i)
				{
					var areaName = br.ReadUnicodeString();
					var area = new Area(Path.Combine(Path.GetDirectoryName(path), areaName + ".area"));
					Areas.Add(area);
				}

				br.Skip(0x1B); // Unk
				XML = br.ReadUnicodeString();
			}
		}
	}

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

		public Area(string path)
		{
			this.Props = new List<Prop>();
			this.Events = new List<Event>();

			using (var fs = new FileStream(path, FileMode.Open))
			using (var br = new BinaryReader(fs))
			{
				Version = br.ReadInt16();
				if (Version < 202)
					throw new Exception("Invalid file version.");

				br.Skip(0x02); // Unk
				br.Skip(0x04); // Unk
				AreaId = br.ReadInt16();
				br.ReadInt16(); // Unk
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
					var prop = new Prop();
					this.Props.Add(prop);

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
						var dirX1 = br.ReadSingle();
						var dirX2 = br.ReadSingle();
						var dirY1 = br.ReadSingle();
						var dirY2 = br.ReadSingle();
						var lenX = br.ReadSingle();
						var lenY = br.ReadSingle();
						br.Skip(0x04); // Unk
						var posX = br.ReadSingle();
						var posY = br.ReadSingle();
						br.Skip(0x10); // Unk

						prop.Shape.AddRange(GetShapeLines(dirX1, dirX2, dirY1, dirY2, lenX, lenY, posX, posY));
					}

					prop.Solid = (br.ReadByte() != 0);
					br.ReadByte(); // Unk
					prop.Scale = br.ReadSingle();
					prop.Direction = br.ReadSingle();
					br.Skip(0x40); // Unk
					br.ReadUnicodeString(); // title
					br.ReadUnicodeString(); // state
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

					float test = 0;
					for (int j = 0; j < shapeCount; ++j)
					{
						var dirX1 = br.ReadSingle();
						var dirX2 = br.ReadSingle();
						var dirY1 = br.ReadSingle();
						var dirY2 = br.ReadSingle();
						var lenX = br.ReadSingle();
						var lenY = br.ReadSingle();
						br.Skip(0x04); // Unk
						var posX = br.ReadSingle();
						var posY = test = br.ReadSingle();
						br.Skip(0x10); // Unk

						ev.Shape.AddRange(GetShapeLines(dirX1, dirX2, dirY1, dirY2, lenX, lenY, posX, posY));
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

		private Line[] GetShapeLines(float dirX1, float dirX2, float dirY1, float dirY2, float lenX, float lenY, float posX, float posY)
		{
			var points = new Point[4];

			double a00 = dirX1 * lenX;
			double a01 = dirX2 * lenX;
			double a02 = dirY1 * lenY;
			double a03 = dirY2 * lenY;

			double sx1 = posX - a00 - a02; if (sx1 < posX) sx1 = Math.Ceiling(sx1);
			double sy1 = posY - a01 - a03; if (sy1 < posY) sy1 = Math.Ceiling(sy1);
			double sx2 = posX + a00 - a02; if (sx2 < posX) sx2 = Math.Ceiling(sx2);
			double sy2 = posY + a01 - a03; if (sy2 < posY) sy2 = Math.Ceiling(sy2);
			double sx3 = posX + a00 + a02; if (sx3 < posX) sx3 = Math.Ceiling(sx3);
			double sy3 = posY + a01 + a03; if (sy3 < posY) sy3 = Math.Ceiling(sy3);
			double sx4 = posX - a00 + a02; if (sx4 < posX) sx4 = Math.Ceiling(sx4);
			double sy4 = posY - a01 + a03; if (sy4 < posY) sy4 = Math.Ceiling(sy4);

			if (a02 * a01 > a03 * a00)
			{
				points[0] = new Point((int)sx1, (int)sy1);
				points[1] = new Point((int)sx2, (int)sy2);
				points[2] = new Point((int)sx3, (int)sy3);
				points[3] = new Point((int)sx4, (int)sy4);
			}
			else
			{
				points[0] = new Point((int)sx1, (int)sy1);
				points[3] = new Point((int)sx2, (int)sy2);
				points[2] = new Point((int)sx3, (int)sy3);
				points[1] = new Point((int)sx4, (int)sy4);
			}

			var result = new Line[2];
			result[0] = new Line(points[0], points[1]);
			result[1] = new Line(points[2], points[3]);

			return result;
		}
	}

	public class Prop
	{
		public int ClassId { get; set; }
		public long PropId { get; set; }
		public string Name { get; set; }
		public float X { get; set; }
		public float Y { get; set; }
		public List<Line> Shape { get; set; }
		public bool Solid { get; set; }
		public float Scale { get; set; }
		public float Direction { get; set; }
		public List<PropParameter> Parameters { get; set; }

		public Prop()
		{
			Shape = new List<Line>();
			Parameters = new List<PropParameter>();
		}
	}

	public class Event
	{
		public long EventId { get; set; }
		public string Name { get; set; }
		public float X { get; set; }
		public float Y { get; set; }
		public List<Line> Shape { get; set; }
		public int EventType { get; set; }
		public List<PropParameter> Parameters { get; set; }

		public Event()
		{
			Shape = new List<Line>();
			Parameters = new List<PropParameter>();
		}
	}

	public class PropParameter
	{
		public int EventType { get; set; }
		public int SignalType { get; set; }
		public string Name { get; set; }
		public string XML { get; set; }

		public PropParameter(int eventType, int signalType, string name, string xml)
		{
			EventType = eventType;
			SignalType = signalType;
			Name = name;
			XML = xml;
		}
	}

	public struct Line
	{
		public Point P1, P2;

		public Line(Point p1, Point p2)
		{
			P1 = p1;
			P2 = p2;
		}
	}

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
