using System;
using System.Collections.Generic;
using System.Drawing;
using Console = Colorful.Console;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
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
        public static async Task SayCommand(string message) => await Con.SendAsync(MessageType.Chat, "/" + message);
        public static async Task SayPrivate(string player, string msg) => await Con.SendAsync(MessageType.Chat, "/pm " + player + " [Bot] " + msg);
        public static async Task SayPrivate(Player player, string msg) => await Con.SendAsync(MessageType.Chat, "/pm " + player.Name + " [Bot] " + msg);

        public static string TimeToString(TimeSpan ts) => String.Format("{0:0}.{1:00}", ts.TotalSeconds, ts.Milliseconds / 10) +  "s";

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
            foreach (MapInfo k in Maps)
                n = Math.Max(n, k.Id);
            n++;

            Directory.CreateDirectory($"../../../levels/{n}");

            using (StreamWriter file = File.CreateText($"../../../levels/{n}/map.json"))
            {
                var serializer = JsonConvert.SerializeObject(game, Json_settings);
                file.WriteLine(serializer);
            }

            Maps.Add(new MapInfo
            {
                Id = n,
                Title = "Untitled Map"
            });

            using (StreamWriter file = File.CreateText($"../../../levels/list.json"))
            {
                var serializer = JsonConvert.SerializeObject(Maps, Json_settings);
                file.WriteLine(serializer);
            }

            await Say("Map saved!");
        }

        public static async Task BuildMap(int id)
        {
            await BuildMap(Maps.FirstOrDefault(m => m.Id == id));
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
            currentMap = info;

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
                    Thread.Sleep(10);
                    await PlaceBlock(pair.Key.L, pair.Key.X, pair.Key.Y, pair.Value.Id);
                }
                buffer.Remove(pair.Key);
            }
        }

        public static async Task OpenEntrance()
        {
            var map = JsonConvert.DeserializeObject<Block[,,]>(File.ReadAllText($"../../../levels/{currentMap.Id}/map.json"), Json_settings);

            // Where is the entrance?
            bool isEntranceLeft = false;
            //bool isEntranceRight = false;

            for (int y = 0; y < 22; y++)
            {
                if (map[1, 0, y].Id == 16)
                {
                    if (!isEntranceLeft)
                    {
                        isEntranceLeft = true;
                        await PlaceBlock(1, 30, y + 63, 96);
                    }
                    for (int i = 0; i < 2; i++)
                    {
                        await PlaceBlock(1, 30 + i, y + 64, 15);
                    }
                }
            }

            // Prepare countdown to close the door.
            EntranceCooldown = new System.Timers.Timer(5000);
            EntranceCooldown.Elapsed += async (Object s, ElapsedEventArgs e) => {
                await CloseEntrance();
                EntranceCooldown.Stop();
            };

            EntranceMovement = new System.Timers.Timer(500);
            EntranceMovement.Elapsed += async (Object s, ElapsedEventArgs e) => {
                await CloseEntrance();
                EntranceMovement.Stop();
            };
        }

        public static async Task CloseEntrance()
        {
            var map = JsonConvert.DeserializeObject<Block[,,]>(File.ReadAllText($"../../../levels/{currentMap.Id}/map.json"), Json_settings);

            // Where is the entrance?
            bool isEntranceLeft = false;
            //bool isEntranceRight = false;

            for (int y = 0; y < 22; y++)
            {
                if (map[1, 0, y].Id == 16)
                {
                    if (!isEntranceLeft)
                    {
                        isEntranceLeft = true;
                        await PlaceBlock(1, 30, y + 63, 14);
                    }
                    await PlaceBlock(1, 30, y + 64, 14);
                    await PlaceBlock(1, 31, y + 64, 96);
                    //await PlaceBlock(1, 32, y + 64, map[1, 0, y].Id);
                }
            }

        }

        public static async Task CreateExit()
        {
            var map = JsonConvert.DeserializeObject<Block[,,]>(File.ReadAllText($"../../../levels/{currentMap.Id}/map.json"), Json_settings);

            int coins = 0;
            for (int y = 0; y < 22; y++)
                for (int x = 0; x < 38; x++)
                    if (map[1, 0, y].Id == 11)
                        coins++;

            /*
            for (int y = 0; y < 22; y++)
            {
                if (map[1, 0, y].Id == 71)
                {
                    
                }
            }
            */

        }

        public static async Task ReleasePlayers()
        {
            await PlaceBlock(1, 46, 86, 13);
            EntranceCooldown.Start();
        }

        public static async Task CreateSafeArea()
        {
            await PlaceBlock(1, 46, 86, 96);
            await PlaceBlock(1, 47, 86, 0);
            await PlaceBlock(1, 49, 86, 70);
        }

        public static async Task CloseSafeArea()
        {
            await PlaceBlock(1, 53, 86, 96);
        }


        /// <summary>
        /// Start or continue a running game process
        /// </summary>
        public static async Task ContinueGame()
        {
            if (Tick != null)
                Tick.Stop();
            if (Eliminator != null)
                Eliminator.Stop();
            if (TimeLimit != null)
                TimeLimit.Stop();

            await ClearGameArea();

            Thread.Sleep(6000);
            await BuildMap(Maps.ElementAt(new Random().Next(0, Maps.Count)));
            await CreateExit();

            Console.Write("GAME!", Color.IndianRed);
            if (PlayersSafe.Count < 2)
            {
                Console.WriteLine(" START!");
                round = 1;

                PlayersInGame = new List<Player>();
                PlayersSafe = new Dictionary<Player, TimeSpan>();

                foreach (Player p in Players)
                {
                    await SayCommand($"reset {p.Name}");
                    await SayCommand($"tp {p.Name} 47 86");
                    PlayersInGame.Add(p);
                }
            }
            else
            {
                Console.WriteLine(" CONTINUE!");
                round++;

                PlayersInGame = new List<Player>();

                foreach (KeyValuePair<Player, TimeSpan> p in PlayersSafe)
                {
                    PlayersInGame.Add(p.Key);
                }

                PlayersSafe = new Dictionary<Player, TimeSpan>();
            }

            TimeLimit = new System.Timers.Timer(150 * 1000);
            TimeLimit.Elapsed += async (Object s, ElapsedEventArgs e) => {
                await Say($"Time over!");
                await ContinueGame();
            };

            int k = 5000 * Math.Min((int)Math.Ceiling(PlayersInGame.Count / 5f), 5);
            Eliminator = new System.Timers.Timer(k);
            Eliminator.Elapsed += async (Object s, ElapsedEventArgs e) => {
                await ContinueGame();
            };

            Thread.Sleep(2000);
            await MakeGravity();
            await OpenEntrance();

            Thread.Sleep(2000);
            await ReleasePlayers();
            startTime = DateTime.Now;
            TimeLimit.Start();

            Thread.Sleep(2000);
            await CreateSafeArea();
            Tick.Start();
        }

        /// <summary>
        /// Occurs when any player touched the crown.
        /// </summary>
        public static async Task PlayerWon(Player player)
        {
            TimeSpan ts = DateTime.Now - startTime;

            if (PlayersSafe.Count == 0) // First
            {
                firstPerson = DateTime.Now;

                int k = 5000 * Math.Min((int)Math.Ceiling(PlayersInGame.Count / 5f), 5);
                string s = "{k} seconds left!";
                await Say($"{player.Name.ToUpper()} {(PlayersInGame.Count > 2 ? "finished! " + s : "won!")}");
                Eliminator.Start();
            }

            if (PlayersInGame.FirstOrDefault(p => p.Id == player.Id) != null)
            {
                PlayersSafe.Add(player, ts);
                await SayCommand($"reset {player.Name}");
                await SayCommand($"tp {player.Name} 47 86");

                string elapsedTime = String.Format("{0:0}.{1:00}", ts.TotalSeconds, ts.Milliseconds / 10);
                await SayPrivate(player, $"Your Time: {elapsedTime}s");
                await UpdateSign();
            }

            if (2 * PlayersSafe.Count >= PlayersInGame.Count) // 50 % completed - eliminate (For 5 players: 3, For 16 players: 8)
            {
                if (Tick != null)
                    Tick.Stop();
                await ContinueGame();
            }
        }
    }
}
