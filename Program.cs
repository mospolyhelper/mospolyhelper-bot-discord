using System;
using System.IO;
using System.Security.Permissions;
using System.Threading.Tasks;
using Autofac;
using Autofac.Core;
using Mospolyhelper.DI;
using Mospolyhelper.Features.Clients;

namespace Mospolyhelper
{
    class Program
    {
        private static IContainer container;

        [SecurityPermission(SecurityAction.Demand, Flags = SecurityPermissionFlag.ControlAppDomain)]
        public static void Main(string[] args)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);

            new Program().MainAsync().GetAwaiter().GetResult();
        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            File.WriteAllText("error.log", "UnhandledException: " + e.ToString() +
                                           "\n\nRuntime terminating: " + args.IsTerminating);
        }

        public async Task MainAsync()
        {
            container = BuildContainer();
            await new MainClient().Launch();
            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private IContainer BuildContainer()
        {
            var containerBuilder = new ContainerBuilder();
            containerBuilder.RegisterModule(new ScheduleModule());
            return containerBuilder.Build();
        }

        public static TService GetService<TService>() where TService : notnull
        {
            // TODO: Where TService : IService
            return container.Resolve<TService>();
        }
    }
}
