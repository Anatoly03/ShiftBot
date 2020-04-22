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
using Colorful;

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

        /// <summary>
        /// Eliminates players
        /// </summary>
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

        /// <summary>
        /// Builds a map by id
        /// </summary>
        public static async Task BuildMap(int id)
        {
            await BuildMap(Maps.FirstOrDefault(m => m.Id == id));
        }

        /// <summary>
        /// Builds a map by MapInfo
        /// </summary>
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
                    await pair.Value.Place(pair.Key.L, pair.Key.X, pair.Key.Y);
                }
                buffer.Remove(pair.Key);
            }
        }

        /// <summary>
        /// Creates the gravity tunnels
        /// </summary>
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
                    Thread.Sleep(15);
                    await PlaceBlock(pair.Key.L, pair.Key.X, pair.Key.Y, pair.Value.Id);
                }
                buffer.Remove(pair.Key);
            }
        }

        /// <summary>
        /// Creates the entrances of the current map
        /// </summary>
        public static async Task OpenEntrance()
        {
            var map = JsonConvert.DeserializeObject<Block[,,]>(File.ReadAllText($"../../../levels/{currentMap.Id}/map.json"), Json_settings);

            // Where is the entrance?
            bool isEntranceLeft = false;
            bool isEntranceTop = false;
            bool isEntranceRight = false;
            bool isEntranceBottom = false;

            // Left?
            for (int y = 0; y < 22; y++)
            {
                if (map[1, 0, y].Id == 16)
                {
                    if (!isEntranceLeft)
                    {
                        isEntranceLeft = true;
                        await PlaceBlock(1, 30, y + 63, 96);
                    }
                    for (int i = 0; i < 3; i++)
                    {
                        await PlaceBlock(1, 30 + i, y + 64, 15);
                    }
                }
            }

            // Top?
            if (!isEntranceLeft)
                for (int x = 37; x > -1; x--)
                {
                    if (map[1, x,0].Id == 16)
                    {
                        if (!isEntranceTop)
                        {
                            isEntranceTop = true;
                            await PlaceBlock(1, x + 32, 63, 96);
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            await PlaceBlock(1, x + 31, 63 + i, 0);
                        }
                    }
                }

            // Right?
            if (!isEntranceLeft && !isEntranceTop)
                for (int y = 21; y > -1; y--)
                {
                    if (map[1, 37, y].Id == 16)
                    {
                        if (!isEntranceRight)
                        {
                            isEntranceRight = true;
                            await PlaceBlock(1, 69, y + 65, 96);
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            await PlaceBlock(1, 67 + i, y + 64, 13);
                        }
                    }
                }

            // Bottom?
            if (!isEntranceLeft && !isEntranceTop && !isEntranceRight)
                for (int x = 0; x < 38; x++)
                {
                    if (map[1, x, 21].Id == 16)
                    {
                        if (!isEntranceBottom)
                        {
                            isEntranceBottom = true;
                            await PlaceBlock(1, x + 31, 86, 96);
                        }
                        for (int i = 0; i < 3; i++)
                        {
                            await PlaceBlock(1, x + 31, 86 - i, 14);
                        }
                    }
                }

            // Prepare countdown to close the door.
            EntranceCooldown = new System.Timers.Timer(10000);
            EntranceCooldown.Elapsed += async (Object s, ElapsedEventArgs e) => {
                await CloseEntrance();
                EntranceCooldown.Stop();
            };

            EntranceMovement = new System.Timers.Timer(500);
            EntranceMovement.Elapsed += async (Object s, ElapsedEventArgs e) => {
                await CloseEntrance();
                EntranceMovement.Stop();
            };

            isDoorOpen = true;
        }

        /// <summary>
        /// Closes the entrances of the current map
        /// </summary>
        public static async Task CloseEntrance()
        {
            if (!isBuilding)
            {
                var map = JsonConvert.DeserializeObject<Block[,,]>(File.ReadAllText($"../../../levels/{currentMap.Id}/map.json"), Json_settings);

                // Where is the entrance?
                bool isEntranceLeft = false;
                bool isEntranceTop = false;
                bool isEntranceRight = false;
                bool isEntranceBottom = false;

                // Left?
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
                        await map[1, 36, y].Place(1, 32, y + 64);
                    }
                }

                // Top?
                if (!isEntranceLeft)
                    for (int x = 37; x > -1; x--)
                    {
                        if (map[1, x, 0].Id == 16)
                        {
                            if (!isEntranceTop)
                            {
                                isEntranceTop = true;
                                await PlaceBlock(1, x + 32, 63, 15);
                            }
                            await PlaceBlock(1, x + 31, 63, 15);
                            await PlaceBlock(1, x + 31, 64, 96);
                            await PlaceBlock(1, x + 31, 64, 96);
                            await map[1, x, 0].Place(1, x + 31, 65);
                        }
                    }

                // Right?
                if (!isEntranceLeft && !isEntranceTop)
                    for (int y = 21; y > -1; y--)
                    {
                        if (map[1, 37, y].Id == 16)
                        {
                            if (!isEntranceRight)
                            {
                                isEntranceRight = true;
                                await PlaceBlock(1, 69, y + 65, 0);
                            }
                            await map[1, 36, y].Place(1, 67, y + 64);
                            await PlaceBlock(1, 68, y + 64, 96);
                            await PlaceBlock(1, 69, y + 64, 0);
                        }
                    }

                // Bottom?
                if (!isEntranceLeft && !isEntranceTop && !isEntranceRight)
                    for (int x = 0; x < 38; x++)
                    {
                        if (map[1, x, 21].Id == 16)
                        {
                            if (!isEntranceBottom)
                            {
                                isEntranceBottom = true;
                                await PlaceBlock(1, x + 31, 86, 96);
                            }
                            await PlaceBlock(1, x + 31, 86, 13);
                            await PlaceBlock(1, x + 31, 85, 96);
                            await map[1, x, 20].Place(1, x + 31, 84);
                        }
                    }
            }

            isDoorOpen = false;
        }

        /// <summary>
        /// Creates coin door exits in the current map
        /// </summary>
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

        /// <summary>
        /// Open the safe area, to release the players.
        /// </summary>
        public static async Task ReleasePlayers()
        {
            await PlaceBlock(1, 46, 86, 13);
            EntranceCooldown.Start();
        }

        /// <summary>
        /// Generates a new safe area
        /// </summary>
        public static async Task CreateSafeArea()
        {
            await PlaceBlock(1, 46, 86, 96);
            await PlaceBlock(1, 47, 86, 0);
            await PlaceBlock(1, 49, 86, 70);
        }

        /// <summary>
        /// Closes the safe area
        /// </summary>
        public static async Task CloseSafeArea()
        {
            await PlaceBlock(1, 53, 86, 96);
        }

        /// <summary>
        /// Generates new maps to vote on
        /// </summary>
        public static async Task RegenerateMapVoters()
        {
            var buffer = new List<MapInfo>();
            var r = new Random();

            foreach (MapInfo k in Maps)
            {
                if (currentMap != null)
                {
                    if (k.Id != currentMap.Id)
                        buffer.Insert(r.Next(0, buffer.Count), k);
                }
                else
                {
                    buffer.Insert(r.Next(0, buffer.Count), k);
                }
            }

            foreach (MapVote mv in MapVoteSigns)
            {
                await mv.SetMap(buffer.ElementAt(0));
                buffer.RemoveAt(0);
            }
        }

        /// <summary>
        /// Closes map voting poll
        /// </summary>
        public static async Task HideMapVoters()
        {
            foreach (MapVote mv in MapVoteSigns)
            {
                await mv.Close();
            }
        }


        /// <summary>
        /// Start or continue a running game process
        /// </summary>
        public static async Task ContinueGame()
        {
            if (!isBuilding)
            {
                isBuilding = true;

                if (Tick != null)
                    Tick.Stop();
                if (Eliminator != null)
                    Eliminator.Stop();
                if (TimeLimit != null)
                    TimeLimit.Stop();

                await HideMapVoters();
                await ClearGameArea();

                Thread.Sleep(6000);

                int maxVotes = 0;
                var chose = new List<MapInfo>();
                foreach (MapVote mv in MapVoteSigns)
                {
                    if (maxVotes < mv.Votes)
                    {
                        maxVotes = mv.Votes;
                        chose.Clear();
                        chose.Add(Maps.FirstOrDefault(map => map.Id == mv.MapId));
                    }
                    else if (maxVotes == mv.Votes)
                    {
                        chose.Add(Maps.FirstOrDefault(map => map.Id == mv.MapId));
                    }
                }

                MapInfo newMap = Maps.ElementAt(0);
                if (chose.Count == 1)
                {
                    newMap = chose.ElementAt(0);
                }
                else if (chose.Count > 1)
                {
                    newMap = chose.ElementAt(new Random().Next(0, chose.Count));
                }
                await BuildMap(newMap);
                await CreateExit();

                {
                    Formatter[] format =
                    {
                    new Formatter(newMap.Title, Color.Cyan),
                    new Formatter(newMap.Id.ToString(), Color.Gray),
                    new Formatter(newMap.Creator, Color.Cyan),
                };

                    Console.WriteLineFormatted("  Map {0} ({1}) By {2}", Color.Silver, format);
                }


                if (PlayersSafe.Count < 2)
                {
                    round = 1;

                    PlayersInGame = new List<Player>();
                    PlayersSafe = new Dictionary<Player, TimeSpan>();

                    foreach (Player p in Players)
                    {
                        await SayCommand($"reset {p.Name}");
                        await SayCommand($"tp {p.Name} 47 86");
                        PlayersInGame.Add(p);
                    }

                    Console.Write("* ", Color.Silver);
                    Console.Write("NEW GAME! ");
                    Console.WriteLine($"{Players.Count} in world, {PlayersInGame.Count} joined", Color.DarkGray);
                }
                else
                {
                    round++;

                    int playersBefore = PlayersInGame.Count;
                    PlayersInGame = new List<Player>();

                    foreach (KeyValuePair<Player, TimeSpan> p in PlayersSafe)
                    {
                        PlayersInGame.Add(p.Key);
                    }

                    PlayersSafe = new Dictionary<Player, TimeSpan>();

                    Formatter[] format =
                    {
                    new Formatter("Round", Color.White),
                    new Formatter(round, Color.Gold),
                    new Formatter(PlayersInGame.Count, Color.Green),
                    new Formatter(playersBefore - PlayersInGame.Count, Color.Green),
                };

                    Console.WriteLineFormatted("    {0} {1}! {2} joined the round, {3} eliminated", Color.Silver, format);
                }

                TimeLimit = new System.Timers.Timer(150 * 1000);
                TimeLimit.Elapsed += async (Object s, ElapsedEventArgs e) => {
                    await Say($"Time's over!");
                    await ContinueGame();
                };

                int k = 20 * 1000;
                //int k = 5000 * Math.Min((int)Math.Ceiling(PlayersInGame.Count / 5f), 5);
                Eliminator = new System.Timers.Timer(k);
                Eliminator.Elapsed += async (Object s, ElapsedEventArgs e) => {
                    await ContinueGame();
                };

                Thread.Sleep(5000);
                await MakeGravity();
                await OpenEntrance();
                Thread.Sleep(500);
                await ReleasePlayers();
                startTime = DateTime.Now;
                TimeLimit.Start();

                Thread.Sleep(2000);
                await CreateSafeArea();
                foreach (Player p in Players) p.Vote = -1;
                await RegenerateMapVoters();
                Tick.Start();
                isBuilding = false;
            }
        }

        /// <summary>
        /// Occurs when <i>any</i> player touched the crown.
        /// </summary>
        public static async Task PlayerWon(Player player)
        {
            TimeSpan ts = DateTime.Now - startTime;

            if (PlayersInGame.FirstOrDefault(p => p.Id == player.Id) != null && !isBuilding)
            {
                if (PlayersSafe.Count == 0) // First
                {
                    firstPerson = DateTime.Now;

                    int k = 20;
                    //int k = 5000 * Math.Min((int)Math.Ceiling(PlayersInGame.Count / 5f), 5);
                    string s = $"{k} seconds left!";
                    await Say($"{player.Name.ToUpper()} {(PlayersInGame.Count > 2 ? "finished! " + s : "won!")}");
                    Eliminator.Start();
                }

                string elapsedTime = TimeToString(ts);
                PlayersSafe.Add(player, ts);

                await SayCommand($"reset {player.Name}");
                await SayCommand($"tp {player.Name} 47 86");

                await SayPrivate(player, $"Your Time: {elapsedTime}s");
                await UpdateSign();

                Formatter[] format =
                {
                    new Formatter(PlayersSafe.Count, Color.Gold),
                    new Formatter(player.Name, Color.Green),
                    new Formatter(elapsedTime, Color.Green),
                };
                Console.WriteLineFormatted("    {0}. {1} in {2}", Color.Silver, format);

                if (2 * PlayersSafe.Count >= PlayersInGame.Count) // 50 % completed - eliminate (For 5 players: 3, For 16 players: 8)
                {
                    if (PlayersSafe.Count > 1)
                        await Say($"Round over! Players not finished are eliminated!");
                    await ContinueGame();
                }
            }
        }
    }
}
