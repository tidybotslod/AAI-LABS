using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;
using AAI;

namespace chat
{
    class Program
    {
        Uri WebSite;
        HttpClient Client;
        public async Task<CustomerChatResponse> postQnA(string question)
        {

        }
        public async Task<CustomerChatResponse> postSuggestion(List<object> list)
        {

        }
        
        public async Task sendQnA()
        {
            CustomerChatRequest request = new CustomerChatRequest();
            Console.Write("Enter question: ");
            string question = Console.ReadLine();
            CustomerChatResponse chat = await postQnA(question);
            foreach (QnASearchResult possiblity in chat.Answer.Answers)
            {
                Console.WriteLine($"Answer: {possiblity.Answer}");
                Console.WriteLine($"Score: {possiblity.Score}\n");
            }
        }
        public bool GetFeatureValue(string feature, List<string> select, List<string> answers)
        {
            var value = Personalizer.SelectFeatureInteractively(feature);
            if (value == null || value.Equals("Q"))
            {
                return false;
            }
            if (!value.Equals("I"))
            {
                select.Add(feature);
                answers.Add(value);
            }
            return true;
        }
        public async Task getSuggestion()
        {
            string[] features = Personalizer.AvailableFeatures();
            if (features != null)
            {
                try
                {
                    await SelectFeatures(features);
                }
                catch (Exception e)
                {
                    Console.Write(e.Message);
                }
            }
            else
            {
                Console.WriteLine("Features were not loaded.");
            }
        }
        private async Task SelectFeatures(string[] features)
        {
            Console.WriteLine("Choose feature's value, enter 'I' to ignore feature, enter 'Q' to stop adding features.");
            List<string> answers = new List<string>();
            List<string> select = new List<string>();
            foreach (string feature in features)
            {
                if (GetFeatureValue(feature, select, answers) == false)
                {
                    break;
                }
            }
            if (answers.Count > 0)
            {
                await CallPersonalizer(answers, select);
            }
        }
        private async Task CallPersonalizer(List<string> answers, List<string> select)
        {
            var list = Personalizer.FeatureList(select.ToArray(), answers.ToArray());
            CustomerChatResponse chat = await postSuggestion((List<object>) list);
            if (chat.Result != null && chat.Result.Ranking != null)
            {
                foreach (PersonalizerRankedAction action in chat.Result.Ranking)
                {
                    Console.WriteLine($"Id: {action.Id}, Propability: {action.Probability}");
                }
            }
        }
        PersonalizerService Personalizer;
        private string ConfigurationValue(IConfiguration config, string name)
        {
            string value = config[name];
            if (value != null && value.Length == 0)
            {
                value = null;
            }
            return value;
        }
        public Program()
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: false).Build();
            Personalizer = new PersonalizerService();
            WebSite = new Uri(ConfigurationValue(config, "RemoteUrl") + "/api/CustomerChat");
            Client = new HttpClient();
            Personalizer = new PersonalizerService(); // Used to get features, will not be used to directly communicate with personalizer service.
            //Personalizer.LoadFeatures(@"D:\LabFiles\AAI-009\Data\Features.json");
            Personalizer.LoadFeatures(@"c:\users\craig\repos\aai-009\data\features.json");
        }
        public void chat()
        {
            do
            {
                Console.WriteLine("1. Get a suggestion on a sample, 2. Ask a question, 3. quit (enter 1, 2 or 3)");
                string entry = Console.ReadLine();
                Console.WriteLine();
                if (!int.TryParse(entry, out int index))
                {
                    Console.WriteLine("Invalid selection!\n");
                }
                else
                {
                    switch (index)
                    {
                        case 1:
                            Task.Run(async () => { await getSuggestion(); }).Wait();
                            break;
                        case 2:
                            Task.Run(async () => { await sendQnA(); }).Wait();
                            break;
                        case 3:
                            return;
                        default:
                            Console.WriteLine("Select number 1, 2, or 3");
                            break;
                    }
                }
            } while (true);
        }
        static void Main(string[] args)
        {
            Program program = new Program();
            program.chat();
        }
    }
}
