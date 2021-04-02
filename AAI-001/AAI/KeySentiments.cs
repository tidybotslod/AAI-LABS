using System;
using System.Collections.Generic;
using System.IO;
using Azure;
using Azure.AI.TextAnalytics;

namespace AAI
{
    public class KeySentiments
    {
        private const string Key = "695ab76cad3243038e1dce82e9c2521f";
        private const string Endpoint = "https://textanalyticslab002.cognitiveservices.azure.com/";

        public TextAnalyticsClient AzureTextAnalyticsService { get; }

        public KeySentiments()
        {
            AzureKeyCredential credentials = new AzureKeyCredential(Key);
            Uri endpoint = new Uri(Endpoint);
            AzureTextAnalyticsService = new TextAnalyticsClient(endpoint, credentials);
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
