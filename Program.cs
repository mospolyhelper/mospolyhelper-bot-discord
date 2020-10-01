using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace ScheduleDiscordBot
{
    class Program
    {
        public static void Main(string[] args) =>
            new Program().MainAsync().GetAwaiter().GetResult();

        private DiscordSocketClient client;

        public async Task MainAsync()
        {
            client = new DiscordSocketClient();

            client.Log += Log;

            // Remember to keep token private or to read it from an 
            // external source! In this case, we are reading the token 
            // from an environment variable. If you do not know how to set-up
            // environment variables, you may find more information on the 
            // Internet or by using other methods such as reading from 
            // a configuration.
            await client.LoginAsync(TokenType.Bot,
                Environment.GetEnvironmentVariable("BOT_TOKEN"));
            await client.StartAsync();


            client.GuildAvailable += Client_GuildAvailable;
            
            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private Task Client_GuildAvailable(SocketGuild arg)
        {
            var embed = new EmbedBuilder()
                .WithColor(new Color(0x0099ff))
                .WithTitle("Some title")
                .WithUrl("https://discord.js.org/")
                .WithAuthor("Some name", "https://i.imgur.com/wSTFkRM.png", "https://discord.js.org")
                .WithDescription("Some description here")
                .WithThumbnailUrl("https://i.imgur.com/wSTFkRM.png")
                .WithFields(
                new EmbedFieldBuilder()
                .WithName("Regular field title")
                .WithValue("Some value here"),
                new EmbedFieldBuilder()
                .WithName("\u200B")
                .WithValue("\u200B"),
                new EmbedFieldBuilder()
                .WithName("Inline field title")
                .WithValue("Some value here")
                .WithIsInline(true),
                new EmbedFieldBuilder()
                .WithName("Inline field title")
                .WithValue("Some value here")
                .WithIsInline(true)
                )
                .AddField("Inline field title", "Some value here", true)
                .WithImageUrl("https://i.imgur.com/wSTFkRM.png")
                .WithTimestamp(DateTimeOffset.Now)
                .WithFooter("Some footer text here", "https://i.imgur.com/wSTFkRM.png");

            var groupChannel = arg.GetTextChannel(761249029647892480);
            return groupChannel.SendMessageAsync(text: "Test", embed: embed.Build());
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
