using DSharpPlus;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace SteamUpdateProject.DiscordLogic.Commands
{
	/// <summary>
	/// Handles all of the discord commands related to subscriptions, this including adding/removing apps, displaying that information and configuration options.
	/// </summary>
	public class SubscriptionModule : BaseCommandModule
	{
		[Command("del"), Aliases("removeapp", "delapp", "deleteapp", "remove", "unsubscribe"), Description("Remove a subscription to a Steam Application so you no longer see when it updates by appid (Ex: del 730 or del 730 530)")]
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

				List<uint> AppsThatHaveBeenRemoved = GuildInfo.RemoveMultipleApps(ListOfAppIDS);

				if (AppsThatHaveBeenRemoved.Count == 0)
				{
					await ctx.RespondAsync("You're not subscribed to these apps!");
					return;
				}

				foreach (uint app in AppsThatHaveBeenRemoved)
				{
					AppInfo AppInfo = new AppInfo()
					{
						AppID = app
					};

					AppInfo.Name = await SteamUpdateBot.SteamClient.GetAppName(app);

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
				if (!GuildInfo.RemoveApp(appid))
				{
					await ctx.RespondAsync("You're not subscribed to this app!");
					return;
				}

				AppInfo AppInfo = new AppInfo()
				{
					AppID = appid
				};

				AppInfo.Name = await SteamUpdateBot.SteamClient.GetAppName(appid);

				embedBuilder.Title = "Steam Apps removed:";
				embedBuilder.AddField("Steam App:", $"{AppInfo.Name} ({appid})");

				await ctx.RespondAsync(embed: embedBuilder.Build());
				return;
			}

			await ctx.RespondAsync("ERROR using remove command! Type `!help` to get help on using this command!");
		}

		[Command("sub"), Aliases("addapp", "subscribeapp", "add", "subscribe", "subapp"), Description("Subscribe to a Steam Application to see when it updates by appid (Ex: sub 730 or sub 730 530)")]
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

				List<uint> ListOfConfirmedAppsAdded = GuildInfo.SubMultipleApps(ListOfAppIDS);

				if (ListOfConfirmedAppsAdded.Count == 0)
				{
					await ctx.RespondAsync("Already subscribed to all apps!");
					return;
				}
				StringBuilder builderToReturn = new StringBuilder();

				foreach (uint app in ListOfConfirmedAppsAdded)
				{
					AppInfo AppInfo = new AppInfo()
					{
						AppID = app
					};

					AppInfo.Name = await SteamUpdateBot.SteamClient.GetAppName(app);

					builderToReturn.AppendLine($"{AppInfo.Name} ({AppInfo.AppID})");
				}

				embedBuilder.AddField("Apps:", builderToReturn.ToString());
				await ctx.RespondAsync(embed: embedBuilder.Build());
				return;
			}
			else if (uint.TryParse(objects[0], out uint appid)) //single app
			{
				if (!GuildInfo.SubApp(appid))
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
			await ctx.RespondAsync("ERROR using add command! Type `!help` to get help on using this command!");
		}

		[Command("list"), Aliases("apps"), Description("Displays all of the subscribed apps for this channel.")]
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

			StringBuilder builderToReturn = new StringBuilder();

			foreach (SubbedApp SubbedApp in GuildInfo.SubscribedApps)
			{
				AppInfo AppInfo = DiscordBot.GetCachedAppInfo(SubbedApp.AppID);

				if (AppInfo.Name == null || AppInfo.Name.Length == 0) AppInfo.Name = "Unknown App";

				if (AppInfo.LastUpdated != null)
				{
					builderToReturn.Append($"{AppInfo.Name} ({SubbedApp.AppID})");
					builderToReturn.AppendLine($" was last updated {SteamUpdateBot.DiscordClient.ElapsedTime(AppInfo.LastUpdated)}.");
				}
				else
				{
					builderToReturn.AppendLine($"{AppInfo.Name} ({SubbedApp.AppID})");
				}
			}

			embedBuilder.AddField("Apps", builderToReturn.ToString());

			await ctx.RespondAsync(embed: embedBuilder.Build());
		}

		[Command("showall"), Aliases("all"), Description("Do we show only content changes (Content as in downloadable updates)")]
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

		[Command("showall"), Description("Do we show only content changes (Content as in downloadable updates)")]
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
					context.GuildInformation.RemoveRange(context.AllGuilds.FindAll(guild => guild == GuildInfo));
					GuildInfo.ShowContent = Set;
					context.GuildInformation.Add(GuildInfo);
					context.SaveChanges();
				}
			}

			await ctx.RespondAsync($"Set show all to {Set}.");
		}

		[Command("debug"), Description("***WARNING*** *EVERY* steam update goes through as if you were subscribed to it.")]
		public async Task DebugBool(CommandContext ctx)
		{
			await ctx.TriggerTypingAsync();

			GuildInfo GuildInfo = GetGuildInfo(ctx.Guild == null ? 0 : ctx.Guild.Id, ctx.Guild == null ? ctx.User.Id : ctx.Channel.Id);

			await ctx.RespondAsync($"Debug mode is currently set to {GuildInfo.DebugMode}.");
		}

		[Command("debug"), Description("***WARNING*** *EVERY* steam update goes through as if you were subscribed to it.")]
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
					context.GuildInformation.RemoveRange(context.AllGuilds.FindAll(guild => guild == GuildInfo));
					GuildInfo.DebugMode = Set;
					context.GuildInformation.Add(GuildInfo);
					context.SaveChanges();
				}
			}

			await ctx.RespondAsync($"Debug mode set to {Set}.");

		}

		[Command("public"), Description("Should we only notify this channel if the update is on the default public branch.")]
		public async Task PublicBool(CommandContext ctx)
		{
			await ctx.TriggerTypingAsync();

			GuildInfo GuildInfo = GetGuildInfo(ctx.Guild == null ? 0 : ctx.Guild.Id, ctx.Guild == null ? ctx.User.Id : ctx.Channel.Id);

			await ctx.RespondAsync($"Public mode is currently set to {GuildInfo.PublicDepoOnly}.");
		}

		[Command("public"), Description("Should we only notify this channel if the update is on the default public branch.")]
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
					context.AllGuilds.FindAll(guild => guild == GuildInfo);
					GuildInfo.PublicDepoOnly = Set;
					context.GuildInformation.Add(GuildInfo);
					context.SaveChanges();
				}
			}

			await ctx.RespondAsync($"Public mode set to {Set}.");
		}

		public bool HasPermission(DiscordMember u, DiscordChannel c)
		{
			if (u.Id == SteamUpdateBot.OverrideDiscordID && SteamUpdateBot.DiscordClient.DevOverride)
				return true;

			return u.PermissionsIn(c).HasPermission(Permissions.Administrator) || u.PermissionsIn(c).HasPermission(Permissions.ManageChannels) || u.PermissionsIn(c).HasPermission(Permissions.All);
		}

		public GuildInfo GetGuildInfo(ulong GuildID, ulong ChannelID)
		{
			return DiscordBot.GetGuildInfo(GuildID, ChannelID);
		}
	}
}