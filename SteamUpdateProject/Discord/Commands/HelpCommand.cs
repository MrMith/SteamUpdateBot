﻿using System;
using System.Collections.Generic;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Builders;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace SteamUpdateProject.DiscordLogic.Commands
{
	/// <summary>
	/// Generic help command from DSharpPlus. To-Do: rewrite to not be hardcoded.
	/// </summary>
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
			HelpBuilder.AddField("**[NOTICE]**", "This bot has full DM Support!");
			foreach (var cmd in cmds)
			{
				if (cmd.Description == null || cmd.Name == "help") //Help
					continue;

				HelpBuilder.AddField($"{Context.Prefix}{cmd.Name}", cmd.Description);
			}


			return this;
		}

		public override CommandHelpMessage Build()
		{
			return new CommandHelpMessage(embed: HelpBuilder);
		}
	}
}