using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker;

namespace AAI
{
    /// <summary>
    /// Manage a connection to a knowledge base. 
    /// </summary>
    public partial class QnAService
    {
        /// <value>
        /// Authoring key for a QnAMaker, set to a key from the azure App Server created for the QnAMaker (see https://portal.azure.com).
        /// </value>
        public string? AuthoringKey { get; set; }
        /// <value>
        /// Resource name for a QnAMaker, set to the resource name for the App Server created for the QnAMaker (see https://portal.azure.com).
        /// </value>
        public string? ResourceName { get; set; }
        /// <value>
        /// Application name for a QnAMaker, usually the same as the ResourceName.
        /// </value>
        public string? ApplicationName { get; set; }
        /// <value>
        /// Unique identifier for the created published Knowledge base. (see https://qnamaker.ai)
        /// </value>
        public string? KnowledgeBaseID { get; set; }
        /// <value>
        /// Key for the created published Knowledge base. (see https://qnamaker.ai)
        /// </value>
        public string? QueryEndpointKey { get; set; }

        /// <summary>
        /// Default constructor, requires AuthoringKey, ResourceName, and ApplicationName to be set prior to acccessing Azure QnAMaker App Service. 
        /// Requires KnowledgeBaseID to be set prior to accessing the knowledge base associated with QnAMaker service. 
        ///
        /// <example>
        /// For example, to create a QnAService reading authoring key, resource name, application name knowledge base id, and query endpoint key from the enivronment:
        ///
        /// <code>
        /// private static string ProcessConfig(string key) 
        ///   { return System.Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process); }
        /// 
        /// AAI.QnAService service = new AAI.QnAService
        /// {
        ///     AuthoringKey = ProcessConfig("AuthoringKey"),
        ///     ResourceName = ProcessConfig("ResourceName"),
        ///     ApplicationName = ProcessConfig("ApplicationName"),
        ///     KnowledgeBaseID = ProcessConfig("KnowledgeBaseID"),
        ///     QueryEndpointKey = ProcessConfig("QueryEndpointKey")
        /// };
        /// </code>
        /// </example>
        /// </summary>
        /// <remarks>
        /// Not all of the values need to be available during construction.
        /// (e.g., Knowledge base is not available until the QnA has created one - see CreateQnA method):
        /// </remarks>
        public QnAService()
        {
        }
        /// <summary>
        /// Create QnAService passing in AuthoringKey, ResourceName, ApplicationName , KnowledgeBaseID and QueryEndpointKey. Valid values for AuthoringKey, 
        /// ResourceName are required before accessing the Azure QnAMaker service. A valid KnowledgeBaseID is required before accessing the knowledge base associated
        /// with the QnAMaker service can be accesssed (see CreateQnA() method).
        ///
        /// <example>
        /// For example, to create a QnAService reading authoring key, resource name, application name knowledge base id, and query endpoint key from the enivronment:
        /// 
        /// <code>
        /// private static string ProcessConfig(string key) 
        ///   { return System.Environment.GetEnvironmentVariable(key, EnvironmentVariableTarget.Process); }
        /// 
        /// AAI.QnAService service = new AAI.QnAService(
        ///     ProcessConfig("AuthoringKey"), 
        ///     ProcessConfig("ResourceName"), 
        ///     ProcessConfig("ApplicationName"),
        ///     ProcessConfig("KnowledgeBaseID"),
        ///     ProcessConfig("QueryEndpointKey") );
        /// </code>
        /// </example> 
        /// </summary>
        /// <remarks>
        /// Some or all of the values can be null. However, access to the QnaMaker service or knowlege base is not possible until the correct properties are set.
        /// (e.g., Knowledge base is not available until the QnA has created one - see CreateQnA method):
        /// </remarks>
        /// <param name="authoringKey">Access key to Azure QnAMaker service, obtained from the Azure portal.</param>
        /// <param name="resourceName">Resource name given to Azure QnAMaker service when created, obtained from the Azure portal.</param>
        /// <param name="applicationName">Application name given to Azure QnAMaker service when created, obtained from the Azure portal.</param>
        /// <param name="knowledgeBaseID">Knowledge base id, returned when knowledge base is created (see CreateQnA())</param>
        /// <param name="queryEndpointKey">Key for knowledge base, returned when knowledge base is created and can be obtained by querying Azure QnAMaker (see QueryKey())</param>
        public QnAService(string authoringKey, string resourceName, string applicationName, string knowledgeBaseID, string queryEndpointKey)
        {
            AuthoringKey = authoringKey;
            ResourceName = resourceName;
            ApplicationName = applicationName;
            KnowledgeBaseID = knowledgeBaseID;
            QueryEndpointKey = queryEndpointKey;
        }

        /// <summary>
        /// Download knowledge base creating dictionary where the lookup is the answer. There can be multiple questions per answer.
        /// </summary>
        /// <param name="published"></param>
        /// <returns>dictionary of entire knowledge base</returns>
        public async Task<Dictionary<string, QnADTO>> GetExistingAnswers(bool published)
        {
            QnADocumentsDTO kb = await AzureEndpoint().Knowledgebase.DownloadAsync(KnowledgeBaseID, published ? EnvironmentType.Prod : EnvironmentType.Test);
            Dictionary<string, QnADTO> existing = new Dictionary<string, QnADTO>();
            foreach (QnADTO entry in kb.QnaDocuments)
            {
                existing.Add(entry.Answer, entry);
            }
            return existing;
        }
        /// <summary>
        /// QnA endpoint is used to query and train knowledge base.
        /// </summary>
        /// <returns>QnA endpoint object</returns>
        public async Task<QnAMakerRuntimeClient> QnAEndpoint()
        {
            return qnaEndpoint ?? await CreateQnAEndpoint();
        }
        /// <summary>
        /// Azure endpoint is used to create, publish, download, update, and delete QnA knowledge bases. Creates
        /// one using configured data.
        /// </summary>
        /// <returns>azure endpoint object</returns>
        public QnAMakerClient AzureEndpoint()
        {
            return azureEndpoint ?? CreateConfiguredAzureEndpoint();
        }
        /// <summary>
        /// Returns the Query key required when accessing the QnA
        /// </summary>
        /// <returns>QnA Auth Key</returns>
        public async Task<string?> QueryKey()
        {
            await RetrieveEndpointKey();
            return QueryEndpointKey;
        }

        /// <summary>
        /// Queries the knowledge base for
        /// </summary>
        /// <param name="question">Question being asked of the knowledge base.</param>
        /// <param name="published">Use the published (true) or test knowledge base (false).</param>
        /// <param name="topQuestions">The number of answers to return, defaults to one.</param>
        /// <returns></returns>
        public async Task<QnASearchResultList> Ask(string question, bool published, int topQuestions = 1)
        {
            QnAMakerRuntimeClient client = await QnAEndpoint();
            QueryDTO query = new QueryDTO { Question = question, Top = topQuestions, IsTest = !published };
            QnASearchResultList response =  await client.Runtime.GenerateAnswerAsync(KnowledgeBaseID, query);

            return response;
        }
        /// <summary>
        /// Create a new QnA Knowledge base, the returned ID and endpoint key should be retained
        /// (e.g., appsettings.json file) so they can be reused when instantiating other QnAService's.
        /// The knowledge base ID and endpoint key values can be located in http://qnamaker.ai for
        /// published knowledges bases.
        /// </summary>
        /// <param name="qnaData">Name and QnaList fields musb be populated.</param>
        /// <returns>( status,
        ///            knowledgeBaseId,
        ///            endPointKey</returns>
        public async Task<(string, string?)> CreateQnA(CreateKbDTO qnaData)
        {
            QnAMakerClient endpoint = AzureEndpoint();
            Operation op = await endpoint.Knowledgebase.CreateAsync(qnaData);
            op = await MonitorOperation(op);

            string result = op.ResourceLocation;
            if (op.OperationState == OperationStateType.Succeeded)
            {
                KnowledgeBaseID = op.ResourceLocation.Replace("/knowledgebases/", string.Empty);
                return (op.OperationState, KnowledgeBaseID);
            }
            else
            {
                return (op.OperationState, null);
            }
        }
        /// <summary>
        /// Delete the knowledge base.
        /// </summary>
        /// <returns></returns>
        public async Task DeleteKnowledgeBase()
        {
            await AzureEndpoint().Knowledgebase.DeleteAsync(KnowledgeBaseID);
        }


        /// <summary>
        /// Moves the test knowledge base to production.
        /// </summary>
        /// <returns></returns>
        public async Task Publish()
        {
            await AzureEndpoint().Knowledgebase.PublishAsync(KnowledgeBaseID);
        }
        /// <summary>
        ///  Add/update one or more entries in the QnA knowledge base. When the answer in
        ///  an entry matches (string match) an existing value in the knowledge base the
        ///  QnADTO is converted to an UpdateQnaDTO and added to a list of udpates.
        ///
        ///  To perform udpates, the entire knwoledge base is pulled down.
        /// </summary>
        /// <param name="entries"></param>
        /// <returns></returns>
        public async Task<(string, string?)> AddToQnA(IList<QnADTO> entries)
        {
            Dictionary<string, QnADTO> existing = await GetExistingAnswers(false);

            List<UpdateQnaDTO> updates = new List<UpdateQnaDTO>();
            List<QnADTO> additions = new List<QnADTO>();
            if (entries != null && entries.Count > 0)
            {
                foreach (QnADTO update in entries)
                {
                    QnADTO? value = null;
                    if (existing.TryGetValue(update.Answer, out value))
                    {
                        UpdateQnaDTO modified = new UpdateQnaDTO();
                        modified.Answer = value.Answer;
                        modified.Id = value.Id;
                        modified.Questions = new UpdateQnaDTOQuestions { Add = update.Questions };
                        updates.Add(modified);
                    }
                    else
                    {
                        additions.Add(update);
                    }
                }
            }
            return await AlterKb(additions, updates, null);
        }
    }
}
