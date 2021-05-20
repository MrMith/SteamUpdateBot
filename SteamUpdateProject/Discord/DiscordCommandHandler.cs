using System;
using System.Collections.Generic;
using System.Text;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace SteamUpdateProject.DiscordLogic
{
	class DiscordCommandHandler
	{
		private readonly CommandService _commands;
		private readonly DiscordSocketClient _discord;
		private readonly IServiceProvider _services;

		public DiscordCommandHandler(IServiceProvider services)
		{
			_commands = services.GetRequiredService<CommandService>();
			_discord = services.GetRequiredService<DiscordSocketClient>();
			_services = services;

			// Hook CommandExecuted to handle post-command-execution logic.
			_commands.CommandExecuted += CommandExecutedAsync;
			// Hook MessageReceived so we can process each message to see
			// if it qualifies as a command.
			_discord.MessageReceived += MessageReceivedAsync;
		}

		public async Task InitializeAsync()
		{
			// Register modules that are public and inherit ModuleBase<T>.
			await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
		}

		public async Task MessageReceivedAsync(SocketMessage rawMessage)
		{
			if (!(rawMessage is SocketUserMessage message)) return;
			if (message.Source != MessageSource.User) return;

			int argPos = 0;

			if (!message.HasCharPrefix('!', ref argPos)) return;

			SocketCommandContext context = new SocketCommandContext(_discord, message);

			await _commands.ExecuteAsync(context, argPos, _services);
		}

		public async Task CommandExecutedAsync(Optional<CommandInfo> command, ICommandContext context, IResult result)
		{
			if (!command.IsSpecified)
				return;

			if (result.IsSuccess)
				return;

			await context.Channel.SendMessageAsync($"error: {result}");
		}
	}
}
