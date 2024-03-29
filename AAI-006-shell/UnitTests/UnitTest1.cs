using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;
using Newtonsoft.Json;

using AAI;

namespace UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        private static string url;
        private static QnAService service;
        private static KeySentiments analyzer;
        private static string ConfigurationValue(IConfiguration config, string name)
        {
            string value = config[name];
            if (value != null && value.Length == 0)
            {
                value = null;
            }
            return value;
        }

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            IConfiguration config; // Load configuration data found in appsettings.json, need Azure authoring key and resource name to build URL to azure.
            config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
            // text analyzer
            analyzer = new KeySentiments
            {
                Key = ConfigurationValue(config, "TextAnalyticsKey"),
                ResourceName = ConfigurationValue(config, "TextAnalyticsResourceName")
            };
            // setup QnA Maker service
            service = new QnAService
            {
                AuthoringKey = ConfigurationValue(config, "AuthoringKey"),
                ResourceName = ConfigurationValue(config, "ResourceName"),
                ApplicationName = ConfigurationValue(config, "ApplicationName"),
                KnowledgeBaseID = ConfigurationValue(config, "KnowledgeBaseID"),
                QueryEndpointKey = ConfigurationValue(config, "QueryEndpointKey")
            };
            // set up function defaults
            url = ConfigurationValue(config, "FunctionUrl");
        }

#if (TextAnalyzer)
        [TestMethod]
        public void PerformSentimentTest()
        {
            string input = "The quick brown fox jumps over the lazy dog";
            string[] answer = "Negative, 0.00, 0.99, 0.01, \"quick brown fox jumps\", \"lazy dog\"".Split(',');
            using (System.IO.MemoryStream memory = new System.IO.MemoryStream())
            {
                System.IO.StreamWriter writer = new System.IO.StreamWriter(memory, Console.OutputEncoding);
                analyzer.Sentiment(input, writer);
                memory.Seek(0, System.IO.SeekOrigin.Begin);
                System.IO.StreamReader reader = new System.IO.StreamReader(memory, Console.InputEncoding);
                string[] result = reader.ReadLine().Split(',');
                Assert.AreEqual(answer[0], result[0]);
				Assert.IsTrue((result.Length > 3));
            }
        }
#endif
#if (CreateFAQ)
        [TestMethod()]
        public void CreateFAQ()
        {
            try
            {
                Task.Run(async () => { Assert.IsTrue(await CreateDatabase()); }).Wait();
                Task.Run(async () => { Assert.IsTrue(await PublishDatabase()); }).Wait();
                Console.WriteLine($"Knowledge base id: {service.KnowledgeBaseID}");
                Task.Run(async() =>
                {
                    string queryString = await service.QueryKey();
                    Console.WriteLine($"Query end point: {queryString}");
                }).Wait();
            }
            finally
            {
                Console.WriteLine("Created and published FAQ for Customer Support");
            }
        }
        //
        // Create QnA knowledge base.
        // Will contain one Question and Answer, the answer has temporary text that will be removed in the update test.
        private async Task<bool> CreateDatabase()
        {
            Guid name = Guid.NewGuid();
            var create = new CreateKbDTO
            {
                Name = $"{name} Custom Support KB",
                QnaList = QnAFile.LoadCSV("..\\..\\..\\..\\Data\\full-faq.csv")
            };

            var (status, id) = await service.CreateQnA(create);
            Assert.AreEqual(OperationStateType.Succeeded, status);
            Assert.IsNotNull(id);
            service.KnowledgeBaseID = id;
            await service.Publish();
            return true;
        }
        //
        // Publish the QnA knowledge base.
        private async Task<bool> PublishDatabase()
        {
            await service.Publish();
            return true;
        }
#endif
#if (CallFunctions)

        class CustomerSupportRequest
        {
            public CustomerSupportRequest()
            {
                Rating = null;
                Question = null;
            }
            public string Rating;
            public string Question;
        }
        class CustomerSupportResponse
        {
            public CustomerSupportResponse()
            {
                Sentiment = null;
                Answer = null;
            }
            public string Sentiment;
            public QnASearchResultList Answer;
        }
        [TestMethod]
        public async Task RatingTest()
        {
            //string url = "<remote function site - from portal>/api/CustomerSupportService";
            CustomerSupportRequest test = new CustomerSupportRequest
            {
                Rating = "The quick brown fox jumps over the lazy dog"
            };

            string[] answer = "Negative, 0.00, 0.99, 0.01, \"quick brown fox jumps\", \"lazy dog\"".Split(',');
            Uri site = new Uri(url + "api/CustomerSupport");
            var client = new HttpClient();
            var response = await client.PostAsync(site, new StringContent(JsonConvert.SerializeObject(test), System.Text.Encoding.UTF8, "application/json"));
            var data = await response.Content.ReadAsStringAsync();
            //
            // Reading as a string instead of reading as a line leaves extra characters on the end, remove them.
            var result  = JsonConvert.DeserializeObject<CustomerSupportResponse>(data);
            string[] sentiment = result.Sentiment.Trim(new char[] { '\r', '\n' }).Split(',');
            Assert.AreEqual(answer[0], sentiment[0]);
			Assert.IsTrue((sentiment.Length > 3));
        }

        [TestMethod]
        public async Task QuestionTest()
        {
            //string url = "<remote function site - from portal>/api/CustomerSupportService";
            CustomerSupportRequest test = new CustomerSupportRequest
            {
                Question = "What perks do I get for shopping with you?"
            };

            Uri site = new Uri(url + "api/CustomerSupport");
            var client = new HttpClient();
            var response = await client.PostAsync(site, new StringContent(JsonConvert.SerializeObject(test), System.Text.Encoding.UTF8, "application/json"));
            var data = await response.Content.ReadAsStringAsync();
            //
            // Reading as a string instead of reading as a line leaves extra characters on the end, remove them.
            var result  = JsonConvert.DeserializeObject<CustomerSupportResponse>(data);
            Assert.IsNotNull(result);

            var answer = result.Answer.Answers;
            Assert.IsNotNull(answer);
            Assert.AreEqual(1, answer.Count);
            bool found = answer[0].Answer.Contains("Because you are important to us");
            Assert.IsTrue(found);
        }
#endif
    }
}
