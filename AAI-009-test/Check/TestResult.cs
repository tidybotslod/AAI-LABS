using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Check
{
    class TestResult
    {
        bool fault;
        public List<string> exceptions { get; }
        public List<string> errors { get; }
        public bool Fault { get { return fault; } }
        public string result { get; set; }
        public TestResult()
        {
            errors = new List<string>();
            exceptions = new List<string>();
            fault = false;
            result = null;
        }
        public void AddError(string err)
        {
            if (err != null)
            {
                fault = true;
                errors.Add(err);
            }
        }
        public void AddException(string excep)
        {
            if (excep != null)
            {
                fault = true;
                exceptions.Add(excep);
            }
        }
        public void AreEqual(int a, int b, string msg)
        {
            if (a != b)
            {
                AddError(msg);
            }
        }
    }

}
