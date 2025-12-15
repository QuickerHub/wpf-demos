using System;

namespace BatchRenameTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var tests = new BatchRenameExecutorTests();
            tests.RunAllTests();
        }
    }
}
