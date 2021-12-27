using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.CognitiveServices.Personalizer;
using Microsoft.Azure.CognitiveServices.Personalizer.Models;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;
using AAI;

namespace CustomerChatTests
{
    [TestClass]
    public class Tests
    {
        private static string LocalUrl;
        private static string RemoteUrl;
        private static QnAService service;

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            IConfiguration config; // Load configuration data found in appsettings.json, need Azure authoring key and resource name to build URL to azure.
            config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
            service = new QnAService
            {
                AuthoringKey = GetConfigString(config, "AuthoringKey"),
                ResourceName = GetConfigString(config, "ResourceName"),
                ApplicationName = GetConfigString(config, "ApplicationName"),
                KnowledgeBaseID = GetConfigString(config, "KnowledgeBaseID"),
                QueryEndpointKey = GetConfigString(config, "QueryEndpointKey")
            };
            LocalUrl = GetConfigString(config, "LocalUrl");
            RemoteUrl = GetConfigString(config, "RemoteUrl");
        }

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
                    Console.WriteLine($"Query end point key: {queryString}");
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
                QnaList = QnAFile.LoadCSV("..\\..\\..\\..\\Data\\sample-faq.csv")
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
#if TestLocalCustomerChat
        [TestMethod]
        public async Task TestLocalCustomerChat()
        {
            await PostToCustomerChat(LocalUrl);
        }
#endif
#if TestRemoteCustomerChat
        [TestMethod]
        public async Task TestRemoteCustomerChat()
        {
            await PostToCustomerChat(RemoteUrl);
        }
#endif
#if TestService

        [TestMethod]
        public void TestService()
        {
            IConfiguration config; // Load configuration data found in appsettings.json, need Azure authoring key and resource name to build URL to azure.
            config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
            PersonalizerService personalizer = new PersonalizerService(
                GetConfigString(config, "PersonalizerEndpointKey"),
                GetConfigString(config, "PersonalizerResourceName"));
            RunTasteTest(personalizer);
        }
#endif
#if TestCreate

        [TestMethod]
        public void TestCreate()
        {
            Program program = new Program();
            RunTasteTest(program.Personalizer);
        }
#endif
#if TestFeatures
        [TestMethod]
        public void TestFeatures()
        {
            Program program = new Program();
            program.LoadFeatures(@"D:\LabFiles\AAI-008\Data\Features.json");
            Assert.AreEqual(5, program.Personalizer.Features.Length);
        }
#endif
#if TestActions
        [TestMethod]
        public void TestActions()
        {
            Program program = new Program();
            program.LoadActions(@"D:\LabFiles\AAI-008\Data\Actions.json");
            Assert.AreEqual(4, program.Personalizer.Actions.Count);

        }
#endif
#if TestTraining
        [TestMethod]
        public void TestTraining()
        {
            TrainingCase[] simple = new TrainingCase[]
            {
                new TrainingCase
                {
                    Name = "SimpleCase",
                    Features = new object[] { new { Location = "Bedroom", Color = "Pastel"} },
                    Exclude = new string[] { "Comfortable Sample" },
                    Expected = "Sleepy Sample"
                }
            };

            Program program = new Program();
            program.LoadActions(@"D:\LabFiles\AAI-008\Data\Actions.json");
            program.LoadFeatures(@"D:\LabFiles\AAI-008\Data\Features.json");
            program.Personalizer.Train(simple);
            // Ensures no exceptions are thrown.
        }
#endif
#if TestTrainingFile
        [TestMethod]
        public void TestTrainingFile()
        {
            Program program = new Program();
            program.LoadActions(@"D:\LabFiles\AAI-008\Data\Actions.json");
            program.LoadFeatures(@"D:\LabFiles\AAI-008\Data\Features.json");
            program.TrainingFile(@"D:\LabFiles\AAI-008\Data\Training.json");
            // Ensures no exceptions are thrown.
        }
#endif

        private static string GetConfigString(IConfiguration config, string key)
        {
            string result = config[key];
            if (result != null && result.Length == 0)
            {
                result = null;
            }
            return result;
        }

        private static void RunTasteTest(PersonalizerService personalizer)
        {
            IList<RankableAction> actions = new List<RankableAction>
            {
                new RankableAction
                {
                    Id = "pasta",
                    Features =
                    new List<object>() { new { taste = "salty", spiceLevel = "medium" }, new { nutritionLevel = 5, cuisine = "italian" } }
                },

                new RankableAction
                {
                    Id = "salad",
                    Features =
                    new List<object>() { new { taste = "salty", spiceLevel = "low" }, new { nutritionLevel = 8 } }
                }
            };

            string id = Guid.NewGuid().ToString();
            IList<object> currentContext = new List<object>() {
                new { time = "morning" },
                new { taste = "salty" }
            };

            IList<string> exclude = new List<string> { "pasta" };
            var request = new RankRequest(actions, currentContext, exclude, id);
            RankResponse resp = personalizer.Client.Rank(request);
            Assert.AreEqual("salad", resp.RewardActionId);
        }

        class CustomerRatingPost
        {
            public string Id { get; set; }
            public double Rank { get; set; }
        }

        class CustomerChatRequest
        {
            public String Question { get; set; }
            public List<object> Suggest { get; set; }
            public CustomerRatingPost Rating { get; set; }
        }

        // Ugh, redeclare the response to get around deserialization issue. RankResponse is
        // declared with getters only since the properties are readonly. This breaks deserialization
        // using 'System.Text.Json'. RankResponse has a hack for Newtonsoft which does not work for
        // system implementation.
        public class TestRankResponse
        {
            public TestRankResponse() { }
            public IList<RankedAction> Ranking { get; set;  }
            public string EventId { get; set;  }
            public string RewardActionId { get; set; }
        }

        class CustomerChatResponse
        {
            public string Error { get; set; }
            public QnASearchResultList Answer { get; set; }
            public TestRankResponse Result { get; set; }
        }

        public async Task PostToCustomerChat(string url)
        {
            string functionUrl = url + "/api/CustomerChat";
            await PostToQnA(functionUrl);
            await PostToPersonalizer(functionUrl);
        }
        public async Task PostToQnA(string functionUrl)
        {
            CustomerChatRequest post = new CustomerChatRequest();
            post.Question = "How can I track my orders ?";
            post.Suggest = null;
            post.Rating = null;
            string s = JsonSerializer.Serialize(post);
            Console.WriteLine(s);
            try
            {
                var client = new HttpClient();
                var response = await client.PostAsync(functionUrl, new StringContent(JsonSerializer.Serialize(post)));
                var data = await response.Content.ReadAsStringAsync();
                try
                {
                    CustomerChatResponse resp = JsonSerializer.Deserialize<CustomerChatResponse>(data);
                    Assert.IsNull(resp.Error);
                    Assert.AreEqual(null, resp.Result);
                    Assert.AreEqual(1, resp.Answer.Answers.Count);
                    Assert.AreEqual(5, resp.Answer.Answers[0].Id);
                }
                catch (Exception jsonException)
                {
                    Console.WriteLine("Got unexpected data back from function.");
                    Console.WriteLine(data);
                    Assert.IsNull(jsonException);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR connecting to {functionUrl}");
                Assert.AreEqual(null, e);
            }
        }
        public async Task PostToPersonalizer(string functionUrl)
        {
            CustomerChatRequest post = new CustomerChatRequest();
            post.Question = null;
            post.Suggest = new List<object> {
                new { texture = "Smooth" },
                new { style = "Modern" }
            };
            post.Rating = null;
            string s = JsonSerializer.Serialize(post);
            Console.WriteLine(s);
            try
            {
                var client = new HttpClient();
                var response = await client.PostAsync(functionUrl, new StringContent(JsonSerializer.Serialize(post)));
                var data = await response.Content.ReadAsStringAsync();
                try
                {
                    CustomerChatResponse resp = JsonSerializer.Deserialize<CustomerChatResponse>(data);
                    Assert.IsNull(resp.Error);
                    Assert.IsNull(resp.Answer);
                    Assert.IsTrue(resp.Result.Ranking.Count > 0);
                }
                catch (Exception jsonException)
                {
                    Console.WriteLine("Got unexpected data back from function.");
                    Console.WriteLine(data);
                    Assert.AreEqual(null, jsonException);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR connecting to {functionUrl}");
                Assert.AreEqual(null, e);
            }
        }
    }

}
