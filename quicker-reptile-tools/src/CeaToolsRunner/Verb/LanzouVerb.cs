using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cea.Utils.Extension;
using CeaToolsRunner.Reptile;
using CommandLine;

namespace CeaToolsRunner.Verb
{
    [Verb("lanzou")]
    public class LanzouVerb : CmdVerbBase
    {
        [Option('c', "cookie")]
        public string Cookie { get; set; } = "";

        [Option('v', "vei")]
        public string Vei { get; set; } = "";

        [Option("folder_id")]
        public string FolderId { get; set; } = "-1";

        [Option("uid")]
        public string Uid { get; set; } = "";

        public override async Task RunOptions()
        {
            var reptile = new Reptile_Lanzou(Cookie, Vei, Uid);

            var res = await reptile.GetAllFilesWithUrl(FolderId);

            Reptile_Lanzou.SaveData(res);
        }
    }
}
