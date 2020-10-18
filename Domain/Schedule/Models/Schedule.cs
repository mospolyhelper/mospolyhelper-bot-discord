using System;
using System.Collections.Generic;
using System.Linq;

namespace Mospolyhelper.Domain.Schedule.Models
{
    public class Schedule
    {
        public static Schedule From(IList<IList<Lesson>> dailySchedules)
        {
            var dateFrom = DateTime.MinValue;
            var dateTo = DateTime.MaxValue;

            foreach (var dailySchedule in dailySchedules)
            {
                foreach (var lesson in dailySchedule)
                {
                    if (dateFrom < lesson.DateFrom)
                    {
                        dateFrom = lesson.DateFrom;
                    }

                    if (dateTo > lesson.DateTo)
                    {
                        dateTo = lesson.DateTo;
                    }
                }
            }

            return new Schedule(
                dailySchedules,
                dateFrom,
                dateTo
            );
        }


        public IList<IList<Lesson>> DailySchedules { get; }
        public DateTime DateFrom { get; }
        public DateTime DateTo { get; }

        public Schedule(IList<IList<Lesson>> dailySchedules, DateTime dateFrom, DateTime dateTo)
        {
            this.DailySchedules = dailySchedules;
            this.DateFrom = dateFrom;
            this.DateTo = dateTo;
        }

        public IList<Lesson> GetSchedule(
            DateTime date,
            bool showEnded = false,
            bool showCurrent = true,
            bool showNotStarted = false
        ) => ScheduleExt.FilterByDate(
            DailySchedules[(int) date.DayOfWeek % 7],
            date,
            showEnded,
            showCurrent,
            showNotStarted
        );
    }

    static class ScheduleExt
    {
        public static Schedule Combine(Schedule? schedule1, Schedule? schedule2)
        {
            if (schedule1 == null) return schedule2;
            if (schedule2 == null) return schedule1;
            if (schedule1 == schedule2) return schedule1;


            var resList = schedule1.DailySchedules
                .Zip(
                    schedule2.DailySchedules,
                    (day1, day2) =>
                    {
                        var newList = new List<Lesson>(day1.Count + day2.Count);
                        newList.AddRange(day1);
                        newList.AddRange(day2);
                        newList.Sort();
                        return newList as IList<Lesson>;
                    }
                )
                .ToList();
            var dateFrom = schedule1.DateFrom < schedule2.DateFrom
                ? schedule1.DateFrom
                : schedule2.DateFrom;
            var dateTo = schedule1.DateTo < schedule2.DateTo
                ? schedule1.DateTo
                : schedule2.DateTo;

            return new Schedule(resList, dateFrom, dateTo);
        }

        public static List<Lesson> FilterByDate(
            IList<Lesson> dailySchedule,
            DateTime date,
            bool showEnded,
            bool showCurrent,
            bool showNotStarted
        )
        {
            return dailySchedule.Where(it =>
            {
                if (showEnded && showCurrent && showNotStarted) return true;
                if (!showEnded && !showCurrent && !showNotStarted) return true;

                if (showEnded && !showCurrent && !showNotStarted) return date > it.DateTo;
                if (showEnded && showCurrent && !showNotStarted) return date >= it.DateFrom;

                if (!showEnded && !showCurrent && showNotStarted) return date < it.DateFrom;
                if (!showEnded && showCurrent && showNotStarted) return date <= it.DateTo;

                if (!showEnded && showCurrent && !showNotStarted)
                    return date >= it.DateFrom && date <= it.DateTo;

                if (showEnded && !showCurrent && showNotStarted)
                    return date < it.DateFrom || date > it.DateTo;

                return true;
            }).ToList();
        }

        public static Schedule Filter(
            this IEnumerable<Schedule> schedules,
            IEnumerable<string>? titles = null,
            IEnumerable<string>? types = null,
            IEnumerable<string>? auditoriums = null,
            IEnumerable<string>? teachers = null,
            IEnumerable<string>? groups = null
        )
        {
            var filterTitles = titles == null;
            var filterTypes = types == null;
            var filterAuditoriums = auditoriums == null;
            var filterTeachers = teachers == null;
            var filterGroups = groups == null;

            IList<IList<Lesson>> tempList = new IList<Lesson>[]
            {
                new List<Lesson>(), new List<Lesson>(), new List<Lesson>(),
                new List<Lesson>(), new List<Lesson>(), new List<Lesson>(), new List<Lesson>(),
            };


            foreach (var schedule in schedules)
            {
                for (var i = 0; i < schedule.DailySchedules.Count; i++)
                {
                    (tempList[i] as List<Lesson>)!.AddRange(
                        schedule.DailySchedules[i].Where(
                            lesson =>
                                (filterTitles ||
                                 CheckFilter(titles!, lesson.Title)) &&
                                (filterTypes ||
                                 CheckFilter(types!, lesson.Type)) &&
                                (filterTeachers ||
                                 CheckFilter(teachers!, lesson.Teachers.Select(it => it.FullName))) &&
                                (filterGroups ||
                                 CheckFilter(groups!, lesson.Groups.Select(it => it.Title))) &&
                                (filterAuditoriums ||
                                 CheckFilter(auditoriums!, lesson.Auditoriums.Select(it => it.Title)))

                        )
                    );
                }
            }

            foreach (var lessons in tempList)
            {
                (lessons as List<Lesson>)!.Sort();
            }

            IList<IList<Lesson>> tempListNew = new IList<Lesson>[]
            {
                new List<Lesson>(), new List<Lesson>(), new List<Lesson>(),
                new List<Lesson>(), new List<Lesson>(), new List<Lesson>(), new List<Lesson>(),
            };

            for (var i = 0; i < tempList.Count; i++)
            {
                var day = tempList[i];
                var dayNew = tempListNew[i];
                foreach (var lesson in day)
                {
                    var index = -1;
                    for (var j = 0; j < dayNew.Count; j++)
                    {
                        if (isEqualForGroups(lesson, dayNew[j]))
                        {
                            index = j;
                            break;
                        }
                    }
                    if (index == -1)
                    {
                        dayNew.Add(lesson);
                    }
                    else
                    {
                        var lessonEqualForGroups = dayNew[index];
                        var newGroups = lessonEqualForGroups.Groups.ToList();
                        newGroups.AddRange(lesson.Groups);
                        dayNew[index] = new Lesson(
                            lesson.Order,
                            lesson.Title,
                            lesson.Type,
                            lesson.Teachers,
                            lesson.Auditoriums,
                            newGroups,
                            lesson.DateFrom,
                            lesson.DateTo
                        );
                    }
                }
            }

            return Schedule.From(tempListNew);
        }

        private static bool isEqualForGroups(Lesson l1, Lesson l2)
        {
            return l1.Order == l2.Order &&
                   l1.Title == l2.Title &&
                   l1.Auditoriums.SequenceEqual(l2.Auditoriums) &&
                   l1.Teachers.SequenceEqual(l2.Teachers) &&
                   l1.DateFrom == l2.DateFrom &&
                   l1.DateTo == l2.DateTo;
        }

    public static Schedule Filter(
            this Schedule schedule,
            IEnumerable<string>? titles = null,
            IEnumerable<string>? types = null,
            IEnumerable<string>? auditoriums = null,
            IEnumerable<string>? teachers = null,
            IEnumerable<string>? groups = null
        )
        {
            var filterTitles = titles == null;
            var filterTypes = types == null;
            var filterAuditoriums = auditoriums == null;
            var filterTeachers = teachers == null;
            var filterGroups = groups == null;

            IList<IList<Lesson>> tempList = new IList<Lesson>[]
            {
                new List<Lesson>(), new List<Lesson>(), new List<Lesson>(),
                new List<Lesson>(), new List<Lesson>(), new List<Lesson>(), new List<Lesson>(),
            };


            for (var i = 0; i < schedule.DailySchedules.Count; i++)
            {
                (tempList[i] as List<Lesson>)!.AddRange(
                    schedule.DailySchedules[i].Where(
                        lesson =>
                            (filterTitles || 
                             CheckFilter(titles!, lesson.Title)) &&
                            (filterTypes || 
                             CheckFilter(types!, lesson.Type)) &&
                            (filterTeachers || 
                             CheckFilter(teachers!, lesson.Teachers.Select(it => it.FullName))) &&
                            (filterGroups || 
                             CheckFilter(groups!, lesson.Groups.Select(it => it.Title))) &&
                            (filterAuditoriums || 
                             CheckFilter(auditoriums!, lesson.Auditoriums.Select(it => it.Title)))

                    )
                );
            }

            return Schedule.From(tempList);
        }

        private static bool CheckFilter(IEnumerable<string> filterList, string value)
        {
            using var iterator = filterList.GetEnumerator();
            if (iterator.MoveNext())
            {
                do
                {
                    if (iterator.Current == value) return true;
                } while (iterator.MoveNext());

                return false;
            }
            else
            {
                return true;
            }
        }

        private static bool CheckFilter(IEnumerable<string> filterList, IEnumerable<string> values)
        {
            using var filterIterator = filterList.GetEnumerator();
            // if filters are not empty
            if (filterIterator.MoveNext())
            {
                do
                {
                    using var valueIterator = values.GetEnumerator();
                    // return if empty
                    if (!valueIterator.MoveNext())
                        return false;
                    do
                    {
                        if (valueIterator.Current == filterIterator.Current)
                        {
                            return true;
                        }
                    } while (valueIterator.MoveNext());
                } while (filterIterator.MoveNext());

                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
