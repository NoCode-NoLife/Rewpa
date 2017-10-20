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
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace rewpa
{
	public class PropClass
	{
		public int ClassID { get; private set; }
		public string ClassName { get; private set; }
		public string StringID { get; private set; }
		public bool UsedServer { get; private set; }
		public Dictionary<string, string> ExtraXML { get; private set; }

		public PropClass()
		{
			ExtraXML = new Dictionary<string, string>();
		}

		public string GetExtra(string name)
		{
			string result;
			ExtraXML.TryGetValue(name, out result);
			return result;
		}

		public static Dictionary<int, PropClass> ReadFromXml(Stream stream)
		{
			var result = new Dictionary<int, PropClass>();

			using (var reader = XmlReader.Create(stream))
			{
				while (reader.ReadToFollowing("PropClass"))
				{
					var data = new PropClass();

					data.ClassID = Convert.ToInt32(reader.GetAttribute("ClassID"));
					data.ClassName = reader.GetAttribute("ClassName");
					data.StringID = reader.GetAttribute("StringID") ?? "";
					data.UsedServer = (reader.GetAttribute("UsedServer") != null && reader.GetAttribute("UsedServer").ToLower() == "true");

					var extraXml = reader.GetAttribute("ExtraXML");
					if (!string.IsNullOrWhiteSpace(extraXml))
					{
						if (!extraXml.Trim().EndsWith("/>") && !extraXml.Trim().EndsWith("</xml>"))
							extraXml = extraXml.Trim('>') + "/>";

						if (extraXml.Contains("sit_motion=\"98\"hideidle=\""))
							extraXml = extraXml.Replace("sit_motion=\"98\"hideidle=\"", "sit_motion=\"98\" hideidle=\"");
						if (extraXml.Contains("sit_motion=\"89\"sit_motion2=\"90\""))
							extraXml = extraXml.Replace("sit_motion=\"89\"sit_motion2=\"90\"", "sit_motion=\"89\" sit_motion2=\"90\"");
						if (extraXml.Contains("sit_motion = \"27\" sit_motion_category=\"2\" sit_motion=\"102\""))
							extraXml = extraXml.Replace("sit_motion = \"27\" sit_motion_category=\"2\" sit_motion=\"102\"", "sit_motion = \"27\" sit_motion_category=\"2\"");
						if (extraXml.Contains("\"101\"hideidle=\"false\""))
							extraXml = extraXml.Replace("\"101\"hideidle=\"false\"", "\"101\" hideidle=\"false\"");

						var xel = XElement.Parse(extraXml);
						foreach (var attr in xel.Attributes())
							data.ExtraXML[attr.Name.ToString()] = attr.Value;
					}

					result[data.ClassID] = data;
				}
			}

			return result;
		}
	}
}
