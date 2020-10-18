using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

namespace Mospolyhelper.Features.Clients
{
    class MainClient : DiscordSocketClient
    {
        //private DiscordSocketClient client;
        private readonly CommandService commands;

        public CommandService Commands => commands;

        public MainClient()
        {
            //client = new DiscordSocketClient();
            commands = new CommandService();
        }

        public async Task Launch()
        {
            base.Log += Log;

            // Remember to keep token private or to read it from an 
            // external source! In this case, we are reading the token 
            // from an environment variable. If you do not know how to set-up
            // environment variables, you may find more information on the 
            // Internet or by using other methods such as reading from 
            // a configuration.
            await LoginAsync(
                TokenType.Bot,
                Environment.GetEnvironmentVariable("BOT_TOKEN")
            );
            await StartAsync();
            Ready += MainClient_Ready;
            MessageReceived += Client_MessageReceived;

            // Here we discover all of the command modules in the entry 
            // assembly and load them. Starting from Discord.NET 2.0, a
            // service provider is required to be passed into the
            // module registration method to inject the 
            // required dependencies.
            //
            // If you do not use Dependency Injection, pass null.
            // See Dependency Injection guide for more information.
            await commands.AddModulesAsync(Assembly.GetEntryAssembly(), null);
        }

        private Task MainClient_Ready()
        {
            this.SetGameAsync(
                "bit.ly/mospolyhelper", 
                
                type: ActivityType.Playing
                );
            return Task.CompletedTask;
        }

        private Task Client_MessageReceived(SocketMessage arg)
        {
            return Task.Run(() => ProcessMessage(arg));
        }

        private async void ProcessMessage(SocketMessage msg)
        {
            // Don't process the command if it was a system message
            if (!(msg is SocketUserMessage message))
                return;

            // Create a number to track where the prefix ends and the command begins
            var argPos = 0;

            // Determine if the message is a command based on the prefix and make sure no bots trigger commands
            if (!(message.HasCharPrefix('!', ref argPos) ||
                  message.HasMentionPrefix(CurrentUser, ref argPos)) ||
                message.Author.IsBot)
                return;

            // Create a WebSocket-based command context based on the message
            var context = new SocketCommandContext(this, message);

            // Execute the command with the command context we just
            // created, along with the service provider for precondition checks.
            await commands.ExecuteAsync(context, argPos, null);
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
