using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SteamUpdateProject.Entities
{
    /// <summary>
    /// This is the dummy class that is stored in the database.
    /// </summary>
    public class GuildInfo : IEquatable<GuildInfo>
    {
        [Key]
        public int Key { get; set; }

        /// <summary>
        /// Relevant GuildID
        /// </summary>
        public long GuildID { get; set; }

        /// <summary>
        /// Text Channel ID to push updates to.
        /// </summary>
        public long ChannelID { get; set; }

        /// <summary>
        /// Only show meaningful updates or not.
        /// </summary>
        public bool ShowContent { get; set; }

        /// <summary>
        /// Debug mode that just spams any update that comes through the pipeline.
        /// </summary>
        public bool DebugMode { get; set; }

        /// <summary>
        /// Only pipe through updates that have changes from the default public branch of a steam app.
        /// </summary>
        public bool PublicDepoOnly { get; set; }

        /// <summary>
        /// List of apps this certain channel is subscribed to.
        /// </summary>
        public virtual List<SubbedApp> SubscribedApps { get; set; } = new List<SubbedApp>();

        /// <summary>
        /// Checks if this Guild is subscribed to this app.
        /// </summary>
        /// <param name="appID">Relevant AppID.</param>
        /// <returns>Is this guild subscribed to this app.</returns>
        public bool IsSubbed(long appID)
        {
            return SubscribedApps.Exists(subbedApp => subbedApp.AppID == appID);
        }

        /// <summary>
        /// Takes one steam AppID and adds them to the subscribed list of the relevant <see cref="GuildInfo"/>.
        /// </summary>
        /// <param name="appid">Relevant AppID.</param>
        /// <param name="info">Relevant <see cref="GuildInfo"/></param>
        /// <returns>If the app was successfully added to the subscription list.</returns>
        public bool SubApp(uint appid)
        {
            if (!IsSubbed(appid))
            {
                using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
                {
                    context.GuildInformation.RemoveRange(context.AllGuilds.FindAll(guild => guild == this));
                    SubscribedApps.Add(new SubbedApp(appid));
                    context.GuildInformation.Add(this);
                    context.SaveChanges();
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Takes list of steam appIDs and adds them to the subscribed list of the relevant <see cref="GuildInfo"/>.
        /// </summary>
        /// <param name="listofapps">List of apps to add.</param>
        /// <returns>List of AppIDs that were added to the subscribed list of <see cref="GuildInfo"/>.</returns>
        public List<uint> SubMultipleApps(List<uint> listOfApps)
        {
            List<uint> ListOfAddedApps = new List<uint>();

            foreach (uint appid in listOfApps)
            {
                if (!IsSubbed(appid))
                {
                    ListOfAddedApps.Add(appid);
                }
            }

            if (ListOfAddedApps.Count != 0)
            {
                using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
                {
                    context.GuildInformation.RemoveRange(context.AllGuilds.FindAll(guild => guild == this));
                    foreach (uint app in ListOfAddedApps)
                    {
                        SubscribedApps.Add(new SubbedApp(app));
                    }
                    context.GuildInformation.Add(this);
                    context.SaveChanges();
                }
            }

            return ListOfAddedApps;
        }

        /// <summary>
        /// Removes multiple one AppID from a <see cref="GuildInfo"/>
        /// </summary>
        /// <param name="appid">Relevant AppID</param>
        /// <returns>Is the app successfully removed</returns>
        public bool RemoveApp(uint appid)
        {
            if (!IsSubbed(appid))
                return false;

            using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
            {
                context.GuildInformation.RemoveRange(context.AllGuilds.FindAll(guild => guild == this));
                SubscribedApps.RemoveAll(SubbedApp => SubbedApp.AppID == appid);
                context.GuildInformation.Add(this);
                context.SaveChanges();
            }

            return true;
        }

        /// <summary>
        /// Removes multiple subscribed AppIDs from a <see cref="GuildInfo"/>
        /// </summary>
        /// <param name="listofapps">Relevant list of AppIDs</param>
        /// <returns>List of AppIDs that have been removed successfully.</returns>
        public List<uint> RemoveMultipleApps(List<uint> listOfApps)
        {
            List<uint> AppsThatHaveBeenRemoved = new List<uint>();

            foreach (uint appid in listOfApps)
            {
                if (IsSubbed(appid))
                {
                    AppsThatHaveBeenRemoved.Add(appid);
                }
            }

            if (AppsThatHaveBeenRemoved.Count != 0)
            {
                using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
                {
                    context.GuildInformation.RemoveRange(context.AllGuilds.FindAll(guild => guild == this));
                    SubscribedApps.RemoveAll(SubbedApp => AppsThatHaveBeenRemoved.Contains((uint)SubbedApp.AppID));
                    context.GuildInformation.Add(this);
                    context.SaveChanges();
                }
            }

            return AppsThatHaveBeenRemoved;
        }

        #region Equality Functions
        public override bool Equals(object obj)
        {
            return Equals(obj as GuildInfo);
        }

        public bool Equals(GuildInfo other)
        {
            return other != null &&
                   GuildID == other.GuildID && ChannelID == other.ChannelID;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(GuildID, ChannelID);
        }

        public static bool operator ==(GuildInfo left, GuildInfo right)
        {
            return EqualityComparer<GuildInfo>.Default.Equals(left, right);
        }

        public static bool operator !=(GuildInfo left, GuildInfo right)
        {
            return !(left == right);
        }
        #endregion
    }
}
