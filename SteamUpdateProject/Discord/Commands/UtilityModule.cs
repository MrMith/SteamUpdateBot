using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;
using MongoDB.Driver;
using SteamKit2;
using SteamUpdateProject.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SteamUpdateProject.Discord.Commands
{
	/// <summary>
	/// This Module contains commands that are just extra utility like a steam app's name or their branches and when they updated.
	/// </summary>
	internal class UtilityModule : BaseCommandModule
	{
		[Command("devoverride"), Hidden]
		public async Task Devoverride(CommandContext ctx, bool ov)
		{
			if (!SubscriptionModule.IsDev(ctx.User))
				return;

			await ctx.TriggerTypingAsync();

			SteamUpdateBot.DiscordClient.DevOverride = ov;

			await ctx.RespondAsync($"Set override to {SteamUpdateBot.DiscordClient.DevOverride}.");
		}

		[Command("devoverride"), Hidden]
		public async Task Devoverride(CommandContext ctx)
		{
			if (!SubscriptionModule.IsDev(ctx.User))
				return;

			await ctx.TriggerTypingAsync();

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
		public async Task Branches(CommandContext ctx, uint appID)
		{
			await ctx.TriggerTypingAsync();

			if (await SteamUpdateBot.SteamClient.IsSteamDown())
			{
				await ctx.RespondAsync("Steam seems to be down at the moment, see https://steamstat.us/ for more information!");
				return;
			}

			DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder
			{
				Title = $"{await SteamUpdateBot.SteamClient.GetAppName(appID)} ({appID})"
			};

			CustomProductInfo customProductInfo = await Steam.SteamBot.GetFullProductInfo(appID);

			ReadOnlyCollection<SteamApps.PICSProductInfoCallback> completeInfo = customProductInfo.ProductInfo;

			foreach (SteamApps.PICSProductInfoCallback callBackInfo in completeInfo)
			{
				foreach (KeyValuePair<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> callBackInfoApps in callBackInfo.Apps)
				{
					KeyValue depotKV = callBackInfoApps.Value.KeyValues.Children.Find(child => child.Name == "depots");

					if (depotKV == null)
						continue;

					KeyValue branchesKVP = depotKV["branches"];

					if (branchesKVP == null)
						continue;

					foreach (KeyValue branchKVP in branchesKVP.Children)
					{
						foreach (KeyValue branchData in branchKVP.Children)
						{
							if (branchData.Name != "timeupdated")
								continue;

							if (branchKVP.Name == "public")
							{
								IMongoDatabase db = SteamUpdateBot.DB.Client.GetDatabase(SteamUpdateBot.DatabaseName);

								IMongoCollection<AppInfo> aI_Collection = db.GetCollection<AppInfo>(AppInfo.DBName);

								FilterDefinition<AppInfo> aI_Filter = Builders<AppInfo>.Filter.Eq("AppID", appID);

								AppInfo app = aI_Collection.Find(aI_Filter).SortBy(x => x.LastUpdated).Limit(1).SingleOrDefault();

								DateTime branchUpdateTime = DateTime.UnixEpoch.AddSeconds(double.Parse(branchData.Value));

								if (app.LastUpdated == null || app.LastUpdated.Value.Ticks > branchUpdateTime.Ticks)
								{
									app.LastUpdated = branchUpdateTime;
									try
									{
										await aI_Collection.InsertOneAsync(app);
									}
									catch (MongoWriteException e)
									{
										Console.WriteLine(e.Message);
									}

								}
							}

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

			if (await SteamUpdateBot.SteamClient.IsSteamDown())
			{
				await ctx.RespondAsync($"Steam seems to be down at the moment, see https://steamstat.us/ for more information!");
				return;
			}

			DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();

			foreach (string obj in objects)
			{
				if (uint.TryParse(obj, out uint appID))
				{
					embedBuilder.AddField(appID.ToString(), await SteamUpdateBot.SteamClient.GetAppName(appID));
				}
			}

			await ctx.RespondAsync(embedBuilder.Build());
		}

		[Command("forceupdate"), Hidden]
		public async Task ForceUpdate(CommandContext ctx, params string[] objects)
		{
			await ctx.TriggerTypingAsync();

			if (!SubscriptionModule.IsDev(ctx.User))
				return;

			AppUpdate appUpdate = new AppUpdate
			{
				AppID = 570,
				Content = true,
				Name = "Dota 2",
				DepoName = "public",
				ChangeNumber = 1,
				LastUpdated = DateTime.UtcNow.AddYears(50)
			};

			SteamUpdateBot.DiscordClient.AppUpdated(appUpdate);

			await ctx.RespondAsync($"Force updated {appUpdate.AppID}.");
		}

		[Command("status"), Description("Shows statistics about updates, if steam is down and ping.")]
		public async Task Status(CommandContext ctx)
		{
			await ctx.TriggerTypingAsync();

			if (SteamUpdateBot.SteamClient == null)
			{
				await ctx.RespondAsync($"SteamBot not ready.");
				return;
			}

			bool steamStatus = await SteamUpdateBot.SteamClient.IsSteamDown();

			Process currentProcess = Process.GetCurrentProcess();

			DiscordEmbedBuilder builder = new DiscordEmbedBuilder
			{
				Title = "Status"
			};

			builder.AddField("Ping:", ctx.Client.Ping.ToString(), true);
			builder.AddField("Steam Status:", (!steamStatus ? "Online" : "Offline"), false);

			string updatesProcessed = LoggingAndErrorHandler.Updates != 0 && LoggingAndErrorHandler.MinutesRunning != 0 ? ((int) (LoggingAndErrorHandler.Updates / LoggingAndErrorHandler.MinutesRunning)).ToString() : "0";
			builder.AddField("Total updates processed:", $"{LoggingAndErrorHandler.Updates} ({updatesProcessed} per minute)", true);

			builder.AddField("Total content updates:", LoggingAndErrorHandler.ContentUpdates.ToString());
			builder.AddField("Total Exceptions:", LoggingAndErrorHandler.Exceptions.ToString());
			builder.AddField("Total minutes running:", LoggingAndErrorHandler.MinutesRunning.ToString());

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				
				ProcessStartInfo startInfo = new ProcessStartInfo() { FileName = "/usr/bin/vcgencmd", Arguments = "measure_temp", RedirectStandardOutput = true };
				Process proc = new Process() { StartInfo = startInfo, };
				proc.Start();
				string output = proc.StandardOutput.ReadToEnd();
				proc.WaitForExit();

				builder.AddField("CPU Temperature:", output.Replace("temp=", ""));
			}

			if (currentProcess != null)
			{
				builder.AddField("Memory Used:", $"{(currentProcess.WorkingSet64 / 1048576)} MB");
				builder.AddField("Peak Memory:", $"{currentProcess.PeakWorkingSet64 / 1048576} MB");
			}
			await ctx.RespondAsync(builder.Build());
		}

		[Command("log"), Hidden]
		public async Task OpenLog(CommandContext ctx)
		{
			if (!SubscriptionModule.IsDev(ctx.User) || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				return;

			await ctx.TriggerTypingAsync();

			ProcessStartInfo startInfo = new ProcessStartInfo();

			startInfo = new ProcessStartInfo()
			{
				Arguments = Directory.GetCurrentDirectory(),
				FileName = "explorer.exe"
			};

			Process.Start(startInfo);

			await ctx.RespondAsync("Done.");
		}

		[Command("feedback"), Description("Provide Feedback to the bot developer!")]
		public async Task UserFeedBack(CommandContext ctx)
		{
			await ctx.TriggerTypingAsync();

			await ctx.RespondAsync("Do `!feedback <User Input>`!");
		}

		[Command("feedback")]
		public async Task UserFeedBack(CommandContext ctx, params string[] objects)
		{
			await ctx.TriggerTypingAsync();

			StringBuilder stringBuilder = new StringBuilder();

			foreach (string obj in objects)
				stringBuilder.Append(obj + " ");

			FeedbackHandler.AddFeedback(stringBuilder.ToString(), $"{ctx.User.Username}#{ctx.User.Discriminator}: ");

			DiscordMember dev = await SteamUpdateBot.DiscordClient.GetDiscordMember(SteamUpdateBot.OverrideDiscordID);

			if (dev != null)
				await dev.SendMessageAsync($"{ctx.User.Username}#{ctx.User.Discriminator}: " + stringBuilder.ToString());

			await ctx.RespondAsync("Sent to the developer!");
		}

		///Not the best way to do it since its hard-coded 
		///But I like the pain.
		private readonly string[] _allPatchNotes = new string[]
		{
			"Jan 12th 2025\nStarting to see a theme of when I want to update this. Updated to .NET 9.0, upgraded and implemented all breaking changes with nuget packages, and make this a little more friendly to host.",
			"June 12th 2022\nAdded **history** command *(Ex: !history 570)* which allows you to list all of the updates of an app in the database.",
			"June 11th 2022\nDatabase no longer deletes every entry from the database when an app updates.",
			"June 10th 2022\nSwapped the backend database from Entity Framework to MongoDB because its just much easier to work with on a small scale project.",
			"Jan 11th 2022\nAdded mass delete and to use just do the removeapp command with * (Ex: !remove *) and follow the prompt, fixed the branch command not working and fixed some error spams when Steam is down (Thanks Maintenance day)",
			"Jan 10th 2022\nAdded config verification for when people try to clone the project so they get a input instead of a spam of errors.",
			"Jan 5th 2022\nAdded Patchnotes command.",
			"Jan 4th 2022\nFilter out logging related to unknown commands because I do not want to spy on other people's bot useage.",
			"Jan 3rd 2022\nSwapped over to using interactive pages (this type of embed) when it is useful to do so and switched to using tabs over spaces.",
			"Dec 30th 2021\nFixed bug related saving data to the database.",
			"Dec 30th 2021\nOptimized the useage of the bot by reducing the amount of times I am an idiot.",
			"Dec 22nd 2021\nSwapped over to .NET 6.0 and changed some of the code to reflect using those new features.",
			"Dec 13th 2021\nAdded a new **feedback** command so you can more easily leave feedback for me."
		};

		[Command("patchnotes"), Aliases("pn", "changes", "updates", "update", "patchnote"), Description("Shows changes made to the bot.")]
		public async Task PatchNotes(CommandContext ctx)
		{
			await ctx.TriggerTypingAsync();

			List<Page> pagesToShow = new List<Page>();

			foreach (string note in _allPatchNotes)
			{
				string[] notes = note.Split("\n");

				DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder();
				discordEmbed.Title = "Patch Notes";
				discordEmbed.AddField(notes[0], notes[1]);

				pagesToShow.Add(new Page("", discordEmbed));
			}

			InteractivityExtension interactivity = ctx.Client.GetInteractivity();

			await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, pagesToShow, DSharpPlus.Interactivity.Enums.PaginationBehaviour.WrapAround, DSharpPlus.Interactivity.Enums.ButtonPaginationBehavior.DeleteButtons);
		}

		[Command("history"), Description("Lists all of updates in located within the database of this project.")]
		//[Aliases("history")]
		public async Task History(CommandContext ctx)
		{
			await ctx.TriggerTypingAsync();

			await ctx.RespondAsync("Useage: branches <AppID>");
		}

		[Command("history"), Description("Lists all of updates in located within the database of this project.")]
		public async Task History(CommandContext ctx, uint appID)
		{
			await ctx.TriggerTypingAsync();

			if (await SteamUpdateBot.SteamClient.IsSteamDown())
			{
				await ctx.RespondAsync("Steam seems to be down at the moment, see https://steamstat.us/ for more information!");
				return;
			}

			DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();

			StringBuilder builderToReturn = new StringBuilder();

			embedBuilder.Title = $"{await SteamUpdateBot.SteamClient.GetAppName(appID)} ({appID})";

			IMongoDatabase db = SteamUpdateBot.DB.Client.GetDatabase(SteamUpdateBot.DatabaseName);

			FilterDefinition<AppInfo> aI_Filter = Builders<AppInfo>.Filter.Eq("AppID", appID);

			IMongoCollection<AppInfo> aI_Collection = db.GetCollection<AppInfo>(AppInfo.DBName);

			List<AppInfo> local_AI_List = aI_Collection.Find(aI_Filter).SortByDescending(x => x.LastUpdated).ToList();

			foreach (AppInfo aI in local_AI_List)
			{
				if (aI.LastUpdated == null || string.IsNullOrEmpty(aI.DepoName))
					continue;

				builderToReturn.AppendLine($"{aI.DepoName} was updated {SteamUpdateBot.DiscordClient.ElapsedTime(aI.LastUpdated)}.");
			}

			if (builderToReturn.Length == 0)
			{
				await ctx.RespondAsync("Unable to find anything for this AppID!");
				return;
			}

			InteractivityExtension interactivity = ctx.Client.GetInteractivity();

			IEnumerable<Page> list_pages = interactivity.GeneratePagesInEmbed(builderToReturn.ToString(), DSharpPlus.Interactivity.Enums.SplitType.Line, embedBuilder);

			await interactivity.SendPaginatedMessageAsync(ctx.Channel, ctx.User, list_pages, DSharpPlus.Interactivity.Enums.PaginationBehaviour.WrapAround, DSharpPlus.Interactivity.Enums.ButtonPaginationBehavior.DeleteButtons);
		}
	}
}
