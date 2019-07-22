// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License.  See License.txt in the project root for license information.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Owin.Hosting;
using Owin;
using todo;

namespace DynamicEdmModelCreation
{
    class Program
    {
        private static readonly string serviceUrl = "http://localhost:54321";

        public static void Main(string[] args)
        {
            GetTrippin().Wait();

            using (WebApp.Start(serviceUrl, Configuration))
            {
                Console.WriteLine("Server listening at {0}", serviceUrl);

                QueryTheService().Wait();

                Console.WriteLine("Press Any Key to Exit ...");
                Console.ReadKey();
            }
        }

        public static async Task GetTrippin()
        {
            DocumentDBRepository<Item>.Initialize();
            var item = await DocumentDBRepository<Item>.GetSchema(("trippinschema"));
            Console.Write(item.csdl);
            Console.WriteLine("\nDone fetching the schema");
            Console.ReadKey();
            return;
        }

        private static async Task QueryTheService()
        {
            await SendQuery("/odata/mydatasource/", "Query service document.");
            await SendQuery("/odata/mydatasource/$metadata", "Query $metadata.");
            await SendQuery("/odata/mydatasource/Products", "Query the Products entity set.");
            await SendQuery("/odata/mydatasource/Products(1)", "Query a Product entry.");

            await SendQuery("/odata/anotherdatasource/", "Query service document.");
            await SendQuery("/odata/anotherdatasource/$metadata", "Query $metadata.");
            await SendQuery("/odata/anotherdatasource/Students", "Query the Students entity set.");
            await SendQuery("/odata/anotherdatasource/Students(100)", "Query a Student entry.");

            await SendQuery("/odata/mydatasource/Products(1)/Name", "Query the name of Products(1).");
            await SendQuery("/odata/anotherdatasource/Students(100)/Name", "Query the name of Students(100).");

            await SendQuery("/odata/mydatasource/Products(1)/DetailInfo", "Query the navigation property 'DetailInfo' of Products(1).");
            await SendQuery("/odata/anotherdatasource/Students(100)/School", "Query the navigation proeprty 'School' of Students(100).");
        }

        private static async Task SendQuery(string query, string queryDescription)
        {
            Console.WriteLine("Sending request to: {0}. Executing {1}...", query, queryDescription);

            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, serviceUrl + query);
            HttpResponseMessage response = await client.SendAsync(request);

            Console.WriteLine("\r\nResult:");
            Console.WriteLine(response.StatusCode);
            Console.WriteLine(await response.Content.ReadAsStringAsync());
            Console.WriteLine();
        }

        private static void Configuration(IAppBuilder builder)
        {
            HttpConfiguration configuration = new HttpConfiguration();
            WebApiConfig.Register(configuration);
            builder.UseWebApi(configuration);
        }
    }
}
