﻿using Nekara.Core;
using Nekara.Networking;

namespace Nekara.Client
{
    // This is a global singleton that gets updated dynamically during runtime
    public static class RuntimeEnvironment
    {
        public static NekaraClient Client { get; set; }

        static RuntimeEnvironment()
        {
            // client-side socket
            OmniClient socket = new OmniClient(new OmniClientConfiguration());

            // testing service proxy object;uses the socket to communicate to the actual testing service
            Client = new NekaraClient(socket);

            socket.ReadyFlag.Wait();    // synchronously wait till the socket establishes connection
        }
    }
}