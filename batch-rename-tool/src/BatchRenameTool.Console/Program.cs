using System;

namespace BatchRenameTool
{
    class Program
    {
        static void Main(string[] args)
        {
            // Test template parser with method calls
            var templateTests = new TemplateParserTests();
            templateTests.RunAllTests();
            
            // Test batch rename executor (optional, can be commented out)
            // var renameTests = new BatchRenameExecutorTests();
            // renameTests.RunAllTests();
        }
    }
}
