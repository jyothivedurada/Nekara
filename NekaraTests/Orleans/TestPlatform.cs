﻿using System;
using System.Linq;
using System.Threading.Tasks;
using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;

namespace Nekara.Tests.Orleans
{
    class TestPlatform
    {
        private static async Task<ISiloHost> StartSilo(Type[] types)
        {
            // define the cluster configuration
            var builder = new SiloHostBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "OrleansBasics";
                });
                //.ConfigureLogging(logging => logging.AddConsole());

            types.ToList().ForEach(t => builder.ConfigureApplicationParts(parts => parts.AddApplicationPart(t.Assembly).WithReferences()));

            var host = builder.Build();
            await host.StartAsync();
            Console.WriteLine("Silo successfully started \n");
            return host;
        }

        /* Client */
        private static async Task<IClusterClient> ConnectClient()
        {
            IClusterClient client;
            client = new ClientBuilder()
                .UseLocalhostClustering()
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = "dev";
                    options.ServiceId = "OrleansBasics";
                })
                //.ConfigureLogging(logging => logging.AddConsole())
                .Build();

            await client.Connect();
            Console.WriteLine("Client successfully connected to silo host \n");
            return client;
        }

        /* End Client */

        public static (ISiloHost, IClusterClient) Setup(params Type[] types)
        {
            var host = StartSilo(types).Result;
            var client = ConnectClient().Result;
            return (host, client);
        }
    }
}