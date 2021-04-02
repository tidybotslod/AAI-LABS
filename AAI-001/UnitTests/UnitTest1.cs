using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using AAI;

namespace UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        private static KeySentiments analyzer;
        [AssemblyInitialize]
        public static void AssemblyInit(TestContext context)
        {
            analyzer = new KeySentiments();
        }

        [TestMethod]
        public void CheckAzureObject()
        {
            Assert.AreNotEqual(null, analyzer.AzureTextAnalyticsService);
        }

        [TestMethod]
        public void performKeyWordTest()
        {
            string input = "We love this trail and make the trip every year. The views are breathtaking and well worth the hike!";
            string[] answer = new string[] { "year", "trail", "trip", "views", "hike" };

            List<String> keyWords = analyzer.KeyWords(input);
            Assert.AreNotEqual(null, keyWords);
            int i;
            for (i = 0; i < keyWords.Count; i++)
            {
                Assert.AreEqual(answer[i], keyWords[i]);
            }
            Assert.AreEqual(answer.Length, i);
        }

        [TestMethod]
        public void performSentimentTest()
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
    }
}
