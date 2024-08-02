using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.GameFunctions;
using ECommons.Logging;
using Splatoon.SplatoonScripting;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Splatoon;
using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using ECommons.Schedulers;
using ECommons.Hooks.ActionEffectTypes;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public class M1_Quadruple_Crossing : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = new HashSet<uint> { 1226 };

    public override Metadata? Metadata => new(1, "damolitionn");

    Element? PlayerBaits;
    private bool IsRegular = false;
    private bool IsLeaping = false;
    private TickScheduler? sched = null;

    IBattleNpc? BlackCat => Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.DataId == 17193 && b.IsTargetable) as IBattleNpc;
    IBattleNpc? JumpMarker => Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.DataId == 17195 && !b.IsTargetable) as IBattleNpc;

    public override void OnSetup()
    {
        //Snapshotted Cones
        Controller.RegisterElementFromCode("Snapshots", "{\"Name\":\"Snapshots\",\"type\":4,\"radius\":30.0,\"coneAngleMin\":-22,\"coneAngleMax\":22,\"color\":4278190335,\"fillIntensity\":0.3,\"originFillColor\":838861055,\"endFillColor\":838861055,\"thicc\":3.0,\"refActorNPCNameID\":12686,\"refActorRequireCast\":true,\"refActorCastId\":[37952, 37980],\"refActorComparisonType\":6,\"includeRotation\":true,\"DistanceMax\":13.2,\"FillStep\":4.0}");

        //Baited Cones
        for (int i = 0; i < 4; i++)
        {
            if (!Controller.TryRegisterElement($"PlayerConeBait{i}", new(0)
            {
                Name = "",
                Enabled = false,
                type = 4,
                radius = 30.0f,
                coneAngleMin = -22,
                coneAngleMax = 22,
                color = 4278190335,
                fillIntensity = 0.3f,
                originFillColor = 1677721855,
                endFillColor = 1677721855,
                thicc = 3.0f,
                refActorDataID = 17193,
                refActorComparisonType = 3,
                includeRotation = true,
                FaceMe = true,
            }))
            {
                DuoLog.Error("Could not register layout");
            }
        }
    }

    public override void OnMessage(string Message)
    {
        //Regular Quadruple Crossing
        if (Message.Contains("(12686>37948)"))
        {
            IsRegular = true;
            sched?.Dispose();
            sched = new TickScheduler(() =>
            {
                IsRegular = false;
                HideCones();
            }, 10000);
        }
        //Leaping Quadruple Crossing
        if (Message.Contains("(12686>38959)"))
        {
            sched?.Dispose();
            sched = new TickScheduler(() =>
            {
                IsLeaping = true;
                sched = new TickScheduler(() =>
                {
                    IsLeaping = false;
                    HideCones();
                }, 4500);
            }, 6000);
        }
    }

    private void ShowCones()
    {
        var boss = BlackCat;
        if (IsRegular)
        {
            boss = BlackCat;
        }
        if (IsLeaping)
        {
            if (JumpMarker == null)
            {
                boss = BlackCat;
            }
            else
            {
                boss = JumpMarker;
            }
        }

        if (boss == null) { return; }

        var partyList = FakeParty.Get().ToList();
        var players = Svc.Objects
            .OfType<IPlayerCharacter>()
            .OrderBy(obj => Vector3.Distance(boss.Position, obj.Position))
            .Take(4)
            .ToList();

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            var partyIndex = partyList.FindIndex(p => p.Name.TextValue == player.Name.TextValue);
            if (Controller.TryGetElementByName($"PlayerConeBait{i}", out var e))
            {
                e.Enabled = true;
                e.faceplayer = $"<{(partyIndex + 1)}>";
            }
        }
    }

    private void HideCones()
    {
        for (int i = 0; i < 4; i++)
        {
            if (Controller.TryGetElementByName($"PlayerConeBait{i}", out var e))
            {
                e.Enabled = false;
            }
        }
    }

    public override void OnUpdate()
    {
        if (IsRegular || IsLeaping)
        {
            ShowCones();
        }
        else
        {
            var boss = BlackCat;
            if (boss == null || !Svc.Objects.Any(x => x.DataId == 17193 && x.IsTargetable))
            {
                IsRegular = false;
                IsLeaping = false;
                HideCones();
            }
        }

        if (Controller.TryGetElementByName($"Snapshots", out var e))
        {
            e.Enabled = true;
        }
    }

    public override void OnCombatEnd()
    {
        IsRegular = false;
        IsLeaping = false;
        HideCones();
        sched?.Dispose();
    }

    public override void OnCombatStart()
    {
        this.OnCombatEnd();
    }
}
