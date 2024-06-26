using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;
using System.Collections.Generic;

namespace SteamUpdateProject.Discord.Commands
{
	/// <summary>
	/// Generic help command from DSharpPlus. Lists all of the commands.
	/// </summary>
	public class CustomHelpFormatter : BaseHelpFormatter
	{
		protected DiscordEmbedBuilder HelpBuilder;

		// Ideally I'd use reflection and grab the attributes for the commands so I don't have treadmill work
		private readonly Dictionary<string, string> _godHasLeftUs = new Dictionary<string, string>()
		{
			{"sub","<AppID> or <AppID1 AppID2> `**`" },
			{"del","<AppID> or <AppID1 AppID2> `**`" },
			{"name","<AppID> or <AppID1 AppID2>" },
			{"branches","<AppID>" },
			{"showall","<True/False> `**`" },
			{"debug","<True/False> `**`" },
			{"public","<True/False> `**`" },
			{"history","<AppID>"},
		};

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
			foreach (Command cmd in cmds)
			{
				if (cmd.Description == null || cmd.Name == "help") //Help
					continue;

				HelpBuilder.AddField($"{Context.Prefix}{cmd.Name} {(_godHasLeftUs.ContainsKey(cmd.Name) ? _godHasLeftUs[cmd.Name] : "")}", cmd.Description);
			}
			HelpBuilder.AddField("**[NOTICE]**", "Commands with `**` are limited to anyone with Admin, Manage Channels, or All permissions.");
			return this;
		}

		public override CommandHelpMessage Build()
		{
			return new CommandHelpMessage(embed: HelpBuilder);
		}
	}
}
