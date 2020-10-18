using System;
using System.Collections.Generic;
using System.Text;
using Autofac;
using Mospolyhelper.Data.Schedule.Api;
using Mospolyhelper.Data.Schedule.Converters;
using Mospolyhelper.Data.Schedule.Remote;
using Mospolyhelper.Data.Schedule.Repository;
using Mospolyhelper.Features.Services.Schedule;

namespace Mospolyhelper.DI
{
    class ScheduleModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            var q = new ScheduleRepository(
                new ScheduleRemoteDataSource(
                    new ScheduleClient(),
                    new ScheduleRemoteConverter()
                )
            );
                
            builder
                .Register(c => new ScheduleNotificationService(q))
                .As<ScheduleNotificationService>()
                .SingleInstance();
        }
    }
}
