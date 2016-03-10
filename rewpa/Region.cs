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
		public int AreaType { get; set; }
		public int IndoorType { get; set; }
		public string Camera { get; set; }
		public string Light { get; set; }
		public string XML { get; set; }

		public List<Area> Areas { get; set; }

		public Region(PackReader pack, string workDir, string fileName)
		{
			this.Areas = new List<Area>();

			var regionFilePath = Path.Combine("world", workDir, fileName + ".rgn");

			using (var ms = pack.GetEntry(regionFilePath).GetDataAsStream())
			using (var br = new BinaryReader(ms))
			{
				Version = br.ReadInt32();
				br.ReadInt32(); // Unk
				RegionId = br.ReadInt32();
				GroupId = br.ReadInt32();
				ClientName = br.ReadUnicodeString();
				CellSize = br.ReadInt32();
				Sight = br.ReadByte();
				var areaCount = br.ReadInt32();
				br.Skip(0x34); // Unk
				AreaType = br.ReadInt32();
				IndoorType = br.ReadInt32();
				br.Skip(0x4); // Unk
				Scene = br.ReadUnicodeString();
				br.Skip(0x2D); // Unk
				Camera = br.ReadUnicodeString();
				Light = br.ReadUnicodeString();
				br.Skip(0x0C); // Unk

				if (IndoorType != 100 && IndoorType != 200)
					throw new Exception("Unknown indoor type.");

				for (int i = 0; i < areaCount; ++i)
				{
					var areaName = br.ReadUnicodeString();
					var area = new Area(pack, workDir, areaName);
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
}
