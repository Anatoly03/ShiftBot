using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EEUniverse.Library;
using EEUniverse.LoginExtensions;
using Newtonsoft.Json;

namespace ShiftBot
{
    partial class Program
    {
        public static MapScanner Scanner = new MapScanner();

        public static async Task OpenScanner(string worldid, Player p)
        {
            Scanner.ScannerOpenedBy = p;
            await Scanner.Open(worldid);
        }
    }

    public class MapScanner
    {
        public Client client;
        public Connection Con;
        public Block[,,] World;
        public List<Player> Players = new List<Player>();
        public string WorldId;
        public Player ScannerOpenedBy;

        public async Task Open(string worldid)
        {
            WorldId = worldid;

            string[] data = File.ReadAllLines("../../../cookie.txt");

            bool isGoogleToken = data[0] == "google" ? true : false;
            if (isGoogleToken)
                client = await GoogleLogin.GetClientFromCookieAsync(data[1]);
            else
                client = new Client(data[1]);

            await client.ConnectAsync();

            try
            {
                Con = (Connection) client.CreateWorldConnection(WorldId);

                await Program.SayPrivate(ScannerOpenedBy, "[Scanner] Logged in!");

                Console.Write("Opened scanner in ");
                Console.Write(WorldId, Color.Orange);
                Console.WriteLine("!");

                Con.OnMessage += async (s, m) =>
                {
                    try
                    {
                        await MessageHandler(m);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Bot almost crashed!");
                        Console.Write(e);
                    }
                };
                await Con.SendAsync(MessageType.Init, 0);

                //Thread.Sleep(-1);
            }
            catch
            {
                await Program.SayPrivate(ScannerOpenedBy, "[Scanner] Failed to log in!");
            }
        }

        public async Task MessageHandler(Message m)
        {
            Player player;

            switch (m.Type)
            {
                case MessageType.Init:
                    await Con.SendAsync(MessageType.Chat, "[ShiftBot Scanner] Connected!");
                    World = new Block[2, m.GetInt(9), m.GetInt(10)];

                    int index = 11;
                    for (int y = 0; y < m.GetInt(9); y++)
                        for (int x = 0; x < m.GetInt(10); x++)
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

                    break;

                case MessageType.PlayerJoin:
                case MessageType.PlayerAdd:
                    Players.Add(new Player(m.GetInt(0), m.GetString(1).ToLower()));
                    break;


                case MessageType.PlayerExit:
                    Players.RemoveAll(p => p.Id == m.GetInt(0));
                    break;

                case MessageType.PlaceBlock:
                    int L = m.GetInt(1);
                    int X = m.GetInt(2);
                    int Y = m.GetInt(3);

                    // Assign block to the world
                    World[L, X, Y] = new Block(m.GetInt(4));

                    switch (m.GetInt(4))
                    {
                        // Signs
                        case 55:
                        case 56:
                        case 57:
                        case 58:
                            string text = m.GetString(5);
                            int morph = m.GetInt(6);
                            World[L, X, Y] = new Sign(m.GetInt(4), text, morph);
                            break;

                        // Portals
                        case 59:
                            int rotation = m.GetInt(5);
                            int p_id = m.GetInt(6);
                            int t_id = m.GetInt(7);
                            bool flip = m.GetBool(8);
                            World[L, X, Y] = new Portal(m.GetInt(4), rotation, p_id, t_id, flip);
                            break;

                        // Effects
                        case 93:
                        case 94:
                            int r = m.GetInt(5);
                            World[L, X, Y] = new Effect(m.GetInt(4), r);
                            break;
                    }
                    break;

                case MessageType.Chat:
                    if (m.GetString(1).StartsWith("!", StringComparison.Ordinal) || m.GetString(1).StartsWith(".", StringComparison.Ordinal))
                    {
                        string[] param = m.GetString(1).ToLower().Substring(1).Split(" ", StringSplitOptions.RemoveEmptyEntries);
                        string cmd = param[0];
                        player = Players.FirstOrDefault(p => p.Id == m.GetInt(0));

                        switch (cmd)
                        {
                            case "scan":
                                if (param.Length > 2)
                                {
                                    try
                                    {
                                        int _x = int.Parse(param[1]);
                                        int _y = int.Parse(param[2]);

                                        Block[,,] game = new Block[2, 38, 22];

                                        for (int l = 0; l < 2; l++)
                                            for (int x = _x; x < _x + 38; x++)
                                                for (int y = _y; y < _y + 22; y++)
                                                    game[l, x - _x, y - _y] = World[l, x, y];

                                        int n = 0;
                                        foreach (MapInfo k in Program.Maps)
                                            n = Math.Max(n, k.Id);
                                        n++;

                                        Directory.CreateDirectory($"../../../levels/{n}");

                                        using (StreamWriter file = File.CreateText($"../../../levels/{n}/map.json"))
                                        {
                                            var serializer = JsonConvert.SerializeObject(game, Program.Json_settings);
                                            file.WriteLine(serializer);
                                        }

                                        Program.Maps.Add(new MapInfo
                                        {
                                            Id = n,
                                            Title = "Untitled Map"
                                        });

                                        using (StreamWriter file = File.CreateText($"../../../levels/list.json"))
                                        {
                                            var serializer = JsonConvert.SerializeObject(Program.Maps, Program.Json_settings);
                                            file.WriteLine(serializer);
                                        }

                                        await Con.SendAsync(MessageType.Chat, "[ShiftBot Scanner] Scanned and saved!");
                                    }
                                    catch
                                    {
                                        await Con.SendAsync(MessageType.Chat, "[ShiftBot Scanner] An error occured!");
                                    }
                                }
                                break;
                        }
                    }
                    break;
            }
        }
    }
}
