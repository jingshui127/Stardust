﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NewLife;
using NewLife.Log;
using NewLife.Security;
using NewLife.Threading;
using Stardust.Data;
using Stardust.Data.Monitors;
using Stardust.Data.Nodes;

namespace Stardust.Server.Services;

public class DataRetentionService : IHostedService
{
    private readonly Setting _setting;
    private readonly ITracer _tracer;
    private TimerX _timer;
    public DataRetentionService(Setting setting, ITracer tracer)
    {
        _setting = setting;
        _tracer = tracer;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new TimerX(DoWork, null, DateTime.Today.AddMinutes(Rand.Next(60)), 600 * 1000) { Async = true };

        // 临时来一次
        TimerX.Delay(DoWork, 10_000);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer.TryDispose();

        return Task.CompletedTask;
    }

    private void DoWork(Object state)
    {
        var set = _setting;
        if (set.DataRetention <= 0) return;

        // 保留数据的起点
        var time = DateTime.Now.AddDays(-set.DataRetention);
        var time2 = DateTime.Now.AddDays(-set.DataRetention2);
        //var time3 = DateTime.Now.AddDays(-set.DataRetention2 * 10);

        using var span = _tracer?.NewSpan("DataRetention", $"{time} {time2}");
        try
        {
            // 删除节点数据
            var rs = NodeData.DeleteBefore(time2);
            XTrace.WriteLine("删除[{0}]之前的NodeData共：{1:n0}", time2.ToFullString(), rs);

            // 删除Redis节点数据
            rs = RedisData.DeleteBefore(time2);
            XTrace.WriteLine("删除[{0}]之前的RedisData共：{1:n0}", time2.ToFullString(), rs);

            // 删除应用性能数据
            rs = AppMeter.DeleteBefore(time2);
            XTrace.WriteLine("删除[{0}]之前的AppMeter共：{1:n0}", time2.ToFullString(), rs);

            // 删除应用分钟统计数据
            rs = AppMinuteStat.DeleteBefore(time);
            XTrace.WriteLine("删除[{0}]之前的AppMinuteStat共：{1:n0}", time.ToFullString(), rs);

            // 删除追踪分钟统计数据
            rs = TraceMinuteStat.DeleteBefore(time);
            XTrace.WriteLine("删除[{0}]之前的TraceMinuteStat共：{1:n0}", time.ToFullString(), rs);

            // 删除追踪小时统计数据
            rs = TraceHourStat.DeleteBefore(time2);
            XTrace.WriteLine("删除[{0}]之前的TraceHourStat共：{1:n0}", time2.ToFullString(), rs);

            //rs = TraceDayStat.DeleteBefore(time3);
            //XTrace.WriteLine("删除[{0}]之前的TraceDayStat共：{1:n0}", time3.ToFullString(), rs);

            //// 删除监控明细数据
            //rs = TraceData.DeleteBefore(time);
            //XTrace.WriteLine("删除[{0}]之前的TraceData共：{1:n0}", time.ToFullString(), rs);
            //rs = SampleData.DeleteBefore(time);
            //XTrace.WriteLine("删除[{0}]之前的SampleData共：{1:n0}", time.ToFullString(), rs);
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
        }
    }
}