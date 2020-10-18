using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Mospolyhelper.Domain.Schedule.Models;

namespace Mospolyhelper.Data.Schedule.Converters
{
    class ScheduleRemoteConverter
    {
        #region Constants

        private const string StatusKey = "status";
        private const string StatusOk = "ok";
        private const string StatusError = "error";
        private const string MessageKey = "message";
        private const string IsSession = "isSession";
        private const string GroupKey = "group";
        private const string ScheduleGridKey = "grid";

        private const string GroupTitleKey = "title";
        private const string GroupDateFromKey = "dateFrom";
        private const string GroupDateToKey = "dateTo";
        private const string GroupEveningKey = "evening";
        private const string GroupCommentKey = "comment";
        private const string GroupCourseKey = "course";

        private const string LessonTitleKey = "sbj";
        private const string LessonTeacherKey = "teacher";
        private const string LessonDateFromKey = "df";
        private const string LessonDateToKey = "dt";
        private const string LessonAuditoriumsKey = "auditories";
        private const string LessonTypeKey = "type";
        private const string LessonWebinarLinkKey = "wl";
        private const string LessonWeekKey = "week";

        private const string FirstModuleKey = "fm";
        private const string SecondModuleKey = "sm";
        private const string NoModuleKey = "no";

        private const string AuditoriumTitleKey = "title";
        private const string AuditoriumColorKey = "color";

        private const string DateFormat = "yyyy-MM-dd";

        #endregion

        public IList<Domain.Schedule.Models.Schedule> ParseSchedules(string schedulesString)
        {
            using var json = JsonDocument.Parse(schedulesString);
            var root = json.RootElement;
            var contents = root.GetProperty("contents");
            var schedules = new List<Domain.Schedule.Models.Schedule>();
            foreach (var scheduleJson in contents.EnumerateArray())
            {
                schedules.Add(ParseSchedule(scheduleJson));
            }
            return schedules;
        }


        public Domain.Schedule.Models.Schedule Parse(string scheduleString)
        {
            using var json = JsonDocument.Parse(scheduleString);
            var root = json.RootElement;

            if (!root.TryGetProperty(StatusKey, out var statusElement))
            {
                Console.WriteLine("Schedule does not have status");
            }

            var status = statusElement.GetString();

            var message = string.Empty;
            if (root.TryGetProperty(MessageKey, out var messageElement))
            {
                message = messageElement.GetString();
            }

            if (status == StatusError)
            {
                throw new JsonException(
                    "Schedule was returned with error status. " +
                    $"Message: \"{message}\""
                );
            }
            else if (status != StatusOk)
            {
                Console.WriteLine($"Schedule does not have status \"{StatusOk}\" both \"{StatusError}\". " +
                                  $"Message: \"{message}\"");
            }


            return ParseSchedule(root);
        }

        private Domain.Schedule.Models.Schedule ParseSchedule(JsonElement json)
        {
            var isByDate = json.GetProperty(IsSession).GetBoolean();
            var group = json.TryGetProperty(GroupKey, out var groupElement) ? ParseGroup(groupElement) : Group.Empty;
            var dailySchedules = ParseDailySchedules(
                json.GetProperty(ScheduleGridKey),
                group,
                isByDate
            );


            return Domain.Schedule.Models.Schedule.From(dailySchedules);
        }

        private Group ParseGroup(JsonElement json)
        {
            string title;
            if (json.TryGetProperty(GroupTitleKey, out var titleElement))
            {
                title = titleElement.GetString();
            }
            else
            {
                title = string.Empty;
                Console.WriteLine($"Group title key {GroupTitleKey} not found");
            }

            bool isEvening;
            if (json.TryGetProperty(GroupEveningKey, out var eveningElement))
            {
                isEvening = eveningElement.GetInt32() != 0;
            }
            else
            {
                isEvening = false;
                Console.WriteLine($"Group title key {GroupTitleKey} not found");
            }

            return new Group(title, isEvening);
        }

        //private DateTime ParseGroupDateFrom(string json)
        //{
        //    throw new NotImplementedException();
        //}

        //private DateTime ParseGroupDateTo(string json)
        //{
        //    throw new NotImplementedException();
        //}

        private IList<IList<Lesson>> ParseDailySchedules(
            JsonElement json,
            Group group,
            bool isByDate
        )
        {
            IList<IList<Lesson>> tempList = new IList<Lesson>[]
            {
                new List<Lesson>(), new List<Lesson>(), new List<Lesson>(),
                new List<Lesson>(), new List<Lesson>(), new List<Lesson>(), new List<Lesson>(),
            };

            foreach (var dayToDailySchedule in json.EnumerateObject())
            {
                var day = dayToDailySchedule.Name;
                var dailySchedule = dayToDailySchedule.Value;

                if (!dailySchedule.EnumerateObject().MoveNext()) continue;

                int parsedDay;
                var date = DateTime.MinValue;

                if (isByDate)
                {
                    try
                    {
                        date = DateTime.ParseExact(day, DateFormat, CultureInfo.InvariantCulture);
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    parsedDay = (int) date.DayOfWeek;
                }
                else
                {
                    if (!int.TryParse(day, out parsedDay))
                    {
                        continue;
                    }
                }

                // DayOfWeek 1..7
                if (parsedDay < 1 || parsedDay > 7)
                {
                    continue;
                }

                parsedDay %= 7;

                foreach (var indexToLessonPlace in dailySchedule.EnumerateObject())
                {
                    var index = indexToLessonPlace.Name;
                    var lessonPlace = indexToLessonPlace.Value;

                    if (lessonPlace.GetArrayLength() == 0) continue;

                    var parsedOrder = int.Parse(index) - 1;
                    foreach (var lesson in lessonPlace.EnumerateArray())
                    {
                        var parsedLesson = ParseLesson(lesson, parsedOrder, group, isByDate, date);
                        tempList[parsedDay].Add(parsedLesson);
                    }

                    (tempList[parsedDay] as List<Lesson>)!.Sort();
                }
            }

            return tempList;
        }

        private Lesson ParseLesson(
            JsonElement json,
            int order,
            Group group,
            bool isByDate,
            DateTime date
        )
        {
            string title = json.TryGetProperty(LessonTitleKey, out var titleElement)
                ? titleElement.GetString()
                : "Не найден ключ названия занятия. " +
                  $"Возможно, структура расписания была обновлена: {json}";

            var teachers = json.TryGetProperty(LessonTeacherKey, out var teachersElement)
                ? ParseTeachers(teachersElement.GetString())
                : Array.Empty<Teacher>();

            DateTime dateFrom;
            DateTime dateTo;

            if (isByDate)
            {
                dateFrom = date;
                dateTo = date;
            }
            else
            {
                dateFrom = json.TryGetProperty(LessonDateFromKey, out var dateFromElement) 
                    ? ParseDateFrom(dateFromElement.GetString()) 
                    : DateTime.MinValue;

                dateTo = json.TryGetProperty(LessonDateToKey, out var dateToElement)
                    ? ParseDateTo(dateToElement.GetString())
                    : DateTime.MaxValue;
            }

            if (dateTo < dateFrom)
            {
                var buf = dateTo;
                dateTo = dateFrom;
                dateFrom = buf;
            }

            var auditoriums = json.TryGetProperty(LessonAuditoriumsKey, out var auditoriumsElement)
                ? ParseAuditoriums(auditoriumsElement)
                : Array.Empty<Auditorium>();

            string type;
            if (json.TryGetProperty(LessonTypeKey, out var typeElement))
            {
                type = typeElement.GetString();
            }
            else
            {
                type = string.Empty;
                Console.WriteLine($"Lesson type key {LessonTypeKey} not found");
            }

            return new Lesson(
                order,
                title,
                LessonExt.FixType(type, title),
                teachers,
                auditoriums,
                new[] {group},
                dateFrom,
                dateTo
            );
        }

        private DateTime ParseDateFrom(string json)
        {
            try
            {
                return DateTime.ParseExact(json, DateFormat, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return DateTime.MinValue;
            }
        }

        private DateTime ParseDateTo(string json)
        {
            try
            {
                return DateTime.ParseExact(json, DateFormat, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return DateTime.MaxValue;
            }
        }

        private IList<Teacher> ParseTeachers(string json)
        {
            return json.Trim()
                .Split(',')
                .Where(it => it != string.Empty)
                .Select(Teacher.FromFullName)
                .ToArray();

        }

        private IList<Auditorium> ParseAuditoriums(JsonElement json)
        {
            var tempList = new List<Auditorium>();
            foreach (var auditorium in json.EnumerateArray())
            {
                if (!auditorium.TryGetProperty(AuditoriumTitleKey, out var titleElement))
                {
                    continue;
                }

                var name = titleElement.GetString().Trim();
                //name = Auditorium.parseEmoji(name)

                var color = auditorium.TryGetProperty(AuditoriumColorKey, out var colorElement)
                    ? colorElement.GetString()
                    : string.Empty;


                tempList.Add(new Auditorium(
                    AuditoriumExt.ReplaceEmojiByText(name),
                    color
                ));
            }

            return tempList;
        }
    }
}
