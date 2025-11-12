using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CeaToolsRunner.Reptile;
using CommandLine;

namespace CeaToolsRunner.Verb
{
    [Verb("3dcu")]
    public class _3dcuVerb : CmdVerbBase
    {
        [Option('f', "file")]
        public string FileName { get; set; } = "";

        public override async Task RunOptions()
        {
            var reptile = new Reptile_3dcu(FileName);

            await reptile.Run();
        }
    }
}
