using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.CognitiveServices.Personalizer;
using Microsoft.Azure.CognitiveServices.Personalizer.Models;

namespace AAI
{
    /// <summary>
    /// Interacts with an Azure Personalizer Service
    /// </summary>
    public partial class PersonalizerService
    {
        /// <summary>
        /// Authoring key for the personalizer service hosted in Azure. 
        /// </summary>
        public String PersonalizerEndpointKey { set; get; }
        /// <summary>
        /// Endpoint resource name, used to construct the http address of the personalizer service hosted in Azure (e.g., https://{PersonalizerResourceName}.cognitiveservice.azure.com/... ).
        /// </summary>
        public String PersonalizerResourceName { set; get; }
        /// <summary>
        /// Constructor for Personalizer service, requires the endpoint key and resource name to be set prior to accessing the Azure Personalizer service.
        /// </summary>
        public PersonalizerService()
        { }
        /// <summary>
        /// Constructor for PersonalizerService, requires the endpoint key and resource name to be set prior to accessing the Azure Personalizer service.
        /// The endpoint key and resource name can found from the App Service for the Personalizer via the Azure portal. These values are typically added
        /// to the environment and retrieved from there.
        /// </summary>
        /// <param name="endpointKey">Security key for Asure Personalizer service</param>
        /// <param name="endpointResourceName">Resource name for Asure Personalizer srevice</param>
        /// <example>
        /// Create a PersonalizerService using environment variables set to the access key and resource name. 
        /// The access key can be obtained from the Azure portal Home -> Personalizer App Service -> Configuration.
        /// For local services these values are added to the local.settings.json and added to as environment variables
        /// for the Azure service that is hosting the PersonalizerService class. 
        ///        
        /// <code>
        /// private static string ProcessConfig(string key) 
        ///   { return System.Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process); }
        /// 
        /// AAI.PersonalizerService service = new AAI.PersonalizerService(
        ///     ProcessConfig("PersonalizerEndpointKey"),
        ///     ProcessConfig("PersonalizerResourceName"));
        /// </code>
        /// </example>
        public PersonalizerService(string endpointKey, string endpointResourceName)
        {
            PersonalizerEndpointKey = endpointKey;
            PersonalizerResourceName = endpointResourceName;
        }
        /// <summary>
        /// The Azure Peronsalizer client object, used to rank and reward actons. (https://docs.microsoft.com/en-us/python/api/azure-cognitiveservices-personalizer/azure.cognitiveservices.personalizer.personalizer_client.personalizerclient?view=azure-python)
        /// </summary>
        /// <value>
        /// Instance of Azure SDK PersonalizerClient object 
        /// </value>
        public PersonalizerClient Client
        {
            get
            {
                return client ?? CreatePersonalizer();
            }
        }
        /// <summary>
        /// Get the features names, ex. "Texture", "
        /// </summary>
        /// <returns>Array of Feature names</returns>
        public string[] AvailableFeatures()
        {
            var keys = Lookup.Keys;
            if(keys.Count == 0)
            {
                return null;
            }
            var result = new String[keys.Count];
            keys.CopyTo(result, 0);
            return result;
        }
        /// <summary>
        /// Load features from file containing JSON objects of the form:
        /// [
        ///   {
        ///     "Name": "Location",
        ///     "Prompt": "What room is this for?",
        ///     "Values":     [
        ///       "Living room",
        ///       "Bedroom",
        ///       "Family room",
        ///       "Kitchen"
        ///     ]
        ///   }
        /// ]
        /// </summary>
        /// <param name="featureFile">File name containing an array of JSON objects</param>
        public void LoadFeatures(string featureFile)
        {
            try
            {
                string input = File.ReadAllText(featureFile);
                if (input != null && input.Length > 0)
                {
                    Features = JsonSerializer.Deserialize<List<PersonalizationFeature>>(input).ToArray();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
        /// <summary>
        /// The users selects a value from the list defined for the feature. The features must be preloaded and the feature must exist otherwise null is returned.
        /// </summary>
        /// <param name="name">Feature name</param>
        /// <returns>selected value, "I" to ignore, "Q" to Quit, null as error</returns>
        public string SelectFeatureInteractively(string name)
        {
            InteractiveFeature feature = LookupFeature(name);
            if (feature == null)
            {
                return null;
            }

            do
            {
                Console.WriteLine(feature.InteractivePrompt);
                string entry = GetKey();
                Console.WriteLine();
                if (!int.TryParse(entry, out int index) || index < 1 || index > feature.Values.Length)
                {

                    if (entry[0] == 'Q' || entry[0] == 'q')
                    {
                        return "Q";
                    }
                    else if (entry[0] == 'I' || entry[0] == 'I')
                    {
                        return "I";
                    }
                    Console.WriteLine("Invalid selection!\n");
                }
                else
                {
                    return feature.Values[index - 1];
                }
            } while (true);
        }
        /// <summary>
        /// Interatively asks for feature values to rank the actions. Rewards based on whether the returned action is the one the user expected.
        /// Uses console input/output for training.
        /// </summary>
        /// <param name="select">Features to use in selection process</param>
        /// <param name="ignore">List of actions to ignore</param>
        public void InteractiveTraining(string[] select, string[] ignore)
        {
            if (Actions == null || Actions.Count == 0)
            {
                Console.WriteLine("Nothing to select.");
                return;
            }

            if (select == null || select.Length == 0)
            {
                Console.WriteLine("No features selected.");
                return;
            }

            int lessonCount = 1;
            do
            {
                Console.WriteLine($"Lesson {lessonCount++}");

                // Build context list by creating a JSON string and then convert it to a list of objects.
                string[] answers = new string[select.Length];
                for (int i = 0; i < select.Length; i++)
                {
                    answers[i] = SelectFeatureInteractively(select[i]);
                    if (answers[i] == "Q")
                    {
                        // When null is returned the training session is over.
                        return;
                    }
                }
                IList<Object> contextFeatures = FeatureList(select, answers);

                // Create an id for this lesson, used when setting the reward.
                string lessonId = Guid.NewGuid().ToString();

                // Create a list of Personalizer.Actions that should be excluded from the ranking
                List<string> excludeActions = null;
                if (ignore != null && ignore.Length > 0)
                {
                    excludeActions = new List<string>(ignore);
                }

                // Create the rank requester
                var request = new RankRequest(Actions, contextFeatures, excludeActions, lessonId, false);
                RankResponse response = null;
                response = Client.Rank(request);
                //response = new RankResponse();
                Console.WriteLine($"Personalizer service thinks you would like to have: {response.RewardActionId}. Is this correct (y/n)?");
                string answer = GetKey();
                Console.WriteLine();
                double reward = 0.0;
                if (answer == "Y")
                {
                    reward = 1.0;
                    Client.Reward(response.EventId, new RewardRequest(reward));
                    Console.WriteLine($"Set reward: {reward}");
                }
                else if (answer == "N")
                {
                    Client.Reward(response.EventId, new RewardRequest(reward));
                    Console.WriteLine($"Set reward: {reward}");
                }
                else
                {
                    Console.WriteLine("Entered choice is invalid. Not setting reward.");
                }
            } while (true);
        }
        /// <summary>
        /// Given an array of features and values for the features create a list suitable for the
        /// personalizer service.
        /// </summary>
        /// <param name="select">Array of feature names</param>
        /// <param name="answers">Array of values for each feature</param>
        /// <returns>a generic List of objects that can be used when calling the personalizer service</returns>
        public IList<object> FeatureList(string[] select, string[] answers)
        {
            if ((select == null || select.Length == 0) ||
                (answers == null || answers.Length == 0) ||
                (answers.Length != select.Length))
            {
                return null;
            }
            StringBuilder contextFeaturesJson = new StringBuilder("[");
            contextFeaturesJson.Append($"{{ \"{select[0]}\": \"{answers[0]}\" }}");
            for (int i = 1; i < select.Length; i++)
            {
                contextFeaturesJson.Append($",{{ \"{select[i]}\": \"{answers[i]}\" }}");
            }
            contextFeaturesJson.Append("]");
            return JsonSerializer.Deserialize<List<object>>(contextFeaturesJson.ToString());
        }
        /// <summary>
        /// Train a personalizer service using a set of features and an expected result.
        /// </summary>
        /// <param name="cases"></param>
        /// <example>
        /// Load a JSON training file and submit for training.
        /// <code>
        /// string input = File.ReadAllText(trainingFile);
        /// if (input != null &amp;&amp; input.Length &gt; 0)
        /// {
        ///      TrainingCase[] trainingData = JsonSerializer.Deserialize&lt;TrainingCase[]&gt;(input);
        ///      Personalizer.Train(trainingData);
        /// }
        ///</code>
        ///</example>
        ///
        public void Train(TrainingCase[] cases)
        {
            if (cases != null)
            {
                foreach (TrainingCase trainingCase in cases)
                {
                    string lessonId = Guid.NewGuid().ToString();
                    var request = new RankRequest(Actions, trainingCase.Features, trainingCase.Exclude, lessonId, false);
                    RankResponse response = Client.Rank(request);
                    double reward = 0.0;
                    if (response.RewardActionId.Equals(trainingCase.Expected))
                    {
                        reward = 1.0;
                    }
                    Client.Reward(response.EventId, new RewardRequest(reward));
                }
            }
        }
    }
}