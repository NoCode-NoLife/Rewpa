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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using rewpa.Properties;
using System.Text.RegularExpressions;

namespace rewpa
{
	class Program
	{
		static void Main(string[] args)
		{
			var wPath = Settings.Default.World;
			var path = "";

			Console.WriteLine("rewpa");
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

			Settings.Default.World = wPath;

			Console.WriteLine("I've found a world consisting of {0} regions.", world.Regions.Count);

			string method = "0";
			while (method == "0")
			{
				Console.WriteLine("What would you like to export?");
				Console.WriteLine("  1) World as .dat");
				Console.WriteLine("  2) Spawn information as .txt");
				Console.WriteLine("  3) Region list as .txt");
				Console.Write(": ");

				method = Console.ReadLine();
			}

			Console.WriteLine("Where should I save my results?");

			switch (method)
			{
				case "1":
					{
						path = GetOutputPath(Settings.Default.SaveWorldPath, "regioninfo.dat");
						world.ExportWorldAsDat(path);
						Settings.Default.SaveWorldPath = path;
						break;
					}
				case "2":
					{
						path = GetOutputPath(Settings.Default.SaveSpawnPath, "creaturespawns.txt");
						world.ExportSpawnsAsTxt(path);
						Settings.Default.SaveSpawnPath = path;
						break;
					}
				case "3":
					{
						path = GetOutputPath(Settings.Default.SaveRegionsPath, "regions.txt");
						world.ExportRegionsAsTxt(path);
						Settings.Default.SaveRegionsPath = path;
						break;
					}
			}

			Settings.Default.Save();

			Console.WriteLine();
			Console.WriteLine("I'm done. You can close me now.");
			Console.WriteLine("I've written the result to '{0}'.", Path.GetFullPath(path));
			Console.ReadLine();
		}

		static string GetOutputPath(string defaultPath, string defaultFileName)
		{
			var path = "";

			Console.Write("[{0}]: ", defaultPath);
			path = Console.ReadLine();
			if (string.IsNullOrWhiteSpace(path))
				path = defaultPath;

			if (Directory.Exists(path))
				path = Path.Combine(path, defaultFileName);

			Console.WriteLine("Very well.");

			return path;
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
			sb.AppendLine("// ");
			sb.AppendLine("// Structure:");
			sb.AppendLine("// Id  Name");
			sb.AppendLine("//---------------------------------------------------------------------------");
			sb.AppendLine();

			foreach (var region in Regions.OrderBy(a => a.RegionId))
			{
				sb.AppendFormat("{0}, \"{2}\"", region.RegionId, region.Name, region.ClientName);
				sb.AppendLine();
			}

			File.WriteAllText(filepath, sb.ToString());
		}
	}

	public class Region
	{
		public int Version { get; set; }
		public int RegionId { get; set; }
		public int GroupId { get; set; }
		public string Name { get; set; }
		public string ClientName { get; set; }
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
				ClientName = br.ReadUnicodeString();
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

			Name = ClientName;
			//Name = Name.Replace("Tin_Beginner_Tutorial", "tir_beginner");
			//Name = Name.Replace("Uladh_Cobh_to_Belfast", "cobh_to_belfast");
			//Name = Name.Replace("Uladh_Belfast_to_Cobh", "belfast_to_cobh");
			//Name = Name.Replace("Cobh_to_Belfast", "cobh_to_belfast_ocean");
			//Name = Name.Replace("Belfast_to_Cobh", "belfast_to_cobh_ocean");
			//Name = Name.Replace("MonsterRegion", "monster_region");
			//Name = Name.Replace("Uladh_main", "tir");
			//Name = Name.Replace("Uladh_TirCho_", "tir_");
			//Name = Name.Replace("Uladh_Dunbarton", "dunbarton");
			//Name = Name.Replace("Uladh_Dun_to_Tircho", "dugald_aisle");
			//Name = Name.Replace("Ula_Tirnanog", "tnn");
			//Name = Name.Replace("Ula_DgnHall_Dunbarton_before1", "rabbie_altar");
			//Name = Name.Replace("Ula_DgnHall_Dunbarton_before2", "math_altar");
			//Name = Name.Replace("MiscShop", "general");
			//Name = Name.Replace("tir_ChiefHouse", "tir_duncan");
			//Name = Name.Replace("Uladh_Dungeon_Black_Wolfs_Hall1", "ciar_altar");
			//Name = Name.Replace("Uladh_Dungeon_Black_Wolfs_Hall2", "ciar_entrance");
			//Name = Name.Replace("Uladh_Dungeon_Beginners_Hall1", "alby_altar");
			//Name = Name.Replace("Uladh_Cobh_harbor", "cobh");
			//Name = Name.Replace("Ula_DgnHall_Dunbarton_after", "rabbie_entrance");
			//Name = Name.Replace("Ula_hardmode_DgnHall_TirChonaill_before", "alby_hard_altar");
			//Name = Name.Replace("Ula_DgnArena_Tircho_Lobby", "alby_arena_lobby");
			//Name = Name.Replace("Ula_DgnArena_Tircho_Arena", "alby_arena");
			//Name = Name.Replace("Ula_Dun_to_Bangor", "gairech");
			//Name = Name.Replace("Ula_Bangor", "bangor");
			//Name = Name.Replace("Ula_DgnHall_Bangor_before1", "barri_altar");
			//Name = Name.Replace("Ula_DgnHall_Bangor_before2", "barri_entrance_test");
			//Name = Name.Replace("Ula_DgnHall_Bangor_after", "barri_entrance");
			//Name = Name.Replace("tnn_ChiefHouse", "tnn_duncan");
			//Name = Name.Replace("Ula_DgnHall_Tirnanog_before1", "albey_altar");
			//Name = Name.Replace("Ula_DgnHall_Tirnanog_before2", "albey_altar_test");
			//Name = Name.Replace("Ula_DgnHall_Tirnanog_after", "albey_altar_entrance");
			//Name = Name.Replace("Sidhe_Sneachta_S", "sidhe_north");
			//Name = Name.Replace("Sidhe_Sneachta_N", "sidhe_south");
			//Name = Name.Replace("Ula_DgnHall_Danu_before", "fiodh_altar");
			//Name = Name.Replace("Ula_DgnHall_Danu_after", "fiodh_entrance");
			//Name = Name.Replace("Ula_", "");
			//Name = Name.Replace("Emainmacha", "emain_macha");
			//Name = Name.Replace("DgnHall_Coill_before", "coill_altar");
			//Name = Name.Replace("DgnHall_Coill_after", "coill_entrance");
			//Name = Name.Replace("emain_macha_Ceo", "ceo");
			//Name = Name.Replace("DgnHall_Runda_before", "rundal_altar");
			//Name = Name.Replace("DgnHall_Runda_after", "rundal_entrance");
			//Name = Name.Replace("emain_macha_OidTobar_Hall", "ceo_cellar");
			//Name = Name.Replace("Studio_Runda", "studio_rundal_boss");
			//Name = Name.Replace("dunbarton_SchoolHall_before", "dunbarton_school_altar");
			//Name = Name.Replace("dunbarton_School_LectureRoom", "dunbarton_school_library");
			//Name = Name.Replace("Dgnhall_Peaca_before", "peaca_altar");
			//Name = Name.Replace("Dgnhall_Peaca_after", "peaca_entrance");
			//Name = Name.Replace("DgnHall_Tirnanog_G3_before", "baol_altar");
			//Name = Name.Replace("DgnHall_Tirnanog_G3_after", "baol_entrance");
			//Name = Name.Replace("Private_Wedding_waitingroom", "emain_macha_wedding_waiting");
			//Name = Name.Replace("Private_Wedding_ceremonialhall", "emain_macha_wedding_ceremony");
			//Name = Name.Replace("Dugald_Aisle_UserHouse", "dugald_userhouse");
			//Name = Name.Replace("tnn_G3_Gairech_Hill", "tnn_gairech");
			//Name = Name.Replace("Dugald_Aisle_UserCastleTest1", "user_castle_test_1");
			//Name = Name.Replace("Dugald_Aisle_UserCastleTest2", "user_castle_test_2");
			//Name = Name.Replace("tnn_G3", "tnn_bangor");
			//Name = Regex.Replace(Name, "_TestRegion([0-9]+)", "test_region_$1");
			//Name = Regex.Replace(Name, "dugald_userhouse_int_([0-9]+)", "user_house_int_$1");
			//Name = Name.Replace("Dugald_Aisle_UserCastle_", "user_castle_");
			//Name = Name.Replace("Dugald_Aisle_ModelHouse", "model_house");
			//Name = Name.Replace("DgnArena_Dunbarton_Arena", "rabbie_battle_arena");
			//Name = Name.Replace("DgnArena_Dunbarton_Lobby", "rabbie_battle_arena_lobby");
			//Name = Name.Replace("DgnArena_Dunbarton_waitingroom", "rabbie_battle_arena_waiting");
			//Name = Name.Replace("Iria_Harbor_01", "iria_harbor");
			//Name = Name.Replace("Iria_SW_ruins_DG_before", "rano_ruins_altar");
			//Name = Name.Replace("Iria_SW_ruins_DG_after", "rano_ruins_entrance");
			//Name = Name.Replace("ArenaTest0", "arena_test_0");
			//Name = Name.Replace("Loginstage_0", "login_stage_0");
			//Name = Name.Replace("Iria_NN_dragoncave01", "renes");
			//Name = Name.Replace("hardmode_DgnHall_TirChonaill_after", "alby_hard_entrance");
			//Name = Name.Replace("hardmode_DgnHall_Ciar_before", "ciar_hard_altar");
			//Name = Name.Replace("hardmode_DgnHall_Ciar_after", "ciar_hard_entrance");
			//Name = Name.Replace("hardmode_rundal_altar", "rundal_hard_altar");
			//Name = Name.Replace("hardmode_rundal_entrance", "rundal_hard_entrance");
			//Name = Name.Replace("Uladh_Cobh", "cobh");
			//Name = Name.Replace("Dunbarton_LectureRoom", "dunbarton_school_library");
			//Name = Name.Replace("OidTobar_Hall", "ceo_cellar");
			//Name = Name.Replace("Dugald_Aisle_Town", "dugald_residential");
			//Name = Name.Replace("_Keep", "_castle_entrance");
			//Name = Name.Replace("Dugald_Aisle", "dugald");
			//Name = Name.Replace("keep_DgnHall_after", "dungeon_altar");
			//Name = Name.Replace("keep_DgnHall_before", "dungeon_entrance");
			//Name = Name.Replace("Studio_keep_DG", "studio_residential");
			//Name = Name.Replace("Housing_CharDummyStage", "housing_dummy");
			//Name = Name.Replace("Private_giant_Wedding_ceremonialhall", "vales_wedding_ceremony");
			//Name = Name.Replace("Private_giant_Wedding_waitingroom", "vales_wedding_waiting");
			//Name = Name.Replace("Private_Promotion_testRoom_waiting", "advancement_test_waiting");
			//Name = Name.Replace("Private_Promotion_testRoom", "advancement_test");
			//Name = Regex.Replace(Name, "_town$", "_residential");
			//Name = Name.Replace("Soulstream", "soul_stream");
			//Name = Name.Replace("soul_stream_region", "soul_stream_battle");
			//Name = Name.Replace("taillteann_main_field", "taillteann");
			//Name = Name.Replace("Taillteann_E_field", "sliab_cuilin");
			//Name = Name.Replace("Taillteann_SE_field", "abb_neagh");
			//Name = Name.Replace("Tara_N_Field", "comb_valley");
			//Name = Name.Replace("Tara_main_field", "tara");
			//Name = Name.Replace("Tara_SE_Field", "blago_prairie");
			//Name = Name.Replace("Tara_tournament_field", "tara_jousting");
			//Name = Name.Replace("Tara_cloth", "tara_clothing");
			//Name = Regex.Replace(Name, "_misc$", "_general");
			//Name = Name.Replace("Falias_main_field", "falias");
			//Name = Name.Replace("Avon_main_field", "avon");
			//Name = Name.Replace("JP_Nekojima_islet", "nekojima");
			//Name = Name.Replace("JP_Nekojima_dungeon_hall_after", "nekojima_dungeon_entrance");
			//Name = Name.Replace("JP_Nekojima", "nekojima");
			//Name = Name.Replace("TirnanogDG", "Tirnanog_DG");
			//Name = Name.Replace("Tirnanog", "tnn");
			//Name = Name.Replace("Nao_tutorial", "soul_stream");
			//Name = Name.Replace("G1_GoddessStage", "morrighan");
			//Name = Name.Replace("Event_moonsurface", "event_moon");
			//Name = Name.Replace("pvp_event", "event_pvp");
			//Name = Name.Replace("Event", "event");
			//Name = Name.Replace("event_impdream", "event_imp_dream");
			//Name = Name.Replace("Iria_SW_main_field", "rano");
			//Name = Name.Replace("Iria_Uladh_Ocean_fishingboat_float", "rano_fishing_boat");
			//Name = Name.Replace("Iria_to_fishingboat", "rano_to_fishingboat");
			//Name = Name.Replace("fishingboat_to_Iria", "fishingboat_to_rano");
			//Name = Name.Replace("Iria_SE_main_field", "connous");
			//Name = Name.Replace("Iria_SE_Desert_underground", "ant_tunnel");
			//Name = Name.Replace("Iria_SE", "filia");
			//Name = Name.Replace("Iria_NW", "physis");
			//Name = Name.Replace("Iria_SW", "rano");
			//Name = Name.Replace("MineField", "mine_field");
			//Name = Name.Replace("monsterraid01", "monster_raid");
			//Name = Name.Replace("ElfArena", "arena");
			//Name = Name.Replace("Iria_Elf", "elf");
			//Name = Name.Replace("physis_main_field", "physis");
			//Name = Name.Replace("physis_tunnel_S", "physis_tunnel_south");
			//Name = Name.Replace("physis_tunnel_N", "physis_tunnel_north");
			//Name = Name.Replace("physis_tunnel_Outside", "solea");
			//Name = Name.Replace("physis_Tutorial", "giant_tutorial");
			//Name = Name.Replace("Studio", "studio");
			//Name = Name.Replace("_mineB", "_mine_B");
			//Name = Name.Replace("Iria_C", "courcle");
			//Name = Name.Replace("Iria_NN", "zardine");
			//Name = Name.Replace("_main_field", "");
			//Name = Name.Replace("Belfast_human", "belfast");
			//Name = Name.Replace("Qwest", "quest");
			//Name = Name.Replace("Belfast_Skatha", "scathach");
			//Name = Name.Replace("physis_glacier01_DG", "par");
			//Name = Name.Replace("par_after", "par_altar");
			//Name = Name.Replace("par_before", "par_entrance");
			//Name = Name.Replace("Test01", "test_01");
			//Name = Name.Replace("Test02", "test_02");
			//Name = Name.Replace("Tara_keep_RG", "tara_castle");
			//Name = Name.Replace("Tara_town_RG", "tara_residential");
			//Name = Name.Replace("_TestRegion", "gm_island");
			//Name = Regex.Replace(Name, "_Cloth$", "_clothing");
			//Name = Name.Replace("filia_Desert_01_DG_after", "longa_altar");
			//Name = Name.Replace("filia_Desert_01_DG_before", "longa_entrance");
			//Name = Name.Replace("Private_igloo_01", "igloo");
			//Name = Name.Replace("BlockRegion", "block_region");
			//Name = Name.Replace("soul_stream_past_region", "soul_stream_past");
			//Name = Name.Replace("soul_stream_future_region", "soul_stream_future");
			//Name = Name.Replace("RE_Nekojima_islet", "doki_doki_island");
			//Name = Name.Replace("DramaIriaS2", "drama_iria_s2");

			//Name = Name.ToLower();
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

			return prop;
		}
	}

	public class Prop
	{
		public int ClassId { get; set; }
		public long PropId { get; set; }
		public string Name { get; set; }
		public float X { get; set; }
		public float Y { get; set; }
		public List<Shape> Shapes { get; set; }
		public bool Solid { get; set; }
		public float Scale { get; set; }
		public float Direction { get; set; }
		public List<PropParameter> Parameters { get; set; }

		public Prop()
		{
			Shapes = new List<Shape>();
			Parameters = new List<PropParameter>();
		}
	}

	public class Event
	{
		public long EventId { get; set; }
		public string Name { get; set; }
		public float X { get; set; }
		public float Y { get; set; }
		public List<Shape> Shapes { get; set; }
		public int EventType { get; set; }
		public List<PropParameter> Parameters { get; set; }

		public Event()
		{
			Shapes = new List<Shape>();
			Parameters = new List<PropParameter>();
		}
	}

	public class Shape
	{
		public float DirX1 { get; set; }
		public float DirX2 { get; set; }
		public float DirY1 { get; set; }
		public float DirY2 { get; set; }
		public float LenX { get; set; }
		public float LenY { get; set; }
		public float PosX { get; set; }
		public float PosY { get; set; }

		public Point[] GetPoints()
		{
			var points = new Point[4];

			double a00 = this.DirX1 * this.LenX;
			double a01 = this.DirX2 * this.LenX;
			double a02 = this.DirY1 * this.LenY;
			double a03 = this.DirY2 * this.LenY;

			double sx1 = this.PosX - a00 - a02; if (sx1 < this.PosX) sx1 = Math.Ceiling(sx1);
			double sy1 = this.PosY - a01 - a03; if (sy1 < this.PosY) sy1 = Math.Ceiling(sy1);
			double sx2 = this.PosX + a00 - a02; if (sx2 < this.PosX) sx2 = Math.Ceiling(sx2);
			double sy2 = this.PosY + a01 - a03; if (sy2 < this.PosY) sy2 = Math.Ceiling(sy2);
			double sx3 = this.PosX + a00 + a02; if (sx3 < this.PosX) sx3 = Math.Ceiling(sx3);
			double sy3 = this.PosY + a01 + a03; if (sy3 < this.PosY) sy3 = Math.Ceiling(sy3);
			double sx4 = this.PosX - a00 + a02; if (sx4 < this.PosX) sx4 = Math.Ceiling(sx4);
			double sy4 = this.PosY - a01 + a03; if (sy4 < this.PosY) sy4 = Math.Ceiling(sy4);

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

			return points;
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
