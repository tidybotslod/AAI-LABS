using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AAI {
    /// <summary>
    /// Used to load training cases from a file.
    /// </summary>
    public class TrainingCase
    {
        /// <summary>
        /// Names the training case.
        /// </summary>
        /// <value>
        /// Unique id
        /// </value>
        public string Name { get; set; }
        /// <summary>
        /// List of features used to describe the context or item
        /// </summary>
        /// <value>
        /// Array of objects such as AAI.PersonalizerFeature
        /// </value>
        public object[] Features { get; set; }
        /// <summary>
        /// List of items to exclude from result
        /// </summary>
        /// <value>
        /// Array of id's (e.g., item name)
        /// </value>
        public string[] Exclude { get; set; }
        /// <summary>
        /// The item that is expected in the training case.
        /// </summary>
        /// <value>
        /// Id of the item that is expected to be returned
        /// </value>
        public string Expected { get; set; }
    }
}
