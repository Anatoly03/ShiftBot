using System;
using System.Collections.Generic;
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
        public bool Inited;
        public List<string> Voters;

        public MapVote(int id, int x, int y)
        {
            Id = id;
            X = x;
            Y = y;
            Inited = false;
        }

        public async Task SetMap(MapInfo m)
        {
            MapId = m.Id;
            Inited = true;
            Voters = new List<string> { };
            await Program.PlaceSign(X, Y, 55, $"{m.Title}\nBy {m.Creator}", 1);

            int bid = 20;
            if (m.Difficulty == 0) bid = 25;
            if (m.Difficulty == 1) bid = 24;
            if (m.Difficulty == 2) bid = 23;
            if (m.Difficulty == 3) bid = 22;
            if (m.Difficulty == 4) bid = 21;
            await Program.PlaceBlock(1, X, Y + 1, bid);
        }

        public async Task Close()
        {
            Inited = false;
            MapInfo m = Program.Maps.FirstOrDefault(map => map.Id == MapId);
            await Program.PlaceSign(X, Y, 58, $"{m.Title}\nBy {m.Creator}\n\n[{Voters.Count} votes]\nVoting closed", 1);
        }

        /// <summary>
        /// Update all signs based on a voting request
        /// </summary>
        public async Task UpdateSigns()
        {
            foreach (MapVote mv in Program.MapVoteSigns)
            {
                MapInfo m = Program.Maps.FirstOrDefault(map => map.Id == mv.MapId);
                await Program.PlaceSign(mv.X, mv.Y, 55, $"{m.Title}\nBy {m.Creator}" + (mv.Voters.Count > 0 ? $"\n\n{mv.Voters.Count} Vote" + ((mv.Voters.Count > 1) ? "s" : "") : ""), 1);

                int bid = 20;
                if (m.Difficulty == 0) bid = 25;
                if (m.Difficulty == 1) bid = 24;
                if (m.Difficulty == 2) bid = 23;
                if (m.Difficulty == 3) bid = 22;
                if (m.Difficulty == 4) bid = 21;
                await Program.PlaceBlock(1, mv.X, mv.Y + 1, bid);
            }
        }

        public async Task NewVote(Player p)
        {
            if (p.Vote != -1)
                Program.MapVoteSigns.ElementAt(p.Vote).Voters.RemoveAll(player => player == p.Name);
            Voters.Add(p.Name);
            p.Vote = Id;
            await UpdateSigns();
        }
    }
}
