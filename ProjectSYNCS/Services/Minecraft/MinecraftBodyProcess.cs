using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace ProjectSYNCS.Services;

/// <summary>
/// The Node child process that is her body on the Minecraft server (<c>MinecraftBody/index.js</c>):
/// one JSON command per line in, one JSON event per line out, its logs on stderr.
/// </summary>
/// <remarks>
/// The child starts with an <b>empty environment</b> plus the few variables it needs. The bot's own
/// environment carries the Discord token (<c>Discord__Token</c>, set by <c>run.sh</c>), and a third-party
/// npm tree has no business reading it.
/// </remarks>
internal sealed class MinecraftBodyProcess
{
    // Kept from the parent: what Node needs to start and open sockets, nothing that holds a secret.
    // SYSTEMROOT matters on Windows only (Node's networking fails without it), for dev runs.
    private static readonly string[] InheritedVariables = { "PATH", "SYSTEMROOT", "HOME", "TMPDIR", "TEMP", "TMP" };

    private readonly Process _process;
    private readonly object _writeLock = new();

    private MinecraftBodyProcess(Process process) => _process = process;

    public int Id => _process.Id;

    /// <summary>
    /// Starts the body. <paramref name="onEvent"/> gets each stdout line, <paramref name="onLog"/> each
    /// stderr line, <paramref name="onExit"/> runs once after the process has exited and both streams
    /// are drained. Throws if Node can't be started.
    /// </summary>
    public static MinecraftBodyProcess Start(string node, string script, IReadOnlyDictionary<string, string> variables,
        Action<string> onEvent, Action<string> onLog, Action<MinecraftBodyProcess> onExit)
    {
        var psi = new ProcessStartInfo
        {
            FileName = node,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            WorkingDirectory = Path.GetDirectoryName(script)!,
        };
        psi.ArgumentList.Add(script);

        psi.Environment.Clear();
        foreach (var name in InheritedVariables)
            if (Environment.GetEnvironmentVariable(name) is { } value)
                psi.Environment[name] = value;
        foreach (var (name, value) in variables)
            psi.Environment[name] = value;

        var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        var body = new MinecraftBodyProcess(process);
        process.OutputDataReceived += (_, e) => { if (e.Data is { Length: > 0 } line) onEvent(line); };
        process.ErrorDataReceived += (_, e) => { if (e.Data is { Length: > 0 } line) onLog(line); };
        process.Exited += (_, _) => Task.Run(() =>
        {
            // The parameterless wait also waits for the redirected streams to reach EOF, so every
            // event the body wrote is handled before onExit runs.
            try { process.WaitForExit(); } catch { /* already gone */ }
            onExit(body);
            process.Dispose();
        });

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        return body;
    }

    /// <summary>Sends one command. False if the body is gone (its stdin is closed).</summary>
    public bool Send(object command)
    {
        var line = JsonSerializer.Serialize(command);
        lock (_writeLock)
        {
            try
            {
                _process.StandardInput.WriteLine(line);
                _process.StandardInput.Flush();
                return true;
            }
            catch (Exception ex) when (ex is IOException or ObjectDisposedException or InvalidOperationException)
            {
                return false;
            }
        }
    }

    /// <summary>Ends the process outright, for a body that didn't leave when asked.</summary>
    public void Kill()
    {
        try
        {
            if (!_process.HasExited) _process.Kill(entireProcessTree: true);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or NotSupportedException)
        {
            // Exited in between, or already disposed.
        }
    }
}
