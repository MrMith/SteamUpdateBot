﻿using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using SteamUpdateProject.Discord.Commands;
using SteamUpdateProject.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using MongoDB;
using MongoDB.Bson;
using MongoDB.Driver;

namespace SteamUpdateProject.Discord
{
	/// <summary>
	/// This handles everything related to the discord-side of the bot and some minor utility methods.
	/// </summary>
	internal class DiscordBot
	{
		/// <summary>
		/// The bot <see cref="DiscordClient"/> that we talk to..
		/// </summary>
		public DiscordClient Client;

		/// <summary>
		/// If we have a dev override for any commands needing certain channel permissions, uses <see cref="SteamUpdateBot.OverrideDiscordID"/> when checking permission if this is true.
		/// </summary>
		public bool DevOverride = false;

		private readonly Random _rand = new Random();
		private DateTime _timeForStatusUpdate = DateTime.Now;
		private bool _botReady = false;

		/// <summary>
		/// Starts discord bot and setups the commands.
		/// </summary>
		/// <param name="token">Discord Bot Token</param>
		public async Task StartDiscordBot(string token)
		{
			Client = new DiscordClient(new DiscordConfiguration()
			{
				Token = token,
				TokenType = TokenType.Bot,
				Intents = DiscordIntents.AllUnprivileged,
			});

			CommandsNextExtension commands = Client.UseCommandsNext(new CommandsNextConfiguration()
			{
				StringPrefixes = new[] { "!" },
				EnableDms = true,
				EnableMentionPrefix = true,
			});

			commands.RegisterCommands<SubscriptionModule>();
			commands.RegisterCommands<UtilityModule>();
			commands.SetHelpFormatter<CustomHelpFormatter>();

			this.Client.UseInteractivity(new InteractivityConfiguration
			{
				PaginationBehaviour = PaginationBehaviour.WrapAround,
				Timeout = TimeSpan.FromMinutes(4),
				AckPaginationButtons = true,
			});

			commands.CommandExecuted += Commands_CommandExecuted;
			commands.CommandErrored += Commands_CommandErrored;

			await Client.ConnectAsync();
			_botReady = true;
		}

		private Task Commands_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
		{
			if (e.Exception is DSharpPlus.CommandsNext.Exceptions.CommandNotFoundException) //I do not want to spy on people.
				return Task.CompletedTask;

			var ExceptionToSend = new Exception($"{e.Command} Was Executed: {e.Exception.ToString()}");

			SteamUpdateBot.LAEH.BadlyFormattedFunction(ExceptionToSend);
			return Task.CompletedTask;
		}

		private Task Commands_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
		{
			//To-do add logging to prevent abuse.
			return Task.CompletedTask;
		}

		/// <summary>
		/// This will take an <see cref="AppUpdate"/> and take that information and send discord messages to <see cref="GuildInfo"/> that have those Apps subscribed.
		/// </summary>
		/// <param name="app"><see cref="AppUpdate"/> that has information like the Steam AppID, if its a content update and the Depo name.</param>
		public async void AppUpdated(AppUpdate app)
		{
			if (!_botReady)
				return;

			Console.WriteLine($"AppUpdated: {app.AppID} {(app.Content ? "(Content)" : "")}");

			if (DateTime.Now > _timeForStatusUpdate)
				UpdateStatus();

			DiscordColor Color = new DiscordColor((float)_rand.Next(1, 100) / 100, (float)_rand.Next(1, 100) / 100, (float)_rand.Next(1, 100) / 100);

			DiscordEmbedBuilder AppEmbed = new DiscordEmbedBuilder
			{
				Color = Color,
				Timestamp = DateTimeOffset.UtcNow,
				Title = "Steam App Update!"
			};

			//If this URL goes down then the bot has more things to worry about than a URL not working.
			AppEmbed.ImageUrl = app.Name == null ? "https://steamstore-a.akamaihd.net/public/shared/images/header/globalheader_logo.png?t=962016" : "https://steamcdn-a.akamaihd.net/steam/apps/" + app.AppID + "/header.jpg";
			AppEmbed.AddField("Name", app.Name ?? "Unknown App", true);
			AppEmbed.AddField("Change Number", app.ChangeNumber == 1 ? "DEBUG TEST UPDATE - IGNORE" : app.ChangeNumber.ToString(), true);
			AppEmbed.AddField("AppID", app.AppID.ToString());

			//Hope this is okay XPaw :sweat:
			AppEmbed.Url = $"https://steamdb.info/changelist/{app.ChangeNumber}/";

			if (app.DepoName != null)
				AppEmbed.AddField("Depo Changed", app.DepoName, true);

			DiscordEmbed AppUpdate = AppEmbed.Build();

			var db = SteamUpdateBot.DB.Client.GetDatabase(SteamUpdateBot.DatabaseName);

			var filterAppID = Builders<GuildInfo>.Filter.ElemMatch(x => x.SubscribedApps, x => x.AppID == app.AppID);

			var GIcollection = db.GetCollection<GuildInfo>(GuildInfo.DBName);

			List<GuildInfo> res = await GIcollection.Find(filterAppID).ToListAsync();

			var filterDebug = Builders<GuildInfo>.Filter.Eq("DebugMode", true);

			res.AddRange(GIcollection.Find(filterDebug).ToList());

			foreach (GuildInfo ServerInfo in res)
			{
				//If guild isn't subscribed to given app.
				if (!ServerInfo.SubscribedApps.Exists(ExistingApp => ExistingApp.AppID == app.AppID) && !ServerInfo.DebugMode)
					continue;

				//If app has content updates (files changed) and guild has option to show only content updates.
				if (!app.Content && !ServerInfo.ShowContent && !ServerInfo.DebugMode)
					continue;

				//If guild has option to show only public (main default steam branch) updates or any update.
				if (app.DepoName != null && ServerInfo.PublicDepoOnly && app.DepoName != "public")
					continue;

				if (ServerInfo.GuildID == 0) //DMs
				{
					try
					{
						DiscordMember DMUser = await GetDiscordMember((ulong)ServerInfo.ChannelID);

						//If we cannot find the user (we're not in the same server) then we continue on because discord will block DMs.
						if (DMUser == null)
							continue;

						await DMUser.SendMessageAsync(embed: AppUpdate);
					}
					catch (Exception e)
					{
						if (e is not DSharpPlus.Exceptions.UnauthorizedException)
							SteamUpdateBot.LAEH.CustomError(LoggingAndErrorHandler.CustomErrorType.Discord_DM, LoggingAndErrorHandler.Platform.Discord, e);

						//Bot can get kicked from servers :(
						//I've been thinking about it and I think we should keep user data within DMs (we're storing their discord ID and subbed apps) because...
						//...they might not have control over the bot being kicked.
					}
				}
				else //Server
				{
					try
					{
						//This is only seperated for debugging.
						DiscordGuild _1st = await Client.GetGuildAsync((ulong)ServerInfo.GuildID);
						DiscordChannel _2nd = _1st.GetChannel((ulong)ServerInfo.ChannelID);
						await _2nd.SendMessageAsync(embed: AppUpdate);
					}
					catch (Exception e)
					{
						if (e is not DSharpPlus.Exceptions.UnauthorizedException) //Bot can get kicked from servers :(
							SteamUpdateBot.LAEH.CustomError(LoggingAndErrorHandler.CustomErrorType.Discord_AppUpdate, LoggingAndErrorHandler.Platform.Discord, e);
						else if ((e as DSharpPlus.Exceptions.UnauthorizedException).JsonMessage == "Missing Access")
						{
							var deleteFilter = Builders<GuildInfo>.Filter.And(
								Builders<GuildInfo>.Filter.Eq("ChannelID", ServerInfo.ChannelID),
								Builders<GuildInfo>.Filter.Eq("GuildID", ServerInfo.GuildID));

							GIcollection.DeleteOne(deleteFilter);

							Console.WriteLine("Missing Permissions, removing server's data from bot.");
						}
					}
				}
			}
		}

		/// <summary>
		///  Update status of bot on discord, update time running and save data relating to <see cref="MinorDataHandler"/>.
		/// </summary>
		private async void UpdateStatus()
		{
			await Client.UpdateStatusAsync(new DiscordActivity($"Total Steam updates: {LoggingAndErrorHandler.Updates}", ActivityType.Playing));
			Console.WriteLine("Updated Time: " + LoggingAndErrorHandler.Updates);
			LoggingAndErrorHandler.MinutesRunning += 5;
			SteamUpdateBot.MinorDataHandler.WriteData();
			_timeForStatusUpdate = DateTime.Now.AddMinutes(5);
		}

		#region Utility Methods
		/// <summary>
		/// Gets <see cref="DiscordMember"/> by <see cref="CommandContext.User.Id"/>
		/// </summary>
		/// <param name="_memberID">User's ID</param>
		/// <returns>The <see cref="DiscordMember"/> with the ID specified</returns>
		public async Task<DiscordMember> GetDiscordMember(ulong _memberID)
		{
			foreach (KeyValuePair<ulong, DiscordGuild> _guildKVP in Client.Guilds)
			{
				try
				{
					foreach (DiscordMember _member in await _guildKVP.Value.GetAllMembersAsync())
					{
						if (_member.Id == _memberID)
						{
							return _member;
						}
					}
				}
				catch (Exception e)
				{
					if (e is DSharpPlus.Exceptions.UnauthorizedException)
					{
						Console.WriteLine($"{_guildKVP.Value.Name} is that bastard that doesn't have bot perms set-up correctly.");
					}
				}
			}

			return null;
		}

		/// <summary>
		/// Will attempt to get <see cref="AppInfo"/> from database and if it cannot it will create that information in the database (Only QuickSearch is false)
		/// </summary>
		/// <param name="appid">Relevant Steam AppID</param>
		/// <param name="QuickSearch">Do we only search Database or check Database and nothing is found then we search info from Steam</param>
		/// <returns><see cref="AppInfo"/> for the given appID.</returns>
		public static AppInfo GetCachedAppInfo(long appid, bool QuickSearch = false)
		{
			var db = SteamUpdateBot.DB.Client.GetDatabase(SteamUpdateBot.DatabaseName);

			var filterAppID = Builders<AppInfo>.Filter.Eq("AppID", appid);

			var AIcollection = db.GetCollection<AppInfo>(AppInfo.DBName);

			AppInfo temp = AIcollection.Find(filterAppID).Limit(1).SingleOrDefault();

			if (temp != null)
			{
				return temp;
			}

			if (QuickSearch)
				return null;

			AppInfo LocalAppInfo = new AppInfo()
			{
				AppID = appid,
				Name = SteamUpdateBot.SteamClient.GetAppName((uint)appid).Result
			};

			if (LocalAppInfo.Name != null)
			{
				AIcollection.InsertOne(LocalAppInfo);
				return LocalAppInfo;
			}

			return null;
		}

		/// <summary>
		/// Gets <see cref="GuildInfo"/> from database and if there isn't one it will create one and save it in the database.
		/// </summary>
		/// <param name="uguildid">Discord Guild ID</param>
		/// <param name="uchannelid">Discord Text Channel ID</param>
		/// <returns><see cref="GuildInfo"/> for the given guildID and channelID.</returns>
		public static GuildInfo GetGuildInfo(ulong uguildid, ulong uchannelid)
		{
			long guildid = (long)uguildid;
			long channelid = (long)uchannelid;

			var db = SteamUpdateBot.DB.Client.GetDatabase(SteamUpdateBot.DatabaseName);

			var chadFilter = Builders<GuildInfo>.Filter.And(
								Builders<GuildInfo>.Filter.Eq("ChannelID", channelid),
								Builders<GuildInfo>.Filter.Eq("GuildID", guildid));

			var GIcollection = db.GetCollection<GuildInfo>(GuildInfo.DBName);

			var LocalGI = GIcollection.Find(chadFilter).Limit(1).SingleOrDefault();

			if(LocalGI != null)
			{
				return LocalGI;
			}

			GuildInfo LocalGuildInfo = new GuildInfo()
			{
				ChannelID = channelid,
				GuildID = guildid
			};

			GIcollection.InsertOne(LocalGuildInfo);

			return LocalGuildInfo;
		}

		/// <summary>
		/// This checks if a steam app has a guild that is subscribed to it. This is a temp solution to help with steam rate limiting because I don't want to refactor the entire program to add a job system similar to how SteamDB's job system works.
		/// </summary>
		/// <param name="appid"></param>
		/// <returns>If this steam AppID has a server that is subscribed to it.</returns>
		public bool IsAppSubscribed(uint appid)
		{
			var db = SteamUpdateBot.DB.Client.GetDatabase(SteamUpdateBot.DatabaseName);

			var filterAppID = Builders<GuildInfo>.Filter.ElemMatch(x => x.SubscribedApps, x => x.AppID == appid);

			var GIcollection = db.GetCollection<GuildInfo>(GuildInfo.DBName);

			var res = GIcollection.Find(filterAppID).Limit(1).SingleOrDefault();

			return res != null;
		}

		private const int SECOND = 1;
		private const int MINUTE = 60 * SECOND;
		private const int HOUR = 60 * MINUTE;
		private const int DAY = 24 * HOUR;
		private const int MONTH = 30 * DAY;

		/// <summary>
		/// Just a nice method so instead of "Updated at June 21th 2025 at 5:54 PM" we got "Updated 10 minutes ago".
		/// Not mine.
		/// </summary>
		public string ElapsedTime(DateTime? nullabledtEvent)
		{
			DateTime dtEvent = (DateTime)nullabledtEvent;
			TimeSpan ts = new TimeSpan(DateTime.UtcNow.Ticks - dtEvent.Ticks);
			double delta = Math.Abs(ts.TotalSeconds);

			if (delta < 1 * MINUTE)
				return ts.Seconds == 1 ? "one second ago" : ts.Seconds + " seconds ago";

			if (delta < 2 * MINUTE)
				return "a minute ago";

			if (delta < 45 * MINUTE)
				return ts.Minutes + " minutes ago";

			if (delta < 90 * MINUTE)
				return "an hour ago";

			if (delta < 24 * HOUR)
				return ts.Hours + " hours ago";

			if (delta < 48 * HOUR)
				return "yesterday";

			if (delta < 30 * DAY)
				return ts.Days + " days ago";

			if (delta < 12 * MONTH)
			{
				int months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
				return months <= 1 ? "one month ago" : months + " months ago";
			}
			else
			{
				int years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
				return years <= 1 ? "one year ago" : years + " years ago";
			}
		}
		#endregion
	}
}
