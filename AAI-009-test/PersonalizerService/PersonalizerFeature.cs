using System.Text;

namespace AAI
{
    ///<summary>
    /// A feature is information about the item or the context that describe aggregatable information such as textures and colors. The name
    /// or a specific time is not aggregatable information so cannot be a feature. 
    ///</summary>
    public class PersonalizationFeature
    {
        /// <summary>
        /// Create empty feature, JSON serialization and deserialization is the primary purpose for this constructor.
        /// </summary>
        public PersonalizationFeature() {}
        /// <summary>
        /// Copy construtor.
        /// </summary>
        /// <param name="other">Personalization feature to duplicate.</param>
        public PersonalizationFeature(PersonalizationFeature other)
        {
            Name = other.Name;
            Prompt = other.Prompt;
            Values = other.Values;
        }
        /// <summary>
        /// Name of the feature.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Prompt used to interactively choose one of the values.
        /// </summary>
        public string Prompt { get; set; }
        /// <summary>
        /// List of values the feature can be. 
        /// </summary>
        public string[] Values { get; set; }
    }
}
