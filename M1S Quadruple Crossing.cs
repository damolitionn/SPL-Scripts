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

    private bool IsRegular = false;
    private bool IsLeapingCone = false;
    private bool IsLeapingCleave = false;
    private bool LeftFirst = false;
    private bool RightFirst = false;
    private bool PreMouser = false;
    private bool PostMouser = false;
    private TickScheduler? sched = null;
    private List<(string, string)> buffPairs = new List<(string, string)>();

    IBattleNpc? BlackCat => Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.DataId == 17193 && b.IsTargetable) as IBattleNpc;
    IBattleNpc? JumpMarker => Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.DataId == 17195 && !b.IsTargetable) as IBattleNpc;
    IBattleNpc? Clone => Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.DataId == 17196 && !b.IsTargetable && b.IsCharacterVisible()) as IBattleNpc;

    public override void OnSetup()
    {
        //Cleaves
        if (!Controller.TryRegisterElement("LeftCleave", new(0)
        {
            Name = "LeftCleave",
            Enabled = false,
            type = 3,
            refY = 30.0f,
            radius = 30.0f,
            refActorComparisonType = 3,
            includeRotation = true,
            AdditionalRotation = 4.712389f
        }))
        {
            DuoLog.Error("Could not register layout");
        }
        if (!Controller.TryRegisterElement("RightCleave", new(0)
        {
            Name = "RightCleave",
            Enabled = false,
            type = 3,
            refY = 30.0f,
            radius = 30.0f,
            refActorComparisonType = 3,
            includeRotation = true,
            AdditionalRotation = 1.5707964f
        }))
        {
            DuoLog.Error("Could not register layout");
        }

        //Snapshot Clones
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

    public override void OnVFXSpawn(uint target, string vfxPath) 
    
    { 
        DuoLog.Debug($"VFX Spawned: Target:{target.GetObject().Name} VFX:{vfxPath}");
    }


    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (source.GetObject().DataId == 17196 && target.GetObject().DataId == 17193)
        {
            CheckBossBuffs();
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
                IsLeapingCone = true;
                sched = new TickScheduler(() =>
                {
                    IsLeapingCone = false;
                    HideCones();
                }, 4500);
            }, 6000);
        }

        //Left First Cleave
        if (Message.Contains("(12686>37945)"))
        {
            IsLeapingCleave = false;
            LeftFirst = true;
            HandleCleaveSequence();
        }

        //Leaping Right First Cleave
        /*if (Message.Contains("(12686>37965)"))
        {
            IsLeapingCleave = true;
            RightFirst = true;
            HandleCleaveSequence();
        }*/


        //Right First Cleave
        if (Message.Contains("(12686>37942)"))
        {
            IsLeapingCleave = false;
            RightFirst = true;
            HandleCleaveSequence();
        }
    }

    private void HandleCloneAttacks()
    {
        if (PreMouser == true)
        {
            if (LeftFirst)
            {
                LeftCleaveFirst(20000, 4000, 17196, 17196);
            }
            else
            {
                RightCleaveFirst(20000, 4000, 17196, 17196);
            }
        }
    }

    private void HandleCleaveSequence()
    {
        if (IsLeapingCleave == true)
        {
            if (LeftFirst)
            {
                LeftCleaveFirst(7000, 3000, 17195, 17193);
            }
            else
            {
                RightCleaveFirst(7000, 3000, 17195, 17193);
            }

        }
        else
        {
            if (LeftFirst)
            {
                LeftCleaveFirst(6000, 3000, 17193, 17193);
            }
            else
            {
                RightCleaveFirst(6000, 3000, 17193, 17193);
            }
        }

    }

    private void LeftCleaveFirst(uint t1, uint t2, uint dataID1, uint dataID2)
    {
        if (Controller.TryGetElementByName("LeftCleave", out var leftCleave))
        {
            leftCleave.Enabled = true;
            leftCleave.refActorDataID = dataID1;
            leftCleave.onlyVisible = true;
        }

        sched?.Dispose();
        sched = new TickScheduler(() =>
        {
            if (Controller.TryGetElementByName("LeftCleave", out var leftCleave))
            {
                leftCleave.Enabled = false;
            }
            if (Controller.TryGetElementByName("RightCleave", out var rightCleave))
            {
                rightCleave.Enabled = true;
                rightCleave.refActorDataID = dataID2;
                rightCleave.onlyVisible = true;
            }

            sched = new TickScheduler(() =>
            {
                if (Controller.TryGetElementByName("RightCleave", out var rightCleave))
                {
                    rightCleave.Enabled = false;
                }
            }, t2);
        }, t1);
    }

    private void RightCleaveFirst(uint t1, uint t2, uint dataID1, uint dataID2)
    {
        if (Controller.TryGetElementByName("RightCleave", out var rightCleave))
        {
            rightCleave.Enabled = true;
            rightCleave.refActorDataID = dataID1;
            rightCleave.onlyVisible = true;
        }

        sched?.Dispose();
        sched = new TickScheduler(() =>
        {
            if (Controller.TryGetElementByName("RightCleave", out var rightCleave))
            {
                rightCleave.Enabled = false;
            }
            if (Controller.TryGetElementByName("LeftCleave", out var leftCleave))
            {
                leftCleave.Enabled = true;
                leftCleave.refActorDataID = dataID2;
                leftCleave.onlyVisible = true;
            }

            sched = new TickScheduler(() =>
            {
                if (Controller.TryGetElementByName("LeftCleave", out var leftCleave))
                {
                    leftCleave.Enabled = false;
                }
            }, t2);
        }, t1);
    }

    private void CheckBossBuffs()
    {
        var boss = BlackCat;
        if (boss == null) { return; }
        string movement = "";
        string attack = "";

        if (boss.StatusList.Any(status => status.StatusId == 4050))
        {
            movement = "Right";
        }
        else if (boss.StatusList.Any(status => status.StatusId == 4051))
        {
            movement = "Left";
        }
        if (boss.StatusList.Any(status => status.StatusId == 4048))
        {
            attack = "Cleave";
        }
        else if (boss.StatusList.Any(status => status.StatusId == 4049))
        {
            attack = "Claw";
        }
        if (!string.IsNullOrEmpty(movement) && !string.IsNullOrEmpty(attack))
        {
            PostMouser = true;
            PreMouser = false;
            buffPairs.Add((movement, attack));
        }
        if (string.IsNullOrEmpty(movement) && !string.IsNullOrEmpty(attack))
        {
            PreMouser = true;
            PostMouser = false;
            if (movement == "" && attack == "Cleave")
            {
                HandleCloneAttacks();
            }
        }
    }

    private void ShowCleaves()
    {
        if (LeftFirst)
        {
            if (Controller.TryGetElementByName("LeftCleave", out var e))
            {
                e.Enabled = true;
                e.refActorDataID = 17193;
            }

        }
        else
        {
            if (Controller.TryGetElementByName("RightCleave", out var e))
            {
                e.Enabled = true;
                e.refActorDataID = 17193;
            }
        }

    }

    private void ShowCones()
    {
        var boss = BlackCat;
        if (IsRegular)
        {
            boss = BlackCat;
        }
        if (IsLeapingCone)
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
        if (IsRegular || IsLeapingCone)
        {
            ShowCones();
        }
        else
        {
            var boss = BlackCat;
            if (boss == null || !Svc.Objects.Any(x => x.DataId == 17193 && x.IsTargetable))
            {
                IsRegular = false;
                IsLeapingCone = false;
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
        IsLeapingCone = false;
        IsLeapingCleave = false;
        LeftFirst = false;
        RightFirst = false;
        PreMouser = false;
        PostMouser = false;
        HideCones();
        sched?.Dispose();
        buffPairs.Clear();
    }

    public override void OnCombatStart()
    {
        this.OnCombatEnd();
    }
}
