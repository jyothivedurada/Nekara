﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using AsyncTester.Networking;
using Newtonsoft.Json.Linq;

namespace AsyncTester.Core
{
    public class OmniClient : IClient
    {
        private OmniClientConfiguration config;
        private Func<string, JArray, Task<JToken>> _SendRequest;    // delegate method to be implemented by differnet transport mechanisms
        // private Func<string, Task> Subscribe;            // using topic-based Publish-Subscribe
        // private Func<string, string, Task> Publish;      // using topic-based Publish-Subscribe

        public OmniClient(OmniClientConfiguration config)
        {
            this.config = config;

            // Depending on the transport, create the appropriate communication interface
            switch (this.config.transport)
            {
                case Transport.IPC:
                    SetupTransportIPC();
                    break;
                case Transport.HTTP:
                    SetupTransportHTTP();
                    break;
                case Transport.GRPC:
                    SetupTransportGRPC();
                    break;
                case Transport.WS:
                    SetupTransportWS();
                    break;
                case Transport.TCP:
                    SetupTransportTCP();
                    break;
                default: throw new Exception(); // TODO: make a proper exception later
            }

            // __testStart();
        }

        private void SetupTransportIPC()
        {
            // Create and register the IPC channel
            IpcClientChannel channel = new IpcClientChannel();
            ChannelServices.RegisterChannel(channel, false);

            // Fetch the proxy object -- an interface to the service
            RemotingConfiguration.RegisterWellKnownClientType(typeof(TestingService), "ipc://tester/service");

            TestingService service = new TestingService();

            // Assign the appropriate SendRequest method
            this._SendRequest = (string func, JArray args) =>
            {
                // TODO: this function is incomplete - work on it later
                return Task.FromResult(JValue.Parse("true"));
            };
        }

        private void SetupTransportHTTP()
        {
            HttpClient client = new HttpClient("http://localhost:8080/");

            // Assign the appropriate SendRequest method
            this._SendRequest = (string func, JArray args) =>
            {
                return client.Post("rpc/", new RequestMessage("Tester-Client", "Tester-Server", func, args))
                    .ContinueWith(prev => JToken.Parse(prev.Result));
            };
        }

        private void SetupTransportGRPC()
        {

        }

        private void SetupTransportWS()
        {
            WebSocketClient client = new WebSocketClient("ws://localhost:8080/ws/");
            /*client.onMessage += (string data) =>
            {
                Console.WriteLine("WSC.onMessage triggered: {0}", data);
            };*/

            // Assign the appropriate SendRequest method
            this._SendRequest = (string func, JArray args) => client.Request("Tester-Server", func, args);
        }

        private void SetupTransportTCP()
        {

        }

        // overloading the main SendRequest method to deal with variadic arguments
        public Task<JToken> SendRequest(string func)
        {
            return this._SendRequest(func, JArray.Parse("[]"));
        }

        public Task<JToken> SendRequest(string func, JArray args)
        {
            return this._SendRequest(func, args);
        }

        public Task<JToken> SendRequest(string func, params bool[] args)
        {
            return this._SendRequest(func, new JArray(args));
        }

        public Task<JToken> SendRequest(string func, params int[] args)
        {
            return this._SendRequest(func, new JArray(args));
        }

        public Task<JToken> SendRequest(string func, params string[] args)
        {
            return this._SendRequest(func, new JArray(args));
        }

        // Using this method only during the early stages of development
        // Will be removed after everything is setup
        private void __testStart()
        {

            Helpers.AsyncTaskLoop(() =>
            {
                Console.Write("HTTP: ");
                string input = Console.ReadLine();
                input = Regex.Replace(input, @"[ \t]+", " ");

                string[] tokens = input.Split(' ');
                if (tokens.Length > 0)
                {
                    string cmd = tokens[0].ToLower();
                    if (cmd == "exit" || cmd == "quit") Environment.Exit(0);
                    else if (tokens.Length > 2)
                    {
                        if (cmd == "echo")
                        {
                            return this._SendRequest(cmd, new JArray(tokens.Skip(1)));
                        }
                        else if (cmd == "do")
                        {
                            string func = tokens[1];
                            JArray args = new JArray(tokens.Skip(2));
                            return this._SendRequest(func, args);
                        }
                    }
                }

                return Task.Run(() => { });
            });

            // block the main thread here to prevent exiting - as AsyncTaskLoop will return immediately
            while (true)
            {
            }
        }
    }
}