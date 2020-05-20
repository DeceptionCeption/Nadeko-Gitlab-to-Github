//using Ayu.Common;
//using Newtonsoft.Json;
//using Serilog;
//using StackExchange.Redis;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;

//namespace Nadeko.Common.Local
//{
//    public class CredsService : ServiceConfig<Creds>
//    {
//        // todo 3.1 credentials should be moved for selfhosters from ./ to ./data
//        //private const string oldCredsFileName = "data/credentials.json";
//        //private const string oldCredsBackupFileName = "data/old_credentials.json";
//        public const string CredsFileName = "config/creds.yml";
//        private readonly Random _rand;

//        private readonly ConnectionMultiplexer _multi;
//        private readonly string _key;

//        //static CredsService()
//        //{
//        //    MigrateCredentials();
//        //}

//        public CredsService(bool isMaster = false) : base(CredsFileName, isMaster)
//        {
//            _rand = new Random();
//            _multi = ConnectionMultiplexer.Connect(Data.RedisOptions);
//            _key = $"nadeko:config_reloaded:creds";

//            // this will be true only on shardid = 0, that's the master creds, it will
//            if (isMaster)
//            {
//                //TryMigrateCredentials();
//                GenerateDbId();
//                // creds service will have it's own watcher, which will publish when a change has been made
//                // andother creds services will read from the file when they get the event
//            }

//            RedisServiceConfigHelper.SubscribeToChanges(_multi, _key, ReloadConfig);
//        }

//        public ConnectionMultiplexer GetRedisConnectionMultiplexer() => _multi;

//        protected override void OnFileEvent(object sender, FileSystemEventArgs ev)
//            => RedisServiceConfigHelper.OnFileEvent(_multi, _key, ev, _fullFilePath);

//        private void GenerateDbId()
//        {
//            string GenRandomString() => new string(Enumerable.Range(0, 5).Select(x => (char)_rand.Next(97, 97 + 26)).ToArray());
//            var newData = Data;
//            if (newData.Db is null)
//            {
//                newData.Db = new Creds.DbOptions();
//            }

//            if (string.IsNullOrWhiteSpace(newData.Db.UniqueId))
//            {
//                newData.Db.UniqueId = GenRandomString();
//            }

//            SetCustomConfig(ref newData);
//        }

//        //private void TryMigrateCredentials()
//        //{
//        //    // Check if there's a V2 credentials file present. If so,
//        //    // load it, convert it to YAML, save the yml file, and rename
//        //    // the JSON file.
//        //    if (File.Exists(oldCredsFileName))
//        //    {
//        //        Log.Information("Migrating old creds...");
//        //        var jsonCredentialsFileText = File.ReadAllText(oldCredsFileName);
//        //        var oldCreds = JsonConvert.DeserializeObject<Creds.Old>(jsonCredentialsFileText);

//        //        var creds = Data;
//        //        creds.CleverbotApiKey = oldCreds.CleverbotApiKey;
//        //        creds.Votes = new Creds.VotesSettings
//        //        {
//        //            Key = oldCreds.VotesToken,
//        //            Url = oldCreds.VotesUrl,
//        //        };
//        //        creds.BotListToken = oldCreds.BotListToken;
//        //        creds.GoogleApiKey = oldCreds.GoogleApiKey;
//        //        creds.CarbonKey = oldCreds.CarbonKey;
//        //        creds.ClientId = oldCreds.ClientId;
//        //        creds.OwnerIds = oldCreds.OwnerIds.Distinct().ToHashSet();
//        //        creds.Patreon = new Creds.PatreonSettings
//        //        {
//        //            AccessToken = oldCreds.PatreonAccessToken,
//        //            CampaignId = oldCreds.PatreonCampaignId,
//        //        };
//        //        creds.Token = oldCreds.Token;
//        //        creds.TotalShards = oldCreds.TotalShards <= 1 ? 1 : oldCreds.TotalShards;
//        //        creds.Version = 1;

//        //        SetCustomConfig(ref creds);
//        //        //var serializedCredentials = Yaml.Serializer.Serialize(creds);
//        //        //File.WriteAllText(CredsFileName, serializedCredentials);
//        //        File.Move(oldCredsFileName, oldCredsBackupFileName, true);

//        //        Log.Warning("OLD CREDS MIGRATED, RESTART THE BOT ONCE DATA MIGRATION IS DONE!!!");
//        //    }
//        //}

//    }
//}
