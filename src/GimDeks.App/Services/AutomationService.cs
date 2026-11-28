using GimDeks.Core.Interfaces;
using GimDeks.Core.Models;

namespace GimDeks.App.Services;

/// <summary>Applies user-defined rules to games. Rules only change local fields.</summary>
public sealed class AutomationService(IAutomationRuleRepository rules, IGameRepository games)
{
    public async Task<int> ApplyAsync(CancellationToken ct = default)
    {
        var active = (await rules.GetAllAsync(ct)).Where(r => r.Aktif).OrderBy(r => r.Urutan).ToList();
        if (active.Count == 0)
        {
            return 0;
        }

        var all = await games.GetAllAsync(ct);
        var changed = 0;
        foreach (var game in all)
        {
            var modified = false;
            foreach (var rule in active)
            {
                if (!Matches(game, rule))
                {
                    continue;
                }
                if (ApplyAction(game, rule))
                {
                    modified = true;
                }
            }
            if (modified)
            {
                await games.UpdateAsync(game, ct);
                changed++;
            }
        }
        return changed;
    }

    private static bool Matches(Game game, AutomationRule rule)
    {
        var value = rule.Field.ToLowerInvariant() switch
        {
            "status" => game.Status.ToString(),
            "nama" => game.Nama,
            "ukuran_gb" => game.UkuranGb.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "favorit" => game.Favorit ? "1" : "0",
            "rating" => game.Rating.ToString(System.Globalization.CultureInfo.InvariantCulture),
            "path_exe" => game.PathExe ?? "",
            _ => "",
        };
        var target = rule.Nilai ?? "";
        return rule.Operator.ToLowerInvariant() switch
        {
            "equals" => string.Equals(value, target, StringComparison.OrdinalIgnoreCase),
            "contains" => value.Contains(target, StringComparison.OrdinalIgnoreCase),
            "greater" => double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var a)
                && double.TryParse(target, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var b) && a > b,
            "less" => double.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var a)
                && double.TryParse(target, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var b) && a < b,
            "empty" => string.IsNullOrWhiteSpace(value),
            "notempty" => !string.IsNullOrWhiteSpace(value),
            _ => false,
        };
    }

    private static bool ApplyAction(Game game, AutomationRule rule)
    {
        var arg = rule.Argumen ?? "";
        switch (rule.Aksi.ToLowerInvariant())
        {
            case "set_status":
                if (Enum.TryParse<GameStatus>(arg, ignoreCase: true, out var status) && status != game.Status)
                {
                    game.Status = status;
                    return true;
                }
                break;
            case "set_favorite":
                var favorite = arg == "1" || arg.Equals("true", StringComparison.OrdinalIgnoreCase);
                if (game.Favorit != favorite)
                {
                    game.Favorit = favorite;
                    return true;
                }
                break;
            case "set_priority":
                if (int.TryParse(arg, out var priority) && game.Prioritas != priority)
                {
                    game.Prioritas = priority;
                    return true;
                }
                break;
            case "set_rating":
                if (int.TryParse(arg, out var rating) && game.Rating != rating)
                {
                    game.Rating = rating;
                    return true;
                }
                break;
        }
        return false;
    }
}
