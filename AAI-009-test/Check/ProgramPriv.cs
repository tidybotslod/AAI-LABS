using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.CommandLine;
using System.CommandLine.Invocation;
using Microsoft.Extensions.Configuration;

namespace Check
{
    partial class Program
    {
        Dictionary<string, TestEntry> testList;

        private static string GetConfigString(IConfiguration config, string key)
        {
            string result = config[key];
            if (result != null && result.Length == 0)
            {
                result = null;
            }
            return result;
        }
        static public async Task<TestResult> Post(string url, string input)
        {
            TestResult test = new TestResult();
            try
            {
                string functionUrl = url + "/api/CustomerChat";
                var client = new HttpClient();
                var response = await client.PostAsync(functionUrl, new StringContent(input));
                if (response.IsSuccessStatusCode)
                {
                    test.result = await response.Content.ReadAsStringAsync();
                    if (test.result == null)
                    {
                        test.AddError("No data returned from post");
                    }
                }
                else
                {
                    test.AddError($"Failed - Status Code: {response.StatusCode}, message: {response.ReasonPhrase}");
                }
            }
            catch (Exception e)
            {
                test.AddException(e.Message);
            }
            return test;
        }
        int Execute(string configFile, string[] tests)
        {
            IConfiguration config = new ConfigurationBuilder().AddJsonFile(configFile, optional: false, reloadOnChange: true).Build();
            int returnValue = 0;
            foreach (string testName in tests)
            {
                TestEntry run = testList[testName];
                if (run != null)
                {
                    Console.WriteLine($"Test: {testName}");
                    Task.Run(async () => {
                        TestResult result = await run.Func(config);
                        if (result.Fault)
                        {
                            if (result.errors.Count > 0)
                            {
                                Console.WriteLine("Errors:");
                                foreach (string error in result.errors)
                                {
                                    Console.WriteLine(error);
                                }
                                returnValue = returnValue + result.errors.Count;
                            }
                            if (result.exceptions.Count > 0)
                            {
                                Console.WriteLine("Exceptions:");
                                foreach (string exception in result.exceptions)
                                {
                                    Console.WriteLine(exception);
                                }
                                returnValue = returnValue + result.exceptions.Count;
                            }
                        }
                    }).Wait();
                }
            }
            return returnValue;
        }
        void List()
        {
            foreach (KeyValuePair<string, TestEntry> test in testList)
            {
                Console.WriteLine($"{test.Key} : {test.Value.Comment}");
            }
        }

        static int Main(string[] args)
        {
            var course = "AAI-009";
            var defaultConfigFile = $"D:\\LabFiles\\{ course}\\UnitTests\\appsettings.json";
            int returnCode = 0;

            var rootCommand = new RootCommand
            {
                new Option<string>(
                    new string[] { "-c", "--configuration"},
                    description: $"Specify configuration file [defaults to: {defaultConfigFile}"),
                new Option<bool>(
                    new string[] { "-l", "--list"},
                    description: "List tests."),
                new Option<string[]>(
                    new string [] { "-t", "--tests" },
                    description: "Run tests, print out the results.")
            };
            rootCommand.Description = $"Test for {course}";
            rootCommand.Handler = CommandHandler.Create<string, bool, string[]>((config, list, tests) =>
            {
                if (config == null)
                {
                    config = defaultConfigFile;
                }
                Program p = new Program();
                if (list)
                {
                    p.List();
                }
                else if (tests.Length > 0)
                {
                    returnCode += p.Execute(config, tests);
                }
                else
                {
                    rootCommand.Invoke("--help");
                }
            });
            rootCommand.Invoke(args);
            return returnCode;
        }
    }
}
