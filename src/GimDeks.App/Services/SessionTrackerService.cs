using System.Diagnostics;
using System.Windows.Threading;
using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;

namespace GimDeks.App.Services;

/// <summary>
/// Launches a validated local executable and records a play session until the
/// process exits. Only a process this service started is ever tracked.
/// </summary>
public sealed class SessionTrackerService
{
    private sealed record ActiveSession(Process Process, long SessionId, DateTime Started);

    private readonly IPlaySessionRepository _sessions;
    private readonly IGameRepository _games;
    private readonly DispatcherTimer _timer;
    private readonly Dictionary<long, ActiveSession> _active = new();

    public event Action? SessionEnded;

    public SessionTrackerService(IPlaySessionRepository sessions, IGameRepository games)
    {
        _sessions = sessions;
        _games = games;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTick;
    }

    public bool IsRunning(long gameId) => _active.ContainsKey(gameId);

    public async Task<bool> LaunchAsync(Game game)
    {
        if (_active.ContainsKey(game.Id))
        {
            return false;
        }

        var process = GameLauncherService.LaunchAndGetProcess(game.PathExe);
        if (process is null)
        {
            return false;
        }

        var sessionId = await _sessions.StartAsync(game.Id, "launcher");
        _active[game.Id] = new ActiveSession(process, sessionId, DateTime.UtcNow);
        game.JumlahMain += 1;
        game.DimainkanPada = DateTime.UtcNow;
        await _games.UpdateAsync(game);
        if (!_timer.IsEnabled)
        {
            _timer.Start();
        }
        return true;
    }

    public async Task CloseOpenSessionsAsync()
    {
        _timer.Stop();
        foreach (var session in _active.Values)
        {
            try { session.Process.Dispose(); } catch { /* best effort */ }
        }
        _active.Clear();
        await _sessions.CloseOpenSessionsAsync();
    }

    private async void OnTick(object? sender, EventArgs e)
    {
        foreach (var pair in _active.ToArray())
        {
            var active = pair.Value;
            if (!active.Process.HasExited)
            {
                continue;
            }

            var seconds = Math.Max(1, (int)(DateTime.UtcNow - active.Started).TotalSeconds);
            try
            {
                await _sessions.EndAsync(active.SessionId, seconds);
                var game = await _games.GetByIdAsync(pair.Key);
                if (game is not null)
                {
                    game.TotalDetik += seconds;
                    await _games.UpdateAsync(game);
                }
            }
            catch (Exception exception)
            {
                CrashLog.Write(exception);
            }

            _active.Remove(pair.Key);
            try { active.Process.Dispose(); } catch { /* best effort */ }
            SessionEnded?.Invoke();
        }

        if (_active.Count == 0)
        {
            _timer.Stop();
        }
    }
}
