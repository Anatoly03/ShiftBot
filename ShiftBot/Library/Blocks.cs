using System;
using System.Linq;
using System.Threading.Tasks;
using EEUniverse.Library;

namespace ShiftBot
{
    /*
     * Blocks
     */

    ///<summary>
    /// A block.
    ///</summary>
    public class Block // <- let me fix a bit the conventions
    {
        public int Id { get; set; }
        public Block(int i)
        {
            Id = i;
        }

        public virtual async Task Place(int l, int x, int y)
        {
            if (x >= 0 && y >= 0 && x < Program.Width && y < Program.Height)
                if (Program.World[l, x, y].Id != Id)
                    await Program.Con.SendAsync(MessageType.PlaceBlock, l, x, y, Id);
        }
    }

    public class Effect : Block
    {
        public int Value { get; set; }

        public Effect(int i, int v) : base(i)
        {
            Value = v;
        }

        public override async Task Place(int l, int x, int y)
        {
            if (x >= 0 && y >= 0 && x < Program.Width && y < Program.Height)
                if (Program.World[l, x, y].Id != Id)
                    await Program.Con.SendAsync(MessageType.PlaceBlock, l, x, y, Id, Value);
        }
    }

    public class Portal : Block
    {
        public int Rotation { get; set; }
        public int PortalId { get; set; }
        public int TargetId { get; set; }
        public bool Flip { get; set; }

        public Portal(int i, int r, int pid, int tid, bool f) : base(i)
        {
            Rotation = r;
            PortalId = pid;
            TargetId = tid;
            Flip = f;
        }

        public override async Task Place(int l, int x, int y)
        {
            if (x >= 0 && y >= 0 && x < Program.Width && y < Program.Height)
                if (Program.World[l, x, y].Id != Id)
                    await Program.Con.SendAsync(MessageType.PlaceBlock, l, x, y, Id, Rotation, PortalId, TargetId, Flip);
        }
    }

    public class Sign : Block
    {
        public string Text { get; set; }
        public int Morph { get; set; }

        public Sign(int i, string t, int m) : base(i)
        {
            Text = t;
            Morph = m;
        }

        public override async Task Place(int l, int x, int y)
        {
            if (x >= 0 && y >= 0 && x < Program.Width && y < Program.Height)
                if (Program.World[l, x, y].Id != Id)
                    await Program.Con.SendAsync(MessageType.PlaceBlock, l, x, y, Id, Text, Morph);
        }
    }

    /*public class Switch : Block
    {
        publics??

        public Switch(what do we need?) : base(i)
        {
            init publics
        }

        public override async Task Place(int l, int x, int y)
        {
            if (x >= 0 && y >= 0 && x < Program.Width && y < Program.Height)
                if (Program.World[l, x, y].Id != Id)
                    await Program.Con.SendAsync(MessageType.PlaceBlock, l, x, y, Id, publics);
        }
    }*/

    /*
     *  Coordinates
     */

    public class Coordinate
    {
        public int X { get; set; }
        public int Y { get; set; }

        public Coordinate(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    // Layer Coordinate
    public class LCoordinate : Coordinate
    {
        public int L { get; set; }

        public LCoordinate(int l, int x, int y) : base (x, y)
        {
            L = l;
        }

        public static implicit operator int[](LCoordinate c)
        {
            return new int[] { c.L, c.X, c.Y };
        }
    }
}