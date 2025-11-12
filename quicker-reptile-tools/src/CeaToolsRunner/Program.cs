using System.IO;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Text;
using CeaToolsRunner.Verb;
using CeaToolsRunner.Reptile;
using System.Web;


#if DEBUG

//await reptile.Flow(reptile.GetSearchTextList()[9]);

await Reptile_Miyoushe.Test();

//var res = MimeMapping.GetMimeMapping(".jpg");
//Console.WriteLine(res);

#else

await Main();

#endif

async Task Main()
{
    await CommandRunner.RunCommand(args);
}