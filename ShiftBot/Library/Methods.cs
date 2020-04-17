using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EEUniverse.Library;
using Newtonsoft.Json;

namespace ShiftBot
{
    partial class Program
    {
        /*
         * General methods
         */

        public static async Task Place(LCoordinate lc, Block b)
        {
            await b.Place(lc.L, lc.X, lc.Y);
            World[lc.L, lc.X, lc.Y] = b;
        }

        public static async Task PlaceBlock(int l, int x, int y, int id)
        {
            if (World[l, x, y].Id != id)
            {
                await Con.SendAsync(MessageType.PlaceBlock, l, x, y, id);
                World[l, x, y] = new Block(id);
            }
        }

        public static async Task PlaceSign(int x, int y, int id, string text, int morph)
        {
            await Con.SendAsync(MessageType.PlaceBlock, 1, x, y, id, text, morph);
            World[1, x, y] = new Sign(id, text, morph);
        }

        public static async Task PlacePortal(int x, int y, int id, int morph, int _p_id, int _t_id, bool _flip)
        {
            await Con.SendAsync(MessageType.PlaceBlock, 1, x, y, id, morph, _p_id, _t_id, _flip);
            World[1, x, y] = new Portal(id, morph, _p_id, _t_id, _flip);
        }

        public static async Task Say(string message) => await Con.SendAsync(MessageType.Chat, "[Bot] " + message);
        public static async Task SayPrivate(string player, string msg) => await Con.SendAsync(MessageType.Chat, "/pm " + player + " [Bot] " + msg);
        public static async Task SayPrivate(Player player, string msg) => await Con.SendAsync(MessageType.Chat, "/pm " + player.Name + " [Bot] " + msg);

        /*
         * General methods
         */

        public static async Task ClearGameArea()
        {
            var buffer = new Dictionary<LCoordinate, Block>();

            for (int l = 0; l < 2; l++)
                for (int x = 30; x < 70; x++)
                    for (int y = 64; y < 88; y++)
                        if (l == 1 && y == 84 && x > 44 && x < 55)
                            buffer.Add(new LCoordinate(l, x, y), x > 49 ? new Block(15) : new Block(13));
                        else if (!(y > 84 && x > 45 && x < 54) && !(x == 69 && y == 86))
                            buffer.Add(new LCoordinate(l, x, y), new Block(0));

            await CloseSafeArea();
            int max = buffer.Count;
            while (buffer.Count > 0)
            {
                var pair = buffer.ElementAt(new Random().Next(0, buffer.Count));
                if (World[pair.Key.L, pair.Key.X, pair.Key.Y].Id != pair.Value.Id)
                {
                    Thread.Sleep(Math.Max((int)Math.Floor(50 / Math.Log2(max - buffer.Count + 3)), 0));
                    await PlaceBlock(pair.Key.L, pair.Key.X, pair.Key.Y, pair.Value.Id);
                }
                buffer.Remove(pair.Key);
            }

            Thread.Sleep(25);
            await PlaceBlock(1, 49, 86, 13);
            Thread.Sleep(25);
            await PlaceBlock(1, 47, 86, 13);
        }

        public static async Task SaveMap()
        {
            Block[,,] game = new Block[2, 38, 22];

            for (int l = 0; l < 2; l++)
                for (int x = 31; x < 69; x++)
                    for (int y = 64; y < 86; y++)
                        game[l, x - 31, y - 64] = World[l, x, y];

            int n = 0;
            foreach (MapInfo k in maps)
                n = Math.Max(n, k.Id);
            n++;

            Directory.CreateDirectory($"../../../levels/{n}");

            using (StreamWriter file = File.CreateText($"../../../levels/{n}/map.json"))
            {
                var serializer = JsonConvert.SerializeObject(game, Json_settings);
                file.WriteLine(serializer);
            }

            maps.Add(new MapInfo
            {
                Id = n,
                Title = "Untitled Map"
            });

            using (StreamWriter file = File.CreateText($"../../../levels/list.json"))
            {
                var serializer = JsonConvert.SerializeObject(maps, Json_settings);
                file.WriteLine(serializer);
            }

            await Say("Map saved!");
        }

        public static async Task BuildMap(int id)
        {
            await BuildMap(maps.FirstOrDefault(m => m.Id == id));
        }

        public static async Task BuildMap(MapInfo info)
        {
            var buffer = new Dictionary<LCoordinate, Block>();
            var map = JsonConvert.DeserializeObject<Block[,,]>(File.ReadAllText($"../../../levels/{info.Id}/map.json"), Json_settings);

            for (int l = 0; l < 2; l++)
                for (int x = 31; x < 69; x++)
                    for (int y = 64; y < 86; y++)
                        buffer.Add(new LCoordinate(l, x, y), map[l, x - 31, y - 64]);

            for (int x = 30; x < 70; x++)
                buffer.Add(new LCoordinate(1, x, 87), new Block(12));

            await PlaceSign(49, 88, 58, $"{info.Title}\nBy {info.Creator}", 1);

            while (buffer.Count > 0)
            {

                var pair = buffer.ElementAt(new Random().Next(0, buffer.Count));
                if (World[pair.Key.L, pair.Key.X, pair.Key.Y].Id != pair.Value.Id)
                {
                    Thread.Sleep(5);
                    await PlaceBlock(pair.Key.L, pair.Key.X, pair.Key.Y, pair.Value.Id);
                }
                buffer.Remove(pair.Key);
            }
        }

        public static async Task MakeGravity()
        {
            var buffer = new Dictionary<LCoordinate, Block>();

            for (int x = 30; x < 69; x++)
                buffer.Add(new LCoordinate(1, x, 63), new Block(15));

            for (int y = 64; y < 87; y++)
                buffer.Add(new LCoordinate(1, 30, y), new Block(14));

            for (int x = 31; x < 70; x++)
                if (x != 46)
                    buffer.Add(new LCoordinate(1, x, 86), new Block(13));

            while (buffer.Count > 0)
            {

                var pair = buffer.ElementAt(new Random().Next(0, buffer.Count));
                if (World[pair.Key.L, pair.Key.X, pair.Key.Y].Id != pair.Value.Id)
                {
                    Thread.Sleep(5);
                    await PlaceBlock(pair.Key.L, pair.Key.X, pair.Key.Y, pair.Value.Id);
                }
                buffer.Remove(pair.Key);
            }
        }

        public static async Task OpenEntrance()
        {
            
        }

        public static async Task CloseEntrance()
        {

        }

        public static async Task ReleasePlayers()
        {
            await PlaceBlock(1, 46, 86, 13);
        }

        public static async Task CreateSafeArea()
        {
            await PlaceBlock(1, 46, 86, 96);
            await PlaceBlock(1, 49, 86, 70);
        }

        public static async Task CloseSafeArea()
        {
            await PlaceBlock(1, 53, 86, 96);
        }


        /// <summary>
        /// Start a running game process
        /// </summary>
        public static async Task StartGame()
        {
            await ClearGameArea();

            Thread.Sleep(6000);
            await BuildMap(maps.ElementAt(new Random().Next(0, maps.Count)));

            Thread.Sleep(2000);
            await MakeGravity();
            await OpenEntrance();

            Thread.Sleep(2000);
            await ReleasePlayers();
            startTime = DateTime.Now;

            Thread.Sleep(5000);
            await CreateSafeArea();
            tick.Start();
            await CloseEntrance();

            Thread.Sleep(10000);
            tick.Stop();
            await StartGame();
        }
    }
}
