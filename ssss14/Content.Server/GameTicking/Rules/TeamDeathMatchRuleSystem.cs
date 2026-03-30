using Content.Server.GameTicking.Rules.Components;
using Content.Server.KillTracking;
using Content.Server.Mind;
using Content.Server.Points;
using Content.Server.RoundEnd;
using Content.Server.Station.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.GameTicking;
using Content.Shared.GameTicking.Components;
using Content.Shared.Points;
using Robust.Server.Player;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System.Linq;

namespace Content.Server.GameTicking.Rules;

public sealed class TeamDeathMatchRuleSystem : GameRuleSystem<TeamDeathMatchRuleComponent>
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly MindSystem _mindSystem = default!;
    [Dependency] private readonly OutfitSystem _outfitSystem = default!;
    [Dependency] private readonly PointSystem _pointSystem = default!;
    [Dependency] private readonly RespawnRuleSystem _respawnSystem = default!;
    [Dependency] private readonly RoundEndSystem _roundEndSystem = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;

    private int _teamToggle = 0;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerBeforeSpawnEvent>(OnBeforeSpawn);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnSpawnComplete);
        SubscribeLocalEvent<KillReportedEvent>(OnKillReported);
    }

    private void OnBeforeSpawn(PlayerBeforeSpawnEvent ev)
    {
        var query = EntityQueryEnumerator<TeamDeathMatchRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var tdm, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            var mind = _mindSystem.CreateMind(ev.Player.UserId, ev.Profile.Name);
            _mindSystem.SetUserId(mind, ev.Player.UserId);

            var mobMaybe = _stationSpawning.SpawnPlayerCharacterOnStation(ev.Station, null, ev.Profile);
            if (mobMaybe == null) continue;

            var mob = mobMaybe.Value;
            _mindSystem.TransferTo(mind, mob);

            _outfitSystem.SetOutfit(mob, "RobotechGear");   // замени на свою экипировку, если нужно

            EnsureComp<KillTrackerComponent>(mob);
            _respawnSystem.AddToTracker(ev.Player.UserId, (uid, null));

            AssignTeam(mob);

            ev.Handled = true;
            break;
        }
    }

    private void OnSpawnComplete(PlayerSpawnCompleteEvent ev)
    {
        EnsureComp<KillTrackerComponent>(ev.Mob);
    }

    private void OnKillReported(ref KillReportedEvent ev)
    {
        var query = EntityQueryEnumerator<TeamDeathMatchRuleComponent, PointManagerComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var tdm, out var pointManager, out var rule))
        {
            if (!GameTicker.IsGameRuleActive(uid, rule))
                continue;

            if (ev.Primary is not KillPlayerSource killerSource) continue;

            var killer = killerSource.Entity;
            if (!TryComp<TeamComponent>(killer, out var killerTeam)) continue;

            // Защита от тимкилла
            if (TryComp<TeamComponent>(ev.Entity, out var victimTeam) &&
                victimTeam.TeamId == killerTeam.TeamId)
                continue;

            // Начисляем очки команде убийцы
            _pointSystem.AdjustTeamPoints(killerTeam.TeamId, tdm.KillScore, uid);

            // Проверка победы
            CheckWinCondition(uid, tdm, pointManager);
        }
    }

    private void CheckWinCondition(EntityUid ruleUid, TeamDeathMatchRuleComponent component, PointManagerComponent pointManager)
    {
        foreach (var (teamId, score) in pointManager.Teams)
        {
            if (score >= component.WinScore)
            {
                component.VictorTeam = teamId;
                var winnerName = teamId == "RedTeam" ? "Красная команда" : "Синяя команда";
                _roundEndSystem.EndRound($"{winnerName} победила!");
                break;
            }
        }
    }

    private void AssignTeam(EntityUid mob)
    {
        var teamId = (_teamToggle++ % 2 == 0) ? "RedTeam" : "BlueTeam";

        var teamComp = EnsureComp<TeamComponent>(mob);
        teamComp.TeamId = teamId;
        teamComp.Color = teamId == "RedTeam" ? Color.Red : Color.Blue;
    }
}
