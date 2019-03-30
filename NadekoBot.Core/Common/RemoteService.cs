using Grpc.Core;
using System;

namespace NadekoBot.Core.Common
{
    public class RemoteService
    {
        public static T CreateClient<T>(string host, int port) where T : ClientBase<T>
        {
            Channel channel = new Channel(host + ":" + port, ChannelCredentials.Insecure);

            var client = (T)Activator.CreateInstance(typeof(T), channel);
            return client;
        }
    }

}
