﻿using System;
using System.Drawing;
using Console = Colorful.Console;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using static ShiftBot.Program;

namespace ShiftBot
{
    /// <summary>
    /// The object Player is 
    /// </summary>
    public class Player
    {
        public delegate Task MessageHandler(Command c);

        public MessageHandler OnMessage;

        public int Id { get; set; }
        public string Name { get; set; }
        public bool IsMod { get; set; }
        public bool Afk { get; set; }
        public int Vote { get; set; }

        public Player(int _id, string _name)
        {
            Id = _id;
            Name = _name;
            Vote = -1;

            if (Profiles.ContainsKey(Name))
            {
                Profile p = Profiles[Name];
                IsMod = p.IsMod;
            }
            else
            {
                IsMod = false;

                Profiles.Add(Name, new Profile(Name));
                saveProfiles();
            }
        }

        /// <summary>
        /// Sends a private message to the player who sent the command.
        /// </summary>
        /// <param name="message"> The message. </param>
        public async Task Tell(string message) => await SayPrivate(Name, message);

        public async static Task DefaultMessageHandler(Command c)
        {
            Player player = c.Sender;
            Profile profile = Profiles[c.Sender.Name];

            switch (c.Cmd)
            {
                case "ping":
                    await player.Tell("Pong!");
                    break;

                case "help":
                    await player.Tell(".aminoob(nub/n00b/neeb/newb) - shows your noob percentage");
                    await player.Tell(".isnoob(nub/n00b/neeb/newb) player - shows the noob percentage of another player");
                    await player.Tell(".stats, .s <profile?> - shows player staticstics");
                    break;

                case "stats":
                case "s":
                    if (c.ArgNum > 0)
                    {
                        string p = c[0].ToLower();
                        if (Profiles[p] != null)
                        {
                            await player.Tell($"{Profiles[p].Name.ToUpper()} has: {Profiles[p].Wins} wins, {Profiles[p].Points} points, {Profiles[p].Plays} plays");
                        }
                        else
                        {
                            await player.Tell($"{c[0].ToUpper()} does not exist? Typo?");
                        }
                    }
                    else
                        await player.Tell($"You have: {profile.Wins} wins, {profile.Points} points, {profile.Plays} plays");
                    break;

                case "aminoob":
                case "aminub":
                case "amin00b":
                case "amineeb":
                case "aminewb":
                    {
                        int i = new Random(player.Name.GetHashCode()).Next(0, 101);
                        await player.Tell($"You're a noob with a probability of {i}%");
                    }
                    break;

                case "afk":
                    if (player.Afk)
                    {
                        player.Afk = false;
                        await player.Tell($"You are no longer afk!");
                    }
                    else
                    {
                        player.Afk = true;
                        await player.Tell($"You are now afk!");
                    }
                    break;
            }
        }

        public async static Task AdminMessageHandler(Command c)
        {
            Player player = c.Sender;
            switch (c.Cmd)
            {
                case "help":
                    await player.Tell(".fullname, .name - set the world name to.. (.name renames the prefix)");
                    await player.Tell(".scanner worldid - opens a scanner in a new world");
                    await player.Tell(".kill, .start, .resume - continue the game process (eliminate players or start game)");
                    await player.Tell(".stop, .pause - abort the game process");
                    await DefaultMessageHandler(c); //  For complete informations without rewriting code.
                    break;

                case "fullname":
                    string fulltitle = c[0];
                    await SayCommand($"title {fulltitle}");

                    Console.Write("@ ", Color.BlueViolet);
                    Console.Write(player.Name, Color.Orange);
                    Console.Write($" set title to ");
                    Console.WriteLine(fulltitle, Color.Cyan);
                    break;

                case "name":
                    string title = c[0];
                    await SayCommand($"title {title}: CC Shift");

                    Console.Write("@ ", Color.BlueViolet);
                    Console.Write(player.Name, Color.Orange);
                    Console.Write($" set title to ");
                    Console.WriteLine(title + ": CC Shift", Color.Cyan);
                    break;

                case "kill":
                case "start":
                case "resume":
                    Console.Write("@ ", Color.BlueViolet);
                    Console.Write(player.Name, Color.Orange);

                    await Task.Run(async () =>
                    {
                        if (!isBuilding)
                        {
                            if (PlayersInGame.Count > 0)
                                if (PlayersSafe.Count == 0)
                                {
                                    Console.WriteLine(" forced to eliminate everyone!");
                                    await Say($"Round was forced to end! Everyone's eliminated!");
                                }
                                else if (PlayersSafe.Count == 1)
                                {
                                    Console.WriteLine(" forced to end the round!");
                                    await Say($"Round was forced to end! {PlayersSafe.ElementAt(0).Key.Name.ToUpper()} won!");

                                    if (!isTrainMode)
                                    {
                                        Profiles[PlayersSafe.ElementAt(0).Key.Name].Wins++;
                                        saveProfiles();
                                    }
                                }
                                else
                                {
                                    Console.WriteLine(" forced to end the round!");
                                    await Say($"Round was forced to end! Players not finished are eliminated!");
                                }
                            else
                                Console.WriteLine(" started the game!");
                            await ContinueGame();
                        }
                    });
                    break;

                case "stop":
                case "abort":
                case "pause":
                    Console.Write("@ ", Color.BlueViolet);
                    Console.Write(player.Name, Color.Orange);
                    Console.WriteLine(" aborted the game", Color.Cyan);
                    await AbortGame();
                    break;

                case "clear":
                    Console.Write("@ ", Color.BlueViolet);
                    Console.Write(player.Name, Color.Orange);
                    Console.WriteLine(" cleared the map!", Color.Cyan);
                    await ClearGameArea();
                    break;

                case "build":
                    if (c.ArgNum > 0)
                    {
                        Console.Write("@ ", Color.BlueViolet);
                        Console.Write(player.Name, Color.Orange);
                        Console.WriteLine(" built map", Color.Cyan);
                        await BuildMap(int.Parse(c[0]));
                    }
                    break;

                case "scanner":
                    if (c.ArgNum > 0)
                    {
                        Console.Write("@ ", Color.BlueViolet);
                        Console.Write(player.Name, Color.Orange);
                        Console.WriteLine(" proposed to open a scanner", Color.Cyan);
                        await OpenScanner(c[0], player);
                    }
                    break;

                case "regen":
                    switch (c[0])
                    {
                        case "maps":
                            await RegenerateMapVoters();
                            break;
                    }
                    break;

                default:
                    await DefaultMessageHandler(c);
                    break;
            }
        }
    }

    public class Profile
    {
        public string Name { get; set; }
        public bool IsMod { get; set; }
        public int Plays { get; set; }
        public int Points { get; set; }
        public int Rounds { get; set; }
        public int Wins { get; set; }

        public Profile(string _name)
        {
            Name = _name;
            IsMod = false;
            Plays = 0;
            Points = 0;
            Rounds = 0;
            Wins = 0;
        }
    }
}
