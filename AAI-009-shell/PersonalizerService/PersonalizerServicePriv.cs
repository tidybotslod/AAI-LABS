using System;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Personalizer;
using Microsoft.Azure.CognitiveServices.Personalizer.Models;

namespace AAI
{
    /// <summary>
    /// Add a string extension function.
    /// </summary>
    static class Extensions
    {
        /// <summary>
        /// Return a specified number of characters from the end of a string
        /// </summary>
        /// <param name="source">Source string</param>
        /// <param name="numberOfChars">Number of characters to return from the end of the source string</param>
        /// <returns>String containing the specified number of characters or an empty string if not enough characters are available</returns>
        public static string Last(this string source, int numberOfChars = 1)
        {
            if (source == null || numberOfChars > source.Length)
            {
                return "";
            }
            return source.Substring(source.Length - numberOfChars);
        }
    }

    /// <summary>
    /// Service object that manages a connection to the Azure Personalizer for a set of Actions and Features. The action returned is based on past selections and rewards.
    /// </summary>
    public partial class PersonalizerService
    {
        /// <summary>
        /// Actions that are Ranked.
        /// </summary>
        internal List<RankableAction> Actions { get; set; }
        /// <summary>
        /// List of features used to define the context for the ranking actions.
        /// </summary>
        internal PersonalizationFeature[] Features
        {
            get
            {
                return features;
            }
            set
            {
                Dictionary<string, InteractiveFeature> lookup = new Dictionary<string, InteractiveFeature>();
                if (value != null)
                {
                    foreach (PersonalizationFeature entry in value)
                    {
                        lookup.Add(entry.Name, new InteractiveFeature(entry));
                    }
                }
                features = value;
                Lookup = lookup;
            }
        }
        // Private helper methods and data.
        private PersonalizerClient CreatePersonalizer()
        {
            string Endpoint = $"https://{PersonalizerResourceName}.cognitiveservices.azure.com";
            client = new PersonalizerClient(
             new ApiKeyServiceClientCredentials(PersonalizerEndpointKey))
            { Endpoint = Endpoint };
            return client;
        }
        private string GetKey()
        {
            return Console.ReadKey().Key.ToString().Last().ToUpper();
        }
        private Dictionary<string, InteractiveFeature> Lookup { get; set; }
        private InteractiveFeature LookupFeature(string id)
        {
            InteractiveFeature result = null;
            if(Lookup != null)
            {
                result = Lookup[id];
            }
            return result;
        }
        /// <summary>
        /// Features are loaded from a JSON file, an interactive prompt is created that be displayed for a feature.
        /// </summary>
        private PersonalizationFeature[] features;
        private PersonalizerClient client;

    }

}


