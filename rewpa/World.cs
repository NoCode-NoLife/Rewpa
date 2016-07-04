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
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;

namespace rewpa
{
	public class World
	{
		public List<Region> Regions { get; set; }

		public World(PackReader pack)
		{
			this.Regions = new List<Region>();

			Dictionary<int, PropClass> propClasses;
			using (var ms = pack.GetEntry(@"db\propdb.xml").GetDataAsStream())
				propClasses = PropClass.ReadFromXml(ms);

			FeaturesFile features;
			using (var ms = pack.GetEntry(@"features.xml.compiled").GetDataAsStream())
				features = new FeaturesFile(ms, "Regular, USA");

			using (var ms = pack.GetEntry(@"world\world.trn").GetDataAsStream())
			using (var trnReader = XmlReader.Create(ms))
			{
				if (!trnReader.ReadToDescendant("regions"))
					return;

				using (var trnRegionsReader = trnReader.ReadSubtree())
				{
					var i = 1;
					while (trnRegionsReader.ReadToFollowing("region"))
					{
						var workDir = trnRegionsReader.GetAttribute("workdir");
						var fileName = trnReader.GetAttribute("name");

						Console.Write("\r".PadRight(70) + "\r");
						Console.Write("Reading {0}: {1}...", i++, fileName);

						var region = new Region(pack, workDir, fileName, propClasses, features);
						this.Regions.Add(region);
					}

					Console.WriteLine("\r".PadRight(70) + "\r");
				}
			}

			Regions = Regions.OrderBy(a => a.RegionId).ToList();
			Regions.ForEach(region =>
			{
				region.Areas = region.Areas.ToList();
				region.Areas.ForEach(area =>
				{
					area.Props = area.Props.OrderBy(prop => prop.PropId).ToList();
					area.Events = area.Events.OrderBy(ev => ev.EventId).ToList();
				});
			});
		}

		public void ExportWorldAsDat(string path)
		{
			//Console.WriteLine();

			using (var ms = new MemoryStream())
			using (var bw = new BinaryWriter(ms))
			{
				// Regions
				bw.Write(Regions.Count);
				Regions.ForEach(region =>
				{
					bw.Write(region.RegionId);
					bw.Write(region.Name);
					bw.Write(region.GroupId);

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
						bw.Write(area.Name);
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
							bw.Write(prop.Name);
							bw.Write(prop.X);
							bw.Write(prop.Y);
							bw.Write(prop.Direction);
							bw.Write(prop.Scale);
							bw.Write(prop.Title);
							bw.Write(prop.State);
							//bw.Write(prop.Solid);

							// Shape
							bw.Write(prop.Shapes.Count);
							prop.Shapes.ForEach(shape =>
							{
								bw.Write(shape.DirX1);
								bw.Write(shape.DirX2);
								bw.Write(shape.DirY1);
								bw.Write(shape.DirY2);
								bw.Write(shape.LenX);
								bw.Write(shape.LenY);
								bw.Write(shape.PosX);
								bw.Write(shape.PosY);
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
							bw.Write(ei.Name);
							bw.Write(ei.X);
							bw.Write(ei.Y);
							bw.Write(ei.EventType);

							// Shape
							bw.Write(ei.Shapes.Count);
							ei.Shapes.ForEach(shape =>
							{
								bw.Write(shape.DirX1);
								bw.Write(shape.DirX2);
								bw.Write(shape.DirY1);
								bw.Write(shape.DirY2);
								bw.Write(shape.LenX);
								bw.Write(shape.LenY);
								bw.Write(shape.PosX);
								bw.Write(shape.PosY);
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

		public void ExportSpawnsAsTxt(string filepath)
		{
			var sb = new StringBuilder();

			foreach (var region in Regions.OrderBy(a => a.RegionId))
			{
				foreach (var area in region.Areas)
				{
					foreach (var ev in area.Events.OrderBy(a => a.EventId))
					{
						foreach (var parameter in ev.Parameters.Where(a => a.XML.Contains("group")))
						{
							sb.AppendFormat("region: {0}, area: {1}, id: {2:X16}, type: {8}, pType: {3}, name: '{5}/{6}/{7}', xml: '{4}', coords: ",
								region.RegionId, area.AreaId, ev.EventId, parameter.EventType, parameter.XML, region.ClientName, area.Name, ev.Name, ev.EventType);

							foreach (var shape in ev.Shapes)
							{
								var points = shape.GetPoints();
								foreach (var point in points)
									sb.AppendFormat("{0}, ", point);
							}

							sb.Remove(sb.Length - 2, 2);
							sb.AppendLine();
						}
					}
				}
			}

			File.WriteAllText(filepath, sb.ToString());
		}

		public void ExportRegionsAsTxt(string filepath)
		{
			var sb = new StringBuilder();

			sb.AppendLine("// Aura");
			sb.AppendLine("// Database file");
			sb.AppendLine("//---------------------------------------------------------------------------");
			sb.AppendLine();

			sb.AppendLine("[");
			foreach (var region in Regions.OrderBy(a => a.RegionId))
			{
				sb.Append("{ ");
				sb.AppendFormat("id: {0}", region.RegionId);
				sb.AppendFormat(", name: \"{0}\"", region.ClientName);
				sb.AppendFormat(", indoor: {0}", (region.IndoorType == 100).ToString().ToLower());
				sb.AppendLine(" },");
			}
			sb.AppendLine("]");

			File.WriteAllText(filepath, sb.ToString());
		}
	}
}
