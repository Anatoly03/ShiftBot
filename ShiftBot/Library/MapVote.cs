using System;
using System.Linq;
using System.Threading.Tasks;

namespace ShiftBot
{
    public class MapVote
    {
        public int Id;
        public int X;
        public int Y;
        public int MapId;
        public int Votes;
        public bool Inited;

        public MapVote(int id, int x, int y)
        {
            Id = id;
            X = x;
            Y = y;
            Inited = false;
        }

        public async Task SetMap(MapInfo m)
        {
            Votes = 0;
            MapId = m.Id;
            Inited = true;
            await Program.PlaceSign(X, Y, 55, $"{m.Title}\nBy {m.Creator}", 1);
        }

        public void Close()
        {
            Inited = false;
        }

        /// <summary>
        /// Update all signs based on a voting request
        /// </summary>
        public async Task UpdateSigns()
        {
            foreach (MapVote mv in Program.MapVoteSigns)
            {
                MapInfo m = Program.Maps.FirstOrDefault(map => map.Id == mv.MapId);
                await Program.PlaceSign(mv.X, mv.Y, 55, $"{m.Title}\nBy {m.Creator}" + (mv.Votes > 0 ? $"\n\n{mv.Votes} Vote" + (mv.Votes > 1 ? "s" : "") : ""), 1);
            }
        }

        public async Task NewVote(Player p)
        {
            if (p.Vote != -1)
                Program.MapVoteSigns.ElementAt(p.Vote).Votes--;
            Votes++;
            p.Vote = Id;
            await UpdateSigns();
        }
    }
}
