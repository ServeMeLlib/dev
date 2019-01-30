namespace ServeMeLib
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    public class ProgramX
    {
        readonly string[] keywords;

        public ProgramX(string[] keywords)
        {
            this.keywords = keywords;
        }

        public string RunProgram()
        {
            var builder = new StringBuilder();
            ConsoleKeyInfo capturedCharacter = Console.ReadKey(true);

            while (EnterIsNotThe(capturedCharacter))
            {
                KeyInput key = KeyInput.GetKey(builder, this.keywords, capturedCharacter);
                builder = key.UpdateBuilder();
                key.Print();

                capturedCharacter = Console.ReadKey(true);
            }

            //Console.Write(capturedCharacter.KeyChar);
            Console.WriteLine("");
            return builder.ToString();
        }

        static bool EnterIsNotThe(ConsoleKeyInfo capturedCharacter)
        {
            return capturedCharacter.Key != ConsoleKey.Enter;
        }

        public abstract class KeyInput
        {
            readonly StringBuilder builder;
            readonly string[] keyWords;

            public KeyInput(StringBuilder builder, string[] keyWords)
            {
                this.builder = builder;
                this.keyWords = keyWords;
            }

            public abstract StringBuilder UpdateBuilder();

            public abstract void Print();

            #region Factory

            public static KeyInput GetKey(StringBuilder builder, string[] keywords, ConsoleKeyInfo keyInput)
            {
                if (keyInput.Key == ConsoleKey.Tab)
                {
                    KeyInput input = new TabInput(builder, keywords);
                    return input;
                }
                else
                {
                    if (keyInput.Key == ConsoleKey.Backspace && builder.ToString().Length > 0)
                    {
                        // Perform Calculation (nothing here)
                        KeyInput input = new BackspaceInput(builder, keywords);
                        return input;
                    }
                    else
                    {
                        // Perform calculation (nothing here)
                        KeyInput input = new StandardKeyInput(builder, keywords, keyInput.KeyChar);
                        return input;
                    }
                }
            }

            #endregion Factory

            #region Implementations

            public class TabInput : KeyInput
            {
                public string Match;

                public TabInput(StringBuilder builder, string[] keyWords)
                    : base(builder, keyWords)
                {
                    // Perform calculation
                    this.Match = this.extractMatch(this.builder);
                }

                string extractMatch(StringBuilder builder)
                {
                    string match = this.keyWords.FirstOrDefault(item => item != builder.ToString() && item.StartsWith(builder.ToString(), true, CultureInfo.InvariantCulture));

                    if (string.IsNullOrEmpty(match))
                        return "";
                    else
                        return match;
                }

                public override void Print()
                {
                    this.ClearCurrentLine();

                    Console.Write(this.Match);
                }

                void ClearCurrentLine()
                {
                    int currentLine = Console.CursorTop;
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, currentLine);
                }

                public override StringBuilder UpdateBuilder()
                {
                    // Alter the builder
                    this.builder.Clear();
                    this.builder.Append(this.Match);

                    return this.builder;
                }
            }

            public class StandardKeyInput : KeyInput
            {
                readonly char key;

                public StandardKeyInput(StringBuilder builder, string[] keyWords, char key)
                    : base(builder, keyWords)
                {
                    this.key = key;
                }

                public override StringBuilder UpdateBuilder()
                {
                    this.builder.Append(this.key);
                    return this.builder;
                }

                public override void Print()
                {
                    // Print Reuslts
                    Console.Write(this.key);
                }
            }

            public class BackspaceInput : KeyInput
            {
                public BackspaceInput(StringBuilder builder, string[] keyWords)
                    : base(builder, keyWords)
                {
                }

                public override StringBuilder UpdateBuilder()
                {
                    // Alter the builder
                    this.builder.Remove(this.builder.Length - 1, 1);
                    return this.builder;
                }

                public string StringToPrint()
                {
                    return this.builder.ToString().Remove(this.builder.ToString().Length - 1);
                }

                public override void Print()
                {
                    // Print Results
                    this.ClearCurrentLine();
                    Console.Write(this.StringToPrint());
                }

                void ClearCurrentLine()
                {
                    int currentLine = Console.CursorTop;
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write(new string(' ', Console.WindowWidth));
                    Console.SetCursorPosition(0, currentLine);
                }
            }

            #endregion Implementations
        }
    }
}