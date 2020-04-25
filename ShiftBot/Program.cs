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
         * Configuration
         */
        public static string WorldId;
        public static Coordinate TopLeftShiftCoord;

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
        public static List<MapVote> MapVoteSigns;

        public static int Width;
        public static int Height;
        public static int BotId;

        public static DateTime startTime = DateTime.Now;
        public static DateTime firstPerson;
        public static int round;
        public static bool isBuilding;
        public static bool isDoorOpen;
        public static bool isTrainMode;

        public static System.Timers.Timer Tick;
        public static JsonSerializerSettings Json_settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto, Formatting = Formatting.Indented };

        // Control Timers
        public static System.Timers.Timer EntranceCooldown;
        public static System.Timers.Timer EntranceMovement;

        public static System.Timers.Timer Eliminator; // Eliminate after 5-20 seconds, after the first player arrived.
        public static System.Timers.Timer TimeLimit; // Eliminate after 5-20 seconds, after the first player arrived.

        public static CancellationTokenSource isGameAborted = new CancellationTokenSource();

        /*
         * Difficulties
         *
         * 0 - basic
         * 1 - basic_easy
         * 2 - easy
         * 3 - easy_medium
         * 4 - medium
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

            var config = File.ReadAllLines("../../../config.txt");

            foreach (string line in config)
            {
                var param = line.Split(' ');
                switch (param[0])
                {
                    case "worldid":
                        WorldId = param[1];
                        break;

                    case "corner":
                        TopLeftShiftCoord = new Coordinate(int.Parse(param[1]), int.Parse(param[2]));
                        MapVoteSigns = new List<MapVote> {
                            new MapVote(0, TopLeftShiftCoord.X + 13, TopLeftShiftCoord.Y + 26),
                            new MapVote(1, TopLeftShiftCoord.X + 15, TopLeftShiftCoord.Y + 26),
                            new MapVote(2, TopLeftShiftCoord.X + 22, TopLeftShiftCoord.Y + 26),
                            new MapVote(3, TopLeftShiftCoord.X + 24, TopLeftShiftCoord.Y + 26),
                        };
                        break;
                }
            }

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

                Con = (Connection) client.CreateWorldConnection(WorldId);

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
                    {
                        Console.WriteLine("Logged in!");
                        Console.WriteLine();
                        await Say($"Connected!");

                        World = new Block[2, m.GetInt(9), m.GetInt(10)];

                        Width = m.GetInt(9);
                        Height = m.GetInt(10);
                        BotId = m.GetInt(0);

                        int index = 11;
                        for (int _y = 0; _y < Width; _y++)
                            for (int _x = 0; _x < Height; _x++)
                            {
                                int value = 0;
                                if (m[index++] is int iValue)
                                    value = iValue;

                                var backgroundId = value >> 16;
                                var foregroundId = 65535 & value;

                                World[1, _x, _y] = new Block(foregroundId);
                                World[0, _x, _y] = new Block(backgroundId);
                                switch (foregroundId)
                                {
                                    case 55:
                                    case 56:
                                    case 57:
                                    case 58:
                                        string text = m.GetString(index++);
                                        int morph = m.GetInt(index++);
                                        World[1, _x, _y] = new Sign(foregroundId, text, morph);
                                        break;

                                    case 59:
                                        int rotation = m.GetInt(index++);
                                        int p_id = m.GetInt(index++);
                                        int t_id = m.GetInt(index++);
                                        bool flip = m.GetBool(index++);
                                        World[1, _x, _y] = new Portal(foregroundId, rotation, p_id, t_id, flip);
                                        break;

                                    case 93:
                                    case 94:
                                        int r = m.GetInt(index++);
                                        World[1, _x, _y] = new Effect(foregroundId, r);
                                        break;
                                }
                            }

                        /*for (int x = 0; x < Width; x++)
                            for (int y = 0; y < Height; y++)
                                if (World[1, x, y].Id == 20)
                                    //await PlaceBlock(1, x, y, 80);*/

                        startTime = DateTime.Now;
                        Tick = new System.Timers.Timer(50);
                        Tick.Elapsed += async (object s, ElapsedEventArgs e) => { await TickEvent(s, e); };

                        await RegenerateMapVoters();
                    }
                    break;

                case MessageType.PlayerJoin:
                case MessageType.PlayerAdd:
                    var playerExisting = Players.FirstOrDefault(p => p.Name == m.GetString(1).ToLower());
                    Players.Add(new Player(m.GetInt(0), m.GetString(1).ToLower()));
                    player = Players.FirstOrDefault(p => p.Id == m.GetInt(0));

                    Console.Write("+ ", Color.Green);
                    if (player.IsMod)
                    {
                        Console.Write(player.Name, Color.Orange);
                        player.OnMessage = Player.AdminMessageHandler;
                        await SayCommand($"giveedit {player.Name}");
                    }
                    else
                    {
                        Console.Write(player.Name, Color.Silver);
                        player.OnMessage = Player.DefaultMessageHandler;

                        await player.Tell("The game is currently closed for moderators due to testing");
                    }
                    Console.WriteLine($" joined! ({new Random(player.Name.GetHashCode()).Next(0, 101)}% noob)");

                    break;


                case MessageType.PlayerExit:
                    player = Players.FirstOrDefault(p => p.Id == m.GetInt(0));
                    Players.RemoveAll(p => p.Id == m.GetInt(0));

                    Console.Write("- ", Color.Red);
                    if (player.IsMod)
                        Console.Write(player.Name, Color.Orange);
                    else
                        Console.Write(player.Name, Color.Silver);
                    Console.WriteLine(" left!");

                    if (PlayersInGame.FirstOrDefault(p => p.Id == player.Id) != null)
                    {
                        PlayersInGame.RemoveAll(p => p.Id == player.Id);
                        if (PlayersSafe.Keys.FirstOrDefault(p => p.Id == player.Id) != null)
                            PlayersSafe.Remove(player);

                        if (2 * PlayersSafe.Count >= PlayersInGame.Count)
                        {
                            if (PlayersSafe.Count > 1)
                                await Say($"Round over! Players not finished are eliminated!");
                            await ContinueGame();
                        }
                    }

                    break;

                case MessageType.PlayerGod:
                    player = Players.FirstOrDefault(p => p.Id == m.GetInt(0));
                    player.Afk = m.GetBool(1);
                    break;
                    
                case MessageType.PlayerMove:
                    player = Players.FirstOrDefault(p => p.Id == m.GetInt(0));
                    var playerInGame = PlayersInGame.FirstOrDefault(p => p.Id == m.GetInt(0));

                    {
                        int facex = m.GetInt(3);
                        int facey = m.GetInt(4);

                        double mx = m.GetDouble(5);
                        double my = m.GetDouble(6);

                        if (playerInGame != null)
                            if (mx > TopLeftShiftCoord.X + 1 && mx < TopLeftShiftCoord.X + 42 && my > TopLeftShiftCoord.Y + 1 && my < TopLeftShiftCoord.X + 20)
                                if (isDoorOpen)
                                    EntranceMovement.Start();

                        if (!player.Afk)
                            if (!isBuilding)
                                foreach (MapVote mv in MapVoteSigns)
                                    if (mv.Inited)
                                        if (Math.Pow(mx - mv.X, 2) + Math.Pow(my - mv.Y, 2) < 0.5)
                                            if (facey == -1)
                                                await mv.NewVote(player);
                    }

                    break;

                case MessageType.Won:
                    await PlayerWon(Players.FirstOrDefault(p => p.Id == m.GetInt(0)));
                    break;

                case MessageType.PlaceBlock:
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
                    break;

                case MessageType.Chat:
                    if (m.GetInt(0) != BotId)
                    {
                        player = Players.FirstOrDefault(p => p.Id == m.GetInt(0));
                        await player.OnMessage(new Command(player, m.GetString(1)));
                    }
                    
                    break;
            }
        }

        static async Task TickEvent(object s, ElapsedEventArgs e)
        {
            await UpdateSign();
        }

        static async Task UpdateSign()
        {
            if (PlayersSafe.Count > 0)
            {
                string display = TimeToString(DateTime.Now - startTime);

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


                await PlaceSign(TopLeftShiftCoord.X + 16, TopLeftShiftCoord.Y + 22, 58, display, 1);
            }
        }
    }
}
