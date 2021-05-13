using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using Discord.WebSocket;

namespace SteamUpdateProject.Discord
{
	class DiscordCommands
	{
		const int SECOND = 1;
		const int MINUTE = 60 * SECOND;
		const int HOUR = 60 * MINUTE;
		const int DAY = 24 * HOUR;
		const int MONTH = 30 * DAY;

		public class PublicModule : ModuleBase<SocketCommandContext>
		{
			[Command("removeapp")]
			[Alias("delapp", "deleteapp", "remove", "unsubscribe")]
			public async Task RemoveAppAsync(params string[] objects)
			{
				EmbedBuilder embedBuilder = new EmbedBuilder();

				GuildInfo GuildInfo = DiscordBot.GetGuildInfo(Context.Guild == null ? 0 : Context.Guild.Id, Context.Channel.Id);

				StringBuilder stringBuilder = new StringBuilder();

				if (objects.Length > 1) //Multiple
				{
					List<uint> ListOfAppIDS = new List<uint>();
					foreach (string StringAppID in objects)
					{
						if (uint.TryParse(StringAppID, out uint appid))
						{
							ListOfAppIDS.Add(appid);
						}
					}

					List<uint> AppsThatHaveBeenRemoved = DiscordBot.RemoveMultipleApps(ListOfAppIDS, GuildInfo);

					if (AppsThatHaveBeenRemoved.Count == 0)
					{
						await ReplyAsync("You're not subscribed to those apps!");
						return;
					}

					foreach (uint app in AppsThatHaveBeenRemoved)
					{
						AppInfo AppInfo = new AppInfo()
						{
							AppID = app
						};

						AppInfo.Name = SteamUpdateBot.SteamClient.GetAppName(app).Result;

						stringBuilder.AppendLine($"{AppInfo.Name} ({AppInfo.AppID})");

						if (AppInfo.LastUpdated != null && AppInfo.LastUpdated != DateTime.MinValue)
						{
							stringBuilder.Append($". Last updated {AppInfo.LastUpdated?.ToLongDateString()}.");
						}
					}

					embedBuilder.Title = "Apps removed:";
					embedBuilder.AddField("Apps", stringBuilder.ToString(), true);
					await ReplyAsync(embed: embedBuilder.Build());
					return;
				}
				else if (uint.TryParse(objects[0], out uint appid)) //Single
				{
					if (!DiscordBot.RemoveApp(appid, GuildInfo))
					{
						await ReplyAsync("You're not subscribed to this app!");
						return;
					}

					AppInfo AppInfo = new AppInfo()
					{
						AppID = appid
					};

					AppInfo.Name = SteamUpdateBot.SteamClient.GetAppName(appid).Result;

					embedBuilder.Title = "Steam Apps removed:";
					embedBuilder.AddField("Steam App:", $"{AppInfo.Name} ({appid})");

					await ReplyAsync(embed: embedBuilder.Build());
					return;
				}

				await ReplyAsync("ERROR using remove command! Type !help to get help on using this command!");
			}

			[Command("subapp")]
			[Alias("addapp", "subscribeapp", "add", "subscribe")]
			public async Task AddAppAsync(params string[] objects)
			{
				EmbedBuilder embedBuilder = new EmbedBuilder();

				GuildInfo GuildInfo = DiscordBot.GetGuildInfo(Context.Guild == null ? 0 : Context.Guild.Id, Context.Channel.Id);

				//fix this copy and pasted garbage retard
				if (objects.Length > 1) //Multiple apps
				{
					embedBuilder.Title = "Apps added:";
					List<uint> ListOfAppIDS = new List<uint>();
					foreach (string StringAppID in objects)
					{
						if (uint.TryParse(StringAppID, out uint appid))
						{
							ListOfAppIDS.Add(appid);
						}
					}

					var ListOfConfirmedAppsAdded = DiscordBot.SubMultipleApps(ListOfAppIDS, GuildInfo);

					if (ListOfConfirmedAppsAdded.Count == 0)
					{
						await ReplyAsync("Already subscribed to all apps!");
						return;
					}
					StringBuilder ListTest = new StringBuilder();

					foreach (uint app in ListOfConfirmedAppsAdded)
					{
						AppInfo AppInfo = new AppInfo()
						{
							AppID = app
						};

						AppInfo.Name = SteamUpdateBot.SteamClient.GetAppName(app).Result;

						ListTest.AppendLine($"{AppInfo.Name} ({AppInfo.AppID})");
					}

					embedBuilder.AddField("Apps:", ListTest.ToString());
					await ReplyAsync(embed: embedBuilder.Build());
					return;
				}
				else if (uint.TryParse(objects[0], out uint appid)) //single app
				{
					if (!DiscordBot.SubApp(appid, GuildInfo))
					{
						await ReplyAsync($"Already added app! ({appid})");
						return;
					}

					AppInfo AppInfo = new AppInfo()
					{
						AppID = appid
					};

					AppInfo.Name = SteamUpdateBot.SteamClient.GetAppName(appid).Result;

					embedBuilder.Title = "Steam Apps added:";
					embedBuilder.AddField("Steam App:", $"{AppInfo.Name} ({appid})");

					await ReplyAsync(embed: embedBuilder.Build());
					return;
				}
				await ReplyAsync("ERROR using add command! Type !help to get help on using this command!");
			}

			[Command("list")]
			[Alias("apps")]
			public async Task ListAllSubscribedApps()
			{
				GuildInfo GuildInfo = DiscordBot.GetGuildInfo(Context.Guild == null ? 0 : Context.Guild.Id, Context.Channel.Id);

				EmbedBuilder embedBuilder = new EmbedBuilder();
				embedBuilder.Title = "List of subscribed steam apps:";

				if (GuildInfo == null || GuildInfo.SubscribedApps.Count == 0)
				{
					await ReplyAsync("No apps found!");
					return;
				}

				StringBuilder ListTest = new StringBuilder();
				foreach (var SubbedApp in GuildInfo.SubscribedApps.ToList())
				{
					AppInfo AppInfo = DiscordBot.GetCachedInfo(SubbedApp.AppID);

					if (AppInfo.Name == null || AppInfo.Name.Length == 0) AppInfo.Name = "Unknown App";

					if (AppInfo.LastUpdated != null)
					{
						ListTest.Append($"{AppInfo.Name} ({SubbedApp.AppID})");
						ListTest.AppendLine($" was last updated {ElapsedTime(AppInfo.LastUpdated)}.");
					}
					else
					{
						ListTest.AppendLine($"{AppInfo.Name} ({SubbedApp.AppID})");
					}
				}

				embedBuilder.AddField("Apps", ListTest.ToString());

				await ReplyAsync(embed: embedBuilder.Build());
			}

			public string ElapsedTime(DateTime? nullabledtEvent)
			{
				var dtEvent = (DateTime)nullabledtEvent;
				var ts = new TimeSpan(DateTime.UtcNow.Ticks - dtEvent.Ticks);
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
				return String.Format("{0} {1} ago", dtEvent.ToShortDateString(), dtEvent.ToShortTimeString());
			}

			[Command("help")]
			public async Task HelpCommand()
			{
				EmbedBuilder HelpBuilder = new EmbedBuilder()
				{
					Title = "Help Command"
				};

				HelpBuilder.AddField("!add", "Subscribe to a Steam Application to see when it updates by appid (Ex: !add 730 or !add 730 530)");
				HelpBuilder.AddField("!remove", "Remove a subscription to a Steam Application so you no longer see when it updates by appid (Ex: !remove 730 or !remove 730 530)");
				HelpBuilder.AddField("!all", "Show all updates (like if the store tags update) or only content updates. Defaults to false. (Ex: !all true)");
				HelpBuilder.AddField("!status", "Shows the ping of the bot to discord, if steam is down and total updates processed this session.");
				HelpBuilder.AddField("!debug", "**NOT RECOMMENDED** Pipes every update through this channel regardless of subscriptions. (Ex: !debug true or !debug false)");
				await ReplyAsync(embed: HelpBuilder.Build());
			}

			[Command("showall")]
			[Alias("all")]
			public async Task ShowContent()
			{
				GuildInfo GuildInfo = DiscordBot.GetGuildInfo(Context.Guild == null ? 0 : Context.Guild.Id, Context.Channel.Id);

				if (GuildInfo == null)
				{
					await ReplyAsync("Show all is set to False.");
				}

				await ReplyAsync($"Show all is set to: {GuildInfo.ShowContent}.");
			}

			[Command("showall")]
			[Alias("all")]
			public async Task ShowContentBool(bool Set)
			{
				GuildInfo GuildInfo = DiscordBot.GetGuildInfo(Context.Guild == null ? 0 : Context.Guild.Id, Context.Channel.Id);

				if (GuildInfo != null)
				{
					using (var context = new SQLDataBase(SteamUpdateBot.ConnectionString))
					{
						context.GuildInformation.RemoveRange(context.GuildInformation.Include(x => x.SubscribedApps).ToList().Where(x => x.ChannelID == GuildInfo.ChannelID && x.GuildID == GuildInfo.GuildID));
						GuildInfo.ShowContent = Set;
						context.GuildInformation.Add(GuildInfo);
						context.SaveChanges();
					}
				}

				await ReplyAsync($"Set show all to {Set}.");
			}

			[Command("debug")]
			public async Task DebugBool(bool Set)
			{
				bool HasPermission = false;
				if (Context.Guild != null)
				{
					SocketGuildUser user = Context.User as SocketGuildUser;
					var roles = (user as IGuildUser).Guild.Roles;

					foreach (var role in roles)
					{
						if (role.Permissions.Administrator || role.Permissions.ManageChannels)
						{
							HasPermission = true;
							break;
						}
					}
				}
				else //In DMs they should have control ect
				{
					HasPermission = true;
				}
				

				if(!HasPermission)
				{
					await ReplyAsync("Insufficient permissions to execute this command.");
					return;
				}

				GuildInfo GuildInfo = DiscordBot.GetGuildInfo(Context.Guild == null ? 0 : Context.Guild.Id, Context.Channel.Id);

				if (GuildInfo != null)
				{
					using (var context = new SQLDataBase(SteamUpdateBot.ConnectionString))
					{
						context.GuildInformation.RemoveRange(context.GuildInformation.Include(x => x.SubscribedApps).ToList().Where(x => x.ChannelID == GuildInfo.ChannelID && x.GuildID == GuildInfo.GuildID));
						GuildInfo.DebugMode = Set;
						context.GuildInformation.Add(GuildInfo);
						context.SaveChanges();
					}
				}

				await ReplyAsync($"Debug mode set to {Set}.");

			}

			[Command("debug")]
			public async Task DebugBool()
			{
				GuildInfo GuildInfo = DiscordBot.GetGuildInfo(Context.Guild == null ? 0 : Context.Guild.Id, Context.Channel.Id);

				await ReplyAsync($"Debug mode is currently set to {GuildInfo.DebugMode}.");
			}

			[Command("status")]
			public async Task Status()
			{
				if (SteamUpdateBot.SteamClient == null)
				{
					await ReplyAsync($"SteamBot not ready.");
				}
				bool steamStatus = false;

				try
				{
					steamStatus = await SteamUpdateBot.SteamClient.IsSteamDown();
				}
				catch
				{

				}

				await ReplyAsync($"Ping is {Context.Client.Latency}.\nSteam Status: {(steamStatus ? "Online" : "Offline")}.\nTotal updates processed: {SteamUpdateBot.SteamClient.UpdatesProcessed} ({(int)(SteamUpdateBot.SteamClient.UpdatesProcessed / (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalMinutes)} per minute)\nTotal Execeptions: {SteamUpdateBot.Exceptions}");
			}

			[Command("forceupdate")]
			public async Task ForceUpdate(uint appid)
			{
				if (Context.User.Id != 185739967379537920)
				{
					await ReplyAsync($"You're not authorized to use this command.");
				}
				var FakeUpdatedApp = new AppUpdate();
				FakeUpdatedApp.Name = SteamUpdateBot.SteamClient.GetAppName(appid).Result;
				FakeUpdatedApp.AppID = appid;
				FakeUpdatedApp.Content = true;
				FakeUpdatedApp.ChangeNumber = 1;
				FakeUpdatedApp.LastUpdated = DateTime.UtcNow.AddYears(10);

				using (var context = new SQLDataBase(SteamUpdateBot.ConnectionString))
				{
					context.AppInfoData.RemoveRange(context.AppInfoData.ToList().Where(x => x.AppID == FakeUpdatedApp.AppID));
					var test = new AppInfo()
					{
						Name = FakeUpdatedApp.Name,
						AppID = appid,
						LastUpdated = FakeUpdatedApp.LastUpdated
					};

					context.AppInfoData.Add(test);

					context.SaveChanges();
				}

				SteamUpdateBot.DiscordClient.AppUpdated(FakeUpdatedApp);

				await ReplyAsync($"Pushed a fake update for {FakeUpdatedApp.Name} ({FakeUpdatedApp.AppID})");
			}
		}
	}
}
