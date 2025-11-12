using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cea.Utils.Extension;
using CommandLine;

namespace CeaToolsRunner.Verb
{
    public static class CommandRunner
    {
        private static readonly Type[] verbs = new[]
        {
            typeof(LanzouVerb),
            typeof(_3dcuVerb),
            typeof(LcldsssVerb),
        };

        public static async Task<CmdVerbBase> RunCommand(string[] args)
        {
            var res = Parser.Default.ParseArguments(args, verbs);
            if (res.Errors.Any())
            {
                HandleParseError(res.Errors);
            }
            else
            {
                await RunOptions(res.Value);
                await Console.Out.WriteLineAsync("按任意键退出");
                Console.ReadKey();
            }
            return (CmdVerbBase)res.Value;
        }

        private static void HandleParseError(IEnumerable<Error> obj)
        {
            Console.WriteLine("参数不正确, 按任意键退出");
            Console.WriteLine(obj.JoinToString());
            Console.ReadKey();
        }

        public static async Task RunOptions(object obj)
        {
            //Console.WriteLine(obj.ToJson(true));  
            if (obj is CmdVerbBase verb)
            {
                await verb.RunOptions();
            }
        }
    }

    public abstract class CmdVerbBase
    {
        public abstract Task RunOptions();
    }
}
