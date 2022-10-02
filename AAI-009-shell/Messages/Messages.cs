using System;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;
using Microsoft.Azure.CognitiveServices.Personalizer.Models;

namespace AAI
{

    // Classes for JSON deserialization must match layout defined for the request fields above.
    public class CustomerRating
    {
        public string Id { get; set; }
        public double Rank { get; set; }
        public CustomerRating()
        {
            Id = null;
            Rank = 0.0;
        }
    }
    public class CustomerChatRequest
    {
        public String Question { get; set; }
        public List<object> Suggest { get; set; }
        public CustomerRating Rating { get; set; }
        public CustomerChatRequest()
        {
            Question = null;
            Suggest = null;
        }
    }
    // Class for response must match layout defined from the response field above.
    public class CustomerChatResponse
    {
        public string Error { get; set; }
        public QnASearchResultList Answer { get; set; }
        public PersonalizerRankResponse Result { get; set; }
        public CustomerChatResponse()
        {
            Answer = null;
            Result = null;
            Error = null;
        }
    }
}