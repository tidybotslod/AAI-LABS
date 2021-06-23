using System.Text;

namespace AAI
{
    public class InteractiveFeature : PersonalizationFeature
    {
        public InteractiveFeature(PersonalizationFeature feature) : base(feature) {}
        public string InteractivePrompt
        {
            get
            {
                if (interactivePrompt == null)
                {
                    if (Prompt != null && Values != null && Values.Length > 0)
                    {
                        interactivePrompt = BuildPrompt(Prompt, Values);
                    }
                }
                return interactivePrompt;
            }
        }

        // Private methods, not publicly accessible.
        static private string BuildPrompt(string prompt, string[] entries)
        {
            StringBuilder ask = new StringBuilder($"{prompt} (enter number or Q to quit)?", 256);
            if (entries.Length > 0)
            {
                ask.Append($"\n1. {entries[0]}");
                for (int i = 1; i < entries.Length; i++)
                {
                    ask.Append($"\n{i + 1}. {entries[i]}");
                }
            }
            return ask.ToString();
        }
        // Private members
        private string interactivePrompt;
    }
}