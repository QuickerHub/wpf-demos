using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Cea.Utils;

namespace CeaToolsRunner.Reptile
{

    public class Reptile_123pan
    {
        private readonly HttpClient _client;

        public Reptile_123pan(string cookie)
        {
            var handler = new HttpClientHandler
            {
                CookieContainer = new CookieContainer()
            };
            handler.CookieContainer.AddCookieFromString(cookie, "https://www.lcldsss.com");
            _client = new(handler);
        }

        public void Search()
        {

        }
    }
}
