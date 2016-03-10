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
using rewpa.Properties;
using System;
using System.IO;
using System.Linq;

namespace rewpa
{
	class Program
	{
		static void Main(string[] args)
		{
			var wPath = Settings.Default.World;
			var path = "";
			var pack = (PackReader)null;

			Console.WriteLine("rewpa");
			Console.WriteLine();
			Console.WriteLine("Hi there, {0}! Would you be so kind to give me", Environment.UserName);
			Console.WriteLine("the path to the package folder you'd like to use?");

			while (true)
			{
				Console.Write("[{0}]: ", Settings.Default.World);
				path = Console.ReadLine();
				if (!string.IsNullOrWhiteSpace(path))
					wPath = path;

				wPath = wPath.Trim().Trim('"');

				if (Directory.EnumerateFiles(wPath, "*_full.pack", SearchOption.TopDirectoryOnly).Count() == 0)
				{
					Console.WriteLine("No... that doesn't seem to be correct, I can't find a '_full.pack' file.");
					Console.WriteLine("Try again, please.");
					continue;
				}

				pack = new PackReader(wPath);
				if (!pack.Exists(@"world\world.trn"))
				{
					Console.WriteLine("I couldn't find the 'world.trn' file in those packages,");
					Console.WriteLine("please give me the path to a Mabinogi package folder.");
					Console.WriteLine("Try again, please.");
					continue;
				}

				break;
			}

			Console.WriteLine();
			Console.WriteLine("Let's take a look, give me just a minute.");

			var world = new World(pack);

			Settings.Default.World = wPath;

			Console.WriteLine("I've found a world consisting of {0} regions.", world.Regions.Count);

			string method = "0";
			while (method == "0")
			{
				Console.WriteLine("What would you like to export?");
				Console.WriteLine("  1) Region data (regioninfo.dat)");
				Console.WriteLine("  2) Spawn information as .txt");
				Console.WriteLine("  3) Region list (regions.txt)");
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
}
