﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NewLife;
using NewLife.Log;
using NewLife.Remoting;
using NewLife.Web;
using HttpContext = Microsoft.AspNetCore.Http.HttpContext;

namespace Stardust.Extensions
{
    /// <summary>性能跟踪中间件</summary>
    public class TracerMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>跟踪器</summary>
        public static ITracer Tracer { get; set; }

        /// <summary>支持作为标签数据的内容类型</summary>
        public static String[] TagTypes { get; set; } = new[] {
            "text/plain", "text/xml", "application/json", "application/xml", "application/x-www-form-urlencoded"
        };

        /// <summary>实例化</summary>
        /// <param name="next"></param>
        public TracerMiddleware(RequestDelegate next) => _next = next ?? throw new ArgumentNullException(nameof(next));

        /// <summary>调用</summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        public async Task Invoke(HttpContext ctx)
        {
            //!! 以下代码不能封装为独立方法，因为有异步存在，代码被拆分为状态机，导致这里建立的埋点span无法关联页面接口内的下级埋点
            ISpan span = null;
            if (Tracer != null && !ctx.WebSockets.IsWebSocketRequest)
            {
                var action = GetAction(ctx);
                if (!action.IsNullOrEmpty())
                {
                    // 请求主体作为强制采样的数据标签，便于分析链路
                    var req = ctx.Request;

                    span = Tracer.NewSpan(action);
                    span.Tag = $"{ctx.GetUserHost()} {req.Method} {req.GetRawUrl()}";
                    span.Detach(req.Headers);
                    if (span is DefaultSpan ds && ds.TraceFlag > 0)
                    {
                        if (req.ContentLength != null &&
                            req.ContentLength < 1024 * 8 &&
                            req.ContentType != null &&
                            req.ContentType.StartsWithIgnoreCase(TagTypes))
                        {
                            req.EnableBuffering();

                            var buf = new Byte[1024];
                            var count = await req.Body.ReadAsync(buf, 0, buf.Length);
                            span.Tag = Environment.NewLine + buf.ToStr(null, 0, count);
                            req.Body.Position = 0;
                        }

                        if (span.Tag.Length < 500)
                        {
                            var vs = req.Headers.Where(e => !e.Key.EqualIgnoreCase(ExcludeHeaders)).ToDictionary(e => e.Key, e => e.Value + "");
                            span.Tag += Environment.NewLine + vs.Join(Environment.NewLine, e => $"{e.Key}:{e.Value}");
                        }
                    }
                }
            }

            try
            {
                await _next.Invoke(ctx);

                // 根据状态码识别异常
                if (span != null)
                {
                    var code = ctx.Response.StatusCode;
                    if (code >= 400 && code != 404) span.SetError(new HttpRequestException($"Http Error {code} {(HttpStatusCode)code}"), null);
                }
            }
            catch (Exception ex)
            {
                if (span != null)
                {
                    // 接口抛出ApiException时，认为是正常业务行为，埋点不算异常
                    if (ex is ApiException)
                        span.Tag ??= ex.Message;
                    else
                        span.SetError(ex, null);
                }

                throw;
            }
            finally
            {
                span?.Dispose();
            }
        }

        /// <summary>忽略的头部</summary>
        public static String[] ExcludeHeaders { get; set; } = new[] {
            "traceparent", "Authorization", "Cookie"
        };

        /// <summary>忽略的后缀</summary>
        public static String[] ExcludeSuffixes { get; set; } = new[] {
            ".html", ".htm", ".js", ".css", ".map", ".png", ".jpg", ".gif", ".ico",  // 脚本样式图片
            ".woff", ".woff2", ".svg", ".ttf", ".otf", ".eot"   // 字体
        };

        private static String GetAction(HttpContext ctx)
        {
            var p = ctx.Request.Path + "";
            if (p.EndsWithIgnoreCase(ExcludeSuffixes)) return null;

            var ss = p.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (ss.Length == 0) return p;

            // 如果是魔方格式，保留3段，其它webapi接口只留2段
            if (ss.Length >= 3 && ss[2].EqualIgnoreCase("detail", "add", "edit", "delete"))
                p = "/" + ss.Take(3).Join("/");
            else
                p = "/" + ss.Take(2).Join("/");

            return p;
        }
    }
}