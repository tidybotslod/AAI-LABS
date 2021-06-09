using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task LocalPostTest()
        {
            string url = "http://localhost:7071/api/KeySentiments";
            //string url = "<remote function site - from portal>/api/KeySentiments";
            string test = "There is a happy dog barking in the forground.";
            string answer = "Neutral, 0.28, 0.06, 0.66, \"happy dog barking\", \"forground\"";
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
            string url = "http://localhost:7071/api/KeySentiments";
            //string url = "<remote function site - from portal>/api/KeySentiments";
            string[] test =
            {
                "There is a happy dog barking in the foreground. ",
                "There were problems with the packaging so the ACME radio side was cracked and the customer was not happy."
            };
            string[] answer =
            {
                "Neutral, 0.28, 0.06, 0.66, \"happy dog barking\", \"foreground\"",
                "Negative, 0.00, 1.00, 0.00, \"ACME radio\", \"packaging\", \"customer\", \"problems\""
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
    }
}
