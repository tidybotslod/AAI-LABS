using System;
using System.Collections.Generic;
using System.IO;
using Azure;
using Azure.AI.TextAnalytics;

namespace AAI
{
    public class KeySentiments
    {

        public TextAnalyticsClient AzureTextAnalyticsService => azureTextAnalyticsService == null ? BuildTextAnalyticsService() : azureTextAnalyticsService;

        public string Key { get; set; }
        public string ResourceName { get; set; }
        public KeySentiments() { }
        public KeySentiments(string key, string resourceName)
        {
            Key = key;
            ResourceName = resourceName;
            BuildTextAnalyticsService();
        }


        private TextAnalyticsClient azureTextAnalyticsService;
        

        private TextAnalyticsClient BuildTextAnalyticsService()
        {
            AzureKeyCredential credentials = new AzureKeyCredential(Key);
            Uri endpoint = new Uri($"https://{ResourceName}.cognitiveservices.azure.com");
            azureTextAnalyticsService = new TextAnalyticsClient(endpoint, credentials);
            return azureTextAnalyticsService;
        }


        public List<String> KeyWords(String arg)
        {
            List<String> keyWords = new List<String>();
            if (arg != null && arg.Length > 0)
            {
                var response = AzureTextAnalyticsService.ExtractKeyPhrases(arg);
                foreach (string word in response.Value)
                {
                    keyWords.Add(word);
                }
            }
            return keyWords;
        }

        public void Sentiment(String text, StreamWriter writer)
        {
            if (text != null && text.Length > 0)
            {
                DocumentSentiment result = AzureTextAnalyticsService.AnalyzeSentiment(text);
                foreach (var sentence in result.Sentences)
                {
                    writer.Write($"{sentence.Sentiment}, {sentence.ConfidenceScores.Positive:0.00}, {sentence.ConfidenceScores.Negative:0.00}, {sentence.ConfidenceScores.Neutral:0.00}");
                    List<string> keyWords = KeyWords(sentence.Text);
                    foreach (string word in keyWords)
                    {
                        writer.Write($", \"{word}\"");
                    }
                    writer.WriteLine("");
                }
                writer.Flush();
            }
        }
    }
}
