using DSharpPlus;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Data.Entity;
using SteamKit2;

namespace SteamUpdateProject.DiscordLogic.Commands
{
	/// <summary>
	/// This Module contains commands that are just extra utility like a steam app's name or their branches and when they updated.
	/// </summary>
	class UtilityModule : BaseCommandModule
	{
		[Command("devoverride"), Hidden]
		public async Task Devoverride(CommandContext ctx, bool _ov)
		{
			await ctx.TriggerTypingAsync();

			if (ctx.User.Id != 185739967379537920)
			{
				await ctx.RespondAsync($"You're not authorized to use this command. Only the bot owner can use this.");
			}

			SteamUpdateBot.DiscordClient.DevOverride = _ov;

			await ctx.RespondAsync($"Set override to {SteamUpdateBot.DiscordClient.DevOverride}.");
		}

		[Command("devoverride"), Hidden]
		public async Task Devoverride(CommandContext ctx)
		{
			await ctx.TriggerTypingAsync();

			if (ctx.User.Id != 185739967379537920)
			{
				await ctx.RespondAsync($"You're not authorized to use this command. Only the bot owner can use this.");
			}

			await ctx.RespondAsync($"Override is set to {SteamUpdateBot.DiscordClient.DevOverride}.");
		}

		[Command("branches"), Description("Lists all of the branches for a certain steam app.")]
		[Aliases("branch")]
		public async Task Branches(CommandContext ctx)
		{
			await ctx.TriggerTypingAsync();

			await ctx.RespondAsync("Useage: branches <AppID>");
		}

		[Command("branches"), Description("Lists all of the branches for a certain steam app.")]
		public async Task Branches(CommandContext ctx, uint AppID)
		{
			await ctx.TriggerTypingAsync();

			DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();

			embedBuilder.Title = $"{await SteamUpdateBot.SteamClient.GetAppName(AppID)} ({AppID})";

			CustomProductInfo customProductInfo = await Steam.SteamBot.GetFullProductInfo(AppID);

			System.Collections.ObjectModel.ReadOnlyCollection<SteamApps.PICSProductInfoCallback> CompleteInfo = customProductInfo.ProductInfo;

			foreach (SteamApps.PICSProductInfoCallback CallBackInfo in CompleteInfo)
			{
				foreach (KeyValuePair<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> CallBackInfoApps in CallBackInfo.Apps)
				{
					KeyValue depotKV = CallBackInfoApps.Value.KeyValues.Children.Find(child => child.Name == "depots");

					if (depotKV == null)
						continue;

					KeyValue branchesKVP = depotKV["branches"];
					if (branchesKVP == null) continue;

					foreach (KeyValue branchKVP in branchesKVP.Children)
					{
						foreach (KeyValue branchData in branchKVP.Children)
						{
							if (branchData.Name != "timeupdated")
								continue;

							embedBuilder.AddField($"{branchKVP.Name}", $"Last updated {SteamUpdateBot.DiscordClient.ElapsedTime(DateTime.UnixEpoch.AddSeconds(double.Parse(branchData.Value)))}");
						}
					}
				}
			}

			if (embedBuilder.Fields.Count == 0)
			{
				embedBuilder.AddField("N/A", "Unable to find anything for this AppID.");
			}

			await ctx.RespondAsync(embedBuilder.Build());
		}

		[Command("name"), Description("Gets the steam app's name from the steam app's ID.")]
		public async Task IDToName(CommandContext ctx, params string[] objects)
		{
			await ctx.TriggerTypingAsync();

			DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();

			foreach (string obj in objects)
			{
				if (uint.TryParse(obj, out uint AppID))
				{
					embedBuilder.AddField(AppID.ToString(), await SteamUpdateBot.SteamClient.GetAppName(AppID));
				}
			}

			await ctx.RespondAsync(embedBuilder.Build());
		}

		[Command("status"), Description("Shows statistics about updates, if steam is down or ping.")]
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

			await ctx.RespondAsync($"Ping: {ctx.Client.Ping}.\nSteam Status: {(steamStatus ? "Online" : "Offline")}.\nTotal updates processed: {SteamUpdateBot.Updates} ({(int)(SteamUpdateBot.Updates / SteamUpdateBot.MinutesRunning)} per minute)\nTotal content updates: {SteamUpdateBot.ContentUpdates}.\nTotal Exceptions: {SteamUpdateBot.Exceptions}\nTotal minutes running: {SteamUpdateBot.MinutesRunning}");
		}

		private List<string> SecretLinks = new List<string>()
			{
				"⣿⣿⣿⣿⡿⠿⢿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿\n⣿⣿⡿⠁⣀⣤⣤⣄⢿⣿⣿⣿⣿⣿⣿⣿⠋⠁⣀⣀⡀⠙⣿⣿⣿⣿⣿⣿⣿⣿\n⣿⣿⠁⢀⣟⣓⡲⣿⡡⣿⣿⣿⣿⣿⣿⠃⢠⣽⠿⢿⣿⣦⢹⣿⣿⣿⣿⣿⣿⣿\n⣿⣿⣄⣘⣿⡟⡽⠾⠜⢹⣿⣿⣿⣿⠫⡆⣿⣿⣭⣰⡟⢉⢺⣿⣿⣿⣿⣿⣿⣿\n⣿⣿⡵⣿⣿⣿⣶⣬⡶⣸⣿⣿⣿⣿⢺⣻⣿⡟⡵⢿⡅⡇⣿⣿⠟⠻⠿⢿⣿⣿\n⣿⣿⣷⣸⣿⣿⣿⣿⢧⣿⣿⣿⡿⣡⣿⣧⢻⣿⣮⣅⢗⣽⠋⢀⣄⡀⠄⠄⠹⣿\n⣿⣿⣿⢱⣿⣿⣿⣿⣼⣿⣿⢋⣼⣿⣿⣿⠗⣬⣯⣵⣿⡧⢱⣿⢛⢿⣷⣦⣀⣿\n⣿⣿⣿⢸⣿⣿⣿⡇⣿⡿⢡⣿⣿⣿⡿⣣⣾⣿⡿⢟⣻⣅⣿⡷⣾⣟⣑⡮⣼⣿\n⣿⣿⣿⢸⣿⣿⣿⣧⢿⢧⣾⣿⣿⣿⣱⡿⢟⣭⣾⣿⣿⣿⢿⠒⡭⡞⠟⣼⣿⣿\n⣿⣿⣿⡎⣿⣿⣿⣿⣶⣼⣿⣿⣿⣗⣩⣾⣿⣿⡿⢟⣛⣭⣭⣽⣯⣵⣿⣿⣿⣿\n⣿⣿⣿⡇⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⢟⣩⣾⣿⣿⣿⣿⠿⠛⠛⠛⢿⣿⣿\n⣿⣿⣿⡇⢻⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⢃⣾⣿⣿⣿⣿⣿⡏⣤⣶⣤⣄⡀⣼⣿\n⣿⣿⣿⡇⢸⣿⣿⣿⣿⣿⣿⣿⣿⣿⡇⢾⣿⣿⣿⣿⣿⣿⢽⣏⣩⡟⠛⠇⣿⣿\n⣿⣿⣿⣧⢸⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣶⣿⣿⣿⣯⣭⣽⣾⡯⢛⣨⡿⣰⣿⣿\n⣿⣿⣿⣿⠸⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⣿⡿⠿⣿⣿⣛⣛⣛⣱⣊⣴⣿⣿⣿",
				"https://youtu.be/F9Cd7PsVMuQ",
				"https://www.youtube.com/watch?v=NdqbI0_0GsM",
				"https://www.youtube.com/watch?v=c9JNp6kdKqU",
				"https://www.youtube.com/watch?v=1lHXfGAlp58",
			};

		[Command("secret"), Hidden]
		public async Task SecretCommand(CommandContext ctx)
		{
			Random rand = new Random();

			int Index = rand.Next(0, SecretLinks.Count - 1);

			await ctx.RespondAsync(SecretLinks[Index]);
		}
	}
}
