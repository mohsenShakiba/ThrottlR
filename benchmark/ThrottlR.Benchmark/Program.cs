using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using BenchmarkDotNet.Configs;
using CommandLine;

namespace ThrottlR.Benchmark
{
    internal class Program
    {
        private static void Main()
        {
            BenchmarkRunner.Run<ThrottlerOverheadBenchmark>(new DebugInProcessConfig());
        }
    }

    [SimpleJob(RuntimeMoniker.NetCoreApp31)]
    [MemoryDiagnoser]
    [RPlotExporter]
    public class ThrottlerOverheadBenchmark
    {
        private readonly Random _random = new Random();

        private RequestDelegate _app;
        private List<long> _ipList;

        [GlobalSetup]
        public void Setup()
        {
            _ipList = Enumerable.Range(1000, 2000).Select(x => (long)x).ToList();

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddRouting();
            serviceCollection.AddLogging();
            serviceCollection.AddSingleton(new DiagnosticListener(string.Empty));

            serviceCollection.AddThrottlR(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithIpResolver()
                        .WithGeneralRule(TimeSpan.FromDays(1), int.MaxValue);
                });
            }).AddInMemoryCounterStore();

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var app = new ApplicationBuilder(serviceProvider);
            app.UseRouting();
            app.UseThrottler();
            app.UseEndpoints(builder =>
            {
                builder.Map("/WithThrottle", Greetings).Throttle();
                builder.Map("/NoThrottle", Greetings);
            });

            _app = app.Build();

            static async Task Greetings(HttpContext context)
            {
                await Task.Delay(2000);
                await context.Response.WriteAsync("Hello World!");
            }
        }

        // [Benchmark]
        // public Task<int> WithThrottle()
        // {
        //     return Run("/WithThrottle");
        // }
        //
        // [Benchmark]
        // public Task<int> NoThrottle()
        // {
        //     return Run("/NoThrottle");
        // }

        [Benchmark]
        public void WithAsyncLock()
        {
            const string key = "test";
            const int numThread = 8;
            var threadList = new List<Task>();
            var al = new AsyncKeyLock();

            for (var i = 0; i < numThread; i++)
            {
                var t = Task.Factory.StartNew(async () =>
                {
                    for (var i = 0; i <= 1000; i++)
                    {
                        var r = await al.ReaderLockAsync(key + i);
                        r.Dispose();
                    }
                });
                threadList.Add(t);
            }

            Task.WaitAll(threadList.ToArray());
        }

        [Benchmark]
        public void WithTinySemaphore()
        {
            const string key = "test";
            const int numThread = 8;
            var threadList = new List<Task>();
            var al = new AsyncKeyLock2();

            for (var i = 0; i < numThread; i++)
            {
                var t = Task.Factory.StartNew(async () =>
                {
                    for (var i = 0; i <= 1000; i++)
                    {
                        var r = await al.ReaderLockAsync(key + i);
                        r.Dispose();
                    }
                });
                threadList.Add(t);
            }

            Task.WaitAll(threadList.ToArray());
        }

        private async Task<int> Run(string path)
        {
            var httpContext = new DefaultHttpContext();
            httpContext.Connection.RemoteIpAddress = new IPAddress(_ipList[_random.Next(_ipList.Count - 1)]);
            httpContext.Request.Path = new PathString(path);
            await _app(httpContext);
            return httpContext.Response.StatusCode;
        }
    }
}
