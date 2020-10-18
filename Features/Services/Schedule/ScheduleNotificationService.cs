using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Mospolyhelper.Data.Schedule.Api;
using Mospolyhelper.Data.Schedule.Converters;
using Mospolyhelper.Data.Schedule.Remote;
using Mospolyhelper.Data.Schedule.Repository;
using Mospolyhelper.Domain.Schedule.Models;
using Mospolyhelper.Features.Utils;

namespace Mospolyhelper.Features.Services.Schedule
{
    class ScheduleNotificationService
    {
        private Domain.Schedule.Models.Schedule? schedule = null;
        private object key = new object();
        private IEnumerable<IRole> groups = Array.Empty<IRole>();
        private CancellationTokenSource? cts = null;

        private readonly ScheduleRepository repository;

        public ScheduleNotificationService(ScheduleRepository repository)
        {
            this.repository = repository;
        }

        public async void Launch(IMessageChannel channel, SocketGuild guild)
        {
            if (cts != null && !cts.IsCancellationRequested)
            {
                return;
            }

            await channel.SendMessageAsync("Служба оповещения включена");

            cts = new CancellationTokenSource();
            var regex = new Regex("\\d\\d.-...");
            groups = guild.Roles
                .Where(it => regex.IsMatch(it.Name));
            var sch = (await repository.GetAllSchedules())?.Filter(groups: this.groups.Select(it => it.Name));
            lock (key)
            {
                this.schedule = sch;
            }
            if (schedule == null) return;
            ScheduleUpdater();

            var msgFromId = 0UL;
            var msgToId = 0UL;
            var limit = 0;
            while (true)
            {
                var currentTime = GetMoscowDateTime(DateTime.Now);
                Console.WriteLine("Сервис оповещения о расписании: " + currentTime.ToString());
                var shiftedTime = currentTime.AddMinutes(10); // Get lesson 10 min before start
                var currentOrder = Lesson.GetOrder(shiftedTime, false);
                var currentOrderEvening = Lesson.GetOrder(shiftedTime, true);

                var realIsStarted = Lesson.GetTime(currentOrder.Order, currentOrder.Evening).Item1 >=
                                    currentTime.TimeOfDay;
                var realIsStartedEvening = Lesson.GetTime(
                    currentOrderEvening.Order, currentOrderEvening.Evening
                    ).Item1 >= currentTime.TimeOfDay;



                var filteredLessons = schedule.GetSchedule(shiftedTime);
                filteredLessons = filteredLessons.Where(it =>
                    !it.GroupIsEvening && currentOrder.Started && realIsStarted && it.Order == currentOrder.Order
                    || it.GroupIsEvening && currentOrderEvening.Started && realIsStartedEvening && it.Order == currentOrderEvening.Order
                ).ToList();

                if (msgFromId != 0UL && msgToId != 0UL)
                {
                    var flag = false;
                    var collections = channel.GetMessagesAsync(
                        msgToId, Direction.Before, limit: limit
                        );
                    await foreach (var col in collections)
                    {
                        foreach (var message in col)
                        {
                            if (message.Author.IsBot)
                            {
                                await message.DeleteAsync();
                                await Task.Delay(700);
                            }
                            if (message.Id == msgFromId)
                            {
                                flag = true;
                                break;
                            }
                        }

                        if (flag)
                        {
                            break;
                        }
                    }
                    await (await channel.GetMessageAsync(msgToId)).DeleteAsync();
                }
                else if (msgFromId != 0UL)
                {
                    await (await channel.GetMessageAsync(msgFromId)).DeleteAsync();
                }
                limit = 0;
                if (filteredLessons.Count != 0)
                {
                    msgFromId = (await channel.SendMessageAsync("Занятия через 10 минут")).Id;
                    limit = filteredLessons.Count * 2 + 1;
                    foreach (var lesson in filteredLessons)
                    {
                        var mentions = string.Join(", ",
                            lesson.Groups
                                .Select(it =>
                                    {
                                        var q = groups.FirstOrDefault(role =>
                                            role.Name.Equals(it.Title)
                                        );
                                        return q?.Mention ?? string.Empty;
                                    }
                                )
                        );
                        await channel.SendMessageAsync(mentions);
                        msgToId = (await ScheduleMessageUtils.SendLesson(channel, lesson, DateTime.Now)).Id;
                        await Task.Delay(700);
                    }
                }
                else
                {
                    msgFromId = (await channel.SendMessageAsync("Через 10 минут занятий нет")).Id;
                    msgToId = 0UL;
                    Console.WriteLine($"Сейчас {currentOrder.Order + 1}-й занятие или " +
                                      $"{currentOrderEvening.Order + 1}-й занятие");
                }


                var timeToSleep = GetTimeToSleep(currentOrder, currentOrderEvening);

                Console.WriteLine("Следующее оповещение через " + timeToSleep.ToString());

                try
                {
                    await Task.Delay(timeToSleep, cts.Token);
                }
                catch (TaskCanceledException e)
                {
                    Console.WriteLine("Сервис оповещения о расписании остановлен: " + DateTime.Now.ToString());
                    await channel.SendMessageAsync("Служба оповещения остановлена");
                    return;
                }
            }
        }

        public void Stop()
        {
            if (cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
        }

        private DateTime GetMoscowDateTime(DateTime date)
        {
            return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                date,
                TimeZoneInfo.Local.Id,
                "Russian Standard Time"
            );
        }

        private TimeSpan GetTimeToSleep(Lesson.CurrentLesson currentOrder, Lesson.CurrentLesson currentOrderEvening)
        {
            var dateTimeMoscow = GetMoscowDateTime(DateTime.Now.AddMinutes(10));
            var now = dateTimeMoscow.TimeOfDay;
            if (currentOrder.Started && currentOrderEvening.Started && 
                currentOrder.Order == 6 && currentOrderEvening.Order == 6
                || (currentOrder.Order == 7 && currentOrderEvening.Order == 7)
                || (currentOrder.Started && currentOrder.Order == 6 && currentOrderEvening.Order == 7)
                || (currentOrderEvening.Started && currentOrderEvening.Order == 6 && currentOrder.Order == 7))
            {
                return new TimeSpan(1, 0, 0, 0) - now 
                       + Lesson.GetTime(0, false).Item1;
            }
            else if (currentOrder.Started && currentOrder.Order == 6 || currentOrder.Order == 7)
            {
                return Lesson.GetTime(
                    currentOrderEvening.Order + (currentOrderEvening.Started ? 1 : 0),
                    currentOrderEvening.Evening
                ).Item1 - now;
            }
            else if (currentOrderEvening.Started && currentOrderEvening.Order == 6 || currentOrder.Order == 7)
            {
                return Lesson.GetTime(
                    currentOrder.Order + (currentOrder.Started ? 1 : 0),
                    currentOrder.Evening
                ).Item1 - now;
            }
            var time1 = Lesson.GetTime(
                currentOrder.Order + (currentOrder.Started ? 1 : 0), 
                currentOrder.Evening
                );
            var time2 = Lesson.GetTime(
                currentOrderEvening.Order + (currentOrderEvening.Started ? 1 : 0), 
                currentOrderEvening.Evening
                );
            if (time1.Item1 < time2.Item1)
            {
                return time1.Item1 - now;
            }
            else
            {
                return time2.Item1 - now;
            }
        }

        private async void ScheduleUpdater()
        {
            await Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(TimeSpan.FromDays(1));
                    var sch = (await repository.GetAllSchedules()).Filter(groups: this.groups.Select(it => it.Name));
                    if (sch.DailySchedules.Count != 0)
                    {
                        Console.WriteLine("Расписание обновлено и присвоено");
                        lock (key)
                        {
                            schedule = sch;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Расписание не обновлено, так как оказалость пустым");
                    }
                }
            });
        }
    }
}
