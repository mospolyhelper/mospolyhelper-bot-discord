using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Mospolyhelper.Data.Schedule.Api;
using Mospolyhelper.Data.Schedule.Converters;
using Mospolyhelper.Data.Schedule.Remote;
using Mospolyhelper.Data.Schedule.Repository;
using Mospolyhelper.Domain.Schedule.Models;
using Mospolyhelper.Features.Services.Schedule;
using Mospolyhelper.Features.Utils;

namespace Mospolyhelper.Features.Commands.Schedule
{
    [Group("schedule")]
    [Alias("расписание")]
    public class ScheduleCommands : ModuleBase<SocketCommandContext>
    {
        [Group("notify")]
        [Alias("уведомление")]
        public class NotificationCommands : ModuleBase<SocketCommandContext>
        {
            [Command("start")]
            [Alias("начни")]
            [Summary("Запускает службу оповещения о занятиях")]
            public async Task StartNotification()
            {
                var q = Program.GetService<ScheduleNotificationService>();
                q.Launch(this.Context.Channel, Context.Guild);
            }

            [Command("stop")]
            [Summary("Останавливает службу оповещения о занятиях")]
            [Alias("стоп")]
            public async Task StopNotification()
            {
                var q = Program.GetService<ScheduleNotificationService>();
                q.Stop();
            }
        }


        [Command("today")]
        [Alias("сегодня")]
        [Summary("Выводит расписание на сегодня\n" +
                 "Пример 1: schedule today groups: \"181-721, 181-722\" " +
                 "teachers: \"Иван Иванов Иванович\" auditoriums: \"пр2303\" " +
                 "types: \"лекция, практика\" titles: \"Математика\"\n" +
                 "Пример 2: schedule today groups: \"181-721\"")]
        public async Task GetTodaySchedule(ScheduleFilterArguments args)
        {
            var repo = new ScheduleRepository(
                new ScheduleRemoteDataSource(
                    new ScheduleClient(),
                    new ScheduleRemoteConverter()
                )
            );
            var schedule = (await repo.GetAllSchedules())
                .Filter(
                    groups: new HashSet<string>(args.Groups),
                    teachers: new HashSet<string>(args.Teachers),
                    auditoriums: new HashSet<string>(args.Auditoriums),
                    titles: new HashSet<string>(args.Titles),
                    types: new HashSet<string>(args.Types)
                );
            SendTodaySchedule(this.Context.Channel, schedule);
        }

        [NamedArgumentType]
        public class ScheduleFilterArguments
        {
            public IEnumerable<string> Groups { get; set; } = Array.Empty<string>();
            public IEnumerable<string> Teachers { get; set; } = Array.Empty<string>();
            public IEnumerable<string> Auditoriums { get; set; } = Array.Empty<string>();
            public IEnumerable<string> Titles { get; set; } = Array.Empty<string>();
            public IEnumerable<string> Types { get; set; } = Array.Empty<string>();
        }

        [Command("week")]
        [Alias("неделя")]
        [Summary("Выводит расписание на неделю\n" +
                 "Пример 1: schedule week groups: \"181-721, 181-722\" " +
                 "teachers: \"Иван Иванов Иванович\" auditoriums: \"пр2303\" " +
                 "types: \"лекция, практика\" titles: \"Математика\"\n" +
                 "Пример 2: schedule week groups: \"181-721\"")]
        public async Task GetSchedule(ScheduleFilterArguments args)
        {
            var repo = new ScheduleRepository(
                new ScheduleRemoteDataSource(
                    new ScheduleClient(), 
                    new ScheduleRemoteConverter()
                    )
                );
 
            var schedule = (await repo.GetAllSchedules())
                .Filter(
                    groups: new HashSet<string>(args.Groups),
                    teachers: new HashSet<string>(args.Teachers),
                    auditoriums: new HashSet<string>(args.Auditoriums),
                    titles: new HashSet<string>(args.Titles),
                    types: new HashSet<string>(args.Types)
                    );
            SendSchedule(this.Context.Channel, schedule);
        }

        private async void SendSchedule(
            ISocketMessageChannel channel, 
            Domain.Schedule.Models.Schedule? schedule
            )
        {
            if (schedule == null)
            {
                await channel.SendMessageAsync("Расписание не найдено");
                return;
            }

            var date = DateTime.Today;
            var dayOfWeek = (int) date.DayOfWeek;
            if (dayOfWeek == (int) DayOfWeek.Sunday)
            {
                dayOfWeek = 6;
            }
            else
            {
                dayOfWeek--;
            }

            date = date.AddDays(-dayOfWeek);
            for (var i = 0; i < schedule.DailySchedules.Count; i++, date = date.AddDays(1))
            {
                var day = (i + 1) % schedule.DailySchedules.Count;
                await SendDate(channel, date);
                var dailySchedule = schedule.DailySchedules[day];
                if (dailySchedule.Count == 0)
                {
                    await SendEmptyDay(channel);
                }
                else
                {
                    foreach (var lesson in dailySchedule)
                    {
                        await ScheduleMessageUtils.SendLesson(channel, lesson, date);
                        await Task.Delay(600);
                    }
                }
            }
        }

        private async void SendTodaySchedule(
            ISocketMessageChannel channel,
            Domain.Schedule.Models.Schedule? schedule
        )
        {
            if (schedule == null)
            {
                await channel.SendMessageAsync("Расписание не найдено");
                return;
            }

            var date = DateTime.Today;
            await SendDate(channel, date);
            foreach (var lesson in schedule.GetSchedule(date))
            {
                await ScheduleMessageUtils.SendLesson(channel, lesson, DateTime.Now);
                await Task.Delay(650);
            }
        }

        private async Task SendDate(ISocketMessageChannel channel, DateTime date)
        {
            var dateStr = date.ToString("dddd, d MMMM");
            dateStr = char.ToUpperInvariant(dateStr.First()) + dateStr.Substring(1);

            var embed = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithTitle(dateStr);
            await channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task SendDayOfWeek(ISocketMessageChannel channel, DayOfWeek dayOfWeek)
        {
            var day = CultureInfo.CurrentCulture
                .DateTimeFormat.GetDayName(dayOfWeek);
            day = char.ToUpperInvariant(day.First()) + day.Substring(1);

            var embed = new EmbedBuilder()
                .WithColor(Color.Green)
                .WithTitle(day);
            await channel.SendMessageAsync(embed: embed.Build());
        }

        private async Task SendEmptyDay(ISocketMessageChannel channel)
        {
            var embed = new EmbedBuilder()
                .WithColor(Color.DarkerGrey)
                .WithTitle("Нет занятий");
            await channel.SendMessageAsync(embed: embed.Build());
        }
    }
}
