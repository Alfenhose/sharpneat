using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpNeat.Domains.Spelunky
{
    class BFSNode : IComparable, IComparable<BFSNode>
    {
        public int X { get {return Pos.X; } }
        public int Y { get { return Pos.Y; } }
        public MapKey Pos { get; set; }
        public double GScore { get; set; }
        public double FScore { get; set; }
        public bool Solid { get; set; }
        public BFSNode Previous { get; set; }
        public BFSNode(MapKey _pos, double _gscore = double.PositiveInfinity, double _fscore = double.PositiveInfinity, bool _solid = false, BFSNode _prev = null)
        {
            Pos = _pos;
            GScore = _gscore;
            FScore = _fscore;
            Solid = _solid;
            Previous = _prev;
        }
        public override int GetHashCode()
        {
            return (X % (1 << 16)) + (((Y % (1 << 16)) << 16));
        }
        public override bool Equals(object obj)
        {
            BFSNode other = obj as BFSNode;
            if (other == null) return false;
            return (X == other.X && Y == other.Y);
        }
        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }
            BFSNode other = obj as BFSNode;
            if (other == null)
            {
                throw new ArgumentException("A BFSNode object is required for comparison.", "obj");
            }
            return this.CompareTo(other);
        }

        public int CompareTo(BFSNode other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return 1;
            }
            return (Math.Sign(FScore - other.FScore)==-1)?-1:1;
        }
        public static int Compare(BFSNode left, BFSNode right)
        {
            if (object.ReferenceEquals(left, right))
            {
                return 0;
            }
            if (object.ReferenceEquals(left, null))
            {
                return -1;
            }
            return left.CompareTo(right);
        }

        public static bool operator ==(BFSNode left, BFSNode right)
        {
            if (object.ReferenceEquals(left, null))
            {
                return object.ReferenceEquals(right, null);
            }
            return left.Equals(right);
        }
        public static bool operator !=(BFSNode left, BFSNode right)
        {
            return !(left == right);
        }
        public static bool operator <(BFSNode left, BFSNode right)
        {
            return (Compare(left, right) < 0);
        }
        public static bool operator >(BFSNode left, BFSNode right)
        {
            return (Compare(left, right) > 0);
        }
    }
}
