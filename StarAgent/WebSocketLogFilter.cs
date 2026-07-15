using NewLife;
using NewLife.Log;

namespace StarAgent;

/// <summary>WebSocket 异常日志过滤器</summary>
/// <remarks>
/// 屏蔽 WebSocket 连接断开时产生的常见异常日志，避免日志刷屏。
/// 这些异常通常发生在连接正常关闭或网络抖动时，属于预期内的行为。
/// </remarks>
internal class WebSocketLogFilter : Logger
{
    private readonly ILog _inner;

    public WebSocketLogFilter(ILog log) => _inner = log;

    protected override void OnWrite(LogLevel level, String format, params Object?[] args)
    {
        if (IsWebSocketNoise(format, args))
        {
            // DEBUG 模式下仍输出，便于问题排查
#if DEBUG
            _inner.Write(level, "[Filtered] " + format, args);
#endif
            return;
        }

        _inner.Write(level, format, args);
    }

    private static Boolean IsWebSocketNoise(String format, Object?[] args)
    {
        if (format.IsNullOrEmpty()) return false;

        var text = format;
        if (args != null && args.Length > 0)
        {
            try
            {
                text = String.Format(format, args);
            }
            catch { }
        }

        if (text.Contains("WebSocket异常")) return true;
        if (text.Contains("WebSocket") && text.Contains("ObjectDisposedException")) return true;
        if (text.Contains("WebSocket") && text.Contains("SocketException")) return true;
        if (text.Contains("无法访问已释放的对象")) return true;
        if (text.Contains("由于以前的关闭调用")) return true;
        if (text.Contains("通常每个套接字地址")) return true;

        // 检查异常参数
        if (args != null)
        {
            foreach (var arg in args)
            {
                if (arg is Exception ex && IsWebSocketException(ex)) return true;
            }
        }

        return false;
    }

    private static Boolean IsWebSocketException(Exception? ex)
    {
        if (ex == null) return false;

        var name = ex.GetType().Name;
        if (name.EqualIgnoreCase("ObjectDisposedException", "SocketException", "WebSocketException", "IOException")) return true;

        var msg = ex.Message;
        if (msg.Contains("WebSocket")) return true;
        if (msg.Contains("无法访问已释放的对象")) return true;
        if (msg.Contains("由于以前的关闭调用")) return true;
        if (msg.Contains("通常每个套接字地址")) return true;

        return IsWebSocketException(ex.InnerException);
    }
}
