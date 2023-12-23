using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using System.Text.Json.Serialization;
using Timer = CounterStrikeSharp.API.Modules.Timers.Timer;


namespace ChaseMod;


public class ConfigGen : BasePluginConfig
{
    [JsonPropertyName("CT_freeze_time")] public float fTime { get; set; } = 5.0f;
    [JsonPropertyName("KnifeDamage")] public int kDamage { get; set; } = 50;
    [JsonPropertyName("KnifeCountDown")] public float kCountDown { get; set; } = 2.0f;
}


[MinimumApiVersion(43)]
public class ChaseMod : BasePlugin, IPluginConfig<ConfigGen>
{
    public override string ModuleName => "HnS ChaseMod";
    public override string ModuleAuthor => "Franc1sco Franug";
    public override string ModuleVersion => "0.0.1-alpha";

    public ConfigGen Config { get; set; } = null!;
    public void OnConfigParsed(ConfigGen config) { Config = config; }

    private readonly Dictionary<CCSPlayerController, bool> bGod = new();

    public override void Load(bool hotReload)
    {
        if (hotReload)
        {
            Utilities.GetPlayers().ForEach(controller =>
            {
                bGod.Add(controller, false);
            });
        }

        RegisterEventHandler<EventPlayerConnectFull>((@event, info) =>
        {
            var player = @event.Userid;

            if (!player.IsValid)
            {
                return HookResult.Continue;

            }
            else
            {
                bGod.Add(player, false);
                return HookResult.Continue;
            }
        });

        RegisterEventHandler<EventPlayerDisconnect>((@event, info) =>
        {
            var player = @event.Userid;

            if (!player.IsValid)
            {
                return HookResult.Continue;

            }
            else
            {
                if (bGod.ContainsKey(player))
                {
                    bGod.Remove(player);
                }
                return HookResult.Continue;
            }
        });

        RegisterEventHandler<EventPlayerSpawn>((@event, info) =>
        {
            var player = @event.Userid;
            if (!player.IsValid || !player.PlayerPawn.IsValid || player.TeamNum != 3)
            {
                return HookResult.Continue;

            }

            player.MoveType = MoveType_t.MOVETYPE_NONE;
            new Timer(Config.fTime, () =>
            {
                if (!player.IsValid) return;
                if (!player.PlayerPawn.IsValid) return;

                player.MoveType = MoveType_t.MOVETYPE_WALK;
            });

            return HookResult.Continue;
        });

        RegisterEventHandler<EventPlayerHurt>((@event, info) =>
        {
            var player = @event.Userid;
            var attacker = @event.Attacker;
            if (!player.IsValid || !player.PlayerPawn.IsValid || player.TeamNum != 2 
            || !attacker.IsValid || !attacker.PlayerPawn.IsValid || attacker == player)
            {
                return HookResult.Continue;

            }

            @event.DmgHealth = Config.kDamage;
            bGod[player] = true;

            new Timer(Config.kCountDown, () =>
            {
                if (!player.IsValid) return;
                if (!player.PlayerPawn.IsValid) return;

                bGod[player] = false;
            });


            return HookResult.Changed;
        });

        RegisterEventHandler<EventPlayerHurt>((@event, info) =>
        {
            var player = @event.Userid;
            var attacker = @event.Attacker;
            if (!player.IsValid || !player.PlayerPawn.IsValid || player.TeamNum != 2
            || !attacker.IsValid || !attacker.PlayerPawn.IsValid || attacker == player)
            {
                return HookResult.Continue;

            }

            if (bGod[player])
            {
                @event.DmgHealth = 0;
                return HookResult.Changed;
            }


            return HookResult.Continue;
        }, HookMode.Pre);

        RegisterEventHandler<EventRoundEnd>((@event, info) =>
        {
            var reason = @event.Reason;

            if (reason == 10) // Round Draw!
            {
                @event.Reason = 9;
                @event.Winner = 2;
                return HookResult.Changed;
            }

            return HookResult.Continue;
        }, HookMode.Pre);
    }

    public enum RoundEndReason : int
    {
        TargetBombed = 1, // Target Successfully Bombed!
        // 2/3 not in use in CSGO
        TerroristsEscaped = 4, // The terrorists have escaped!
        CTStoppedEscape = 5, // The CTs have prevented most of the terrorists from escaping!
        TerroristsStopped = 6, // Escaping terrorists have all been neutralized!
        BombDefused = 7, // The bomb has been defused!
        CTWin = 8, // Counter-Terrorists Win!
        TerroristWin = 9, // Terrorists Win!
        Draw = 10, // Round Draw!
        HostagesRescued = 11, // All Hostages have been rescued!
        TargetSaved = 12, // Target has been saved!
        HostagesNotRescued = 13, // Hostages have not been rescued!
        TerroristsNotEscaped = 14, // Terrorists have not escaped!
        GameStart = 16, // Game Commencing!
        // 15 not in use in CSGO
        TerroristsSurrender = 17, // Terrorists Surrender
        CTSurrender = 18, // CTs Surrender
        TerroristsPlanted = 19, // Terrorists Planted the bomb
        CTsReachedHostage = 20 // CTs Reached the hostage
    }
}

