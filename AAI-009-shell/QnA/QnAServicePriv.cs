using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker;

namespace AAI
{
    public partial class QnAService
    {

        //======================================
        // Methods to be used only by QnAService
        // Methods to be used only by QnAService
        private QnAMakerClient? azureEndpoint;         // Access to qna service endpoint in azure (see azure portal)
        private QnAMakerRuntimeClient? qnaEndpoint;    // Access to qna maker service endpoint (see www.qnamaker.ai)

        /// <summary>
        /// Make alterations to the knowledge base, polls until the knowledge base has been updated.
        /// </summary>
        /// <param name="additions"></param>
        /// <param name="updates"></param>
        /// <param name="deletes"></param>
        /// <returns>(Operation state, optional error response)</returns>
        private async Task<(string, string?)> AlterKb(IList<QnADTO>? additions, IList<UpdateQnaDTO>? updates, IList<Nullable<Int32>>? deletes)
        {
            var update = new UpdateKbOperationDTO
            {
                Add = additions != null && additions.Count > 0 ? new UpdateKbOperationDTOAdd { QnaList = additions } : null,
                Update = updates != null && updates.Count > 0 ? new UpdateKbOperationDTOUpdate { QnaList = updates } : null,
                Delete = deletes != null && deletes.Count > 0 ? new UpdateKbOperationDTODelete { Ids = deletes } : null
            };
            var op = await AzureEndpoint().Knowledgebase.UpdateAsync(KnowledgeBaseID, update);
            op = await MonitorOperation(op);
            return (op.OperationState, op.ErrorResponse == null ? null : op.ErrorResponse.ToString());
        }
        /// <summary>
        /// Moniter long running operations by polling the status of an operation occuring in Azure QnA service
        /// </summary>
        /// <param name="operation">Operation returned from call to monitor</param>
        /// <returns>Final operation</returns>
        private async Task<Operation> MonitorOperation(Operation operation)
        {
            // Loop while operation is success
            for (int i = 0;
                i < 20 && (operation.OperationState == OperationStateType.NotStarted || operation.OperationState == OperationStateType.Running);
                i++)
            {
                Console.WriteLine("Waiting for operation: {0} to complete.", operation.OperationId);
                await Task.Delay(5000);
                operation = await AzureEndpoint().Operations.GetDetailsAsync(operation.OperationId);
            }

            if (operation.OperationState != OperationStateType.Succeeded)
            {
                string message =  operation.ErrorResponse.Error.Message ?? "No additional information";
                throw new Exception($"Operation {operation.OperationId} failed to completed. Error: {message}");
            }
            return operation;
        }

        //==================================================================
        // Helper methods targeted for QnAServicePriv only, not docuemented.
        private QnAMakerClient CreateConfiguredAzureEndpoint()
        {
            ApiKeyServiceClientCredentials credentials = new ApiKeyServiceClientCredentials(AuthoringKey);
            azureEndpoint = new QnAMakerClient(credentials);
            azureEndpoint.Endpoint = $"https://{ResourceName}.cognitiveservices.azure.com";
            return azureEndpoint;
        }
        private async Task<QnAMakerRuntimeClient> CreateQnAEndpoint()
        {
            var endpointKey = await QueryKey();
            var credentials = new EndpointKeyServiceClientCredentials(endpointKey);
            string queryEndpoint = $"https://{ApplicationName}.azurewebsites.net";
            qnaEndpoint = new QnAMakerRuntimeClient(credentials) { RuntimeEndpoint = queryEndpoint };
            return qnaEndpoint;
        }
        private async Task RetrieveEndpointKey()
        {
            var endpointKeysObject = await AzureEndpoint().EndpointKeys.GetKeysAsync();
            QueryEndpointKey = endpointKeysObject.PrimaryEndpointKey;
        }
    }
}
