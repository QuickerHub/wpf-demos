using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cea.Utils;
using Cea.Utils.Extension;
using CeaToolsRunner.Reptile;
using CommandLine;

namespace CeaToolsRunner.Verb
{
    [Verb("lcldsss")]
    public class LcldsssVerb : CmdVerbBase
    {
        [Option('c', "cookie")]
        public string Cookie { get; set; } = "";

        [Option("jobs")]
        public string Jobs { get; set; } = "";

        [Option("file")]
        public string JobFile { get; set; } = "";

        [Option('d', "dir")]
        public string Dir { get; set; } = "";

        public override async Task RunOptions()
        {
            var reptile = new Reptile_lcldsss(Cookie, "----WebKitFormBoundaryaEB6Wtz7vU7XO2w2");

            IList<string> joblist = File.Exists(JobFile)
                ? File.ReadAllText(JobFile).SplitToList()
                : (IList<string>)Jobs.SplitToList("\\r\\n");

            List<string> res = await reptile.MultiJob(joblist, Dir);

            var temp_file = Path.Combine(Path.GetTempPath(), "S3I2aaZsiyzT.txt");

            File.WriteAllLines(temp_file, res);

            CommonUtil.TryOpenFileOrUrl(temp_file);
        }
    }
}
