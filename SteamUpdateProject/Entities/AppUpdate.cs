using System;
using System.Collections.Generic;

namespace SteamUpdateProject.Entities
{
    /// <summary>
    /// Dummy class that is stored in the SQL database that contains information like <see cref="Name"/>, <see cref="AppID"/>, global steam <see cref="ChangeNumber"/>, Is there any meaningful <see cref="Content"/> updates,
    /// what time (UTC) this app last updated at (that we know of) <see cref="LastUpdated"/> and DepoName that updated <see cref="DepoName"/>.
    /// </summary>
    public class AppUpdate : IEquatable<AppUpdate>
    {
        /// <summary>
        /// Steam Application Name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Steam Application ID.
        /// </summary>
        public uint AppID { get; set; }

        /// <summary>
        /// Steam overall number used for indexing their changes <br/>
        /// Change #10000000 might be that app named "Neon Abyss" updated and changed their experimental steam branchs contents<br/>
        /// While Change #10000036 might be an app named "Hexteria" will have its app and multiple packages with changes.
        /// </summary>
        public uint ChangeNumber { get; set; }

        /// <summary>
        /// If this update has depo content updates.
        /// </summary>
        public bool Content { get; set; }

        /// <summary>
        /// Last time (In Seattle time since that where Valve HQ is at) this app was updated.
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Depo (Depo being the part of the application that holds the physical game data) that was changed.
        /// </summary>
        public string DepoName { get; set; }

        #region Equality Checks
        public override bool Equals(object obj)
        {
            return Equals(obj as AppUpdate);
        }

        public bool Equals(AppUpdate other)
        {
            return AppID == other.AppID;
        }

        public static bool operator ==(AppUpdate left, AppUpdate right)
        {
            return EqualityComparer<AppUpdate>.Default.Equals(left, right);
        }

        public static bool operator !=(AppUpdate left, AppUpdate right)
        {
            return !(left == right);
        }

        public bool Equals(AppInfo other)
        {
            return other != null &&
                   AppID == other.AppID;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(AppID);
        }

        public static bool operator ==(AppInfo left, AppUpdate right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AppInfo left, AppUpdate right)
        {
            return !(left == right);
        }

        public static bool operator ==(AppUpdate left, AppInfo right)
        {
            return left.Equals(right);
        }
        public static bool operator !=(AppUpdate left, AppInfo right)
        {
            return !(left == right);
        }
        #endregion
    }
}
