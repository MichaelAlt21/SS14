using Content.Shared.FixedPoint;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.GameTicking.Rules.Components;

[RegisterComponent, Access(typeof(TeamDeathMatchRuleSystem))]
public sealed partial class TeamDeathMatchRuleComponent : Component
{
    /// <summary>
    /// Количество команд (обычно 2)
    /// </summary>
    [DataField("teamCount")]
    public int TeamCount = 2;

    /// <summary>
    /// Рекомендуемое количество игроков в одной команде
    /// </summary>
    [DataField("playersPerTeam")]
    public int PlayersPerTeam = 8;

    /// <summary>
    /// Максимальное количество игроков в раунде
    /// </summary>
    [DataField("maxPlayers")]
    public int MaxPlayers = 20;

    /// <summary>
    /// Сколько очков нужно набрать команде для победы
    /// </summary>
    [DataField("winScore")]
    public FixedPoint2 WinScore = 50;

    /// <summary>
    /// Сколько очков даётся за одно убийство
    /// </summary>
    [DataField("killScore")]
    public FixedPoint2 KillScore = 1;

    /// <summary>
    /// Задержка перед окончанием раунда после победы (в секундах)
    /// </summary>
    [DataField("restartDelay")]
    public TimeSpan RestartDelay = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Какая команда победила (заполняется системой)
    /// </summary>
    [DataField("victorTeam")]
    public string? VictorTeam;
}
