using System;
using System.Collections.Generic;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Builders;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace SteamUpdateProject.DiscordLogic
{
	public class CustomHelpFormatter : BaseHelpFormatter
	{
		protected DiscordEmbedBuilder HelpBuilder;

		public CustomHelpFormatter(CommandContext ctx) : base(ctx)
		{
			HelpBuilder = new DiscordEmbedBuilder() { Title = "Help Command" };
		}

		public override BaseHelpFormatter WithCommand(Command command)
		{
			HelpBuilder.AddField(command.Name, command.Description);

			return this;
		}

		public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> cmds)
		{
			string P = this.Context.Prefix;
			HelpBuilder.AddField("**[NOTICE]**","This bot has full DM Support!");
			HelpBuilder.AddField($"{P}add", $"Subscribe to a Steam Application to see when it updates by appid (Ex: {P}add 730 or {P}add 730 530)");
			HelpBuilder.AddField($"{P}remove", $"Remove a subscription to a Steam Application so you no longer see when it updates by appid (Ex: {P}remove 730 or {P}remove 730 530)");
			HelpBuilder.AddField($"{P}all", $"Show all updates (like if the store tags update) or only content updates. Defaults to false. (Ex: {P}all true)");
			HelpBuilder.AddField($"{P}status", $"Shows the ping of the bot to discord, if steam is down and total updates processed this session.");
			HelpBuilder.AddField($"{P}name", $"Converts steam app ID to the steam app's name.");
			HelpBuilder.AddField($"{P}public", $"Will only send messages if the default public steam branch is updated. (Ex: {P}public true or {P}debug false)");
			HelpBuilder.AddField($"{P}branches", $"Lists all of the branches for a certain steam app. (Ex: {P}branches <AppID>");
			HelpBuilder.AddField($"{P}debug", $"**NOT RECOMMENDED** Pipes every update through this channel regardless of subscriptions. (Ex: {P}debug true or {P}debug false)");

			return this;
		}

		public override CommandHelpMessage Build()
		{
			return new CommandHelpMessage(embed: HelpBuilder);
		}
	}
}
