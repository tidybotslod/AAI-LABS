using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;

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
            string answer = "Negative, 0.00, 0.99, 0.01, \"quick brown fox jumps\", \"lazy dog\"" ;
            using (System.IO.MemoryStream memory = new System.IO.MemoryStream())
            {
                System.IO.StreamWriter writer = new System.IO.StreamWriter(memory, Console.OutputEncoding);
                analyzer.Sentiment(input, writer);
                memory.Seek(0, System.IO.SeekOrigin.Begin);
                System.IO.StreamReader reader = new System.IO.StreamReader(memory, Console.InputEncoding);
                string result = reader.ReadLine();
                Assert.AreEqual(answer, result);
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
                Console.WriteLine($"Query end point: {service.QueryEndpointKey}");
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
#if (LocalFunction)

        [TestMethod]
        public async Task LocalPostTest()
        {
            //string url = "<remote function site - from portal>/api/CustomerSupportService";
            string test = "There is a happy dog barking in the forground.";
            string answer = "Neutral, 0.28, 0.06, 0.66, \"happy dog\", \"forground\"";
            Uri site = new Uri(url);
            var client = new HttpClient();
            var response = await client.PostAsync(site, new StringContent(test));
            var data = await response.Content.ReadAsStringAsync();
            //
            // Reading as a string instead of reading as a line leaves extra characters on the end, remove them.
            data = data.Trim(new char[] { '\r', '\n' });
            Assert.AreEqual(answer, data);
        }


        [TestMethod]
        public async Task LocalStreamTest()
        {
            //string url = "<remote function site - from portal>/api/CustomerSupportService";
            string[] test =
            {
                "There is a happy dog barking in the foreground. ",
                "There were problems with the packaging so the ACME radio side was cracked and the customer was not happy."
            };
            string[] answer =
            {
                "Neutral, 0.28, 0.06, 0.66, \"happy dog\", \"foreground\"",
                "Negative, 0.00, 1.00, 0.00, \"ACME radio side\", \"problems\", \"packaging\", \"customer\""
            };
            HttpClient client = new HttpClient();
            using (Stream input = new MemoryStream())
            {
                //
                // Write data to stream, flush, set stream position to start, data will be read from the start
                StreamWriter writer = new StreamWriter(input);
                foreach (string line in test)
                {
                    writer.Write(line);
                }
                writer.Flush();
                input.Seek(0, SeekOrigin.Begin);
                //
                // Create message, attach stream to content, send, and only wait for the header of the returned message.
                HttpRequestMessage request = new HttpRequestMessage();
                request.Content = new StreamContent(input);
                request.RequestUri = new Uri(url);
                HttpResponseMessage response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                //
                // Ensure the response indicates the call was successful before reading all the data.
                Assert.IsTrue(response.IsSuccessStatusCode);
                //
                // Stream response data as it becomes available and test against expected result.
                int lineNumber = 0;
                using (Stream data = await response.Content.ReadAsStreamAsync())
                {
                    StreamReader reader = new StreamReader(data);
                    while (reader.EndOfStream == false)
                    {
                        string result = await reader.ReadLineAsync();
                        Assert.AreEqual(answer[lineNumber++], result);
                    }
                }
                Assert.AreEqual(answer.Length, lineNumber);
            }
        }
#endif
    }
}
