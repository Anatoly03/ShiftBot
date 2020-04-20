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
using EEUniverse.LoginExtensions;
using Newtonsoft.Json;

namespace ShiftBot
{
    partial class Program
    {
        /*
         * Variables
         */
        public static Connection Con;
        public static Block[,,] World;
        public static List<Player> Players = new List<Player>();
        public static Dictionary<string, Profile> Profiles = new Dictionary<string, Profile>();
        public static List<Player> PlayersInGame = new List<Player>();
        public static Dictionary<Player, TimeSpan> PlayersSafe = new Dictionary<Player, TimeSpan>();
        public static List<MapInfo> Maps;
        public static MapInfo currentMap;

        public static int Width;
        public static int Height;
        public static int BotId;

        public static DateTime startTime = DateTime.Now;
        public static DateTime firstPerson;
        public static int round;
        public static bool isBuilding;

        public static System.Timers.Timer Tick;
        public static JsonSerializerSettings Json_settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };

        // Control Timers
        public static System.Timers.Timer EntranceCooldown;
        public static System.Timers.Timer EntranceMovement;

        public static System.Timers.Timer Eliminator; // Eliminate after 5-20 seconds, after the first player arrived.
        public static System.Timers.Timer TimeLimit; // Eliminate after 5-20 seconds, after the first player arrived.

        /*
         * Difficulties
         *
         * 0 - basic
         * 1 - basic_easy
         * 2 - easy
         * 3 - easy_medium
         */

        /*
         * Execution
         */

        static async Task Main(string[] args)
        {
            Console.WriteLine("Reading cookie...");

            /*
             * All the saved map non-blocks informations
             */
            Maps = JsonConvert.DeserializeObject<List<MapInfo>>(File.ReadAllText("../../../levels/list.json"), Json_settings);

            /*
             * All the saved profiles
             */
            Profiles = JsonConvert.DeserializeObject<Dictionary<string, Profile>>(File.ReadAllText("../../../profiles.json"), Json_settings);

            await Main2(File.ReadAllLines("../../../cookie.txt"));
        }

        static async Task Main2(string[] data)
        {
            Console.WriteLine("Logging in...");

            try
            {
                bool isGoogleToken = data[0] == "google" ? true : false;
                Client client;
                if (isGoogleToken)
                    client = await GoogleLogin.GetClientFromCookieAsync(data[1]);
                else
                    client = new Client(data[1]);

                await client.ConnectAsync();

                Con = (Connection) client.CreateWorldConnection("tPy8SPndRwTI");

                Con.OnMessage += async (s, m) =>
                {
                    try
                    {
                        await Main3(m);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Bot almost crashed!");
                        Console.Write(e);
                    }
                };
                await Con.SendAsync(MessageType.Init, 0);

                Thread.Sleep(-1);
            }
            catch
            {
                Console.WriteLine("Login failed! Update google cookie!");
            }
        }

        static async Task Main3(Message m)
        {
            Player player;

            switch (m.Type)
            {
                case MessageType.Init:

                    Console.WriteLine("Logged in!");
                    Console.WriteLine();
                    await Say($"Connected!");

                    World = new Block[2, m.GetInt(9), m.GetInt(10)];

                    Width = m.GetInt(9);
                    Height = m.GetInt(10);
                    BotId = m.GetInt(0);

                    int index = 11;
                    for (int y = 0; y < Width; y++)
                        for (int x = 0; x < Height; x++)
                        {
                            int value = 0;
                            if (m[index++] is int iValue)
                                value = iValue;

                            var backgroundId = value >> 16;
                            var foregroundId = 65535 & value;

                            World[1, x, y] = new Block(foregroundId);
                            World[0, x, y] = new Block(backgroundId);
                            switch (foregroundId)
                            {
                                case 55:
                                case 56:
                                case 57:
                                case 58:
                                    string text = m.GetString(index++);
                                    int morph = m.GetInt(index++);
                                    World[1, x, y] = new Sign(foregroundId, text, morph);
                                    break;

                                case 59:
                                    int rotation = m.GetInt(index++);
                                    int p_id = m.GetInt(index++);
                                    int t_id = m.GetInt(index++);
                                    bool flip = m.GetBool(index++);
                                    World[1, x, y] = new Portal(foregroundId, rotation, p_id, t_id, flip);
                                    break;

                                case 93:
                                case 94:
                                    int r = m.GetInt(index++);
                                    World[1, x, y] = new Effect(foregroundId, r);
                                    break;
                            }
                        }

                    /*for (int x = 0; x < Width; x++)
                        for (int y = 0; y < Height; y++)
                            if (World[1, x, y].Id == 20)
                                //await PlaceBlock(1, x, y, 80);*/

                    startTime = DateTime.Now;
                    Tick = new System.Timers.Timer(50);
                    Tick.Elapsed += async (Object s, ElapsedEventArgs e) => { await TickEvent(s, e); };

                    break;

                case MessageType.PlayerJoin:
                case MessageType.PlayerAdd:
                    var playerExisting = Players.FirstOrDefault(p => p.Name == m.GetString(1).ToLower());
                    Players.Add(new Player(m.GetInt(0), m.GetString(1).ToLower()));
                    player = Players.FirstOrDefault(p => p.Id == m.GetInt(0));

                    if (player.IsMod)
                    {
                        Console.Write(player.Name, Color.Orange);
                        await SayCommand($"giveedit {player.Name}");
                    }
                    else
                    {
                        Console.Write(player.Name, Color.Silver);
                    }
                    Console.WriteLine(" joined!");

                    break;


                case MessageType.PlayerExit:
                    player = Players.FirstOrDefault(p => p.Id == m.GetInt(0));
                    Players.RemoveAll(p => p.Id == m.GetInt(0));

                    if (player.IsMod)
                    {
                        Console.Write(player.Name, Color.Orange);
                    }
                    else
                    {
                        Console.Write(player.Name, Color.Silver);
                    }
                    Console.WriteLine(" left!");
                    break;

                case MessageType.PlayerGod:
                    player = Players.FirstOrDefault(p => p.Id == m.GetInt(0));
                    player.afk = m.GetBool(1);
                    break;

                case MessageType.PlayerMove:
                    player = Players.FirstOrDefault(p => p.Id == m.GetInt(0));

                    {

                        int x = (int) Math.Floor(m.GetDouble(5));
                        int y = (int) Math.Floor(m.GetDouble(6));


                        /*if (!player.afk)
                        {

                        }*/
                    }

                    break;

                case MessageType.Won:
                    await PlayerWon(Players.FirstOrDefault(p => p.Id == m.GetInt(0)));
                    break;

                case MessageType.PlaceBlock:
                    if (m.GetInt(0) != BotId)
                    {
                        //Console.WriteLine(m);

                        int l = m.GetInt(1);
                        int x = m.GetInt(2);
                        int y = m.GetInt(3);

                        Block blockBefore = World[l, x, y];

                        // Assign block to the world
                        World[l, x, y] = new Block(m.GetInt(4));

                        switch (m.GetInt(4))
                        {
                            // Signs
                            case 55:
                            case 56:
                            case 57:
                            case 58:
                                string text = m.GetString(5);
                                int morph = m.GetInt(6);
                                World[l, x, y] = new Sign(m.GetInt(4), text, morph);
                                break;

                            // Portals
                            case 59:
                                int rotation = m.GetInt(5);
                                int p_id = m.GetInt(6);
                                int t_id = m.GetInt(7);
                                bool flip = m.GetBool(8);
                                World[l, x, y] = new Portal(m.GetInt(4), rotation, p_id, t_id, flip);
                                break;

                            // Effects
                            case 93:
                            case 94:
                                int r = m.GetInt(5);
                                World[l, x, y] = new Effect(m.GetInt(4), r);
                                break;
                        }
                    }
                    break;

                case MessageType.Chat:

                    if (m.GetString(1).StartsWith("!", StringComparison.Ordinal) || m.GetString(1).StartsWith(".", StringComparison.Ordinal))
                    {
                        string[] param = m.GetString(1).Substring(1).Split(" ", StringSplitOptions.RemoveEmptyEntries);
                        string cmd = param[0].ToLower();
                        player = Players.FirstOrDefault(p => p.Id == m.GetInt(0));

                        switch (cmd)
                        {
                            case "ping":
                                await SayPrivate(player, "Pong!");
                                break;

                            case "clear":
                                if (player.IsMod)
                                {
                                    await ClearGameArea();
                                }
                                break;

                            case "save":
                                if (player.IsMod)
                                {
                                    await SaveMap();
                                }
                                break;

                            case "kill":
                            case "start":
                                if (player.IsMod)
                                {
                                    await ContinueGame();
                                }
                                break;

                            case "scan":
                                if (player.IsMod && param.Length > 1)
                                {
                                    await OpenScanner(param[1], player);
                                }
                                break;
                        }
                    }
                    break;
            }
        }

        static async Task TickEvent(Object s, ElapsedEventArgs e)
        {
            await UpdateSign();
        }

        static async Task UpdateSign()
        {
            if (PlayersSafe.Count > 0)
            {
                TimeSpan ts = DateTime.Now - startTime;
                //string elapsedTime = String.Format("{0:0}.{1:00}", ts.TotalSeconds, ts.Milliseconds / 10);

                string display = TimeToString((TimeSpan)(DateTime.Now - startTime));

                if (PlayersSafe.Count > 2)
                    for (int i = PlayersSafe.Count - 3; i < PlayersSafe.Count; i++)
                    {
                        display += $"\n{i + 1}. {PlayersSafe.ElementAt(i).Key.Name.ToUpper()} {TimeToString(PlayersSafe.ElementAt(i).Value)}";
                    }
                else if (PlayersSafe.Count == 2)
                {
                    display += $"\n1. {PlayersSafe.ElementAt(0).Key.Name.ToUpper()} {TimeToString(PlayersSafe.ElementAt(0).Value)}";
                    display += $"\n2. {PlayersSafe.ElementAt(1).Key.Name.ToUpper()} {TimeToString(PlayersSafe.ElementAt(1).Value)}";
                }
                else
                    display += $"\n1. {PlayersSafe.ElementAt(0).Key.Name.ToUpper()} {TimeToString(PlayersSafe.ElementAt(0).Value)}";


                await PlaceSign(47, 86, 58, display, 1);
            }
        }
    }
}
