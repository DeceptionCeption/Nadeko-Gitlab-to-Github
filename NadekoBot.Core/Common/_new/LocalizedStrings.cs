using Ayu.Common;
using Discord.WebSocket;
using Nadeko.Bot.Common;
using Nadeko.Common.Services;
using Nadeko.Common.Services.Db;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ILocalization = Ayu.Common.ILocalization;

namespace Nadeko.Common.Localization
{
    public class LocalizedStrings : ILocalization
    {

        public CultureInfo DefaultCulture {
            get {
                var cultureString = _config.Data.DefaultLocale;
                try
                {
                    return new CultureInfo(cultureString);
                }
                catch
                {
                    Log.Error("Invalid {CultureString} default culture.", cultureString);
                    return _enUsCulture;
                }
            }
        }
        private readonly CultureInfo _enUsCulture = new CultureInfo("en-US");

        //private readonly ConcurrentDictionary<ulong, CultureInfo> _guildLocales = new ConcurrentDictionary<ulong, CultureInfo>();

        private const string _basePath = "config/_strings/";
        private const string responsesPath = _basePath + "responses";
        private const string commandsPath = _basePath + "commands";

        private string fullResPath = Path.GetFullPath(responsesPath);
        private string fullCmdsPath = Path.GetFullPath(commandsPath);


        private readonly BotConfigService _config;

        // key is locale name
        private readonly ConcurrentDictionary<string, Lazy<LangData<string>>> _responses;
        private readonly ConcurrentDictionary<string, Lazy<LangData<CmdStrings>>> _commands;
        private readonly IReadOnlyDictionary<string, string> _alternativeNames;
        private readonly CommonDb _commonDb;
        private readonly IEventRegistryPusher _erp;
        private readonly FileSystemWatcher _resWatcher;

        private readonly TypedKey<string> _key = new TypedKey<string>("nadeko:locale_file_updated");

        public LocalizedStrings(DiscordSocketClient client, CommonDb commonDb, BotConfigService config,
            IEventRegistryHandler erh, IEventRegistryPusher erp)
        {
            _commonDb = commonDb;
            _erp = erp;
            // todo uncomment
            //var localeData = commonDb.GetDbContext().GuildConfigs
            //    .AsQueryable()
            //    .Select(x => new { x.GuildId, x.Locale })
            //    .AsEnumerable();

            //_guildLocales = new ConcurrentDictionary<ulong, CultureInfo>(localeData.ToDictionary(
            //    x => x.GuildId,
            //    y =>
            //    {
            //        try { return new CultureInfo(y.Locale); }
            //        catch { return _enUsCulture; }
            //    }));

            _config = config;

            _responses = GetResponses();
            _commands = GetCommands();

            _alternativeNames = new Dictionary<string, string>
            {
                {"english", _enUsCulture.Name},
                {"english us", _enUsCulture.Name},
                {"english (us)", _enUsCulture.Name},
            };

            if (client.ShardId == 0)
            {
                _resWatcher = new FileSystemWatcher(_basePath, "*.json")
                {
                    NotifyFilter = NotifyFilters.LastWrite,
                    EnableRaisingEvents = true,
                    IncludeSubdirectories = true,
                };
                _resWatcher.Changed += FileWatcher_OnFileChanged;
                _resWatcher.Created += FileWatcher_OnFileChanged;
            }

            erh.Handle(_key, path => { OnFileChanged(path); return Task.CompletedTask; });
        }

        public CultureInfo? TrySetDefaultLocale(string lang)
        {
            if (_alternativeNames.TryGetValue(lang, out var realName))
                lang = realName;

            try
            {
                var ci = CultureInfo.GetCultureInfo(lang);
                var name = ci.Name;

                if (!_responses.TryGetValue(name, out _) && !_commands.TryGetValue(name, out _))
                    return null;

                _config.ModifyBotConfig((ref BotConfig x) => x.DefaultLocale = name);

                return ci;
            }
            catch
            {
                return null;
            }
        }

        public CultureInfo? TrySetLocale(ulong guildId, string lang)
        {
            return null;
            // todo uncomment
            //lang = lang.ToLowerInvariant();
            //if (_alternativeNames.TryGetValue(lang, out var realName))
            //    lang = realName;

            //try
            //{

            //    var ci = lang == "default"
            //        ? DefaultCulture
            //        : CultureInfo.GetCultureInfo(lang);

            //    using var uow = _commonDb.GetDbContext();
            //    var gc = uow.GuildConfigFor(guildId);
            //    gc.Locale = ci.Name;
            //    uow.SaveChanges();

            //    return _guildLocales.AddOrUpdate(guildId, ci, delegate { return ci; });
            //}
            //catch
            //{
            //    return null;
            //}
        }

        public string[] GetResponseLanguagesList()
            => _responses.Keys.ToArray();

        private void FileWatcher_OnFileChanged(object _, FileSystemEventArgs ev)
        {
            Log.Information("Strings file changed: {FullPath}", ev.FullPath);

            _erp.PushAsync(_key, ev.FullPath);
        }
        private void OnFileChanged(string fullPath)
        {
            var fullDirPath = Path.GetFullPath(Path.GetDirectoryName(fullPath)!);

            if (fullDirPath != fullResPath && fullDirPath != fullCmdsPath)
                return;

            bool resFile = fullDirPath == fullResPath;

            if (resFile)
            {
                var maybeLangData = GetSingleResponseFile(fullPath);
                if (!(maybeLangData is (string _, Lazy<LangData<string>> _) langData))
                    return;

                _responses.AddOrUpdate(langData.LocaleName, langData.Data, (key, old) => langData.Data);
            }
            else
            {
                var maybeLangData = GetSingleCommandFile(fullPath);
                if (!(maybeLangData is (string _, Lazy<LangData<CmdStrings>> _) langData))
                    return;

                _commands.AddOrUpdate(langData.LocaleName, langData.Data, (key, old) => langData.Data);
            }
        }

        private ConcurrentDictionary<string, Lazy<LangData<string>>> GetResponses()
        {
            var files = Directory.GetFiles(responsesPath);

            var dict = new ConcurrentDictionary<string, Lazy<LangData<string>>>();

            foreach (var file in files)
            {
                var maybeLangData = GetSingleResponseFile(file);
                if (!(maybeLangData is (string _, Lazy<LangData<string>> _) langData))
                    continue;

                dict.TryAdd(langData.LocaleName, langData.Data);
            }

            return dict;
        }

        private (string LocaleName, Lazy<LangData<string>> Data)? GetSingleResponseFile(string file)
        {
            var localeName = "";
            try
            {
                localeName = GetLocaleName(file);
            }
            catch
            {
                Log.Error("Response strings file has invalid file name: {File}", file);
                return null;
            }

            return (localeName, new Lazy<LangData<string>>(() =>
            {
                Dictionary<string, string> langDict;
                try
                {
                    langDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(file));
                }
                catch
                {
                    Log.Error("Error parsing {LocaleName} response strings file.", localeName);
                    langDict = new Dictionary<string, string>();
                }
                return new LangData<string>(localeName, langDict);
            }, isThreadSafe: true));
        }

        private ConcurrentDictionary<string, Lazy<LangData<CmdStrings>>> GetCommands()
        {
            var files = Directory.GetFiles(commandsPath);

            var dict = new ConcurrentDictionary<string, Lazy<LangData<CmdStrings>>>();

            foreach (var file in files)
            {
                var maybeLangData = GetSingleCommandFile(file);
                if (!(maybeLangData is (string _, Lazy<LangData<CmdStrings>> _) langData))
                    continue;

                dict.TryAdd(langData.LocaleName, langData.Data);
            }

            return dict;
        }

        private (string LocaleName, Lazy<LangData<CmdStrings>> Data)? GetSingleCommandFile(string file)
        {
            var localeName = "";
            try
            {
                localeName = GetLocaleName(file);
            }
            catch
            {
                Log.Error("Response strings file has invalid file name: {File}", file);
                return null;
            }

            return (localeName, new Lazy<LangData<CmdStrings>>(() =>
            {
                Dictionary<string, CmdStrings> langDict;
                try
                {
                    langDict = JsonConvert.DeserializeObject<Dictionary<string, CmdStrings>>(File.ReadAllText(file));
                }
                catch
                {
                    Log.Error("Error parsing {LocaleName} response strings file.", localeName);
                    langDict = new Dictionary<string, CmdStrings>();
                }
                return new LangData<CmdStrings>(localeName, langDict);
            }, isThreadSafe: true));
        }

        public ILocale GetDefaultLocale() => new Locale(this, DefaultCulture);

        public ILocale GetLocale(ulong guildId)
        {
            //if (_guildLocales.TryGetValue(guildId, out var cultureInfo))
            //    return new Locale(this, cultureInfo);
            return GetDefaultLocale();
        }

        private static string GetLocaleName(string fileName)
        {
            var dotIndex = fileName.IndexOf('.') + 1;
            var secondDotIndex = fileName.LastIndexOf('.');
            return fileName[dotIndex..secondDotIndex];
        }

        private string? GetText(string locale, string key, params object[] replacements)
        {
            if (!_responses.TryGetValue(locale, out var languageData)
                || !languageData.Value.Strings.TryGetValue(key, out var value)
                || string.IsNullOrWhiteSpace(value))
            {
                Log.Logger.Warning("Key '{Key}' not found in {Locale} responses. Falling back to {Name}", key, locale, _enUsCulture.Name);

                if (!_responses.TryGetValue(_enUsCulture.Name, out languageData) ||
                    !languageData.Value.Strings.TryGetValue(key, out value)
                    || string.IsNullOrWhiteSpace(value))
                {
                    Log.Logger.Error("Key '{Key}' not found at all in responses. Please report this.", key);
                    return key;
                }
            }

            try
            {
                return string.Format(value, replacements);
            }
            catch (FormatException fex)
            {
                Log.Error(fex, "Can't properly format '{Key}' command data from '{Locale}' locale.", key, locale);
                return default;
            }
        }

        private CmdStrings? GetCommand(string locale, string key)
        {
            key = key.Trim().ToLowerInvariant();

            if (!_commands.TryGetValue(locale, out var languageData)
                || !languageData.Value.Strings.TryGetValue(key, out var value))
            {
                Log.Logger.Warning("Key '{Key}' not found in {Locale} command strings. Falling back to {Name}", key, locale, _enUsCulture.Name);

                if (!_commands.TryGetValue(_enUsCulture.Name, out languageData)
                    || !languageData.Value.Strings.TryGetValue(key, out value))
                {
                    Log.Logger.Error("Key '{Key}' not found at all in command strings. Please report this.", key);
                    return default;
                }
            }

            //Log.Logger.Verbose("Returning command data for: {Key}", key);
            return value;
        }


        public string GetText(ulong guildId, string key, params object[] replacements)
            => GetLocale(guildId).GetText(key, replacements);

        public string? GetText(CultureInfo culture, string key, params object[] replacements)
           => GetText(culture.Name, key, replacements);

        public CmdStrings GetCommandStrings(ulong guildId, string key)
            => GetLocale(guildId).GetCommand(key);

        public CmdStrings? GetCommandStrings(CultureInfo culture, string key)
           => GetCommand(culture.Name, key);
    }
}
