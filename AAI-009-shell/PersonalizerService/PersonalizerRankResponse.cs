using System;
using System.Collections.Generic;
using Microsoft.Azure.CognitiveServices.Personalizer;
using Microsoft.Azure.CognitiveServices.Personalizer.Models;

namespace AAI
{
	/// <summary>
	/// Ugh, declare a duplicate of RankResponse to get around deserialization issue. RankResponse is
	/// declared with getters making the properties readonly. This breaks deserialization
	/// using 'System.Text.Json'. RankResponse has a hack for Newtonsoft which does not work for the
	/// system implementation.
	/// </summary>
	public class PersonalizerRankResponse
	{
		/// <summary>
		/// Default constuctor, necessary for JSON serialization and deserialization.
		/// </summary>
		public PersonalizerRankResponse() { }
		/// <summary>
		/// </summary>
		public PersonalizerRankResponse(RankResponse modelResponse)
        {
			Ranking = new List<PersonalizerRankedAction>();
			foreach (RankedAction action in modelResponse.Ranking) {
				Ranking.Add(new PersonalizerRankedAction(action));
			}
			EventId = modelResponse.EventId;
			RewardActionId = modelResponse.RewardActionId;
		}
		/// <summary>
		/// A list of suggested actions, each is ranked.
		/// </summary>
		public IList<PersonalizerRankedAction> Ranking { get; set; }
		/// <summary>
		/// Id used to identify the request to the server for ranking.
		/// </summary>
		public string EventId { get; set; }
		/// <summary>
		/// The action id that is suggested by the personalizer.
		/// </summary>
		public string RewardActionId { get; set; }
	}
}
