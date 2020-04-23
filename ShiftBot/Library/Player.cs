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
                    await player.Tell("Pong!");
                    break;

                case "help":
                    await player.Tell(".aminoob - shows your noob percentage");
                    await player.Tell(".stats, .s - shows your player staticstics");
                    break;

                case "stats":
                case "s":
                    await player.Tell("You have 0 wins, 0 players, .. stats basically");
                    break;

                case "aminub":
                case "aminoob":
                case "amin00b":
                case "amineeb":
                case "aminewb":
                    {
                        int i = new Random(player.Name.GetHashCode()).Next(0, 101);
                        await player.Tell($"You're a noob with a probability of {i}%");
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
                    await player.Tell(".scan worldid - opens a scanner in a new world");
                    await player.Tell(".kill, .start - continue the game process (eliminate players or start game)");
                    await DefaultMessageHandler(c); //  For complete informations without rewriting code.
                    break;

                case "build":
                    if (c.ArgNum > 0)
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
                            await player.Tell($"Error! Either there is no map with that id, or this is not an integer.");
                        }
                    }
                    break;

                case "fullname":
                    string fulltitle = c[0];
                    await SayCommand($"title {fulltitle}");
                    break;

                case "name":
                    string title = c[0];
                    await SayCommand($"title {title}: CC Shift");
                    break;

                case "kill":
                case "start":
                    await Task.Run(async () =>
                    {
                        await ContinueGame();
                    });
                    break;

                case "scan":
                    if (c.ArgNum > 0)
                    {
                        await OpenScanner(c[0], player);
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
