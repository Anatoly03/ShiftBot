using System;
using System.IO;
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
                using (StreamWriter file = File.CreateText($"../../../profiles.json"))
                {
                    file.WriteLine(JsonConvert.SerializeObject(Profiles, Json_settings));
                }
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
            switch (c.Cmd)
            {
                case "ping":
                    await SayPrivate(player, "Pong!");
                    break;

                case "help":
                    if (player.IsMod)
                    {
                        await SayPrivate(player, ".fullname, .name - set the world name to.. (.name renames the prefix)");
                        await SayPrivate(player, ".scan worldid - opens a scanner in a new world");
                        await SayPrivate(player, ".kill, .start - continue the game process (eliminate players or start game)");
                    }
                    await SayPrivate(player, ".aminoob - shows your noob percentage");
                    await SayPrivate(player, ".stats, .s - shows your player staticstics");
                    break;

                case "stats":
                case "s":
                    await SayPrivate(player, "You have 0 wins, 0 players, .. stats basically");
                    break;

                case "aminub":
                case "aminoob":
                case "amin00b":
                case "amineeb":
                case "aminewb":
                    {
                        int i = new Random(player.Name.GetHashCode()).Next(0, 101);
                        await SayPrivate(player, $"You're a noob with a probability of {i}%");
                    }
                    break;

                case "build":
                    if (player.IsMod && c.ArgNum > 0)
                    {
                        try
                        {
                            await BuildMap(int.Parse(c[0]));
                            await OpenEntrance();
                            await Task.Delay(1000);
                            await CloseEntrance();
                        }
                        catch
                        {
                            await SayPrivate(player, $"Error! Either there is no map with that id, or this is not an integer.");
                        }
                    }
                    break;

                case "fullname":
                    if (player.IsMod)
                    {
                        string title = c[0];
                        await SayCommand($"title {title}");
                    }
                    break;

                case "name":
                    if (player.IsMod)
                    {
                        string title = c[0];
                        await SayCommand($"title {title}: CC Shift");
                    }
                    break;

                case "kill":
                case "start":
                    if (player.IsMod)
                    {
                        await Task.Run(async () =>
                        {
                            await ContinueGame();
                        });
                    }
                    break;

                case "scan":
                    if (player.IsMod && c.ArgNum > 0)
                    {
                        await OpenScanner(c[0], player);
                    }
                    break;
            }
        }

        public async static Task AdminMessageHandler(Command c)
        {
            await DefaultMessageHandler(c);
        }
    }

    public class Profile
    {
        public string Name { get; set; }
        public bool IsMod { get; set; }
        public int Wins { get; set; }
        public int Plays { get; set; }
        public int Points { get; set; }

        public Profile(string _name)
        {
            Name = _name;
            IsMod = false;
            Wins = 0;
            Plays = 0;
            Points = 0;
        }
    }
}
