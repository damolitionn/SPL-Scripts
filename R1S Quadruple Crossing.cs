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
using Lumina.Data.Files;
using Splatoon.Memory;
using Lumina.Excel.GeneratedSheets2;

namespace SplatoonScriptsOfficial.Duties.Dawntrail;
public class M1_Quadruple_Crossing : SplatoonScript
{
    public override HashSet<uint>? ValidTerritories { get; } = new HashSet<uint> { 1226 };

    private List<Vector3> clonePositions = new List<Vector3>();


    public override Metadata? Metadata => new(1, "damolitionn");

    private bool IsRegular = false;
    private bool IsChipper = false;
    private bool IsLeapingCone = false;
    private bool IsLeapingCleave = false;
    private bool LeftFirst = false;
    private bool RightFirst = false;
    private string movement = "";
    private string attack = "";
    private readonly string[] Leaping = { "(12686>37975)", "(12686>37976)", "(12686>38009)", "(12686>38010)", "(12686>38011)", "(12686>38012)", "(12686>38959)", "(12686>38995)" };

    private TickScheduler? sched = null;
    private Vector3 jumpTargetPosition;

    IBattleNpc? BlackCat => Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.DataId == 17193 && b.IsTargetable) as IBattleNpc;
    IBattleNpc? JumpMarker => Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.DataId == 17195 && !b.IsTargetable) as IBattleNpc;
    IBattleNpc? Clone => Svc.Objects.FirstOrDefault(x => x is IBattleNpc b && b.DataId == 17196) as IBattleNpc;

    public override void OnSetup()
    {
        //Clone Position
        Controller.RegisterElementFromCode("SArrowLeft", "{\"Name\":\"SArrowLeft\",\"enabled\": false,\"type\":2,\"refX\":90.0,\"refY\":105,\"refZ\":0,\"offX\":100.0,\"offY\":105,\"radius\":0.0,\"color\":3355507455,\"thicc\":3.0,\"LineEndA\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("SArrowRight", "{\"Name\":\"SArrowRight\",\"enabled\": false,\"type\":2,\"refX\":110.0,\"refY\":105,\"refZ\":0,\"offX\":100.0,\"offY\":105,\"radius\":0.0,\"color\":3355507455,\"thicc\":3.0,\"LineEndA\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("NArrowRight", "{\"Name\":\"NArrowRight\",\"enabled\": false,\"type\":2,\"refX\":90.0,\"refY\":95,\"refZ\":0,\"offX\":100.0,\"offY\":95,\"radius\":0.0,\"color\":3355507455,\"thicc\":3.0,\"LineEndA\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        Controller.RegisterElementFromCode("NArrowLeft", "{\"Name\":\"NArrowLeft\",\"enabled\": false,\"type\":2,\"refX\":110.0,\"refY\":95,\"refZ\":0,\"offX\":100.0,\"offY\":95,\"radius\":0.0,\"color\":3355507455,\"thicc\":3.0,\"LineEndA\":1,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");

        //Cleaves
        if (!Controller.TryRegisterElement("Cleave", new(0)
        {
            Name = "Cleave",
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

        //Clone Cleaves
        if (!Controller.TryRegisterElement("CloneCleave", new(0)
        {
            Name = "CloneCleave",
            Enabled = false,
            type = 2,
            radius = 30.0f,
        }))
        {
            DuoLog.Error("Could not register layout");
        }
        //Nailchipper AOEs
        Controller.RegisterElementFromCode("NailchipperAOE", "{\"Name\":\"NailchipperAOE\",\"type\":1,\"radius\":5.0,\"color\":4278190335,\"fillIntensity\":0.3,\"originFillColor\":838861055,\"endFillColor\":838861055,\"refActorComparisonType\":7,\"includeRotation\":true,\"FaceMe\":true,\"refActorVFXPath\":\"vfx/lockon/eff/lockon8_t0w.avfx\",\"refActorVFXMax\":8000,\"refActorTetherTimeMin\":0.0,\"refActorTetherTimeMax\":0.0}");
        //Snapshot Clones
        Controller.RegisterElementFromCode("Snapshots", "{\"Name\":\"Snapshots\",\"enabled\": true, \"type\":4,\"radius\":30.0,\"coneAngleMin\":-22,\"coneAngleMax\":22,\"color\":4278190335,\"fillIntensity\":0.3,\"originFillColor\":838861055,\"endFillColor\":838861055,\"thicc\":3.0,\"refActorNPCNameID\":12686,\"refActorRequireCast\":true,\"refActorCastId\":[37952, 37980],\"refActorComparisonType\":6,\"includeRotation\":true,\"DistanceMax\":13.2,\"FillStep\":4.0}");

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

        //Clone Cones
        for (int i = 0; i < 4; i++)
        {
            if (!Controller.TryRegisterElement($"CloneCone{i}", new(0)
            {
                Name = "",
                Enabled = false,
                type = 5,
                radius = 30.0f,
                coneAngleMin = -22,
                coneAngleMax = 22,
                color = 4278190335,
                fillIntensity = 0.3f,
                originFillColor = 1677721855,
                endFillColor = 1677721855,
                thicc = 3.0f,
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
        //NailChipper
        if (Message.Contains("(12686>38021)"))
        {
            sched = new TickScheduler(() =>
            {
                IsChipper = true;
                sched = new TickScheduler(() =>
                {
                    IsChipper = false;
                    HideCones();
                }, 7000);
            }, 4000);
        }

        //Regular Quadruple Crossing
        if (Message.Contains("(12686>37948)"))
        {
            IsRegular = true;
            sched = new TickScheduler(() =>
            {
                IsRegular = false;
                HideCones();
            }, 10000);
        }
        //Leaping Quadruple Crossing
        if (Leaping.Any(Leaping => Message.Contains(Leaping)))
        {
            
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
    }

    public override void OnVFXSpawn(uint target, string vfxPath)
    {
        var obj = target.GetObject();

        if (obj?.DataId == 17193)
        {
            if (clonePositions.Count >= 6)
            {
                IsLeapingCleave = true;
            }
            //Left Cleave First
            if (vfxPath.Contains("vfx/common/eff/m0884_cast_twin02p1.avfx"))
            {
                RightFirst = false;
                LeftFirst = true;
                HandleCleaveSequence();
            }
            //Right Cleave First
            else if (vfxPath.Contains("vfx/common/eff/m0884_cast_twin01p1.avfx"))
            {
                LeftFirst = false;
                RightFirst = true;
                HandleCleaveSequence();
            }
        }

        if (obj?.DataId == 17196 && clonePositions.Count == 9)
        {
            if (vfxPath.Contains("vfx/common/eff/mon_eisyo03t.avfx"))
            {
                GetJumpPositions(8);
            }
            if (vfxPath.Contains("vfx/common/eff/m0884_cast_dbl01p1.avfx"))
            {
                GetJumpPositions(8);
                HandleCloneCleaves();
            }
        }

        if (obj?.DataId == 17196 && clonePositions.Count == 10)
        {
            if (vfxPath.Contains("vfx/common/eff/mon_eisyo03t.avfx"))
            {
                GetJumpPositions(9);
            }
            if (vfxPath.Contains("vfx/common/eff/m0884_cast_dbl01p1.avfx"))
            {
                GetJumpPositions(9);
                HandleCloneCleaves();
            }
        }
    }

    private void HandleCloneCones()
    {
        var jumpPosition = jumpTargetPosition;
        var partyList = FakeParty.Get().ToList();
        var players = Svc.Objects
            .OfType<IPlayerCharacter>()
            .OrderBy(obj => Vector3.Distance(jumpPosition, obj.Position))
            .Take(4)
            .ToList();

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];
            var partyIndex = partyList.FindIndex(p => p.Name.TextValue == player.Name.TextValue);
            if (Controller.TryGetElementByName($"CloneCone{i}", out var e))
            {
                e.Enabled = true;
                e.refX = jumpPosition.X;
                e.refY = jumpPosition.Z;
                e.refZ = jumpPosition.Y;
                e.faceplayer = $"<{(partyIndex + 1)}>";
            }
        }
    }

    private void GetJumpPositions(int cloneNumber)
    {
        if (clonePositions[cloneNumber].Z > 100)
        {
            if (Controller.TryGetElementByName($"SArrowLeft", out var arrow1) && arrow1.Enabled)
            {
                jumpTargetPosition = new Vector3(arrow1.refX, arrow1.refZ, arrow1.refY);
            }
            if (Controller.TryGetElementByName($"SArrowRight", out var arrow2) && arrow2.Enabled)
            {
                jumpTargetPosition = new Vector3(arrow2.refX, arrow2.refZ, arrow2.refY);
            }
        }
        else if (clonePositions[cloneNumber].Z < 100)
        {
            if (Controller.TryGetElementByName($"NArrowLeft", out var arrow1) && arrow1.Enabled)
            {
                jumpTargetPosition = new Vector3(arrow1.refX, arrow1.refZ, arrow1.refY);
            }
            if (Controller.TryGetElementByName($"NArrowRight", out var arrow2) && arrow2.Enabled)
            {
                jumpTargetPosition = new Vector3(arrow2.refX, arrow2.refZ, arrow2.refY);
            }
        }
    }

    public override void OnTetherCreate(uint source, uint target, uint data2, uint data3, uint data5)
    {
        if (source.GetObject().DataId == 17196 && target.GetObject().DataId == 17193)
        {
            var position = source.GetObject().Position;
            clonePositions.Add(position);
            CheckBossBuffs();
        }
    }

    private void HandleCloneAttacks()
    {
        if (clonePositions.Count <= 2)
        {
            if (LeftFirst)
            {
                LeftCleaveFirst(20000, 4000, 17196, 17196);
            }
            else
            {
                RightCleaveFirst(20000, 4000, 17196, 17196);
            }
            var flagResetScheduler = new TickScheduler(() =>
            {
                LeftFirst = false;
                RightFirst = false;
            }, 3000);
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

    private void RightCloneCleave()
    {
        if (Controller.TryGetElementByName("CloneCleave", out var rightCleave))
        {
            rightCleave.Enabled = true;
            rightCleave.refX = jumpTargetPosition.X;
            rightCleave.refY = jumpTargetPosition.Z;
            rightCleave.offX = jumpTargetPosition.X + 30f;
            rightCleave.offY = jumpTargetPosition.Z;
        }

        sched?.Dispose();
        sched = new TickScheduler(() =>
        {
            if (Controller.TryGetElementByName("CloneCleave", out var rightCleave))
            {
                rightCleave.Enabled = false;
            }
            if (Controller.TryGetElementByName("CloneCleave", out var leftCleave))
            {
                leftCleave.Enabled = true;
                leftCleave.refX = jumpTargetPosition.X;
                leftCleave.refY = jumpTargetPosition.Z;
                leftCleave.offX = jumpTargetPosition.X - 30f;
                leftCleave.offY = jumpTargetPosition.Z;
            }

            sched = new TickScheduler(() =>
            {
                if (Controller.TryGetElementByName("CloneCleave", out var leftCleave))
                {
                    leftCleave.Enabled = false;
                }
            }, 2000);
        }, 7000);
    }

    private void LeftCloneCleave()
    {
        if (Controller.TryGetElementByName("CloneCleave", out var leftCleave))
        {
            leftCleave.Enabled = true;
            leftCleave.refX = jumpTargetPosition.X;
            leftCleave.refY = jumpTargetPosition.Z;
            leftCleave.offX = jumpTargetPosition.X - 30f;
            leftCleave.offY = jumpTargetPosition.Z;
        }

        sched?.Dispose();
        sched = new TickScheduler(() =>
        {
            if (Controller.TryGetElementByName("CloneCleave", out var leftCleave))
            {
                leftCleave.Enabled = false;
            }
            if (Controller.TryGetElementByName("CloneCleave", out var rightCleave))
            {
                rightCleave.Enabled = true;
                rightCleave.refX = jumpTargetPosition.X;
                rightCleave.refY = jumpTargetPosition.Z;
                rightCleave.offX = jumpTargetPosition.X + 30f;
                rightCleave.offY = jumpTargetPosition.Z;
            }

            sched = new TickScheduler(() =>
            {
                if (Controller.TryGetElementByName("CloneCleave", out var rightCleave))
                {
                    rightCleave.Enabled = false;
                }
            }, 2000);
        }, 7000);
    }

    private void HandleCloneCleaves()
    {
        if (jumpTargetPosition.Z > 100)
        {
            if (LeftFirst)
            {
                LeftCloneCleave();
            }
            else
            {
                RightCloneCleave();
            }
        }
        else
        {
            if (LeftFirst)
            {
                RightCloneCleave();
            }
            else
            {
                LeftCloneCleave();
            }
        }
    }

    private void LeftCleaveFirst(uint t1, uint t2, uint dataID1, uint dataID2)
    {
        if (Controller.TryGetElementByName("Cleave", out var leftCleave))
        {
            leftCleave.Enabled = true;
            leftCleave.refActorDataID = dataID1;
            leftCleave.onlyVisible = true;
            leftCleave.AdditionalRotation = 4.712389f;
        }

        sched?.Dispose();
        sched = new TickScheduler(() =>
        {
            if (Controller.TryGetElementByName("Cleave", out var leftCleave))
            {
                leftCleave.Enabled = false;
            }
            if (Controller.TryGetElementByName("Cleave", out var rightCleave))
            {
                rightCleave.Enabled = true;
                rightCleave.refActorDataID = dataID2;
                rightCleave.onlyVisible = true;
                rightCleave.AdditionalRotation = 1.5707964f;
            }

            sched = new TickScheduler(() =>
            {
                if (Controller.TryGetElementByName("Cleave", out var rightCleave))
                {
                    rightCleave.Enabled = false;
                }
            }, t2);
        }, t1);
    }

    private void RightCleaveFirst(uint t1, uint t2, uint dataID1, uint dataID2)
    {
        if (Controller.TryGetElementByName("Cleave", out var rightCleave))
        {
            rightCleave.Enabled = true;
            rightCleave.refActorDataID = dataID1;
            rightCleave.onlyVisible = true;
            rightCleave.AdditionalRotation = 1.5707964f;
        }

        sched?.Dispose();
        sched = new TickScheduler(() =>
        {
            if (Controller.TryGetElementByName("Cleave", out var rightCleave))
            {
                rightCleave.Enabled = false;
            }
            if (Controller.TryGetElementByName("Cleave", out var leftCleave))
            {
                leftCleave.Enabled = true;
                leftCleave.refActorDataID = dataID2;
                leftCleave.onlyVisible = true;
                leftCleave.AdditionalRotation = 4.712389f;
            }

            sched = new TickScheduler(() =>
            {
                if (Controller.TryGetElementByName("Cleave", out var leftCleave))
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
            if (clonePositions.Count == 7)
            {
                ShowArrows(6);
            }
            else if (clonePositions.Count == 8)
            {
                ShowArrows(7);
            }
        }
        if (string.IsNullOrEmpty(movement) && !string.IsNullOrEmpty(attack))
        {
            if (movement == "" && attack == "Cleave")
            {
                HandleCloneAttacks();
            }
        }
    }

    private void ShowArrows(int cloneNumber)
    {
        if (clonePositions[cloneNumber].Z > 100)
        {
            if (movement == "Left")
            {
                if (Controller.TryGetElementByName($"SArrowLeft", out var arrow))
                {
                    arrow.Enabled = true;
                }
            }
            else if (movement == "Right")
            {
                if (Controller.TryGetElementByName($"SArrowRight", out var arrow))
                {
                    arrow.Enabled = true;
                }
            }
        }
        else
        {
            if (movement == "Left")
            {
                if (Controller.TryGetElementByName($"NArrowLeft", out var arrow))
                {
                    arrow.Enabled = true;
                }
            }
            else if (movement == "Right")
            {
                if (Controller.TryGetElementByName($"NArrowRight", out var arrow))
                {
                    arrow.Enabled = true;
                }
            }
        }
    }

    private void HideArrows(int cloneNumber)
    {
        if (clonePositions[cloneNumber].Z > 100)
        {
            if (Controller.TryGetElementByName($"SArrowLeft", out var arrow1))
            {
                arrow1.Enabled = false;
            }
            if (Controller.TryGetElementByName($"SArrowRight", out var arrow2))
            {
                arrow2.Enabled = false;
            }
        }
        else
        {
            if (Controller.TryGetElementByName($"NArrowLeft", out var arrow1))
            {
                arrow1.Enabled = false;
            }
            if (Controller.TryGetElementByName($"NArrowRight", out var arrow2))
            {
                arrow2.Enabled = false;
            }
        }
    }

    private void ShowCleaves()
    {
        if (LeftFirst)
        {
            if (Controller.TryGetElementByName("Cleave", out var e))
            {
                e.Enabled = true;
                e.refActorDataID = 17193;
                e.AdditionalRotation = 4.712389f;
            }
        }
        else
        {
            if (Controller.TryGetElementByName("Cleave", out var e))
            {
                e.Enabled = true;
                e.refActorDataID = 17193;
                e.AdditionalRotation = 1.5707964f;
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
            if (Controller.TryGetElementByName($"PlayerConeBait{i}", out var e1))
            {
                e1.Enabled = false;
            }

            if (Controller.TryGetElementByName($"CloneCone{i}", out var e2))
            {
                e2.Enabled = false;
            }
        }
    }

    public override void OnUpdate()
    {
        if (clonePositions.Count <= 8 && (IsRegular || IsLeapingCone))
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

        if (IsChipper)
        {
            HandleCloneCones();
        }
        else
        {
            var boss = BlackCat;
            if (boss == null || !Svc.Objects.Any(x => x.DataId == 17193 && x.IsTargetable))
            {
                IsChipper = false;
                HideCones();
            }
        }

        if (Controller.TryGetElementByName($"Snapshots", out var s))
        {
            s.Enabled = true;
        }

        if (Controller.TryGetElementByName($"NailchipperAOE", out var e))
        {
            e.Enabled = true;
        }
        if (clonePositions.Count == 9)
        {
            sched = new TickScheduler(() =>
            {
                HideArrows(8);
            }, 16000);
        }
        if (clonePositions.Count == 10)
        {
            sched = new TickScheduler(() =>
            {
                HideArrows(9);
            }, 16000);
        }
        if (clonePositions.Count == 0)
        {
            clonePositions.Clear();
            jumpTargetPosition = new Vector3(0, 0, 0);
            HideArrows(8);
            HideArrows(9);
        }
    }

    public override void OnCombatEnd()
    {
        IsRegular = false;
        IsLeapingCone = false;
        IsLeapingCleave = false;
        LeftFirst = false;
        RightFirst = false;
        HideCones();
        sched?.Dispose();
        clonePositions.Clear();
        jumpTargetPosition = new Vector3(0, 0, 0);
        HideArrows(8);
        HideArrows(9);
    }

    public override void OnCombatStart()
    {
        this.OnCombatEnd();
    }
}
