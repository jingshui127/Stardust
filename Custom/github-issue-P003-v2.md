# BUG：RunCurrentProcess 在 Linux dotnet 启动模式下丢失 DLL 路径

## 关联

关联 #151（commit 503cff22 修复硬编码 StarAgent 进程名引入的方法）

## 问题描述

`StarAgent/MyStarClient.cs` 的 `RunCurrentProcess` 方法在 Linux 通过 `dotnet StarAgent.dll` 启动时，`Process.GetCurrentProcess().MainModule?.FileName` 返回的是 `dotnet` 可执行文件路径（如 `/usr/share/dotnet/dotnet`），而非 `StarAgent.dll`。

当前实现直接将 `exePath` 作为 `ProcessStartInfo.FileName`，`args`（如 `-run -delay`）作为 `Arguments`，导致实际执行命令变成：

```
dotnet -run -delay
```

`dotnet` 把 `-run` 当成 DLL 路径，报错：

```
The command could not be loaded, possibly because:
  * You intended to execute a .NET application:
      The application '-run' does not exist or is not a managed .dll or .exe.
  * You intended to execute a .NET SDK command:
      No .NET SDKs were found.
```

## 复现环境

- 操作系统：Linux（Ubuntu 22.04）
- 启动方式：`dotnet StarAgent.dll -s`（systemd 服务）
- StarAgent 版本：v3.7.2026.0721（含 commit 503cff22）
- 触发命令：星尘服务端下发 `node/restart`

## 复现步骤

1. 在 Linux 上通过 `dotnet StarAgent.dll -s` 启动 StarAgent（systemd 服务）
2. 在星尘管理后台对节点执行 `node/restart` 命令
3. 查看 StarAgent 日志，出现 `The application '-run' does not exist` 错误
4. 服务无法正常重启

## 修复方案

参考 `Program.cs` 中 `FixSystemdService` 的兼容逻辑，检测 dotnet/mono 启动模式后补上 DLL 路径：

```csharp
private static Boolean RunCurrentProcess(String args)
{
    try
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (exePath.IsNullOrEmpty()) return false;

        var fileName = Path.GetFileName(exePath);
        var arguments = args;

        // 兼容 dotnet/mono 启动模式（Linux 常见：dotnet StarAgent.dll）
        var cmdArgs = Environment.GetCommandLineArgs();
        if (fileName.EqualIgnoreCase("dotnet", "dotnet.exe", "mono", "mono.exe", "mono-sgen") && cmdArgs.Length >= 1)
        {
            var dll = cmdArgs[0].GetFullPath();
            if (dll.Contains(' ')) dll = $"\"{dll}\"";
            arguments = $"{dll} {args}";
        }

        var si = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = arguments,
            UseShellExecute = false,
        };

        return Process.Start(si) != null;
    }
    catch (Exception ex)
    {
        XTrace.WriteException(ex);
        return false;
    }
}
```

## 修复后日志

```
09:58:19.364 11 Y P [Node][WebSocket] 收到命令: {"Command":"node/restart",...}
09:58:20.373 25 N L 服务停止 Upgrade
09:58:20.508 01 N - StarAgent v3.7.2026.0721 Build 2026-07-21 09:56:26 .NET 10.0
09:58:20.509 01 N - 延迟启动，等待3000秒
09:58:23.604 01 N - ProcessCommand cmd=-run args=-run -delay
正在模拟运行……
09:58:28.608 01 N - 正在初始化星尘……
09:58:28.613 01 N - 接入星尘平台：Server=http://127.0.0.1:6600,http://47.113.219.65:6600
```

服务正常重启，不再报错。

## 补充说明

`Program.cs` 的 `FixSystemdService` 方法已有相同的 dotnet/mono 兼容逻辑（检测 `dotnet`/`mono` 启动模式后补上 DLL 路径），但 `RunCurrentProcess` 方法没有采用相同的兼容处理。

建议将 dotnet/mono 兼容逻辑提取为公共方法，避免重复代码。
