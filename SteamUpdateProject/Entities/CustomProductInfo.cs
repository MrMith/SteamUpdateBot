using SteamKit2;
using System.Collections.ObjectModel;

namespace SteamUpdateProject.Entities
{
    /// <summary>
    /// This exists so we know when an app updates if as a normal user we have access to the depo content.
    /// </summary>
    internal class CustomProductInfo
    {
        /// <summary>
        /// All the changes regarding a given CurrentChangeNumber from steam.
        /// </summary>
        public ReadOnlyCollection<SteamApps.PICSProductInfoCallback> ProductInfo { get; set; }

        /// <summary>
        /// If as a normal user we have access to the depo content.
        /// </summary>
        public bool IsPublic { get; set; }
    }
}
