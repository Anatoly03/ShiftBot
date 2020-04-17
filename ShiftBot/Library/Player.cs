using System;

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
        public bool afk { get; set; }

        public Player(int _id, string _name)
        {
            Id = _id;
            Name = _name;
            IsMod = Name == "anatoly";
        }
    }

    public class Profile
    {
        public int GlobalId { get; set; }
        public int Wins { get; set; }
    }
}
