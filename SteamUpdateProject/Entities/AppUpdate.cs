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
        public string Name { get; set; }
        public uint AppID { get; set; }
        public uint ChangeNumber { get; set; }
        public bool Content { get; set; }
        public DateTime LastUpdated { get; set; }
        public string DepoName { get; set; }

        #region Equality checks

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
