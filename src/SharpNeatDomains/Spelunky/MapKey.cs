using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNeat.Domains.Spelunky
{
    class MapKey
    {
        public int X { get; set; }
        public int Y { get; set; }
        public MapKey(int _x, int _y)
        {
            X = _x;
            Y = _y;
        }
        public static MapKey Create(IntPoint point)
        {
            return new MapKey(point._x, point._y);
        }
        public override int GetHashCode()
        {
            return (X % (1 << 16)) + (((Y % (1 << 16)) << 16));
        }
        public override bool Equals(object obj)
        {
            MapKey other = obj as MapKey;
            if (other == null) return false;
            return (X == other.X && Y == other.Y);
        }
    }
}
