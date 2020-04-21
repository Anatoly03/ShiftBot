using System;
using System.IO;
using Newtonsoft.Json;

namespace ShiftBot
{
    /// <summary>
    /// The object Player is 
    /// </summary>
    public class Player
    {
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

            if (Program.Profiles.ContainsKey(Name))
            {
                Profile p = Program.Profiles[Name];
                IsMod = p.IsMod;
            }
            else
            {
                IsMod = false;

                Program.Profiles.Add(Name, new Profile(Name));
                using (StreamWriter file = File.CreateText($"../../../profiles.json"))
                {
                    file.WriteLine(JsonConvert.SerializeObject(Program.Profiles, Program.Json_settings));
                }
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
