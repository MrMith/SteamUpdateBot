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
			[Command("removeapp"), Aliases("delapp", "deleteapp", "remove", "unsubscribe"), Description("Removes Steam App(s) from this channel's subscription list.")]
			public async Task RemoveAppAsync(CommandContext ctx, params string[] objects)
			{
				await ctx.TriggerTypingAsync();

				if (ctx.Guild != null && !HasPermission(ctx.Member, ctx.Channel))
				{
					await ctx.RespondAsync($"You do not have permission to run {ctx.Command.Name}.");
					return;
				}

				DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();

				GuildInfo GuildInfo = GetGuildInfo(ctx.Guild == null ? 0 : ctx.Guild.Id, ctx.Guild == null ? ctx.User.Id : ctx.Channel.Id);

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

			[Command("subapp"), Aliases("addapp", "subscribeapp", "add", "subscribe"), Description("Adds Steam App(s) from this channel's subscription list.")]
			public async Task AddAppAsync(CommandContext ctx, params string[] objects)
			{
				await ctx.TriggerTypingAsync();

				if (ctx.Guild != null && !HasPermission(ctx.Member, ctx.Channel))
				{
					await ctx.RespondAsync($"You do not have permission to run {ctx.Command.Name}.");
					return;
				}

				DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();

				GuildInfo GuildInfo = GetGuildInfo(ctx.Guild == null ? 0 : ctx.Guild.Id, ctx.Guild == null ? ctx.User.Id : ctx.Channel.Id);

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

			[Command("list"), Aliases("apps"), Description("Displays all of the subscribed apps for this channel and when they were last updated.")]
			public async Task ListAllSubscribedApps(CommandContext ctx)
			{
				await ctx.TriggerTypingAsync();

				GuildInfo GuildInfo = GetGuildInfo(ctx.Guild == null ? 0 : ctx.Guild.Id, ctx.Guild == null ? ctx.User.Id : ctx.Channel.Id);

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

			[Command("showall"), Aliases("all"), Description("Shows the current value for show all. What  `Show all` means is if the bot will notify you for content changes only.")]
			public async Task ShowContent(CommandContext ctx)
			{
				await ctx.TriggerTypingAsync();

				GuildInfo GuildInfo = GetGuildInfo(ctx.Guild == null ? 0 : ctx.Guild.Id, ctx.Guild == null ? ctx.User.Id : ctx.Channel.Id);

				if (GuildInfo == null)
				{
					await ctx.RespondAsync("Show all is set to False.");
				}

				await ctx.RespondAsync($"Show all is set to: {GuildInfo.ShowContent}.");
			}

			[Command("showall"), Description("Enables/Disables the setting that will only push updates when it a content change (aka not store tag changes)")]
			public async Task ShowContentBool(CommandContext ctx, bool Set)
			{
				await ctx.TriggerTypingAsync();

				if (ctx.Guild != null && !HasPermission(ctx.Member, ctx.Channel))
				{
					await ctx.RespondAsync($"You do not have permission to run {ctx.Command.Name}.");
					return;
				}

				GuildInfo GuildInfo = GetGuildInfo(ctx.Guild == null ? 0 : ctx.Guild.Id, ctx.Guild == null ? ctx.User.Id : ctx.Channel.Id);

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

			[Command("debug"), Description("True if you want to push every steam update through this channel.")]
			public async Task DebugBool(CommandContext ctx, bool Set)
			{
				await ctx.TriggerTypingAsync();

				if (ctx.Guild != null && !HasPermission(ctx.Member, ctx.Channel))
				{
					await ctx.RespondAsync($"You do not have permission to run {ctx.Command.Name}.");
					return;
				}

				GuildInfo GuildInfo = GetGuildInfo(ctx.Guild == null ? 0 : ctx.Guild.Id, ctx.Guild == null ? ctx.User.Id : ctx.Channel.Id);

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

			[Command("public"), Description("Value for if you should only notify this channel only when the default public branch is updated.")]
			public async Task PublicBool(CommandContext ctx)
			{
				await ctx.TriggerTypingAsync();

				GuildInfo GuildInfo = GetGuildInfo(ctx.Guild == null ? 0 : ctx.Guild.Id, ctx.Guild == null ? ctx.User.Id : ctx.Channel.Id);

				await ctx.RespondAsync($"Public mode is currently set to {GuildInfo.PublicDepoOnly}.");
			}

			[Command("public"), Description("What `Public` does is only push notifications through this channel if the steam app's default branch is updated.")]
			public async Task PublicBool(CommandContext ctx, bool Set)
			{
				await ctx.TriggerTypingAsync();

				if (ctx.Guild != null && !HasPermission(ctx.Member, ctx.Channel))
				{
					await ctx.RespondAsync($"You do not have permission to run {ctx.Command.Name}.");
					return;
				}

				GuildInfo GuildInfo = GetGuildInfo(ctx.Guild == null ? 0 : ctx.Guild.Id, ctx.Guild == null ? ctx.User.Id : ctx.Channel.Id);

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

			[Command("debug"), Description("Shows you if debug is true/false (aka if the bot should notify you for every update)")]
			public async Task DebugBool(CommandContext ctx)
			{
				await ctx.TriggerTypingAsync();

				GuildInfo GuildInfo = GetGuildInfo(ctx.Guild == null ? 0 : ctx.Guild.Id, ctx.Guild == null ? ctx.User.Id : ctx.Channel.Id);

				await ctx.RespondAsync($"Debug mode is currently set to {GuildInfo.DebugMode}.");
			}

			[Command("status"), Description("Shows statistics about updates, updates with content changes, if steam is down or ping.")]
			public async Task Status(CommandContext ctx)
			{
				await ctx.TriggerTypingAsync();
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

				await ctx.RespondAsync($"Ping: {ctx.Client.Ping}.\nSteam Status: {(steamStatus ? "Online" : "Offline")}.\nTotal updates processed: {SteamUpdateBot.Updates} ({(int)(SteamUpdateBot.Updates / (DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime()).TotalMinutes)} per minute)\nTotal content updates: {SteamUpdateBot.ContentUpdates}.\nTotal Exceptions: {SteamUpdateBot.Exceptions}");
			}

			[Command("forceupdate"), Hidden]
			public async Task ForceUpdate(CommandContext ctx, uint appid)
			{
				await ctx.TriggerTypingAsync();

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

			public bool HasPermission(DiscordMember u, DiscordChannel c)
			{
				return u.PermissionsIn(c).HasPermission(Permissions.Administrator) || u.PermissionsIn(c).HasPermission(Permissions.ManageChannels) || u.PermissionsIn(c).HasPermission(Permissions.All);
			}

			public GuildInfo GetGuildInfo(ulong GuildID, ulong ChannelID)
			{
				return DiscordBot.GetGuildInfo(GuildID, ChannelID);
			}
		}
	}
}
