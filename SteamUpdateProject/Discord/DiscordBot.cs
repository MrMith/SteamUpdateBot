using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Data.Entity;


namespace SteamUpdateProject.Discord
{
	class DiscordBot
	{
		public DiscordSocketClient _client;
		private readonly Random rand = new Random();
		private ServiceProvider services;

		public DiscordBot()
		{
			_client = new DiscordSocketClient();
			_client.Ready += ReadyAsync;
		}

		public async Task MainAsync(string token)
		{
			services = ConfigureServices();
			_client = services.GetRequiredService<DiscordSocketClient>();

			_client.Log += LogAsync;
			services.GetRequiredService<CommandService>().Log += LogAsync;

			try
			{
				await _client.LoginAsync(TokenType.Bot, token);
				await _client.StartAsync();

				await services.GetRequiredService<DiscordCommandHandler>().InitializeAsync();
			}
			catch
			{
				throw new Exception("Error logging into discord bot.");
			}
		}

		private Task LogAsync(LogMessage log)
		{
			Console.WriteLine(log.ToString());
			return Task.CompletedTask;
		}

		private Task ReadyAsync()
		{
			Console.WriteLine($"{_client.CurrentUser} is connected!");

			return Task.CompletedTask;
		}

		private ServiceProvider ConfigureServices()
		{
			return new ServiceCollection()
				.AddSingleton<DiscordSocketClient>()
				.AddSingleton<CommandService>()
				.AddSingleton<DiscordCommandHandler>()
				.AddSingleton<HttpClient>()
				.BuildServiceProvider();
		}

		public async void AppUpdated(AppUpdate app)
		{
			EmbedBuilder AppEmbed = new EmbedBuilder
			{
				Color = new Color(rand.Next(0, 255), rand.Next(0, 255), rand.Next(0, 255)),
				Timestamp = DateTimeOffset.UtcNow,
				Title = "Steam App Update!"
			};

			AppEmbed.ImageUrl = app.Name == null ? "https://steamstore-a.akamaihd.net/public/shared/images/header/globalheader_logo.png?t=962016" : "https://steamcdn-a.akamaihd.net/steam/apps/" + app.AppID + "/header.jpg";
			AppEmbed.AddField("Name", app.Name ?? "Unknown App", true);
			AppEmbed.AddField("Change Number", app.ChangeNumber == 1 ? "DEBUG TEST UPDATE - IGNORE" : app.ChangeNumber.ToString(), true);
			AppEmbed.AddField("AppID", app.AppID);

			Embed AppUpdate = AppEmbed.Build();

			using (var context = new SQLDataBase(SteamUpdateBot.ConnectionString))
			{
				foreach (GuildInfo ServerInfo in context.GuildInformation.Include(x => x.SubscribedApps).ToList())
				{
					if (!ServerInfo.SubscribedApps.Where(x => x.AppID == app.AppID).Any() && !ServerInfo.DebugMode) continue;
					if (!app.Content && !ServerInfo.ShowContent) continue;
					if (ServerInfo.GuildID == 0)
					{
						try
						{
							var _1st = await _client.GetDMChannelAsync((ulong)ServerInfo.ChannelID);
							int tries = 0;
							while (_1st == null || tries >= 25)
							{
								_1st = await _client.GetDMChannelAsync((ulong)ServerInfo.ChannelID);
								tries++;
								Thread.Sleep(100);
							}
							await _1st.SendMessageAsync(embed: AppUpdate);
						}
						catch (Exception e)
						{
							Console.WriteLine("ERROR: Discord is down or you need to check your connection. Code 0.1");
							Console.WriteLine(e.ToString());
							return;
						}
					}
					else
					{
						try
						{
							var _1st = _client.GetGuild((ulong)ServerInfo.GuildID);
							var _2nd = _1st.GetTextChannel((ulong)ServerInfo.ChannelID);
							await _2nd.SendMessageAsync(embed: AppUpdate);
						}
						catch (Exception e)
						{
							Console.WriteLine("ERROR: Discord is down or you need to check your connection. Code 1.1");
							Console.WriteLine(e.ToString());
							return;
						}
					}
				}
			}
		}

		public static bool SubApp(uint appid, GuildInfo info)
		{
			if (!info.SubscribedApps.ToList().Where(x => x.AppID == appid).Any())
			{
				using (var context = new SQLDataBase(SteamUpdateBot.ConnectionString))
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
		/// Takes list of apps and the guild that added them and returns list of apps that they've actually added.
		/// </summary>
		/// <param name="listofapps">List of apps to add.</param>
		/// <param name="info">Information about the guild.</param>
		/// <returns></returns>
		public static List<uint> SubMultipleApps(List<uint> listofapps, GuildInfo info)
		{
			List<uint> ListOfAddedApps = new List<uint>();

			foreach (var appid in listofapps)
			{
				if (!info.SubscribedApps.ToList().Where(x => x.AppID == appid).Any())
				{
					ListOfAddedApps.Add(appid);
				}
			}

			if(ListOfAddedApps.Count != 0)
			{
				using (var context = new SQLDataBase(SteamUpdateBot.ConnectionString))
				{
					context.GuildInformation.RemoveRange(context.GuildInformation.Include(x => x.SubscribedApps).ToList().Where(x => x.ChannelID == info.ChannelID && x.GuildID == info.GuildID));
					foreach(var app in ListOfAddedApps)
					{
						info.SubscribedApps.Add(new SubedApp(app));
					}					
					context.GuildInformation.Add(info);
					context.SaveChanges();
				}
			}

			return ListOfAddedApps;
		}

		public static bool RemoveApp(uint appid, GuildInfo info)
		{
			if (!info.SubscribedApps.ToList().Where(x => x.AppID == appid).Any())
			{
				using (var context = new SQLDataBase(SteamUpdateBot.ConnectionString))
				{
					context.GuildInformation.RemoveRange(context.GuildInformation.ToList().Where(x => x.ChannelID == info.ChannelID && x.GuildID == info.GuildID));
					info.SubscribedApps.Remove(new SubedApp(appid));
					context.GuildInformation.Add(info);
					return true;
				}
			}

			return false;
		}

		public static List<uint> RemoveMultipleApps(List<uint> listofapps, GuildInfo info)
		{
			List<uint> AppsThatHaveBeenRemoved = new List<uint>();

			foreach (var appid in listofapps)
			{
				if (info.SubscribedApps.ToList().Where(x => x.AppID == appid).Any())
				{
					AppsThatHaveBeenRemoved.Add(appid);
				}
			}

			if (AppsThatHaveBeenRemoved.Count != 0)
			{
				using (var context = new SQLDataBase(SteamUpdateBot.ConnectionString))
				{
					context.GuildInformation.RemoveRange(context.GuildInformation.Include(x => x.SubscribedApps).ToList().Where(x => x.ChannelID == info.ChannelID && x.GuildID == info.GuildID));
					foreach (var appid in listofapps)
					{
						foreach(var ToBeRemoved in info.SubscribedApps.Where(x => x.AppID == appid).ToList())
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

		public static AppInfo GetCachedInfo(long appid, bool QuickSearch = false)
		{
			using (var context = new SQLDataBase(SteamUpdateBot.ConnectionString))
			{
				foreach (var test in context.AppInfoData.ToList())
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

		public static GuildInfo GetGuildInfo(ulong uguildid, ulong uchannelid)
		{
			long guildid = (long)uguildid;
			long channelid = (long)uchannelid;
			using (var context = new SQLDataBase(SteamUpdateBot.ConnectionString))
			{
				//GuildInfo = context.GuildInformation.ToList().Where(x => x.GuildID == channelid && guildid == x.GuildID).FirstOrDefault();
				var FUCK = context.GuildInformation.ToList();
				
				foreach (var info in FUCK)
				{
					if (info.GuildID == guildid && info.ChannelID == channelid)
					{
						return new GuildInfo()
						{
							GuildID = info.GuildID,
							ChannelID = info.ChannelID,
							SubscribedApps = info.SubscribedApps,
							ShowContent = info.ShowContent,
							DebugMode = info.DebugMode
						};
					}
				}
			}

			GuildInfo GuildInfo = new GuildInfo();

			GuildInfo.ChannelID = channelid;
			GuildInfo.GuildID = guildid;

			using (var context = new SQLDataBase(SteamUpdateBot.ConnectionString))
			{
				context.GuildInformation.Add(GuildInfo);
				context.SaveChanges();
			}

			return GuildInfo;
		}
	}
}
