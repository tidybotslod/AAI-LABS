using System;
using System.Collections.Generic;

using System.IO;
using System.Text.Json;

using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Azure.CognitiveServices.Personalizer;
using Microsoft.Azure.CognitiveServices.Personalizer.Models;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;

using AAI;

/// <summary>
/// Define tests that can be run from the command line.
/// 1. Create test, must take an IConfiguration parameter. Configuration points to file addeded as command line parameter
/// 2. Add test to testlist. By convention, the key should be the same as the function name of the test.
/// 
/// Note: parameter handling in the main procedure is located in the private side of the Program class.
///
/// Stub: [replace <put test number here> with the number of the test.
//static async Task<TestResult> test<put test number here>(IConfiguration config)
//{
//    TestResult result = new TestResult();
//    try
//    {
//    }
//    catch (Exception testException)
//    {
//        result.AddException(testException.Message);
//    }
//    return result;
//}
/// </summary>
namespace Check
{

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

    public class TestRankResponse
    {
        public TestRankResponse() { }
        public IList<RankedAction> Ranking { get; set; }
        public string EventId { get; set; }
        public string RewardActionId { get; set; }
    }

    class CustomerChatResponse
    {
        public string Error { get; set; }
        public QnASearchResultList Answer { get; set; }
        public TestRankResponse Result { get; set; }
    }

    partial class Program
    {
        static async Task<TestResult> CheckQnA(string url)
        {
            TestResult result = new TestResult();
            try
            {
                CustomerChatRequest post = new CustomerChatRequest();
                post.Question = "How can I track my orders ?";
                post.Suggest = null;
                post.Rating = null;
                string input = JsonSerializer.Serialize(post);
                try
                {
                    TestResult send = await Post(url, input);
                    if (send.Fault)
                    {
                        return send;
                    }
                    CustomerChatResponse resp = JsonSerializer.Deserialize<CustomerChatResponse>(send.result);
                    int correctAnswerCount = 1;
                    result.AreEqual(resp.Answer.Answers.Count,
                                    1,
                                    $"Expected {correctAnswerCount} possible answer, QnA returned {resp.Answer.Answers.Count} possible answers.");
                }
                catch (Exception jsonException)
                {
                    result.AddException(jsonException.Message);
                }
            }
            catch (Exception testException)
            {
                result.AddException(testException.Message);
            }
            return result;
        }

        static async Task<TestResult> test1(IConfiguration config)
        {
            TestResult result = new TestResult();
            try
            {
                QnAService service = new QnAService
                {
                    AuthoringKey = GetConfigString(config, "AuthoringKey"),
                    ResourceName = GetConfigString(config, "ResourceName"),
                    ApplicationName = GetConfigString(config, "ApplicationName")
                    // Do not get Query End point key, force a retrieval from the servicea
                };
                string queryString = await service.QueryKey();
                if(queryString == null)
                {
                    result.AddError("QNA service is not configured");
                }
            }
            catch (Exception testException)
            {
                result.AddException(testException.Message);
            }
            return result;
        }

        static async Task<TestResult> test1a(IConfiguration config)
        {
            TestResult result = new TestResult();
            try
            {
                QnAService service = new QnAService
                {
                    AuthoringKey = GetConfigString(config, "AuthoringKey"),
                    ResourceName = GetConfigString(config, "ResourceName"),
                    ApplicationName = GetConfigString(config, "ApplicationName"),
                    KnowledgeBaseID = GetConfigString(config, "KnowledgeBaseID"),
                    QueryEndpointKey = GetConfigString(config, "QueryEndpointKey")
                };
                QnASearchResultList search = await service.Ask("HOW CAN I CHANGE MY SHIPPING ADDRESS", true, 8);
                if (search.Answers.Count < 1)
                {
                    result.AddError($"Expecting more than zero answers, returned {search.Answers.Count}");
                }
            }
            catch (Exception testException)
            {
                result.AddException(testException.Message);
            }
            return result;
        }
        static async Task<TestResult> test2(IConfiguration config)
        {
            string localUrl = GetConfigString(config, "LocalUrl");
            TestResult result = await CheckQnA(localUrl);
            return result;
        }
        static async Task<TestResult> test3(IConfiguration config)
        {
            string remoteUrl = GetConfigString(config, "RemoteUrl");
            TestResult result = await CheckQnA(remoteUrl);
            return result;
        }
        Program()
        {
            testList = new Dictionary<string, TestEntry>();
            testList.Add(
                "Test1",
                new TestEntry(
                    "Test existence of QnA Knowledge base",
                    test1));
            testList.Add(
                "Test1a",
                new TestEntry(
                    "Get a response from the QnA Knowledge base.",
                    test1a));
            testList.Add(
                "Test2",
                new TestEntry(
                    "Test local server for exact QnA response, posts to function.",
                    test2));
            testList.Add(
                "Test3",
                new TestEntry(
                     "Test remote server for exact QnA response, posts to function.",
                     test3));
        }
    }
}
