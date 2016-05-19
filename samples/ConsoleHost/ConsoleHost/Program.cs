﻿namespace ConsoleHost
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Formatting;
    using System.Threading.Tasks;

    using Nito.AsyncEx;

    using Owin.Scim;
    using Owin.Scim.Model.Users;

    class Program
    {
        static void Main(string[] args)
        {
            using (Microsoft.Owin.Hosting.WebApp.Start<CompositionRoot>("http://+:8080"))
            {
                AsyncContext.Run(TestScimApi);
                Console.ReadLine();
            }
        }

        private static async Task TestScimApi()
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:8080/scim/")
            };

//            await ExecuteServiceProviderConfig(client);

//            await ExecuteResourceTypes(client);

//            await ExecuteSchemas(client);

//            await ExecuteUser(client);

//            await ExecuteCustomResourceType(client);
        }

        private static async Task ExecuteSchemas(HttpClient client)
        {
            Write("");
            Write("Getting schemas ...");
            var response = await client.GetAsync("schemas/" + ScimConstants.Schemas.User);
            Write(await response.Content.ReadAsStringAsync());
            Write("");
        }

        private static async Task ExecuteResourceTypes(HttpClient client)
        {
            Write("");
            Write("Getting resource types ...");
            var response = await client.GetAsync("resourcetypes");
            Write(await response.Content.ReadAsStringAsync());
            Write("");
        }

        private static async Task ExecuteServiceProviderConfig(HttpClient client)
        {
            Write("");
            Write("Getting service provider configuration ...");
            var response = await client.GetAsync("serviceproviderconfig");
            Write(await response.Content.ReadAsStringAsync());
            Write("");
        }

        private static async Task ExecuteCustomResourceType(HttpClient client)
        {
            Write("");
            Write("Creating custom resource type, tenant ...");
            var response =
                await
                    client.PostAsync("tenants",
                        new ObjectContent<Tenant>(new Tenant { Name = "mytenant" }, new JsonMediaTypeFormatter()));
            Write(await response.Content.ReadAsStringAsync());
            if (response.StatusCode == HttpStatusCode.Created)
            {
                var tenant = await response.Content.ReadAsAsync<Tenant>(new[] { new JsonMediaTypeFormatter() });
                Write("");
                Write("Getting custom resource type, tenant " + tenant.Id);
                var json = await client.GetStringAsync("tenants/" + tenant.Id);
                Write(json);
            }
            Write("");
        }

        private static async Task ExecuteUser(HttpClient client)
        {
            Write("");
            Write("Creating user ...");
            var response =
                await
                    client.PostAsync("users",
                        new ObjectContent<User>(new User { UserName = "daniel", NickName = "danny" }, new JsonMediaTypeFormatter()));
            Write(await response.Content.ReadAsStringAsync());
            if (response.StatusCode == HttpStatusCode.Created)
            {
                var user = await response.Content.ReadAsAsync<User>(new[] { new JsonMediaTypeFormatter() });
                Write("");
                Write("Getting user " + user.Id);
                var json = await client.GetStringAsync("users/" + user.Id);
                Write(json);
            }

            Write("");
        }

        private static void Write(string text)
        {
            Console.WriteLine(text);
        }
    }
}
