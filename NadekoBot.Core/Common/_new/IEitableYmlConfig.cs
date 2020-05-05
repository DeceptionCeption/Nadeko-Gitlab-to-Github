namespace Nadeko.Bot.Common
{
    public interface IEditableYmlConfig
    {
        bool IsSensitive => false;
        string ConfigName { get; }
        string GetConfigText();
        void SetConfig(string configText);
    }
}
