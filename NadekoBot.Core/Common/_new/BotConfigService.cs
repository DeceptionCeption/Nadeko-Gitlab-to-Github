using Microsoft.EntityFrameworkCore;
using Nadeko.Bot.Common;
using Nadeko.Common.Services.Db;
using Nadeko.Common.Yml;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System;
using StackExchange.Redis;
using NadekoBot.Core.Services;
using Discord.WebSocket;

namespace Nadeko.Common.Services
{
    public class BotConfigService : RedisServiceConfig<BotConfig>, IEditableYmlConfig, INService
    {
        private readonly SemaphoreSlim _credsSemaphore = new SemaphoreSlim(1, 1);

        public const string ConfigFileName = "config/bot.yml";

        private readonly CommonDb _db;
        private readonly CredsService _creds;
        private readonly IMemoryCache _cache;

        public Creds Creds => _creds.Data;

        public bool IsSensitive => true;
        public string ConfigName => "creds";

        private readonly ConcurrentDictionary<ulong, object> configLocks = new ConcurrentDictionary<ulong, object>();

        public BotConfigService(
            CommonDb configDb,
            CredsService creds,
            DiscordSocketClient client,
            ConnectionMultiplexer multi,
            IMemoryCache cache
        ) : base(multi, ConfigFileName, client.ShardId == 0)
        {
            _db = configDb;
            _creds = creds;

            _cache = cache;
        }

        private object GetLock(ulong guildId) => configLocks.GetOrAdd(guildId, _ => new object());

        public delegate void ActionRef<T>(ref T config);
        public BotConfig ModifyBotConfig(ActionRef<BotConfig> modifyFunc)
        {
            var configLock = GetLock(default);
            lock (configLock)
            {
                var newConf = Data;
                modifyFunc(ref newConf);
                SetCustomConfig(ref newConf);
                return newConf;
            }
        }

        //private GuildConfig? InternalGetGuildConfig(ulong guildId)
        //{
        //    using var uow = _db.GetDbContext();
        //    return uow.GuildConfigs
        //        .AsNoTracking()
        //        .FirstOrDefault(x => x.GuildId == guildId);
        //}

        //public BotConfig GetConfig(ulong? maybeGuildId)
        //{
        //    if (!(maybeGuildId is ulong guildId))
        //    {
        //        return Data;
        //    }

        //    var guildConfig = _cache.GetOrCreate("guild_config:" + guildId, e =>
        //    {
        //        e.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30);
        //        return InternalGetGuildConfig(guildId);
        //    });

        //    var data = Data;

        //    data.Prefix = guildConfig?.CommandString ?? data.Prefix;
        //    data.PrefixIsSuffix = guildConfig?.CommandStringIsSuffix ?? data.PrefixIsSuffix;

        //    return data;
        //}

        public async Task<ulong[]> AddOwnersAsync(ulong[] toAdd)
        {
            await _credsSemaphore.WaitAsync();
            try
            {
                var newConfig = _creds.Data;
                newConfig.OwnerIds = new HashSet<ulong>(newConfig.OwnerIds);
                foreach (var ownerId in toAdd)
                {
                    newConfig.OwnerIds.Add(ownerId);
                }

                _creds.SetCustomConfig(ref newConfig);

                return _creds.Data.OwnerIds.ToArray();
            }
            finally
            {
                _credsSemaphore.Release();
            }
        }

        public async Task<ulong[]> RemoveOwners(ulong[] toRemove)
        {
            await _credsSemaphore.WaitAsync();
            try
            {
                var newConfig = _creds.Data;
                newConfig.OwnerIds = new HashSet<ulong>(newConfig.OwnerIds);
                foreach (var ownerId in toRemove)
                {
                    newConfig.OwnerIds.Remove(ownerId);
                }

                _creds.SetCustomConfig(ref newConfig);
                return _creds.Data.OwnerIds.ToArray();
            }
            finally
            {
                _credsSemaphore.Release();
            }
        }

        //public async Task<bool> SetPrefixAsync(ulong guildId, string? cmdString, bool? isSuffix)
        //{
        //    cmdString = cmdString?.Trim().ToLowerInvariant();

        //    if (guildId == 0)
        //    {
        //        // you can't set default commandstring to null
        //        if (string.IsNullOrWhiteSpace(cmdString))
        //            return false;

        //        var data = Data;
        //        data.Prefix = cmdString;
        //        if (!(isSuffix is null))
        //        {
        //            data.PrefixIsSuffix = isSuffix.Value;
        //        }
        //        SetCustomConfig(ref data);
        //        return true;
        //    }
        //    else
        //    {
        //        using var uow = _db.GetDbContext();
        //        var config = uow.GuildConfigs.FirstOrDefault(x => x.GuildId == guildId);
        //        if (config is null)
        //        {
        //            uow.Add(config = new GuildConfig
        //            {
        //                GuildId = guildId,
        //                CommandString = cmdString,
        //                CommandStringIsSuffix = isSuffix,
        //            });
        //        }
        //        else
        //        {
        //            config.CommandString = cmdString;
        //            config.CommandStringIsSuffix = isSuffix;
        //        }

        //        await uow.SaveChangesAsync();

        //        _cache.Set("guild_config:" + guildId, config, TimeSpan.FromSeconds(30));
        //        return true;
        //    }
        //}

        public string GetConfigText()
            => Yaml.Serializer.Serialize(_creds.Data);

        public void SetConfig(string configText)
        {
            var obj = Yaml.Deserializer.Deserialize<Creds>(configText);
            _creds.SetCustomConfig(ref obj);
        }
    }
}
