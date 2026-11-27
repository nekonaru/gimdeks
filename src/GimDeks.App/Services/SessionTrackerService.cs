using System.Diagnostics;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;

namespace GimDeks.App.Services;

/// <summary>
/// Launches a validated local executable and records a play session until the
/// process exits. Only a process this service started is ever tracked.
/// Process waiting and database writes run off the UI thread.
/// </summary>
public sealed class SessionTrackerService
{
    private sealed record ActiveSession(CancellationTokenSource Cancellation);

    private readonly IPlaySessionRepository _sessions;
    private readonly IGameRepository _games;
    private readonly Dictionary<long, ActiveSession> _active = new();

    public event Action? SessionEnded;

    public SessionTrackerService(IPlaySessionRepository sessions, IGameRepository games)
    {
        _sessions = sessions;
        _games = games;
    }

    public bool IsRunning(long gameId) => _active.ContainsKey(gameId);

    public async Task<bool> LaunchAsync(Game game)
    {
        if (_active.ContainsKey(game.Id))
        {
            return false;
        }

        var process = await Task.Run(() => GameLauncherService.LaunchAndGetProcess(game.PathExe));
        if (process is null)
        {
            return false;
        }

        var sessionId = await Task.Run(() => _sessions.StartAsync(game.Id, "launcher"));
        game.JumlahMain += 1;
        game.DimainkanPada = DateTime.UtcNow;
        await Task.Run(() => _games.UpdateAsync(game));

        var cancellation = new CancellationTokenSource();
        _active[game.Id] = new ActiveSession(cancellation);
        _ = TrackAsync(game.Id, process, sessionId, cancellation.Token);
        return true;
    }

    private async Task TrackAsync(long gameId, Process process, long sessionId, CancellationToken token)
    {
        try
        {
            await process.WaitForExitAsync(token).ConfigureAwait(false);
            if (token.IsCancellationRequested)
            {
                return;
            }

            var seconds = Math.Max(1, (int)(DateTime.UtcNow - process.StartTime.ToUniversalTime()).TotalSeconds);
            await Task.Run(async () =>
            {
                await _sessions.EndAsync(sessionId, seconds);
                var game = await _games.GetByIdAsync(gameId);
                if (game is not null)
                {
                    game.TotalDetik += seconds;
                    await _games.UpdateAsync(game);
                }
            }, token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // App is closing; the open session is closed by CloseOpenSessionsAsync.
        }
        catch (Exception exception)
        {
            CrashLog.Write(exception);
        }
        finally
        {
            _active.Remove(gameId);
            try { process.Dispose(); } catch { /* best effort */ }
            SessionEnded?.Invoke();
        }
    }

    public async Task CloseOpenSessionsAsync()
    {
        foreach (var session in _active.Values)
        {
            session.Cancellation.Cancel();
            session.Cancellation.Dispose();
        }
        _active.Clear();
        await Task.Run(() => _sessions.CloseOpenSessionsAsync());
    }
}
