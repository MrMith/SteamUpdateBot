using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;

namespace SteamUpdateProject.Entities
{
	/// <summary>
	/// This is the class that is stored in the database and handles all of the logic related to adding/removing subscriptions from a given discord guild.
	/// </summary>
	[BsonIgnoreExtraElements]
	public class GuildInfo : IEquatable<GuildInfo>
	{
		/// <summary>
		/// Name of document? in mongoDB.
		/// </summary>
		public const string DBName = "GuildInfo";

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
		public virtual List<SubbedApp> SubscribedApps { get; set; } = [];

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
				SubscribedApps.Add(new SubbedApp(appid));

				IMongoDatabase db = SteamUpdateBot.DB.Client.GetDatabase(SteamUpdateBot.DatabaseName);

				FilterDefinition<GuildInfo> gI_Filter = Builders<GuildInfo>.Filter.And(
								Builders<GuildInfo>.Filter.Eq("ChannelID", ChannelID),
								Builders<GuildInfo>.Filter.Eq("GuildID", GuildID));

				IMongoCollection<GuildInfo> gIcollection = db.GetCollection<GuildInfo>(GuildInfo.DBName);

				gIcollection.ReplaceOne(gI_Filter, this);
				return true;
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
			List<uint> listOfAddedApps = [];

			foreach (uint appid in listOfApps)
			{
				if (!IsSubbed(appid))
				{
					listOfAddedApps.Add(appid);
				}
			}

			if (listOfAddedApps.Count != 0)
			{
				foreach (uint app in listOfAddedApps)
				{
					SubscribedApps.Add(new SubbedApp(app));
				}

				IMongoDatabase db = SteamUpdateBot.DB.Client.GetDatabase(SteamUpdateBot.DatabaseName);

				FilterDefinition<GuildInfo> gI_Filter = Builders<GuildInfo>.Filter.And(
								Builders<GuildInfo>.Filter.Eq("ChannelID", ChannelID),
								Builders<GuildInfo>.Filter.Eq("GuildID", GuildID));

				IMongoCollection<GuildInfo> gIcollection = db.GetCollection<GuildInfo>(GuildInfo.DBName);

				gIcollection.ReplaceOne(gI_Filter, this);
			}

			return listOfAddedApps;
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

			SubscribedApps.RemoveAll(subbedApp => subbedApp.AppID == appid);

			IMongoDatabase db = SteamUpdateBot.DB.Client.GetDatabase(SteamUpdateBot.DatabaseName);

			FilterDefinition<GuildInfo> gI_Filter = Builders<GuildInfo>.Filter.And(
							Builders<GuildInfo>.Filter.Eq("ChannelID", ChannelID),
							Builders<GuildInfo>.Filter.Eq("GuildID", GuildID));

			IMongoCollection<GuildInfo> gIcollection = db.GetCollection<GuildInfo>(GuildInfo.DBName);

			gIcollection.ReplaceOne(gI_Filter, this);

			return true;
		}

		/// <summary>
		/// Removes multiple subscribed AppIDs from a <see cref="GuildInfo"/>
		/// </summary>
		/// <param name="listofapps">Relevant list of AppIDs</param>
		/// <returns>List of AppIDs that have been removed successfully.</returns>
		public List<uint> RemoveMultipleApps(List<uint> listOfApps)
		{
			List<uint> appsThatHaveBeenRemoved = [];

			foreach (uint appid in listOfApps)
			{
				if (IsSubbed(appid))
				{
					appsThatHaveBeenRemoved.Add(appid);
				}
			}

			if (appsThatHaveBeenRemoved.Count != 0)
			{
				SubscribedApps.RemoveAll(subbedApp => appsThatHaveBeenRemoved.Contains((uint) subbedApp.AppID));

				IMongoDatabase db = SteamUpdateBot.DB.Client.GetDatabase(SteamUpdateBot.DatabaseName);

				FilterDefinition<GuildInfo> gI_Filter = Builders<GuildInfo>.Filter.And(
								Builders<GuildInfo>.Filter.Eq("ChannelID", ChannelID),
								Builders<GuildInfo>.Filter.Eq("GuildID", GuildID));

				IMongoCollection<GuildInfo> gIcollection = db.GetCollection<GuildInfo>(GuildInfo.DBName);

				gIcollection.ReplaceOne(gI_Filter, this);
			}

			return appsThatHaveBeenRemoved;
		}

		public List<uint> RemoveMultipleApps(List<SubbedApp> listOfApps)
		{
			List<uint> listToReturn = [];

			foreach (SubbedApp app in listOfApps)
			{
				listToReturn.Add((uint) app.AppID);
			}

			return RemoveMultipleApps(listToReturn);
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
