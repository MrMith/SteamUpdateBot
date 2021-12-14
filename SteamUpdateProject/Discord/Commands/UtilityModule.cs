﻿using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using SteamKit2;
using SteamUpdateProject.Entities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Text;

namespace SteamUpdateProject.Discord.Commands
{
    /// <summary>
    /// This Module contains commands that are just extra utility like a steam app's name or their branches and when they updated.
    /// </summary>
    internal class UtilityModule : BaseCommandModule
    {
        [Command("devoverride"), Hidden]
        public async Task Devoverride(CommandContext ctx, bool _ov)
        {
            await ctx.TriggerTypingAsync();

            if (ctx.User.Id != SteamUpdateBot.OverrideDiscordID)
            {
                await ctx.RespondAsync($"You're not authorized to use this command. Only the override user can use this.");
                return;
            }

            SteamUpdateBot.DiscordClient.DevOverride = _ov;

            await ctx.RespondAsync($"Set override to {SteamUpdateBot.DiscordClient.DevOverride}.");
        }

        [Command("devoverride"), Hidden]
        public async Task Devoverride(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            if (ctx.User.Id != SteamUpdateBot.OverrideDiscordID)
            {
                await ctx.RespondAsync($"You're not authorized to use this command. Only the override user can use this.");
                return;
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

            if (await SteamUpdateBot.SteamClient.IsSteamDown())
            {
                await ctx.RespondAsync("Steam seems to be down at the moment, see https://steamstat.us/ for more information!");
                return;
            }

            DiscordEmbedBuilder embedBuilder = new DiscordEmbedBuilder();

            embedBuilder.Title = $"{await SteamUpdateBot.SteamClient.GetAppName(AppID)} ({AppID})";

            CustomProductInfo customProductInfo = await Steam.SteamBot.GetFullProductInfo(AppID);

            ReadOnlyCollection<SteamApps.PICSProductInfoCallback> CompleteInfo = customProductInfo.ProductInfo;

            foreach (SteamApps.PICSProductInfoCallback CallBackInfo in CompleteInfo)
            {
                foreach (KeyValuePair<uint, SteamApps.PICSProductInfoCallback.PICSProductInfo> CallBackInfoApps in CallBackInfo.Apps)
                {
                    KeyValue depotKV = CallBackInfoApps.Value.KeyValues.Children.Find(child => child.Name == "depots");

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
                                using (SQLDataBase context = new SQLDataBase(SteamUpdateBot.ConnectionString))
                                {
                                    AppInfo app = context.AllApps.FindLast(SubbedApp => SubbedApp.AppID == AppID);

                                    var BranchUpdateTime = DateTime.UnixEpoch.AddSeconds(double.Parse(branchData.Value));

                                    if (app.LastUpdated.Value.Ticks > BranchUpdateTime.Ticks)
                                    {
                                        app.LastUpdated = BranchUpdateTime;

                                        context.AppInfoData.RemoveRange(context.AllApps.FindAll(SubbedApp => SubbedApp == app));


                                        context.AppInfoData.Add(app);
                                        context.SaveChanges();
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
                if (uint.TryParse(obj, out uint AppID))
                {
                    embedBuilder.AddField(AppID.ToString(), await SteamUpdateBot.SteamClient.GetAppName(AppID));
                }
            }

            await ctx.RespondAsync(embedBuilder.Build());
        }

        [Command("forceupdate"), Hidden]
        public async Task ForceUpdate(CommandContext ctx, params string[] objects)
        {

            await ctx.TriggerTypingAsync();

            if (ctx.User.Id != SteamUpdateBot.OverrideDiscordID)
            {
                await ctx.RespondAsync($"You're not authorized to run this command. Die.");
                return;
            }

            AppUpdate AppUpdate = new AppUpdate();

            AppUpdate.AppID = 570;
            AppUpdate.Content = true;
            AppUpdate.Name = "Dota 2";
            AppUpdate.DepoName = "public";
            AppUpdate.ChangeNumber = 1;
            AppUpdate.LastUpdated = DateTime.UtcNow.AddYears(50);

            SteamUpdateBot.DiscordClient.AppUpdated(AppUpdate);

            await ctx.RespondAsync($"Force updated {AppUpdate.AppID}.");
        }

        [Command("status"), Description("Shows statistics about updates, if steam is down and ping.")]
        public async Task Status(CommandContext ctx)
        {
            await ctx.TriggerTypingAsync();

            if (SteamUpdateBot.SteamClient == null)
            {
                await ctx.RespondAsync($"SteamBot not ready.");
            }

            bool steamStatus = await SteamUpdateBot.SteamClient.IsSteamDown();

            await ctx.RespondAsync($"Ping: {ctx.Client.Ping}.\nSteam Status: {(!steamStatus ? "Online" : "Offline")}.\nTotal updates processed: {LoggingAndErrorHandler.Updates} ({(int)(LoggingAndErrorHandler.Updates / LoggingAndErrorHandler.MinutesRunning)} per minute)\nTotal content updates: {LoggingAndErrorHandler.ContentUpdates}.\nTotal Exceptions: {LoggingAndErrorHandler.Exceptions}\nTotal minutes running: {LoggingAndErrorHandler.MinutesRunning}");
        }

        [Command("log"), Hidden]
        public async Task OpenLog(CommandContext ctx)
        {
            if (ctx.User.Id != SteamUpdateBot.OverrideDiscordID)
            {
                return;
            }

            await ctx.TriggerTypingAsync();

            ProcessStartInfo startInfo = new ProcessStartInfo()
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

            foreach (var obj in objects)
                stringBuilder.Append(obj + " ");


            FeedbackHandler.AddFeedback(stringBuilder.ToString(), $"{ctx.User.Username}#{ctx.User.Discriminator}: " );

            var dev = await SteamUpdateBot.DiscordClient.GetDiscordMember(SteamUpdateBot.OverrideDiscordID);

            if (dev != null)
                await dev.SendMessageAsync($"{ctx.User.Username}#{ctx.User.Discriminator}: " + stringBuilder.ToString());

            await ctx.RespondAsync("Sent to the developer!");
        }
    }
}
