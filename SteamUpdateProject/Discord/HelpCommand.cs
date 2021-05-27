using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
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
			HelpBuilder.AddField("!add", "Subscribe to a Steam Application to see when it updates by appid (Ex: !add 730 or !add 730 530)");
			HelpBuilder.AddField("!remove", "Remove a subscription to a Steam Application so you no longer see when it updates by appid (Ex: !remove 730 or !remove 730 530)");
			HelpBuilder.AddField("!all", "Show all updates (like if the store tags update) or only content updates. Defaults to false. (Ex: !all true)");
			HelpBuilder.AddField("!status", "Shows the ping of the bot to discord, if steam is down and total updates processed this session.");
			HelpBuilder.AddField("!public", "Will only send messages if the default public steam branch is updated. (Ex: !public true or !debug false)");
			HelpBuilder.AddField("!debug", "**NOT RECOMMENDED** Pipes every update through this channel regardless of subscriptions. (Ex: !debug true or !debug false)");

			return this;
		}

		public override CommandHelpMessage Build()
		{
			return new CommandHelpMessage(embed: HelpBuilder);
		}
	}
}
