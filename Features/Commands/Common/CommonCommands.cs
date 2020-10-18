using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mospolyhelper.Features.Clients;

namespace Mospolyhelper.Features.Commands.Common
{
    public class CommonCommands : ModuleBase<SocketCommandContext>
    {
        [Command("clear")]
        [Alias("почисти")]
        [Summary("Удаляет введённое количество сообщений")]
        public async Task Clear(int limit)
        {
            if (Context.User.Id != 565615393721286656u) return;

            var cashedMessages = this.Context.Channel.GetMessagesAsync(limit: limit);
            await foreach (var m in cashedMessages)
            {
                foreach (var q in m)
                {
                    await q.DeleteAsync();
                    await Task.Delay(700);
                }
            }
        }

        [Command("help")]
        [Alias("помощь")]
        [Summary("Выводит список всех команд")]
        public async Task Help()
        {
            List<CommandInfo> commands = (this.Context.Client as MainClient).Commands.Commands.ToList();
            var embedBuilder = new EmbedBuilder();
            embedBuilder.WithTitle("Commands");

            foreach (CommandInfo command in commands)
            {
                // Get the command Summary attribute information
                string embedFieldText = command.Summary ?? "No description available\n";

                var par = string.Join(
                    ", ",
                    GetParams(command.Parameters)
                    );
                var modules = string.Join(' ', GetModulesInfo(command));
                embedBuilder.AddField(modules + " " + command.Name, embedFieldText + 
                                                                    "\nАргументы:" + par);
            }

            await ReplyAsync("Список команд, их описание и примеры использования: ", false, embedBuilder.Build());
        }

        private IEnumerable<string> GetModulesInfo(CommandInfo command)
        {
            var currentModule = command.Module;
            if (currentModule.Name != string.Empty)
            {
                yield return currentModule.Name;
            }
            while (currentModule.IsSubmodule)
            {
                currentModule = currentModule.Parent;
                if (currentModule.Name != string.Empty)
                {
                    yield return currentModule.Name;
                }
            }
        }

        private IEnumerable<string> GetParams(IReadOnlyList<ParameterInfo> parameters)
        {
            foreach (var param in parameters)
            {
                var attr = Attribute.GetCustomAttributes(param.Type, typeof(NamedArgumentTypeAttribute));
                if (attr.Length == 0)
                {
                    yield return $"{param.Name}";
                }
                else
                {
                    var props = param.Type.GetProperties();
                    foreach (var property in props)
                    {
                        if (property.GetSetMethod() != null)
                        {
                            yield return $"{property.Name}";
                        }
                    }
                }
            }
        }
    }
}
