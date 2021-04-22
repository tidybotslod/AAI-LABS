using System;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Personalizer;
using Microsoft.Azure.CognitiveServices.Personalizer.Models;

namespace AAI
{
    public partial class PersonalizerService
    {
        public void Train(TrainingCase[] cases)
        {
            if (cases != null)
            {
                Console.WriteLine($"Start training from file:");
                foreach (TrainingCase trainingCase in cases)
                {
                    string lessonId = Guid.NewGuid().ToString();
                    var request = new RankRequest(Actions, trainingCase.Features, trainingCase.Exclude, lessonId, false);
                    RankResponse response = Client.Rank(request);
                    double reward = 0.0;
                    if (response.RewardActionId.Equals(trainingCase.Expected))
                    {
                        reward = 1.0;
                    }
                    Client.Reward(response.EventId, new RewardRequest(reward));
                    Console.WriteLine($"{trainingCase.Name} selected {response.RewardActionId}: Reward = {reward}";
                }
            }
        }
    }
}