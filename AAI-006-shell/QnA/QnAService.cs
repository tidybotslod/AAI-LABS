using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker;

namespace AAI
{
    public partial class QnAService
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="entries"></param>
        /// <param name="published"></param>
        /// <returns></returns>
        public async Task<(string, string)> AddToQnA(IList<QnADTO> entries)
        {
            Dictionary<string, QnADTO> existing = await GetExistingAnswers(false);

            List<UpdateQnaDTO> updates = new List<UpdateQnaDTO>();
            List<QnADTO> additions = new List<QnADTO>();
            if (entries != null && entries.Count > 0)
            {
                foreach (QnADTO update in entries)
                {
                    QnADTO value = null;
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
        /// <summary>
        /// 
        /// </summary>
        /// <param name="question"></param>
        /// <param name="published"></param>
        /// <param name="topQuestions"></param>
        /// <returns></returns>
        public async Task<QnASearchResultList> Ask(string question, bool published, int topQuestions = 1)
        {
            QnAMakerRuntimeClient client = await QnAEndpoint();
            QueryDTO query = new QueryDTO { Question = question, Top = topQuestions, IsTest = !published };
            QnASearchResultList response =  await client.Runtime.GenerateAnswerAsync(KnowledgeBaseID, query);

            return response;
        }
        /// <summary>
        /// Create a new QnA Knowledge base, the returned ID and endpoint key should be placed
        /// in the appsettings.json file or they have to be set manually on every subsequent
        /// operation.
        /// </summary>
        /// <param name="qnaData">Name and QnaList fields musb be populated.</param>
        /// <returns>( status,
        ///            knowledgeBaseId,
        ///            endPointKey</returns>
        public async Task<(string, string)> CreateQnA(CreateKbDTO qnaData)
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
        /// 
        /// </summary>
        /// <param name="entries"></param>
        /// <param name="published"></param>
        /// <returns></returns>
        public async Task<(string, string)> UpdateQnA(IList<QnADTO> entries)
        {
            Dictionary<string, QnADTO> existing = await GetExistingAnswers(false);

            List<UpdateQnaDTO> updates = new List<UpdateQnaDTO>();
            List<QnADTO> additions = new List<QnADTO>();
            if (entries != null && entries.Count > 0)
            {
                foreach (QnADTO update in entries)
                {
                    QnADTO value = null;
                    if (existing.TryGetValue(update.Answer, out value))
                    {
                        UpdateQnaDTO modified = new UpdateQnaDTO();
                        modified.Answer = value.Answer;
                        modified.Id = value.Id;
                        modified.Questions = new UpdateQnaDTOQuestions { Add = update.Questions, Delete = value.Questions };
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
