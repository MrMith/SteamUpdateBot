using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MongoDB.Bson.Serialization.Attributes;

namespace SteamUpdateProject.Entities
{
	/// <summary>
	/// Dummy class that is used to pass information (Like AppID and Name) from one area to another (Ex: SteamBot to DiscordBot)
	/// </summary>
	[BsonIgnoreExtraElements]
	public class AppInfo : IEquatable<AppInfo>
    {
		/// <summary>
		/// Name in MongoDB.
		/// </summary>
		public static string DBName = "AppInfo";

        /// <summary>
        /// Key used by the Database, don't touch!
        /// </summary>
        [Key]
        public int Key { get; set; }

        /// <summary>
        /// Steam Application ID.
        /// </summary>
        public long AppID { get; set; }

        /// <summary>
        /// Steam Application Name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Last time (In Seattle time since that where Valve HQ is at) this app was updated.
        /// </summary>
        public DateTime? LastUpdated { get; set; }

        #region Equality Methods.
        public override bool Equals(object obj)
        {
            return Equals(obj as AppInfo);
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

        public static bool operator ==(AppInfo left, AppInfo right)
        {
            return EqualityComparer<AppInfo>.Default.Equals(left, right);
        }

        public static bool operator !=(AppInfo left, AppInfo right)
        {
            return !(left == right);
        }
        #endregion
    }
}
