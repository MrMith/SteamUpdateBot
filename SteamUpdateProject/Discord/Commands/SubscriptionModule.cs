using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using MongoDB.Driver;
using SteamUpdateProject.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamUpdateProject.Discord.Commands
{
	/// <summary>
	/// Handles all of the discord commands related to subscriptions, this including adding/removing apps, displaying that information, and configuration options.
	/// </summary>
	public class SubscriptionModule : BaseCommandModule
	{
		[Command("del"), Aliases("removeapp", "delapp", "deleteapp", "remove", "unsubscribe", "delete"), Description("Remove a subscription to a Steam Application so you no longer see when it updates by appid (Ex: del 730 or del 730 530)")]
		public async Task RemoveAppAsync(CommandContext ctx, params string[] objects)
		{
			await ctx.TriggerTypingAsync();

			if (ctx.Guild != null && !HasPermission(ctx.Member, ctx.Channel))
			{
				await ctx.RespondAsync($"You do not have permission to run {ctx.Command.Name}.");
				return;
			}

			DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();

			GuildInfo guildInfo = GetGuildInfo(ctx);

			if (guildInfo.SubscribedApps.Count == 0)
			{
				await ctx.RespondAsync("No Steam Application subscriptions in the database for this channel.");
				return;
			}

			StringBuilder stringBuilder = new StringBuilder();

			if (objects.Length > 1 || (objects.Length == 1 && objects[0] == "*")) //Multiple
			{
				List<uint> listOfAppIDS = new List<uint>();
				foreach (string stringAppID in objects)
				{
					if (uint.TryParse(stringAppID, out uint appid))
					{
						listOfAppIDS.Add(appid);
					}
				}

				List<uint> appsThatHaveBeenRemoved = null;

				if (objects[0] == "*")
				{
					await ctx.RespondAsync("Are you sure? Yes/No.");
					InteractivityExtension interact = ctx.Client.GetInteractivity();
					InteractivityResult<DiscordMessage> msg = await interact.WaitForMessageAsync(x => x != null);

					if (msg.Result.Content.Contains("Yes", StringComparison.OrdinalIgnoreCase))
					{
						await ctx.RespondAsync($"Continuing mass {ctx.Command.Name}.");
						await ctx.TriggerTypingAsync();
					}
					else if (msg.Result.Content.Contains("No", StringComparison.OrdinalIgnoreCase))
					{
						await ctx.RespondAsync($"Cancelled mass {ctx.Command.Name}.");
						return;
					}

					appsThatHaveBeenRemoved = guildInfo.RemoveMultipleApps(guildInfo.SubscribedApps);
				}
				else
					appsThatHaveBeenRemoved = guildInfo.RemoveMultipleApps(listOfAppIDS);

				if (appsThatHaveBeenRemoved.Count == 0)
				{
					await ctx.RespondAsync("You're not subscribed to these apps!");
					return;
				}

				foreach (uint app in appsThatHaveBeenRemoved)
				{
					AppInfo appInfo = new AppInfo
					{
						AppID = app,
						Name = await SteamUpdateBot.SteamClient.GetAppName(app)
					};

					stringBuilder.AppendLine($"{appInfo.Name} ({appInfo.AppID})");

					if (appInfo.LastUpdated != null && appInfo.LastUpdated != DateTime.MinValue)
					{
						stringBuilder.Append($". Last updated {appInfo.LastUpdated?.ToLongDateString()}.");
					}
				}

				embedBuilder.Title = "Steam Apps removed:";

				if (stringBuilder.Length > 800)
				{
					//We're over the limit of 1024 characters (Short by 224 to make sure title and stuff can fit) and we need to use pages to display our data.
					InteractivityExtension interactivity = ctx.Client.GetInteractivity();

					IEnumerable<Page> list_pages = interactivity.GeneratePagesInEmbed(stringBuilder.ToString(), DSharpPlus.Interactivity.Enums.SplitType.Line, embedBuilder);

					await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, list_pages, DSharpPlus.Interactivity.Enums.PaginationBehaviour.WrapAround, DSharpPlus.Interactivity.Enums.ButtonPaginationBehavior.DeleteButtons);
					return;
				}

				embedBuilder.AddField("Apps", stringBuilder.ToString());

				await ctx.RespondAsync(embed: embedBuilder.Build());

				return;
			}
			else if (uint.TryParse(objects[0], out uint appid)) //Single
			{
				if (!guildInfo.RemoveApp(appid))
				{
					await ctx.RespondAsync("You're not subscribed to this app!");
					return;
				}

				AppInfo appInfo = new AppInfo
				{
					AppID = appid,
					Name = await SteamUpdateBot.SteamClient.GetAppName(appid)
				};

				embedBuilder.Title = "Steam Apps removed:";
				embedBuilder.AddField("Steam App:", $"{appInfo.Name} ({appid})");

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

			GuildInfo guildInfo = GetGuildInfo(ctx);

			if (objects.Length > 1) //Multiple apps
			{
				embedBuilder.Title = "Apps added:";

				List<uint> listOfAppIDS = new List<uint>();
				foreach (string stringAppID in objects)
				{
					if (uint.TryParse(stringAppID, out uint appid) && !listOfAppIDS.Contains(appid))
					{
						listOfAppIDS.Add(appid);
					}
				}

				List<uint> listOfConfirmedAppsAdded = guildInfo.SubMultipleApps(listOfAppIDS);

				if (listOfConfirmedAppsAdded.Count == 0)
				{
					await ctx.RespondAsync("Already subscribed to all of these steam apps!");
					return;
				}

				StringBuilder builderToReturn = new StringBuilder();

				foreach (uint app in listOfConfirmedAppsAdded)
				{
					AppInfo appInfo = new AppInfo
					{
						AppID = app,
						Name = await SteamUpdateBot.SteamClient.GetAppName(app)
					};

					builderToReturn.AppendLine($"{appInfo.Name} ({appInfo.AppID})");
				}

				if (builderToReturn.Length > 800)
				{
					//We're over the limit of 1024 characters (Short by 224 to make sure title and stuff can fit) and we need to use pages to display our data.
					InteractivityExtension interactivity = ctx.Client.GetInteractivity();

					IEnumerable<Page> list_pages = interactivity.GeneratePagesInEmbed(builderToReturn.ToString(), DSharpPlus.Interactivity.Enums.SplitType.Line, embedBuilder);

					await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, list_pages, DSharpPlus.Interactivity.Enums.PaginationBehaviour.WrapAround, DSharpPlus.Interactivity.Enums.ButtonPaginationBehavior.DeleteButtons);
					return;
				}

				embedBuilder.AddField("Apps", builderToReturn.ToString());

				await ctx.RespondAsync(embed: embedBuilder.Build());
				return;
			}
			else if (uint.TryParse(objects[0], out uint appid)) //single app
			{
				if (!guildInfo.SubApp(appid))
				{
					await ctx.RespondAsync($"Already added app! ({appid})");
					return;
				}

				AppInfo appInfo = new AppInfo
				{
					AppID = appid,
					Name = SteamUpdateBot.SteamClient.GetAppName(appid).Result
				};

				embedBuilder.Title = "Steam Apps added:";
				embedBuilder.AddField("Steam App:", $"{appInfo.Name} ({appid})");

				await ctx.RespondAsync(embed: embedBuilder.Build());
				return;
			}
			await ctx.RespondAsync("ERROR using add command! Type `!help` to get help on using this command!");
		}

		[Command("list"), Aliases("apps"), Description("Displays all of the subscribed apps for this channel.")]
		public async Task ListAllSubscribedApps(CommandContext ctx)
		{
			await ctx.TriggerTypingAsync();

			GuildInfo guildInfo = GetGuildInfo(ctx);

			DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder
			{
				Title = "List of subscribed steam apps:"
			};

			if (guildInfo == null || guildInfo.SubscribedApps.Count == 0)
			{
				await ctx.RespondAsync("No apps found!");
				return;
			}

			StringBuilder builderToReturn = new StringBuilder();

			foreach (SubbedApp subbedApp in guildInfo.SubscribedApps)
			{
				AppInfo appInfo = DiscordBot.GetCachedAppInfo(subbedApp.AppID);

				if (appInfo.Name == null || appInfo.Name.Length == 0) appInfo.Name = "Unknown App";

				if (appInfo.LastUpdated != null)
				{
					builderToReturn.Append($"{appInfo.Name} ({subbedApp.AppID})");
					builderToReturn.AppendLine($" was last updated {SteamUpdateBot.DiscordClient.ElapsedTime(appInfo.LastUpdated)}.");
				}
				else
				{
					builderToReturn.AppendLine($"{appInfo.Name} ({subbedApp.AppID})");
				}
			}

			if (builderToReturn.Length > 800)
			{
				//We're over the limit of 1024 characters (Short by 224 to make sure title and stuff can fit) and we need to use pages to display our data.
				InteractivityExtension interactivity = ctx.Client.GetInteractivity();

				IEnumerable<Page> list_pages = interactivity.GeneratePagesInEmbed(builderToReturn.ToString(), DSharpPlus.Interactivity.Enums.SplitType.Line, embedBuilder);

				await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, list_pages, DSharpPlus.Interactivity.Enums.PaginationBehaviour.WrapAround, DSharpPlus.Interactivity.Enums.ButtonPaginationBehavior.DeleteButtons);
				return;
			}

			embedBuilder.AddField("Apps", builderToReturn.ToString());

			await ctx.RespondAsync(embed: embedBuilder.Build());
		}

		[Command("showall"), Aliases("all"), Description("Do we show only content changes (Content as in file changes for the steam application)")]
		public async Task ShowContent(CommandContext ctx)
		{
			await ctx.TriggerTypingAsync();

			GuildInfo guildInfo = GetGuildInfo(ctx);

			if (guildInfo == null)
			{
				await ctx.RespondAsync("Show all is set to False.");
			}

			await ctx.RespondAsync($"Show all is set to: {guildInfo.ShowContent}.");
		}

		[Command("showall"), Description("Do we show only content changes (Content as in downloadable updates)")]
		public async Task ShowContentBool(CommandContext ctx, bool set)
		{
			await ctx.TriggerTypingAsync();

			if (ctx.Guild != null && !HasPermission(ctx.Member, ctx.Channel))
			{
				await ctx.RespondAsync($"You do not have permission to run {ctx.Command.Name}.");
				return;
			}

			GuildInfo guildInfo = GetGuildInfo(ctx);

			if (guildInfo != null)
			{
				guildInfo.ShowContent = set;

				IMongoDatabase db = SteamUpdateBot.DB.Client.GetDatabase(SteamUpdateBot.DatabaseName);

				FilterDefinition<GuildInfo> gI_Filter = Builders<GuildInfo>.Filter.And(
								Builders<GuildInfo>.Filter.Eq("ChannelID", guildInfo.ChannelID),
								Builders<GuildInfo>.Filter.Eq("GuildID", guildInfo.GuildID));

				IMongoCollection<GuildInfo> gIcollection = db.GetCollection<GuildInfo>(GuildInfo.DBName);

				_ = gIcollection.ReplaceOne(gI_Filter, guildInfo);
			}

			await ctx.RespondAsync($"Set show all to {set}.");
		}

		[Command("debug"), Description("***WARNING*** *EVERY* steam update goes through as if you were subscribed to it."), Hidden]
		public async Task DebugBool(CommandContext ctx)
		{
			if (!IsDev(ctx.User))
				return;

			await ctx.TriggerTypingAsync();

			GuildInfo guildInfo = GetGuildInfo(ctx);

			await ctx.RespondAsync($"Debug mode is currently set to {guildInfo.DebugMode}.");
		}

		[Command("debug"), Description("***WARNING*** *EVERY* steam update goes through as if you were subscribed to it."), Hidden]
		public async Task DebugBool(CommandContext ctx, bool set)
		{
			if (!IsDev(ctx.User))
				return;

			await ctx.TriggerTypingAsync();

			if (ctx.Guild != null && !HasPermission(ctx.Member, ctx.Channel))
			{
				await ctx.RespondAsync($"You do not have permission to run {ctx.Command.Name}.");
				return;
			}

			GuildInfo guildInfo = GetGuildInfo(ctx);

			if (guildInfo != null)
			{
				guildInfo.DebugMode = set;

				IMongoDatabase db = SteamUpdateBot.DB.Client.GetDatabase(SteamUpdateBot.DatabaseName);

				FilterDefinition<GuildInfo> gI_Filter = Builders<GuildInfo>.Filter.And(
								Builders<GuildInfo>.Filter.Eq("ChannelID", guildInfo.ChannelID),
								Builders<GuildInfo>.Filter.Eq("GuildID", guildInfo.GuildID));

				IMongoCollection<GuildInfo> gIcollection = db.GetCollection<GuildInfo>(GuildInfo.DBName);

				gIcollection.ReplaceOne(gI_Filter, guildInfo);
			}
			else
			{
				await ctx.RespondAsync($"Error: Could not find GuildInfo!");
				return;
			}

			await ctx.RespondAsync($"Debug mode set to {set}.");
		}

		[Command("public"), Description("Should we only notify this channel if the update is on the default public branch.")]
		public async Task PublicBool(CommandContext ctx)
		{
			await ctx.TriggerTypingAsync();

			GuildInfo guildInfo = GetGuildInfo(ctx);

			await ctx.RespondAsync($"Public mode is currently set to {guildInfo.PublicDepoOnly}.");
		}

		[Command("public"), Description("Should we only notify this channel if the update is on the default public branch.")]
		public async Task PublicBool(CommandContext ctx, bool set)
		{
			await ctx.TriggerTypingAsync();

			if (ctx.Guild != null && !HasPermission(ctx.Member, ctx.Channel))
			{
				await ctx.RespondAsync($"You do not have permission to run {ctx.Command.Name}.");
				return;
			}

			GuildInfo guildInfo = GetGuildInfo(ctx);

			if (guildInfo != null)
			{
				guildInfo.PublicDepoOnly = set;

				IMongoDatabase db = SteamUpdateBot.DB.Client.GetDatabase(SteamUpdateBot.DatabaseName);

				FilterDefinition<GuildInfo> gI_Filter = Builders<GuildInfo>.Filter.And(
								Builders<GuildInfo>.Filter.Eq("ChannelID", guildInfo.ChannelID),
								Builders<GuildInfo>.Filter.Eq("GuildID", guildInfo.GuildID));

				IMongoCollection<GuildInfo> gIcollection = db.GetCollection<GuildInfo>(GuildInfo.DBName);

				gIcollection.ReplaceOne(gI_Filter, guildInfo);
			}
			else
			{
				await ctx.RespondAsync($"Error: Could not find GuildInfo!");
				return;
			}

			await ctx.RespondAsync($"Public mode set to {set}.");
		}

		[Command("AllSubs"), Hidden]
		public async Task AllSubs(CommandContext ctx)
		{
			await ctx.TriggerTypingAsync();

			if (ctx.Member != null && ctx.Member.Id != SteamUpdateBot.OverrideDiscordID)
			{
				await ctx.RespondAsync($"You're not the dev!");
				return;
			}
			DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder
			{
				Title = "List of subscribed steam apps for this server:"
			};

			StringBuilder stringBuilder = new StringBuilder();

			IMongoDatabase db = SteamUpdateBot.DB.Client.GetDatabase(SteamUpdateBot.DatabaseName);

			FilterDefinition<GuildInfo> gI_Filter = Builders<GuildInfo>.Filter.And(
							Builders<GuildInfo>.Filter.Eq("ChannelID", ctx.Guild == null ? ctx.User.Id : ctx.Channel.Id),
							Builders<GuildInfo>.Filter.Eq("GuildID", ctx.Guild == null ? 0 : ctx.Guild.Id));

			IMongoCollection<GuildInfo> gIcollection = db.GetCollection<GuildInfo>(GuildInfo.DBName);

			GuildInfo local_GI = gIcollection.Find(gI_Filter).Limit(1).SingleOrDefault();

			if (ctx.Guild != null)
			{
				foreach (SubbedApp app in local_GI.SubscribedApps)
				{
					DiscordChannel channel = ctx.Guild.GetChannel((ulong) local_GI.ChannelID);

					stringBuilder.AppendLine($"{await SteamUpdateBot.SteamClient.GetAppName((uint) app.AppID)} ({app.AppID}) {(channel != null ? $"in {channel.Name}" : "")}");
				}
			}
			else
			{
				foreach (SubbedApp app in local_GI.SubscribedApps)
				{
					stringBuilder.AppendLine($"{await SteamUpdateBot.SteamClient.GetAppName((uint) app.AppID)} ({app.AppID})");
				}
			}

			if (stringBuilder.Length > 800)
			{
				//We're over the limit of 1024 characters (Short by 224 to make sure title and stuff can fit) and we need to use pages to display our data.
				InteractivityExtension interactivity = ctx.Client.GetInteractivity();

				IEnumerable<Page> list_pages = interactivity.GeneratePagesInEmbed(stringBuilder.ToString(), DSharpPlus.Interactivity.Enums.SplitType.Line, embedBuilder);

				await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, list_pages, DSharpPlus.Interactivity.Enums.PaginationBehaviour.WrapAround, DSharpPlus.Interactivity.Enums.ButtonPaginationBehavior.DeleteButtons);
				return;
			}

			if (stringBuilder.Length == 0)
			{
				await ctx.RespondAsync("Nothing found!");
				return;
			}

			embedBuilder.AddField("Apps", stringBuilder.ToString());
			await ctx.RespondAsync(embed: embedBuilder.Build());
		}

		/// <summary>
		/// If user has certain permissons (Admin, manage channels, or All) in channel.
		/// </summary>
		/// <param name="u">User</param>
		/// <param name="c">Channel</param>
		/// <returns>If the user has Admin, Manage Channel, or All permission for the given channel.</returns>
		public static bool HasPermission(DiscordMember u, DiscordChannel c)
		{
			if (u.Id == SteamUpdateBot.OverrideDiscordID && SteamUpdateBot.DiscordClient.DevOverride)
				return true;

			return u.PermissionsIn(c).HasPermission(Permissions.Administrator) || u.PermissionsIn(c).HasPermission(Permissions.ManageChannels) || u.PermissionsIn(c).HasPermission(Permissions.All);
		}

		/// <summary>
		/// Checks if the <see cref="DiscordUser"/>'s Id is equal to <see cref="SteamUpdateBot.OverrideDiscordID"/>
		/// </summary>
		/// <param name="u">User running the command</param>
		/// <returns>If the DiscordUser.Id is the same as the provided OverrideDiscordID</returns>
		public static bool IsDev(DiscordUser u)
		{
			return u.Id == SteamUpdateBot.OverrideDiscordID;
		}

		/// <summary>
		/// I didn't wanna type <see cref="DiscordBot.GetGuildInfo"/> every time I wanted it.
		/// </summary>
		/// <returns><see cref="DiscordBot.GetGuildInfo"/></returns>
		public static GuildInfo GetGuildInfo(ulong guildID, ulong channelID)
		{
			return DiscordBot.GetGuildInfo(guildID, channelID);
		}

		/// <summary>
		/// I didn't wanna type <see cref="DiscordBot.GetGuildInfo"/> every time I wanted it.
		/// </summary>
		/// <returns><see cref="DiscordBot.GetGuildInfo"/></returns>
		public static GuildInfo GetGuildInfo(CommandContext ctx)
		{
			return DiscordBot.GetGuildInfo(ctx.Guild == null ? 0 : ctx.Guild.Id, ctx.Guild == null ? ctx.User.Id : ctx.Channel.Id);
		}
	}
}
