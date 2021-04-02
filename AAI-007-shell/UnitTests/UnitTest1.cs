#if (TestCreate)
#define CreateQnA
#endif
#if (TestAsk)
#define AskQnA
#endif

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;
using AAI;

namespace UnitTests
{

    [TestClass]
    public class UnitTest1
    {
        public static QnAService service;

        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            IConfiguration config; // Load configuration data found in appsettings.json, need Azure authoring key and resource name to build URL to azure.
            config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
            service = new QnAService
            {
                AuthoringKey = ConfigurationValue(config, "AuthoringKey"),
                ResourceName = ConfigurationValue(config, "ResourceName"),
                ApplicationName = ConfigurationValue(config, "ApplicationName"),
                KnowledgeBaseID = ConfigurationValue(config, "KnowledgeBaseID"),
                QueryEndpointKey = ConfigurationValue(config, "QueryEndpointKey")
            };
        }

        private async Task<bool> CreateDatabase()
        {
            Guid name = Guid.NewGuid();
            var create = new CreateKbDTO
            {
                Name = $"{name} Test KB",
                QnaList = QnAFile.LoadCSV("..\\..\\..\\..\\Data\\sample-faq.csv")
            };

            var (status, id) = await service.CreateQnA(create);
            Assert.AreEqual(OperationStateType.Succeeded, status);
            Assert.IsNotNull(id);
            Console.WriteLine($"Created QnA: {name}");
            Console.WriteLine($"Status:  {status}, Knowledge Base ID: {id}");
            var key = await service.QueryKey();
            Console.WriteLine($"QnA Authorization Key : {key}");
            service.KnowledgeBaseID = id;
            return true;
        }
        // Create QnA knowledge base.
        // Will contain one Question and Answer, the answer has temporary text that will be removed in the update test.
        private async Task<bool> AskQuestion(bool production)
        {
            // Question has unwanted text in it. Check for the unwanted text. Also question should
            // only return 1 match. The other Answer added should never match this question.
            QnASearchResultList answer = await service.Ask("HOW CAN I CHANGE MY SHIPPING ADDRESS?", production);
            Assert.IsNotNull(answer);
            Assert.IsNotNull(answer.Answers);
            Assert.AreEqual(1, answer.Answers.Count);
            Assert.IsNotNull(answer.Answers[0].Questions);
            Assert.AreEqual(1, answer.Answers[0].Questions.Count);
            return true;
        }

        //
        // Publish the QnA knowledge base.
        private async Task<bool> PublishDatabase()
        {
            await service.Publish();
            return true;
        }
        //
        // Delete the data base
        private async Task<bool> CleanUp()
        {
            await service.DeleteKnowledgeBase();
            return true;
        }


#if (CreateQnA)
        [TestMethod]
        public void Startup()
        {
            Task.Run(async () => { Assert.IsTrue(await CreateDatabase()); }).Wait();
            Task.Run(async () => { Assert.IsTrue(await AskQuestion(false)); }).Wait();
            Task.Run(async() => { Assert.IsTrue(await PublishDatabase()); }).Wait();
            Task.Run(async () => { Assert.IsTrue(await AskQuestion(true)); }).Wait();
        }
#endif

        private static string ConfigurationValue(IConfiguration config, string name)
        {
            string value = config[name];
            if (value != null && value.Length == 0)
            {
                value = null;
            }
            return value;
        }
    }
}
