using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace Check
{
    class TestEntry
    {
        public TestEntry(string c, Func<IConfiguration, Task<TestResult>> f)
        {
            Comment = c;
            Func = f;
        }
        public string Comment { get; }
        public Func<IConfiguration, Task<TestResult>> Func { get; }
    }
}
