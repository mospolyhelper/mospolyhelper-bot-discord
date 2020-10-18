using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mospolyhelper.Data.Schedule.Remote;
using Mospolyhelper.Domain.Schedule.Models;

namespace Mospolyhelper.Data.Schedule.Repository
{
    class ScheduleRepository
    {
        private ScheduleRemoteDataSource remoteDataSource;

        public ScheduleRepository(ScheduleRemoteDataSource remoteDataSource)
        {
            this.remoteDataSource = remoteDataSource;
        }

        public async Task<Domain.Schedule.Models.Schedule?> GetSchedule(string groupTitle)
        {
            return ScheduleExt.Combine(
                await remoteDataSource.Get(groupTitle, false),
                await remoteDataSource.Get(groupTitle, true)
                );
        }

        public async Task<IEnumerable<Domain.Schedule.Models.Schedule>> GetAllSchedules()
        {
            return (await remoteDataSource.GetAll(false))
                .Union(await remoteDataSource.GetAll(true));
        }
    }
}
