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
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace rewpa
{
	public class FeaturesFile
	{
		private FeaturesSetting setting;
		private List<FeaturesFeature> features;

		public FeaturesFile(Stream stream, string settingName)
		{
			features = new List<FeaturesFeature>();

			this.LoadFromCompiled(stream, settingName);
		}

		public static uint GetStringHash(string str)
		{
			int s = 5381;
			foreach (char ch in str) s = s * 33 + (int)ch;
			return (uint)s;
		}

		private void LoadFromCompiled(Stream stream, string settingName)
		{
			var buffer = new byte[0x100];
			var num = 0;

			if (stream.Read(buffer, 0, 2) != 2)
				throw new EndOfStreamException();

			var settings = new List<FeaturesSetting>();

			var settingCount = BitConverter.ToUInt16(buffer, 0);
			for (int i = 0; i < settingCount; i++)
			{
				var fSetting = new FeaturesSetting();

				if (stream.Read(buffer, 0, 2) != 2)
					throw new EndOfStreamException();

				num = BitConverter.ToUInt16(buffer, 0);
				if ((num <= 0) || (num > 0x100))
					throw new NotSupportedException();

				if (stream.Read(buffer, 0, num) != num)
					throw new EndOfStreamException();

				for (int k = 0; k < num; k++)
					buffer[k] = (byte)(buffer[k] ^ 0x80);

				fSetting.Name = Encoding.UTF8.GetString(buffer, 0, num);

				if (stream.Read(buffer, 0, 2) != 2)
					throw new EndOfStreamException();

				num = BitConverter.ToUInt16(buffer, 0);
				if (num <= 0 || num > 0x100)
					throw new NotSupportedException();

				if (stream.Read(buffer, 0, num) != num)
					throw new EndOfStreamException();

				for (int m = 0; m < num; m++)
					buffer[m] = (byte)(buffer[m] ^ 0x80);

				fSetting.Locale = Encoding.UTF8.GetString(buffer, 0, num);

				if (stream.Read(buffer, 0, 3) != 3)
					throw new EndOfStreamException();

				fSetting.Generation = buffer[0];
				fSetting.Season = buffer[1];
				fSetting.Subseason = (byte)(buffer[2] >> 2);
				fSetting.Test = (buffer[2] & 1) != 0;
				fSetting.Development = (buffer[2] & 2) != 0;

				settings.Add(fSetting);
			}

			setting = settings.FirstOrDefault(a => a.Name == settingName);
			if (setting == null)
				throw new ArgumentException("Unknown setting '" + settingName + "'.");

			if (stream.Read(buffer, 0, 2) != 2)
				throw new EndOfStreamException();

			var featureCount = BitConverter.ToUInt16(buffer, 0);
			for (int j = 0; j < featureCount; j++)
			{
				var fFeature = new FeaturesFeature();

				if (stream.Read(buffer, 0, 4) != 4)
					throw new EndOfStreamException();

				fFeature.Hash = BitConverter.ToUInt32(buffer, 0);

				if (stream.Read(buffer, 0, 2) != 2)
					throw new EndOfStreamException();

				num = BitConverter.ToUInt16(buffer, 0);
				if (num > 0x100)
					throw new NotSupportedException();

				if (num > 0)
				{
					if (stream.Read(buffer, 0, num) != num)
						throw new EndOfStreamException();

					for (int n = 0; n < num; n++)
						buffer[n] = (byte)(buffer[n] ^ 0x80);

					fFeature.Default = Encoding.UTF8.GetString(buffer, 0, num);
				}

				if (stream.Read(buffer, 0, 2) != 2)
					throw new EndOfStreamException();

				num = BitConverter.ToUInt16(buffer, 0);
				if (num > 0x100)
					throw new NotSupportedException();

				if (num > 0)
				{
					if (stream.Read(buffer, 0, num) != num)
						throw new EndOfStreamException();

					for (int num9 = 0; num9 < num; num9++)
						buffer[num9] = (byte)(buffer[num9] ^ 0x80);

					fFeature.Enable = Encoding.UTF8.GetString(buffer, 0, num);
				}

				if (stream.Read(buffer, 0, 2) != 2)
					throw new EndOfStreamException();

				num = BitConverter.ToUInt16(buffer, 0);
				if (num > 0x100)
					throw new NotSupportedException();

				if (num > 0)
				{
					if (stream.Read(buffer, 0, num) != num)
						throw new EndOfStreamException();

					for (int num10 = 0; num10 < num; num10++)
						buffer[num10] = (byte)(buffer[num10] ^ 0x80);

					fFeature.Disable = Encoding.UTF8.GetString(buffer, 0, num);
				}

				features.Add(fFeature);
			}
		}

		public bool IsEnabled(string featureName)
		{
			if (string.IsNullOrWhiteSpace(featureName))
				return true;

			var result = false;
			var negate = featureName.StartsWith("-");
			featureName = featureName.Trim('-');

			if (Regex.IsMatch(featureName, @"^[0-9]+$"))
			{
				result = (setting.GS >= Convert.ToInt32(featureName));
			}
			else
			{
				var feature = features.FirstOrDefault(a => a.Hash == GetStringHash(featureName));
				if (feature != null)
				{

				}
			}

			return (negate ? !result : result);
		}
	}

	public class FeaturesSetting
	{
		public bool Development;
		public byte Generation;
		public string Locale = "";
		public string Name = "";
		public byte Season;
		public byte Subseason;
		public bool Test;

		public int GS { get { return Generation * 100 + Season; } }
	}

	public class FeaturesFeature
	{
		public string Default = "";
		public string Disable = "";
		public string Enable = "";
		public uint Hash;

		public bool Enabled;

		public void ParseWith(FeaturesSetting setting)
		{
			if (!string.IsNullOrWhiteSpace(Default))
				Enabled = (setting.GS >= GetGS(Default));

			if (!string.IsNullOrWhiteSpace(Enable))
			{
				var match = Regex.Match(Enable, @"G[^,@]*@" + setting.Locale);
				if (match.Success)
					Enabled = (setting.GS >= GetGS(match.Groups[1].Value));
			}

			if (!string.IsNullOrWhiteSpace(Disable))
			{
				var match = Regex.Match(Disable, @"G[^,@]*@" + setting.Locale);
				if (match.Success)
					Enabled = (setting.GS < GetGS(match.Groups[1].Value));
			}
		}

		private int GetGS(string str)
		{
			var match = Regex.Match(str, @"G(?<g>[0-9]+)S(?<s>[0-9]+)");
			if (!match.Success)
				throw new ArgumentException("Invalid format.");

			return Convert.ToInt32(match.Groups["g"].Value) * 100 + Convert.ToInt32(match.Groups["s"].Value);
		}
	}
}
