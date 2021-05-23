using DSharpPlus;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Data.Entity;

namespace SteamUpdateProject.DiscordLogic
{
	class DiscordCommands
	{
		const int SECOND = 1;
		const int MINUTE = 60 * SECOND;
		const int HOUR = 60 * MINUTE;
		const int DAY = 24 * HOUR;
		const int MONTH = 30 * DAY;

		public class PublicModule : BaseCommandModule
		{
			[Command("removeapp"), Aliases("delapp", "deleteapp", "remove", "unsubscribe")]
			public async Task RemoveAppAsync(CommandContext ctx, params string[] objects)
			{
				DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();

				GuildInfo GuildInfo = DiscordBot.GetGuildInfo(ctx.Guild == null ? 0 : ctx.Guild.Id, ctx.Channel.Id);

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
						await ctx.RespondAsync("You're not subscribed to those apps!");
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
					await ctx.RespondAsync(embed: embedBuilder.Build());
					return;
				}
				else if (uint.TryParse(objects[0], out uint appid)) //Single
				{
					if (!DiscordBot.RemoveApp(appid, GuildInfo))
					{
						await ctx.RespondAsync("You're not subscribed to this app!");
						return;
					}

					AppInfo AppInfo = new AppInfo()
					{
						AppID = appid
					};

					AppInfo.Name = SteamUpdateBot.SteamClient.GetAppName(appid).Result;

					embedBuilder.Title = "Steam Apps removed:";
					embedBuilder.AddField("Steam App:", $"{AppInfo.Name} ({appid})");

					await ctx.RespondAsync(embed: embedBuilder.Build());
					return;
				}

				await ctx.RespondAsync("ERROR using remove command! Type !help to get help on using this command!");
			}

			[Command("subapp"), Aliases("addapp", "subscribeapp", "add", "subscribe")]
			public async Task AddAppAsync(CommandContext ctx, params string[] objects)
			{
				DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();

				GuildInfo GuildInfo = DiscordBot.GetGuildInfo(ctx.Guild == null ? 0 : ctx.Guild.Id, ctx.Channel.Id);

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

					List<uint> ListOfConfirmedAppsAdded = DiscordBot.SubMultipleApps(ListOfAppIDS, GuildInfo);

					if (ListOfConfirmedAppsAdded.Count == 0)
					{
						await ctx.RespondAsync("Already subscribed to all apps!");
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
					await ctx.RespondAsync(embed: embedBuilder.Build());
					return;
				}
				else if (uint.TryParse(objects[0], out uint appid)) //single app
				{
					if (!DiscordBot.SubApp(appid, GuildInfo))
					{
						await ctx.RespondAsync($"Already added app! ({appid})");
						return;
					}

					AppInfo AppInfo = new AppInfo()
					{
						AppID = appid
					};

					AppInfo.Name = SteamUpdateBot.SteamClient.GetAppName(appid).Result;

					embedBuilder.Title = "Steam Apps added:";
					embedBuilder.AddField("Steam App:", $"{AppInfo.Name} ({appid})");

					await ctx.RespondAsync(embed: embedBuilder.Build());
					return;
				}
				await ctx.RespondAsync("ERROR using add command! Type !help to get help on using this command!");
			}

			[Command("list"), Aliases("apps")]
			public async Task ListAllSubscribedApps(CommandContext ctx)
			{
				GuildInfo GuildInfo = DiscordBot.GetGuildInfo(ctx.Guild == null ? 0 : ctx.Guild.Id, ctx.Channel.Id);

				DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();
				embedBuilder.Title = "List of subscribed steam apps:";

				if (GuildInfo == null || GuildInfo.SubscribedApps.Count == 0)
				{
					await ctx.RespondAsync("No apps found!");
					return;
				}

				StringBuilder ListTest = new StringBuilder();
				foreach (SubedApp SubbedApp in GuildInfo.SubscribedApps.ToList())
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

				await ctx.RespondAsync(embed: embedBuilder.Build());
			}

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
				return String.Format("{0} {1} ago", dtEvent.ToShortDateString(), dtEvent.ToShortTimeString());
			}

			[Command("commands")]
			public async Task HelpCommand(CommandContext ctx)
			{
				DiscordEmbedBuilder HelpBuilder = new DiscordEmbedBuilder()
				{
					Title = "Help Command"
				};

				HelpBuilder.AddField("!add", "Subscribe to a Steam Application to see when it updates by appid (Ex: !add 730 or !add 730 530)");
				HelpBuilder.AddField("!remove", "Remove a subscription to a Steam Application so you no longer see when it updates by appid (Ex: !remove 730 or !remove 730 530)");
				HelpBuilder.AddField("!all", "Show all updates (like if the store tags update) or only content updates. Defaults to false. (Ex: !all true)");
				HelpBuilder.AddField("!status", "Shows the ping of the bot to discord, if steam is down and total updates processed this session.");
				HelpBuilder.AddField("!public", "Will only send messages if the default public steam branch is updated. (Ex: !public true or !debug false)");
				HelpBuilder.AddField("!debug", "**NOT RECOMMENDED** Pipes every update through this channel regardless of subscriptions. (Ex: !debug true or !debug false)");
				await ctx.RespondAsync(embed: HelpBuilder.Build());
			}

			[Command("showall"), Aliases("all")]
			public async Task ShowContent(CommandContext ctx)
			{
				GuildInfo GuildInfo = DiscordBot.GetGuildInfo(ctx.Guild == null ? 0 : ctx.Guild.Id, ctx.Channel.Id);

				if (GuildInfo == null)
				{
					await ctx.RespondAsync("Show all is set to False.");
				}

				await ctx.RespondAsync($"Show all is set to: {GuildInfo.ShowContent}.");
			}

			[Command("showall")]
			public async Task ShowContentBool(CommandContext ctx, bool Set)
			{
				GuildInfo GuildInfo = DiscordBot.GetGuildInfo(ctx.Guild == null ? 0 : ctx.Guild.Id, ctx.Channel.Id);

				if (GuildInfo != null)
				{
					using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
					{
						context.GuildInformation.RemoveRange(context.GuildInformation.Include(x => x.SubscribedApps).ToList().Where(x => x.ChannelID == GuildInfo.ChannelID && x.GuildID == GuildInfo.GuildID));
						GuildInfo.ShowContent = Set;
						context.GuildInformation.Add(GuildInfo);
						context.SaveChanges();
					}
				}

				await ctx.RespondAsync($"Set show all to {Set}.");
			}

			[Command("debug")]
			public async Task DebugBool(CommandContext ctx, bool Set)
			{
				if(!UserHasPermission(ctx.Member, ctx.Guild))
				{
					await ctx.RespondAsync("Insufficient permissions to execute this command.");
					return;
				}

				GuildInfo GuildInfo = DiscordBot.GetGuildInfo(ctx.Guild == null ? 0 : ctx.Guild.Id, ctx.Channel.Id);

				if (GuildInfo != null)
				{
					using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
					{
						context.GuildInformation.RemoveRange(context.GuildInformation.Include(x => x.SubscribedApps).ToList().Where(x => x.ChannelID == GuildInfo.ChannelID && x.GuildID == GuildInfo.GuildID));
						GuildInfo.DebugMode = Set;
						context.GuildInformation.Add(GuildInfo);
						context.SaveChanges();
					}
				}

				await ctx.RespondAsync($"Debug mode set to {Set}.");

			}

			[Command("public")]
			public async Task PublicBool(CommandContext ctx)
			{
				GuildInfo GuildInfo = DiscordBot.GetGuildInfo(ctx.Guild == null ? 0 : ctx.Guild.Id, ctx.Channel.Id);

				await ctx.RespondAsync($"Public mode is currently set to {GuildInfo.PublicDepoOnly}.");
			}

			[Command("public")]
			public async Task PublicBool(CommandContext ctx, bool Set)
			{
				if (!UserHasPermission(ctx.Member, ctx.Guild))
				{
					await ctx.RespondAsync("Insufficient permissions to execute this command.");
					return;
				}

				GuildInfo GuildInfo = DiscordBot.GetGuildInfo(ctx.Guild == null ? 0 : ctx.Guild.Id, ctx.Channel.Id);

				if (GuildInfo != null)
				{
					using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
					{
						context.GuildInformation.RemoveRange(context.GuildInformation.Include(x => x.SubscribedApps).ToList().Where(x => x.ChannelID == GuildInfo.ChannelID && x.GuildID == GuildInfo.GuildID));
						GuildInfo.PublicDepoOnly = Set;
						context.GuildInformation.Add(GuildInfo);
						context.SaveChanges();
					}
				}

				await ctx.RespondAsync($"Public mode set to {Set}.");

			}

			[Command("debug")]
			public async Task DebugBool(CommandContext ctx)
			{
				GuildInfo GuildInfo = DiscordBot.GetGuildInfo(ctx.Guild == null ? 0 : ctx.Guild.Id, ctx.Channel.Id);

				await ctx.RespondAsync($"Debug mode is currently set to {GuildInfo.DebugMode}.");
			}



			[Command("status")]
			public async Task Status(CommandContext ctx)
			{
				if (SteamUpdateBot.SteamClient == null)
				{
					await ctx.RespondAsync($"SteamBot not ready.");
				}
				bool steamStatus = false;

				try
				{
					steamStatus = await SteamUpdateBot.SteamClient.IsSteamDown();
				}
				catch
				{

				}

				await ctx.RespondAsync($"Ping: {ctx.Client.Ping}.\nSteam Status: {(steamStatus ? "Online" : "Offline")}.\nTotal updates processed: {SteamUpdateBot.SteamClient.UpdatesProcessed} ({(int)(SteamUpdateBot.SteamClient.UpdatesProcessed / (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalMinutes)} per minute)\nTotal content updates: {SteamUpdateBot.ContentUpdates}.\nTotal Execeptions: {SteamUpdateBot.Exceptions}");
			}

			[Command("forceupdate")]
			public async Task ForceUpdate(CommandContext ctx, uint appid)
			{
				if (ctx.User.Id != 185739967379537920)
				{
					await ctx.RespondAsync($"You're not authorized to use this command.");
				}
				AppUpdate FakeUpdatedApp = new AppUpdate();
				FakeUpdatedApp.Name = SteamUpdateBot.SteamClient.GetAppName(appid).Result;
				FakeUpdatedApp.AppID = appid;
				FakeUpdatedApp.Content = true;
				FakeUpdatedApp.ChangeNumber = 1;
				FakeUpdatedApp.LastUpdated = DateTime.UtcNow.AddYears(10);

				/*
				using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
				{
					ctx.AppInfoData.RemoveRange(ctx.AppInfoData.ToList().Where(x => x.AppID == FakeUpdatedApp.AppID));
					AppInfo test = new AppInfo()
					{
						Name = FakeUpdatedApp.Name,
						AppID = appid,
						LastUpdated = FakeUpdatedApp.LastUpdated
					};

					ctx.AppInfoData.Add(test);

					ctx.SaveChanges();
				}
				*/

				SteamUpdateBot.DiscordClient.AppUpdated(FakeUpdatedApp);

				await ctx.RespondAsync($"Pushed a fake update for {FakeUpdatedApp.Name} ({FakeUpdatedApp.AppID})");
			}

			public bool UserHasPermission(DiscordMember userToBeChecked, DiscordGuild guildToBeChecked)
			{
				if (guildToBeChecked != null)
				{
					if (guildToBeChecked.Permissions == (Permissions.All | Permissions.ManageChannels | Permissions.Administrator))
					{
						return true;
					}

					return false;
				}
				else //In DMs they should have control ect
				{
					return true;
				}
			}
		}
	}
}
