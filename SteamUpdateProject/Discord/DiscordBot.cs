﻿using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using System.Collections.Generic;
using SteamUpdateProject.DiscordLogic.Commands;

namespace SteamUpdateProject.DiscordLogic
{
	/// <summary>
	/// This runs all of the discord bot logic like logging in, sending messages for when an app updates and finally handles subscribing and unsubscribing from apps.
	/// </summary>
	class DiscordBot
	{
		public DiscordClient Client;
		public bool DevOverride = false;
		private readonly Random _rand = new Random();
		private DateTime _timeForStatusUpdate = DateTime.Now;
		private bool _botReady = false;

		public async Task StartDiscordBot(string token)
		{
			Client = new DiscordClient(new DiscordConfiguration()
			{
				Token = token,
				TokenType = TokenType.Bot,
				Intents = DiscordIntents.AllUnprivileged
			});

			CommandsNextExtension commands = Client.UseCommandsNext(new CommandsNextConfiguration()
			{
				StringPrefixes = new[] { "!" }
			});
			commands.RegisterCommands<SubscriptionModule>();
			commands.RegisterCommands<UtilityModule>();
			commands.SetHelpFormatter<CustomHelpFormatter>();

			await Client.ConnectAsync();
			_botReady = true;
		}

		/// <summary>
		/// This will take an <see cref="AppUpdate"/> and take that information and send discord messages to <see cref="GuildInfo"/> that have those Apps subscribed.
		/// </summary>
		/// <param name="app"><see cref="AppUpdate"/> that has information like the Steam AppID, if its a content update and the Depo name.</param>
		public async void AppUpdated(AppUpdate app)
		{
			if (!_botReady) return;

			Console.WriteLine($"AppUpdated: {app.AppID} {(app.Content ? "(Content)" : "")}");

			if (DateTime.Now > _timeForStatusUpdate)
			{
				await Client.UpdateStatusAsync(new DiscordActivity($"Total Steam updates: {SteamUpdateBot.Updates}", ActivityType.Playing));
				Console.WriteLine("Updated Time: " + SteamUpdateBot.Updates);
				SteamUpdateBot.INIHandler.WriteData();

				_timeForStatusUpdate = DateTime.Now.AddMinutes(5);
			}

			DiscordColor Color = new DiscordColor(((float)_rand.Next(1, 100)) / 100, ((float)_rand.Next(1, 100)) / 100, ((float)_rand.Next(1, 100)) / 100);

			DiscordEmbedBuilder AppEmbed = new DiscordEmbedBuilder
			{
				Color = Color,
				Timestamp = DateTimeOffset.UtcNow,
				Title = "Steam App Update!"
			};

			AppEmbed.ImageUrl = app.Name == null ? "https://steamstore-a.akamaihd.net/public/shared/images/header/globalheader_logo.png?t=962016" : "https://steamcdn-a.akamaihd.net/steam/apps/" + app.AppID + "/header.jpg";
			AppEmbed.AddField("Name", app.Name ?? "Unknown App", true);
			AppEmbed.AddField("Change Number", app.ChangeNumber == 1 ? "DEBUG TEST UPDATE - IGNORE" : app.ChangeNumber.ToString(), true);
			AppEmbed.AddField("AppID", app.AppID.ToString());

			if (app.DepoName != null)
			{
				AppEmbed.AddField("Depo Changed", app.DepoName, true);
			}

			DiscordEmbed AppUpdate = AppEmbed.Build();

			using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
			{
				foreach (GuildInfo ServerInfo in context.AllGuilds)
				{
					if (!ServerInfo.SubscribedApps.Exists(ExistingApp => ExistingApp.AppID == app.AppID) && !ServerInfo.DebugMode) continue;
					if (!app.Content && !ServerInfo.ShowContent && !ServerInfo.DebugMode) continue;
					if (app.DepoName != null && ServerInfo.PublicDepoOnly && app.DepoName != "public") continue;
					if (ServerInfo.GuildID == 0) //DMs
					{
						try
						{
							DiscordDmChannel DmChannel = null;

							foreach (KeyValuePair<ulong, DiscordGuild> _guildKVP in Client.Guilds)
							{
								if (DmChannel != null) continue;
								foreach (DiscordMember _member in await _guildKVP.Value.GetAllMembersAsync())
								{
									if (_member.Id == (ulong)ServerInfo.ChannelID)
									{
										DmChannel = await _member.CreateDmChannelAsync();
										await DmChannel.SendMessageAsync(embed: AppUpdate);
										continue;
									}
								}
							}
						}
						catch (Exception e)
						{
							SteamUpdateBot.CustomError("0.1", "Discord", e);
						}
					}
					else //Server
					{
						try
						{
							DiscordGuild _1st = await Client.GetGuildAsync((ulong)ServerInfo.GuildID);
							DiscordChannel _2nd = _1st.GetChannel((ulong)ServerInfo.ChannelID);
							await _2nd.SendMessageAsync(embed: AppUpdate);
						}
						catch (Exception e)
						{
							SteamUpdateBot.CustomError("1.1", "Discord", e);
						}
					}
				}
			}
		}

		/// <summary>
		/// Will attempt to get <see cref="AppInfo"/> from database and if it cannot it will create that information in the database (Only QuickSearch is false)
		/// </summary>
		/// <param name="appid">Relevant Steam AppID</param>
		/// <param name="QuickSearch">Do we only search Database or check database and nothing is found search info from steam</param>
		/// <returns></returns>
		public static AppInfo GetCachedInfo(long appid, bool QuickSearch = false)
		{
			using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
			{
				foreach (AppInfo DBAppInfo in context.AllApps)
				{
					if (DBAppInfo.AppID == appid)
					{
						return DBAppInfo;
					}
				}

				if (QuickSearch) return null;

				AppInfo AppInfo = new AppInfo()
				{
					AppID = appid,
					Name = SteamUpdateProject.SteamUpdateBot.SteamClient.GetAppName((uint)appid).Result
				};

				if (AppInfo.Name != null)
				{
					context.AppInfoData.Add(AppInfo);
					context.SaveChanges();
					return AppInfo;
				}
			}

			return null;
		}

		/// <summary>
		/// Gets <see cref="GuildInfo"/> from database and if there isn't one it will create one and save it in the database.
		/// </summary>
		/// <param name="uguildid">Discord Guild ID</param>
		/// <param name="uchannelid">Discord Text Channel ID</param>
		/// <returns></returns>
		public static GuildInfo GetGuildInfo(ulong uguildid, ulong uchannelid)
		{
			long guildid = (long)uguildid;
			long channelid = (long)uchannelid;
			using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
			{
				foreach (GuildInfo info in context.AllGuilds)
				{
					if (info.GuildID == guildid && info.ChannelID == channelid)
					{
						return new GuildInfo()
						{
							GuildID = info.GuildID,
							ChannelID = info.ChannelID,
							SubscribedApps = info.SubscribedApps,
							ShowContent = info.ShowContent,
							DebugMode = info.DebugMode,
							PublicDepoOnly = info.PublicDepoOnly
						};
					}
				}
			}

			GuildInfo GuildInfo = new GuildInfo()
			{
				ChannelID = channelid,
				GuildID = guildid
			};

			using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
			{
				context.GuildInformation.Add(GuildInfo);
				context.SaveChanges();
			}

			return GuildInfo;
		}

		/// <summary>
		/// This checks if a steam app has a guild that is subscribed to it. This is a temp solution to help with steam rate limiting because I don't want to refactor the entire program to add a job system similar to how SteamDB's job system works.
		/// </summary>
		/// <param name="appid"></param>
		/// <returns>If this steam AppID has a server that is subscribed to it.</returns>
		public bool IsAppSubscribed(uint appid)
		{
			using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
			{
				return context.AllGuilds.Exists(guild => guild.SubscribedApps.Exists(subbedApp => subbedApp.AppID == appid));
			}
		}

		const int SECOND = 1;
		const int MINUTE = 60 * SECOND;
		const int HOUR = 60 * MINUTE;
		const int DAY = 24 * HOUR;
		const int MONTH = 30 * DAY;

		/// <summary>
		/// Just a nice method so instead of "Updated at June 21th 2021 at 5:54 PM" we got "Updated 10 minutes ago". Not mine.
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
	}
}
