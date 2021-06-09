using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using AAI;

using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker.Models;
using Microsoft.Azure.CognitiveServices.Knowledge.QnAMaker;
using Microsoft.Extensions.Configuration;

namespace PQnA
{
    public class Program
    {
        QnAService QnA;
        public Program()
        {
            IConfiguration config; // Load configuration data found in appsettings.json, need Azure authoring key and resource name to build URL to azure.
            config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).Build();
            QnA = new QnAService
            {
                AuthoringKey = ConfigurationValue(config, "AuthoringKey"),
                ResourceName = ConfigurationValue(config, "ResourceName"),
                ApplicationName = ConfigurationValue(config, "ApplicationName"),
                KnowledgeBaseID = ConfigurationValue(config, "KnowledgeBaseID"),
                QueryEndpointKey = ConfigurationValue(config, "QueryEndpointKey")
            };
        }
        public QnAService Service { get { return QnA; } }
        public async Task<bool> CreateQnA(string file)
        {
            var create = new CreateKbDTO
            {
                Name = $"Lab005 Knowledge Base",
                QnaList = QnAFile.LoadCSV(file)
            };
            var (status, knowledgeBaseId) = await QnA.CreateQnA(create);
            Console.WriteLine($"Status:  {status}, Knowledge Base ID: {knowledgeBaseId}");
            return string.Compare(status, OperationStateType.Succeeded) == 0;
        }
        public async Task<int> Ask(string question, bool published, int number)
        {
            QnASearchResultList response = await QnA.Ask(question, published, number);
            foreach (var entry in response.Answers)
            {
                Console.Write($"Score: {entry.Score}");
                Console.WriteLine($", Answer: {entry.Answer}");
            }
            return response.Answers.Count;
        }
        public async Task<bool> AddToQnA(string file)
        {
            List<QnADTO> entries = QnAFile.LoadCSV(file);
            var (status, error) = await QnA.AddToQnA(entries);
            Console.Write($"Status: {status}");
            Console.WriteLine(error == null ? "" : $", Error response: {error}");
            return string.Compare(status, OperationStateType.Succeeded) == 0;
        }
        public async Task<bool> UpdateQnA(string file)
        {
            List<QnADTO> entries = QnAFile.LoadCSV(file);
            var (status, error) = await QnA.UpdateQnA(entries);
            Console.Write($"Status: {status}");
            Console.WriteLine(error == null ? "" : $", Error response: {error}");
            return string.Compare(status, OperationStateType.Succeeded) == 0;
        }
        public async Task Publish()
        {
            await QnA.Publish();
            Console.WriteLine($"Knowledge base published");
        }
        public async Task Dump(bool production)
        {
            Dictionary<string, QnADTO> entries = await QnA.GetExistingAnswers(production);
            foreach (var item in entries)
            {
                QnADTO entry = item.Value;
                Console.WriteLine($"ID: {entry.Id}, Answer: {entry.Answer}");
                if (entry.Questions.Count > 0)
                {
                    Console.WriteLine("Questions:");
                    int id = 1;
                    foreach (var question in entry.Questions)
                    {
                        Console.WriteLine($"\t{id++}: {question}");
                    }
                }
                Console.WriteLine();
            }
        }
        public async Task Train(string file)
        {
            List<QnADTO> entries = QnAFile.LoadCSV(file);
            Dictionary<string, QnADTO> existing = await QnA.GetExistingAnswers(false);
            List<FeedbackRecordDTO> feedback = new List<FeedbackRecordDTO>();
            foreach (QnADTO entry in entries)
            {
                QnADTO answer;
                if (existing.TryGetValue(entry.Answer, out answer))
                {
                    foreach (string question in entry.Questions)
                    {
                        FeedbackRecordDTO update = new FeedbackRecordDTO { UserId = "PQnA", QnaId = answer.Id, UserQuestion = question };
                        feedback.Add(update);
                    }
                }
            }
            if (feedback.Count > 0)
            {
                QnAMakerRuntimeClient client = await QnA.QnAEndpoint();
                FeedbackRecordsDTO records = new FeedbackRecordsDTO { FeedbackRecords = feedback };
                await client.Runtime.TrainAsync(QnA.KnowledgeBaseID, records);
            }
            Console.WriteLine($"Trained {feedback.Count} entries");
        }
        public async Task DeleteKnowledgeBase()
        {
            await QnA.DeleteKnowledgeBase();
            Console.WriteLine($"Knowledge base deleted");
        }
        static void Main(string[] args)
        {
            Program program = new Program();
            var rootCommand = new RootCommand
            {
                new Option< string >(
                new string [] { "-a", "--action" },
                description: "Action to take, one of: [add, create, delete, dump, publish, train, update]"),
                new Option< string >(
                    new string [] { "-f", "--file" },
                    description: "File used in a action (required by add, create, train, update)"),
                new Option< string >(
                    new string [] { "-i", "--id" },
                    description: "Override the default knowledge base id"),
                new Option< int >(
                    new string[] {"-n", "--number"},
                    () => {return 1; },
                    description: "Number of answers to return for questions."),
                new Option< bool >(
                    new string [] { "-p", "--production"},
                    () => {return false; },
                    description: "Perform actions on production knowledge base"),
                new Option< string[] >(
                    new string [] { "-q", "--question" },
                    description: "Enter question(s) to be answered"),
            };
            rootCommand.Handler = CommandHandler.Create<string, string, string, int, bool, string[]>((action, file, id, number, production, question) =>
            {
                if (id != null)
                {
                    program.Service.KnowledgeBaseID = id;
                }
                if (action != null)
                {
                    switch (action.ToLower())
                    {
                        case "add":
                            Task.Run(async () => { await program.AddToQnA(file); }).Wait();
                            break;
                        case "create":
                            Task.Run(async () => { await program.CreateQnA(file); }).Wait();
                            break;
                        case "delete":
                            Task.Run(async () => { await program.DeleteKnowledgeBase(); }).Wait();
                            break;
                        case "Dump":
                            Task.Run(async () => { await program.Dump(production); }).Wait();
                            break;
                        case "publish":
                            Task.Run(async () => { await program.Publish(); }).Wait();
                            break;
                        case "train":
                            Task.Run(async () => { await program.Train(file); }).Wait();
                            break;
                        case "update":
                            Task.Run(async () => { await program.UpdateQnA(file); }).Wait();
                            break;
                    }
                }
                if (question != null)
                {
                    foreach (string q in question)
                    {
                        Task.Run(async () =>
                        {
                            await program.Ask(q, production, number);
                        }).Wait();
                    }
                }
            });
            rootCommand.InvokeAsync(args).Wait();
        }
    
        private static string ConfigurationValue(IConfiguration config, string name)
        {
            string value = config[name];
            if (value != null && value.Length == 0)
            {
                value = null;
            }
            return value;
        }
    }
}
