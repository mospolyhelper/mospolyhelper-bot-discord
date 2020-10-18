using System;
using System.Net;
using System.Threading.Tasks;

namespace Mospolyhelper.Data.Schedule.Api
{
    class ScheduleClient
    {
        private const string UrlBase = "https://rasp.dmami.ru";
        private const string UrlGetSchedule = UrlBase + "/site/group";
        private readonly string UrlGetAllSchedules = 
            UrlBase + Environment.GetEnvironmentVariable("URL_SCHEDULE_ALL");
        private readonly string UrlGetAllSchedulesSession = 
            UrlBase + Environment.GetEnvironmentVariable("URL_SCHEDULE_SESSION_ALL");

        public Task<string> GetSchedule(string groupTitle, bool isSession)
        {
            using var client = new WebClient
            {
                Headers = 
                {
                    [HttpRequestHeader.Referer] = UrlBase, 
                    ["X-Requested-With"] = "XMLHttpRequest"
                },
                QueryString = 
                {
                    ["group"] = groupTitle, 
                    ["session"] = isSession ? "1" : "0"
                }
            };
            return client.DownloadStringTaskAsync(UrlGetSchedule);
        }

        public Task<string> GetAllSchedules(bool isSession)
        {
            using var client = new WebClient
            {
                Headers =
                {
                    [HttpRequestHeader.Referer] = UrlBase,
                    ["X-Requested-With"] = "XMLHttpRequest"
                }
            };
            return client.DownloadStringTaskAsync(
                isSession ? UrlGetAllSchedulesSession : UrlGetAllSchedules
                );
        }
    }
}
