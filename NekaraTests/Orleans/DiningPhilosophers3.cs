﻿using System;
using NativeTasks = System.Threading.Tasks;
using Nekara.Client;
using Nekara.Core;
using Nekara.Models;
using Orleans;
using Orleans.Hosting;

namespace Nekara.Tests.Orleans
{
    class DiningPhilosophers3
    {
        public static ITestingService nekara = RuntimeEnvironment.Client.Api;
        public static ISiloHost silo;
        public static IClusterClient client;

        [TestSetupMethod]
        public static void Setup()
        {
            (silo, client) = TestPlatform.Setup(typeof(PhilosopherGrain3));
            Console.WriteLine("Setup");
        }

        [TestTeardownMethod]
        public static void Teardown()
        {
            silo.StopAsync().Wait();
            Console.WriteLine("Teardown");
        }

        public static int n;
        public static int phil;

        public static Lock countLock;
        public static Lock[] locks;

        [TestMethod]
        public static async void Run()
        {
            n = 3;
            phil = 0;

            countLock = new Lock(0);
            locks = new Lock[n];

            for (int i = 0; i < n; i++)
            {
                locks[i] = new Lock(1 + i);
            }

            Task[] tasks = new Task[n];

            for (int i = 0; i < n; i++)
            {
                var philosopher = client.GetGrain<IPhilosopherGrain3>(i);
                int ci = i;
                tasks[i] = Task.Run(() => philosopher.Eat(ci).Wait());
            }

            await Task.WhenAll(tasks);
        }

        public interface IPhilosopherGrain3 : IGrainWithIntegerKey
        {
            NativeTasks.Task Eat(int id);
        }

        public class PhilosopherGrain3 : Grain, IPhilosopherGrain3
        {
            public NativeTasks.Task Eat(int id)
            {
                Console.WriteLine(n);
                int left = id % n;
                int right = (id + 1) % n;

                nekara.ContextSwitch();
                var releaserR = locks[right].Acquire();

                nekara.ContextSwitch();
                var releaserL = locks[left].Acquire();

                Console.WriteLine("Philosopher {0} eats!", id);

                nekara.ContextSwitch();
                releaserL.Dispose();

                nekara.ContextSwitch();
                releaserR.Dispose();

                using (countLock.Acquire())
                {
                    ++phil;
                    nekara.Assert(phil != n, "Bug found!");
                }

                return NativeTasks.Task.CompletedTask;
            }
        }

    }
}
