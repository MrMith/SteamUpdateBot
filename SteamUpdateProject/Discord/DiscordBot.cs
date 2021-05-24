using System;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Data.Entity;

namespace SteamUpdateProject.DiscordLogic
{
	class DiscordBot
	{
		public DiscordClient _client;
		private readonly Random rand = new Random();
		private ServiceProvider services;
		private DateTime TimeForStatusUpdate = DateTime.Now;
		private bool BotReady = false;

		public async Task StartDiscordBot(string token)
		{
			_client = new DiscordClient(new DiscordConfiguration()
			{
				Token = token,
				TokenType = TokenType.Bot,
				Intents = DiscordIntents.AllUnprivileged

			});
			var commands = _client.UseCommandsNext(new CommandsNextConfiguration()
			{
				StringPrefixes = new[] { "!" }
			});

			commands.RegisterCommands<DiscordCommands.PublicModule>();

			await _client.ConnectAsync();
			BotReady = true;
		}

		//private Task LogAsync(LogMessage log)
		//{
		//	Console.WriteLine(log.ToString());
		//	return Task.CompletedTask;
		//}

		private Task ReadyAsync()
		{
			Console.WriteLine($"{_client.CurrentUser} is connected!");

			return Task.CompletedTask;
		}

		/// <summary>
		/// This will take an <see cref="AppUpdate"/> and take that information and send discord messages to <see cref="GuildInfo"/> that have those Apps subscribed.
		/// </summary>
		/// <param name="app"><see cref="AppUpdate"/> that has information like the Steam AppID, if its a content update and the Depo name.</param>
		public async void AppUpdated(AppUpdate app)
		{
			if (!BotReady) return;

			if(DateTime.Now > TimeForStatusUpdate)
			{
				await _client.UpdateStatusAsync(new DiscordActivity($"Total Steam updates: {SteamUpdateBot.Updates}"));
				TimeForStatusUpdate = DateTime.Now.AddMinutes(5);
			}

			DiscordEmbedBuilder AppEmbed = new DiscordEmbedBuilder
			{
				Color = new DiscordColor(rand.Next(1, 100) / 100, rand.Next(1, 100)/100, rand.Next(1, 100)/100),
				Timestamp = DateTimeOffset.UtcNow,
				Title = "Steam App Update!"
			};

			AppEmbed.ImageUrl = app.Name == null ? "https://steamstore-a.akamaihd.net/public/shared/images/header/globalheader_logo.png?t=962016" : "https://steamcdn-a.akamaihd.net/steam/apps/" + app.AppID + "/header.jpg";
			AppEmbed.AddField("Name", app.Name ?? "Unknown App", true);
			AppEmbed.AddField("Change Number", app.ChangeNumber == 1 ? "DEBUG TEST UPDATE - IGNORE" : app.ChangeNumber.ToString(), true);
			AppEmbed.AddField("AppID", app.AppID.ToString());

			if(app.DepoName != null)
				AppEmbed.AddField("Depo Changed", app.DepoName, true);


			DiscordEmbed AppUpdate = AppEmbed.Build();

			using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
			{
				foreach (GuildInfo ServerInfo in context.GuildInformation.Include(x => x.SubscribedApps).ToList())
				{
					if (!ServerInfo.SubscribedApps.Where(x => x.AppID == app.AppID).Any() && !ServerInfo.DebugMode) continue;
					if (!app.Content && !ServerInfo.ShowContent && !ServerInfo.DebugMode) continue;
					if (app.DepoName != null && ServerInfo.PublicDepoOnly && app.DepoName != "public") continue;
					if (ServerInfo.GuildID == 0)
					{
						//continue; //For some reason DMs are broken af
						
						try
						{
							foreach(var _guildKVP in _client.Guilds)
							{
								foreach(var _member in await _guildKVP.Value.GetAllMembersAsync())
								{
									if (_member.Id == (ulong)ServerInfo.ChannelID)
									{
										var DmChannel = await _member.CreateDmChannelAsync();
										await DmChannel.SendMessageAsync(embed: AppUpdate);
										goto BreakOutOf;
									}
								}
							}
						BreakOutOf: //Honestly it works and I just don't want to spend more time on it :)
							continue;
							
						}
						catch (Exception e)
						{
							Console.WriteLine("ERROR: Discord is down or you need to check your connection. Code 0.1");
							Console.WriteLine(e.ToString());
							Console.WriteLine();
							Console.WriteLine();
						}
						
					}
					else
					{
						try
						{
							var _1st = await _client.GetGuildAsync((ulong)ServerInfo.GuildID);
							var _2nd = _1st.GetChannel((ulong)ServerInfo.ChannelID);
							await _2nd.SendMessageAsync(embed: AppUpdate);
						}
						catch (Exception e)
						{
							Console.WriteLine("ERROR: Discord is down or you need to check your connection. Code 1.1");
							Console.WriteLine(e.ToString());
							Console.WriteLine();
							Console.WriteLine();
						}
					}
				}
			}
		}

		/// <summary>
		/// Takes one steam AppID and adds them to the subscribed list of the relevant <see cref="GuildInfo"/>.
		/// </summary>
		/// <param name="appid">Relevant AppID.</param>
		/// <param name="info">Relevant <see cref="GuildInfo"/></param>
		/// <returns>If the app was added to the relevant <see cref="GuildInfo"/></returns>
		public static bool SubApp(uint appid, GuildInfo info)
		{
			if (!info.SubscribedApps.ToList().Where(x => x.AppID == appid).Any())
			{
				using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
				{
					context.GuildInformation.RemoveRange(context.GuildInformation.Include(x => x.SubscribedApps).ToList().Where(x => x.ChannelID == info.ChannelID && x.GuildID == info.GuildID));
					info.SubscribedApps.Add(new SubedApp(appid));
					
					context.GuildInformation.Add(info);
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
		/// <param name="info">Relevant <see cref="GuildInfo"/></param>
		/// <returns>List of AppIDs that were added to the subscribed list of <see cref="GuildInfo"/>.</returns>
		public static List<uint> SubMultipleApps(List<uint> listofapps, GuildInfo info)
		{
			List<uint> ListOfAddedApps = new List<uint>();

			foreach (uint appid in listofapps)
			{
				if (!info.SubscribedApps.ToList().Where(x => x.AppID == appid).Any())
				{
					ListOfAddedApps.Add(appid);
				}
			}

			if(ListOfAddedApps.Count != 0)
			{
				using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
				{
					context.GuildInformation.RemoveRange(context.GuildInformation.Include(x => x.SubscribedApps).ToList().Where(x => x.ChannelID == info.ChannelID && x.GuildID == info.GuildID));
					foreach(uint app in ListOfAddedApps)
					{
						info.SubscribedApps.Add(new SubedApp(app));
					}					
					context.GuildInformation.Add(info);
					context.SaveChanges();
				}
			}

			return ListOfAddedApps;
		}

		/// <summary>
		/// Removes multiple one AppID from a <see cref="GuildInfo"/>
		/// </summary>
		/// <param name="appid">Relevant AppID</param>
		/// <param name="info">Relevant <see cref="GuildInfo"/></param>
		/// <returns></returns>
		public static bool RemoveApp(uint appid, GuildInfo info)
		{
			if (info.SubscribedApps.ToList().Where(x => x.AppID == appid).Any())
			{
				using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
				{
					context.GuildInformation.RemoveRange(context.GuildInformation.Include(x => x.SubscribedApps).ToList().Where(x => x.ChannelID == info.ChannelID && x.GuildID == info.GuildID));
					foreach(SubedApp ToBeRemoved in info.SubscribedApps.Where(x => x.AppID == appid).ToList())
					{
						info.SubscribedApps.Remove(ToBeRemoved);
					}
					context.GuildInformation.Add(info);
					context.SaveChanges();
				}
				return true;
			}

			return false;
		}

		/// <summary>
		/// Removes multiple subscribed AppIDs from a <see cref="GuildInfo"/>
		/// </summary>
		/// <param name="listofapps">Relevant list of AppIDs</param>
		/// <param name="info">Relevant <see cref="GuildInfo"/></param>
		/// <returns></returns>
		public static List<uint> RemoveMultipleApps(List<uint> listOfApps, GuildInfo info)
		{
			List<uint> AppsThatHaveBeenRemoved = new List<uint>();

			foreach (uint appid in listOfApps)
			{
				if (info.SubscribedApps.ToList().Where(x => x.AppID == appid).Any())
				{
					AppsThatHaveBeenRemoved.Add(appid);
				}
			}

			if (AppsThatHaveBeenRemoved.Count != 0)
			{
				using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
				{
					context.GuildInformation.RemoveRange(context.GuildInformation.Include(x => x.SubscribedApps).ToList().Where(x => x.ChannelID == info.ChannelID && x.GuildID == info.GuildID));
					foreach (uint appid in listOfApps)
					{
						foreach(SubedApp ToBeRemoved in info.SubscribedApps.Where(x => x.AppID == appid).ToList())
						{
							info.SubscribedApps.Remove(ToBeRemoved);
						}					
					}
					context.GuildInformation.Add(info);
					context.SaveChanges();
				}
			}
			
			return AppsThatHaveBeenRemoved;
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
				foreach (AppInfo test in context.AppInfoData.ToList())
				{
					if (test.AppID == appid)
					{
						return test;
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
				//GuildInfo = context.GuildInformation.ToList().Where(x => x.GuildID == channelid && guildid == x.GuildID).FirstOrDefault();
				List<GuildInfo> FUCK = context.GuildInformation.ToList();
				
				foreach (GuildInfo info in FUCK)
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

			GuildInfo GuildInfo = new GuildInfo();

			GuildInfo.ChannelID = channelid;
			GuildInfo.GuildID = guildid;

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
				foreach(GuildInfo Guild in context.GuildInformation.ToList())
				{
					foreach(var AppID in Guild.SubscribedApps)
					{
						if(appid == AppID.AppID)
						{
							return true;
						}
					}
				}
			}

			return false;
		}
	}
}
