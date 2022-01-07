using System;
using Microsoft.Azure.CognitiveServices.Personalizer.Models;

namespace AAI
{
    /// <summary>
    /// Ugh, declare a duplicate of RankedAction to get around deserialization issue. RankedAction is
	/// declared with getters making the properties readonly. This breaks deserialization
	/// using 'System.Text.Json'. RankedAction has a hack for Newtonsoft which does not work for the
	/// system implementation.
    /// </summary>
    public class PersonalizerRankedAction
    {
        /// <summary>
        /// Required for serialization code (System.Text.Json)
        /// </summary>
        public PersonalizerRankedAction()
        {
            Id = null;
            Probability = null;
        }
        /// <summary>
        /// Constructor filling all fields.
        /// </summary>
        /// <param name="id">Action Id</param>
        /// <param name="probability">Probability the action id is the correct action.</param>
        public PersonalizerRankedAction(string id = null, double? probability = null)
        {
            Id = id;
            Probability = probability;
        }
        /// <summary>
        /// Constructor, allow conversion from the built in model. 
        /// </summary>
        /// <param name="model"></param>
        public PersonalizerRankedAction(RankedAction model)
        {
            Id = model.Id;
            Probability = model.Probability;
        }
        /// <summary>
        /// Action id.
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// Propability the action id is the correct one.
        /// </summary>
        public double? Probability { get; set; }
    }
}