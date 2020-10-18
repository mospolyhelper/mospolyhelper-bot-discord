using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mospolyhelper.Data.Schedule.Api;
using Mospolyhelper.Data.Schedule.Converters;

namespace Mospolyhelper.Data.Schedule.Remote
{
    class ScheduleRemoteDataSource
    {
        private readonly ScheduleClient client;
        private readonly ScheduleRemoteConverter converter;

        public ScheduleRemoteDataSource(ScheduleClient client, ScheduleRemoteConverter converter)
        {
            this.client = client;
            this.converter = converter;
        }

        public async Task<Domain.Schedule.Models.Schedule?> Get(string groupTitle, bool isSession)
        {
            try
            {
                var scheduleString = await client.GetSchedule(groupTitle, isSession);
                return converter.Parse(scheduleString);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }

        public async Task<IList<Domain.Schedule.Models.Schedule>> GetAll(bool isSession)
        {
            try
            {
                var scheduleString = await client.GetAllSchedules(isSession);
                return converter.ParseSchedules(scheduleString);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Array.Empty<Domain.Schedule.Models.Schedule>();
            }
        }
    }
}
