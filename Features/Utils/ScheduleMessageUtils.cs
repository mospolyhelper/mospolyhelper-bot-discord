using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Html2Markdown;
using Mospolyhelper.Domain.Schedule.Models;

namespace Mospolyhelper.Features.Utils
{
    class ScheduleMessageUtils
    {
        private static Converter markdownConverter = new Converter();


        public static async Task<IUserMessage> SendLesson(IMessageChannel channel, Lesson lesson, DateTime date)
        {
            var lessonNotStarted = date < lesson.DateFrom;
            var lessonFinished = date > lesson.DateTo;
            var lessonOutDate = lessonNotStarted || lessonFinished;

            var format = "dd MMM";
            string dateString;
            if (lesson.DateFrom == lesson.DateTo)
            {
                dateString = lesson.DateFrom.ToString(format);
            }
            else
            {
                dateString = $"С {lesson.DateFrom.ToString(format)}" +
                             $" по {lesson.DateTo.ToString(format)}";
            }

            var teachers = "🎓  " + string.Join(
                ", ",
                lesson.Teachers.Select(it => it.FullName)
                );

            var auditoriums = "🏛️  " + string.Join(
                ", ",
                lesson.Auditoriums.Select(it => markdownConverter.Convert(it.Title))
                );

            var groups = "👥  " + string.Join(
                ", ",
                lesson.Groups.Select(it => it.Title)
                );

            var res = teachers + "\n" + auditoriums + "\n" + groups;

            var (timeStart, timeEnd) = lesson.Time;
            var time = $"{timeStart} - {timeEnd}, {lesson.Order + 1}-е занятие";
            if (lessonNotStarted)
            {
                dateString += ", ещё не началось";
            }

            if (lessonFinished)
            {
                dateString += ", уже закончилось";
            }

            var type = $"  ` {lesson.Type} `";
            var title = lesson.Title + type;

            var embed = new EmbedBuilder()
                .WithAuthor(time)
                .WithColor(lesson.Important ? Color.Red : lessonOutDate ? Color.DarkerGrey : Color.Blue)
                .WithTitle(title)
                .WithDescription(res)
                .WithFooter(dateString);

            return await channel.SendMessageAsync(embed: embed.Build());
        }
    }
}
