using System.Text;

namespace AAI
{
    /// <summary>
    /// Used to display a feature on the console when an interactive training session takes place.
    /// </summary>
    internal class InteractiveFeature : PersonalizationFeature
    {
     /// <summary>
     /// Create a console prompt for a personalizer feature based on its possible settings. Use this class when doing console based user interaction such as training.
     /// </summary>
     /// <param name="feature"></param>
     internal InteractiveFeature(PersonalizationFeature feature) : base(feature) {}
        /// <summary>
        /// Return a prompt for selecting one of the entries of a feature.
        /// </summary>
        internal string InteractivePrompt
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
        /// <summary>
        /// Use a feature's entries to create an enumerated list, the list is returned as a string.
        /// </summary>
        /// <param name="prompt"></param>
        /// <param name="entries"></param>
        /// <returns>string containing an enumerated list of entries</returns>
        static private string BuildPrompt(string prompt, string[] entries)
        {
            StringBuilder ask = new StringBuilder($"{prompt} (enter number, I to ignore, or Q to quit)?", 256);
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
        private string interactivePrompt;
    }
}