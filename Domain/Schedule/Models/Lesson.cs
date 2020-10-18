using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Mospolyhelper.Domain.Schedule.Models
{
    public class Lesson : IComparable<Lesson>
    {
        public int Order { get; }
        public string Title { get; }
        public string Type { get; }
        public IList<Teacher> Teachers { get; }
        public IList<Auditorium> Auditoriums { get; }
        public IList<Group> Groups { get; }
        public DateTime DateFrom { get; }
        public DateTime DateTo { get; }

        public (string, string) Time => GetTimeString(Order, (Groups.FirstOrDefault() ?? Group.Empty).Evening);
        public bool GroupIsEvening => (Groups.FirstOrDefault() ?? Group.Empty).Evening;
        public bool Important => LessonExt.IsImportant(Type);

        public Lesson(
            int order,
            string title,
            string type,
            IList<Teacher> teachers,
            IList<Auditorium> auditoriums,
            IList<Group> groups,
            DateTime dateFrom,
            DateTime dateTo
        )
        {
            this.Order = order;
            this.Title = title;
            this.Type = type;
            this.Teachers = teachers;
            this.Auditoriums = auditoriums;
            this.Groups = groups;
            this.DateFrom = dateFrom;
            this.DateTo = dateTo;
        }

        public static (string, string) GetTimeString(int order, bool groupIsEvening) => order switch
        {
            0 => LessonTimes.firstPairStr,
            1 => LessonTimes.secondPairStr,
            2 => LessonTimes.thirdPairStr,
            3 => LessonTimes.fourthPairStr,
            4 => LessonTimes.fifthPairStr,
            5 => groupIsEvening
                ? LessonTimes.sixthPairEveningStr
                : LessonTimes.sixthPairStr,
            6 => groupIsEvening
                ? LessonTimes.seventhPairEveningStr
                : LessonTimes.seventhPairStr,
            _ => ("Ошибка", "номера занятия")
        };

        public static (TimeSpan, TimeSpan) GetTime(int order, bool groupIsEvening) => order switch
        {
            0 => LessonTimes.firstPair,
            1 => LessonTimes.secondPair,
            2 => LessonTimes.thirdPair,
            3 => LessonTimes.fourthPair,
            4 => LessonTimes.fifthPair,
            5 => groupIsEvening
                ? LessonTimes.sixthPairEvening
                : LessonTimes.sixthPair,
            6 => groupIsEvening
                ? LessonTimes.seventhPairEvening
                : LessonTimes.seventhPair,
            _ => (new TimeSpan(0, 0, 0, 0), new TimeSpan(0, 0, 0, 0))
        };

        public static CurrentLesson GetOrder(DateTime dateTime, bool groupIsEvening)
        {
            var dateTimeMoscow = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(
                dateTime,
                TimeZoneInfo.Local.Id, 
                "Russian Standard Time"
                );
            var time = new TimeSpan(dateTimeMoscow.Hour, dateTimeMoscow.Minute, dateTimeMoscow.Second);
            if (time > LessonTimes.thirdPair.Item2)
            {
                if (time <= LessonTimes.fourthPair.Item2)
                    return new CurrentLesson(3, time >= LessonTimes.fourthPair.Item1, groupIsEvening);
                if (time <= LessonTimes.fifthPair.Item2)
                    return new CurrentLesson(4, time >= LessonTimes.fifthPair.Item1, groupIsEvening);
                if (groupIsEvening)
                {
                    if (time <= LessonTimes.sixthPairEvening.Item2)
                        return new CurrentLesson(5, time >= LessonTimes.sixthPairEvening.Item1, groupIsEvening);
                    if (time <= LessonTimes.seventhPairEvening.Item2)
                        return new CurrentLesson(6, time >= LessonTimes.seventhPairEvening.Item1, groupIsEvening);
                    return new CurrentLesson(7, false, groupIsEvening);
                }
                else
                {
                    if (time <= LessonTimes.sixthPair.Item2)
                        return new CurrentLesson(5, time >= LessonTimes.sixthPair.Item1, groupIsEvening);
                    if (time <= LessonTimes.seventhPair.Item2)
                        return new CurrentLesson(6, time >= LessonTimes.seventhPair.Item1, groupIsEvening);
                    return new CurrentLesson(7, false, groupIsEvening);
                }
            }
            else
            {
                if (time > LessonTimes.secondPair.Item2)
                    return new CurrentLesson(2, time >= LessonTimes.thirdPair.Item1, groupIsEvening);
                if (time > LessonTimes.firstPair.Item2)
                    return new CurrentLesson(1, time >= LessonTimes.secondPair.Item1, groupIsEvening);
                return new CurrentLesson(0, time >= LessonTimes.firstPair.Item1, groupIsEvening);
            }
        }


        private class LessonTimes
        {
            private static (TimeSpan, TimeSpan) GetTime(int h1, int m1, int h2, int m2)
            {
                return (
                    new TimeSpan(h1, m1, 0),
                    new TimeSpan(h2, m2, 0)
                );
            }


            public static (TimeSpan, TimeSpan) firstPair = GetTime(9, 0, 10, 30);
            public static (TimeSpan, TimeSpan) secondPair = GetTime(10, 40, 12, 10);
            public static (TimeSpan, TimeSpan) thirdPair = GetTime(12, 20, 13, 50);
            public static (TimeSpan, TimeSpan) fourthPair = GetTime(14, 30, 16, 0);
            public static (TimeSpan, TimeSpan) fifthPair = GetTime(16, 10, 17, 40);
            public static (TimeSpan, TimeSpan) sixthPair = GetTime(17, 50, 19, 20);
            public static (TimeSpan, TimeSpan) sixthPairEvening = GetTime(18, 20, 19, 40);
            public static (TimeSpan, TimeSpan) seventhPair = GetTime(19, 30, 21, 0);
            public static (TimeSpan, TimeSpan) seventhPairEvening = GetTime(19, 50, 21, 10);

            public static readonly (string, string) firstPairStr = ("9:00", "10:30");
            public static (string, string) secondPairStr = ("10:40", "12:10");
            public static (string, string) thirdPairStr = ("12:20", "13:50");
            public static (string, string) fourthPairStr = ("14:30", "16:00");
            public static (string, string) fifthPairStr = ("16:10", "17:40");
            public static (string, string) sixthPairStr = ("17:50", "19:20");
            public static (string, string) sixthPairEveningStr = ("18:20", "19:40");
            public static (string, string) seventhPairStr = ("19:30", "21:00");
            public static (string, string) seventhPairEveningStr = ("19:50", "21:10");
        }

        public class CurrentLesson
        {
            private const int ORDER_LESSONS_FINISHED = 7;

            public int Order { get; }
            public bool Started { get; }
            public bool Evening { get; }

            private bool Finished => Order == ORDER_LESSONS_FINISHED;

            public CurrentLesson(int order, bool started, bool evening)
            {
                this.Order = order;
                this.Started = started;
                this.Evening = evening;
            }

            public override bool Equals(object? obj) => false;

            public override int GetHashCode()
            {
                var result = Order;
                result = 31 * result + Started.GetHashCode();
                result = 31 * result + Evening.GetHashCode();
                return result;
            }
        }

        public int CompareTo(Lesson other)
        {
            if (Order != other.Order) return Order.CompareTo(other.Order);
            if (GroupIsEvening != other.GroupIsEvening) return GroupIsEvening ? 1 : -1;
            var g1 = string.Join(' ', Groups);
            var g2 = string.Join(' ', other.Groups);
            var groupComparing = g1.CompareTo(g2);
            if (groupComparing != 0) return groupComparing;
            if (DateFrom != other.DateFrom) return DateFrom.CompareTo(other.DateFrom);
            return DateTo.CompareTo(other.DateTo);
        }

    }

    class LessonExt
    {
        private const string COURSE_PROJECT_SHORT = "КП";
        private const string EXAM_SHORT = "Экз";
        private const string CREDIT_SHORT = "Зач";
        private const string CREDIT_WITH_MARK_SHORT = "ЗСО";
        private const string EXAMINATION_SHOW_SHORT = "ЭП";
        private const string CONSULTATION_SHORT = "Кон";
        private const string LABORATORY_SHORT = "Лаб";
        private const string PRACTICE_SHORT2 = "Пра";
        private const string LECTURE_SHORT = "Лек";
        private const string OTHER_SHORT = "Дру";

        private const string COURSE_PROJECT = "КП";
        private const string EXAM = "Экзамен";
        private const string CREDIT = "Зачет";
        private const string CREDIT_WITH_MARK = "ЗСО";
        private const string EXAMINATION_SHOW = "ЭП";
        private const string CONSULTATION = "Консультация";
        private const string LABORATORY = "Лаб";
        private const string LABORATORY_FULL = "Лаб. работа";
        private const string PRACTICE = "Практика";
        private const string PRACTICE_SHORT = "Пр";
        private const string LECTURE = "Лекция";

        private const string OTHER = "Другое";
        // TODO Установочная лекция

        private const string COURSE_PROJECT_FIXED = "Курсовой проект";
        private const string CREDIT_WITH_MARK_FIXED = "Зачет с оценкой";
        private const string EXAMINATION_SHOW_FIXED = "Экз. показ";
        private const string LECTURE_PRACTICE_LABORATORY = "Лекц., практ., лаб.";
        private const string LECTURE_PRACTICE = "Лекц. и практ.";
        private const string LECTURE_LABORATORY = "Лекц. и лаб.";
        private const string PRACTICE_LABORATORY = "Практ. и лаб.";

        private static readonly string[] importantTypes = {
            EXAM,
            CREDIT,
            COURSE_PROJECT_FIXED,
            CREDIT_WITH_MARK_FIXED,
            EXAMINATION_SHOW_FIXED,
            COURSE_PROJECT,
            CREDIT_WITH_MARK,
            EXAMINATION_SHOW
        };

        public static bool IsImportant(string type) =>
            importantTypes.Any(it => 
                    type.Contains(it, StringComparison.InvariantCultureIgnoreCase) 
                    || it.Contains(type, StringComparison.InvariantCultureIgnoreCase)
                    );


        private static readonly Regex regex = new Regex("\\(.*?\\)");
        public static string FixType(string type, string lessonTitle)
        {
            if (string.Equals(type, COURSE_PROJECT, StringComparison.InvariantCultureIgnoreCase))
            {
                return COURSE_PROJECT_FIXED;
            }
            if (string.Equals(type, CREDIT_WITH_MARK, StringComparison.InvariantCultureIgnoreCase))
            {
                return CREDIT_WITH_MARK_FIXED;
            }
            if (string.Equals(type, EXAMINATION_SHOW, StringComparison.InvariantCultureIgnoreCase))
            {
                return EXAMINATION_SHOW_FIXED;
            }
            if (string.Equals(type, OTHER, StringComparison.InvariantCultureIgnoreCase))
            {
                var res = string.Join(' ', regex.Matches(lessonTitle).Select(it => it.Value));
                
                if (res != string.Empty)
                {
                    return FindCombinedShortTypeOrNull(res) ?? type;
                }
                else
                {
                    return type;
                }
            }

            return type;
        }

        private static string? FindCombinedShortTypeOrNull(string type)
        {
            var lecture = type.Contains(LECTURE_SHORT, StringComparison.InvariantCultureIgnoreCase);
            var practice = type.Contains(PRACTICE_SHORT, StringComparison.InvariantCultureIgnoreCase);
            var lab = type.Contains(LABORATORY, StringComparison.InvariantCultureIgnoreCase);
            if (lecture && practice && lab) 
                return LECTURE_PRACTICE_LABORATORY;
            if (lecture && practice)
                return LECTURE_PRACTICE;
            if (lecture && lab)
                return LECTURE_LABORATORY;
            if (practice && lab)
                return PRACTICE_LABORATORY;
            if (lecture)
                return LECTURE;
            if (practice)
                return PRACTICE;
            if (lab)
                return LABORATORY;
            return null;
        }
    }
}
