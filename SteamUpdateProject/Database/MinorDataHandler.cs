using System.IO;
using Newtonsoft.Json;
using Newtonsoft;
using System.Text;

namespace SteamUpdateProject
{
    /// <summary>
    /// This handles small data like <see cref="LoggingAndErrorHandler.Updates"/>, <see cref="LoggingAndErrorHandler.ContentUpdates"/>, <see cref="LoggingAndErrorHandler.Exceptions"/> and finally <see cref="LoggingAndErrorHandler.MinutesRunning"/> so we can keep track of those small things.
    /// </summary>
    internal class MinorDataHandler
    {
        private readonly string _operatingFile = Directory.GetCurrentDirectory() + "//SteamData.data";

        private enum DataEnum
        {
            Updates = 0,
            Content = 1,
            Exceptions = 2,
            MinutesRunning = 3
        }

        /// <summary>
        /// Writes <see cref="LoggingAndErrorHandler.Updates"/>, <see cref="LoggingAndErrorHandler.ContentUpdates"/>, <see cref="LoggingAndErrorHandler.Exceptions"/> and finally <see cref="LoggingAndErrorHandler.MinutesRunning"/> into <see cref="SteamData.data"/>
        /// </summary>
        public void WriteData()
        {
            using (StreamWriter writer = new StreamWriter(_operatingFile))
            {
                writer.WriteLine(LoggingAndErrorHandler.Updates);
                writer.WriteLine(LoggingAndErrorHandler.ContentUpdates);
                writer.WriteLine(LoggingAndErrorHandler.Exceptions);
                writer.WriteLine(LoggingAndErrorHandler.MinutesRunning);
            }
        }

        /// <summary>
        /// Read contents of SteamData.data and updates <see cref="LoggingAndErrorHandler.Updates"/>, <see cref="LoggingAndErrorHandler.ContentUpdates"/>, <see cref="LoggingAndErrorHandler.Exceptions"/> and finally <see cref="LoggingAndErrorHandler.MinutesRunning"/>
        /// </summary>
        public void ReadData()
        {
            if (!File.Exists(_operatingFile))
                return;

            using StreamReader reader = new StreamReader(_operatingFile);

            string[] dataByLine = reader.ReadToEnd().Split("\n");
            for (int i = 0; i <= dataByLine.Length - 1; i++)
            {
                if (!long.TryParse(dataByLine[i], out long number))
                    return;

                switch ((DataEnum)i)
                {
                    case DataEnum.Updates:
                        {
                            LoggingAndErrorHandler.Updates = number;
                            break;
                        }
                    case DataEnum.Content:
                        {
                            LoggingAndErrorHandler.ContentUpdates = number;
                            break;
                        }
                    case DataEnum.Exceptions:
                        {
                            LoggingAndErrorHandler.Exceptions = number;
                            break;
                        }
                    case DataEnum.MinutesRunning:
                        {
                            LoggingAndErrorHandler.MinutesRunning = number;
                            break;
                        }
                }
            }
        }

        public ConfigJson GetConfig()
        {
            var ConfigJson = "";

            using (var fs = File.OpenRead("config.json"))

            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                ConfigJson = sr.ReadToEnd();

            ConfigJson cfgjson = JsonConvert.DeserializeObject<ConfigJson>(ConfigJson);

            if (cfgjson.DevOverride == null)
                cfgjson.DevOverride = "0";

            return cfgjson;
        }

        public struct ConfigJson
        {
            [JsonProperty("token")]
            public string Token { get; private set; }

            [JsonProperty("prefix")]
            public string CommandPrefix { get; private set; }

            [JsonProperty("steamname")]
            public string SteamName { get; private set; }

            [JsonProperty("steampw")]
            public string SteamPassword { get; private set; }

            [JsonProperty("override")]
            public string DevOverride { get; set; }
        }
    }
}
