using System;
using System.Collections.Generic;
using System.Text;

namespace ShiftBot
{
    public class Command
    {
        private string[] _args;
        public Player Sender { get; private set; }
        public string Cmd { get; private set; }

        /// <summary>
        /// Returns the argument at the selected index.
        /// </summary>
        /// <param name="i"> Index of the argument. </param>
        public string this[int i]
        {
            get => _args[i];
            set { _args[i] = value; }
        }

        public uint ArgNum => _args == null ? 0 : (uint)_args.Length;

        public Command(Player sender, string fullCommand)
        {
            Sender = sender;

            string[] s = fullCommand.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            Cmd = s[0].Substring(1);
            if (s.Length > 1)
            {
                _args = new string[s.Length - 1];
                for (int i = 0; i < _args.Length; i++)
                    _args[i] = s[i + 1];
            }
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        public void Execute() => Sender.OnMessage(this);

        public override string ToString()
        {
            if (_args != null)
                return string.Format("Command: {0}, Args: {1}", Cmd, string.Join(", ", _args));
            return string.Format("Command: {0}", Cmd);
        }
    }
}
