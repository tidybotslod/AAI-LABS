using System.Text;

namespace AAI
{
    ///<summary>
    /// A feature is information about the item or the context that describe aggregatable information such as textures and colors. The name
    /// or a specific time is not aggregatable information so cannot be a feature. 
    ///</summary>
    internal class PersonalizationFeature
    {
        internal PersonalizationFeature() {}
        internal PersonalizationFeature(PersonalizationFeature other)
        {
            Name = other.Name;
            Prompt = other.Prompt;
            Values = other.Values;
        }

        internal string Name { get; set; }
        internal string Prompt { get; set; }
        internal string[] Values { get; set; }
    }
}
