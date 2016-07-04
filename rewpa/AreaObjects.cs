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

namespace rewpa
{
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
		public string Title { get; set; }
		public string State { get; set; }
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
}
