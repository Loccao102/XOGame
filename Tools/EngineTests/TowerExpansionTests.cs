using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using XOPlayable.Core;

namespace XOPlayable.Tests
{
    public class XOTowerExpansionTests
    {
        private static XOCaroEngine New(int size = 15)
        {
            var e = new XOCaroEngine(size, 5);
            e.Initialize(size, 5, spawnSpecialTiles: false, variant: XOVariantRule.ClassicCaro);
            return e;
        }

        private static void P(XOCaroEngine e, int r, int c)
            => Assert.IsTrue(e.PlacePiece(r, c), $"PlacePiece({r},{c}) failed");

        private static XOBotProfile Boss(int elo = 2000) => new XOBotProfile { eloRating = elo, baseDifficulty = AIDifficulty.Boss };

        [Test] public void Catalog_HasFifteenSequentialFloors_WithRisingBossElo()
        {
            var f = XORoguelikeCatalog.FloorBlinds;
            Assert.AreEqual(15, f.Length);
            Assert.AreEqual(f.Length, XORoguelikeCatalog.FloorCount);
            for (int i = 0; i < f.Length; i++) Assert.AreEqual(i + 1, f[i].floorNumber);
            for (int i = 5; i < f.Length; i++) Assert.IsTrue(f[i].bossElo >= f[i - 1].bossElo, $"floor {i + 1} elo drops");
            for (int i = 5; i < f.Length; i++) Assert.AreEqual(AIDifficulty.Boss, f[i].difficulty, $"floor {i + 1}");
        }

        [Test] public void Catalog_EveryRelicTypeHasUniqueData()
        {
            var ids = new HashSet<string>();
            foreach (CaroRelicType t in Enum.GetValues(typeof(CaroRelicType)))
            {
                var d = XORoguelikeCatalog.GetRelicData(t);
                Assert.IsTrue(d != null, t.ToString());
                Assert.IsTrue(ids.Add(d.id), "duplicate id " + d.id);
            }
        }

        [Test] public void BossAI_TakesImmediateWin()
        {
            var e = New();
            // X scatters, O builds four in a row at row 7 (cols 3-6); O to move.
            P(e, 0, 0); P(e, 7, 3); P(e, 0, 2); P(e, 7, 4); P(e, 0, 4); P(e, 7, 5); P(e, 14, 14); P(e, 7, 6); P(e, 14, 12);
            XOCellPos m = e.CalculateAIMove(CellPiece.O, AIDifficulty.Boss, Boss());
            Assert.IsTrue((m.x == 7 && (m.y == 2 || m.y == 7)), $"expected win at (7,2)/(7,7), got ({m.x},{m.y})");
        }

        [Test] public void BossAI_BlocksOpenThree()
        {
            var e = New();
            P(e, 7, 6); P(e, 0, 0); P(e, 7, 7); P(e, 0, 2); P(e, 7, 5);   // X open three, O to move
            XOCellPos m = e.CalculateAIMove(CellPiece.O, AIDifficulty.Boss, Boss());
            Assert.IsTrue(m.x == 7 && (m.y == 4 || m.y == 8 || m.y == 3 || m.y == 9), $"got ({m.x},{m.y})");
        }

        [Test] public void BossAI_BlocksImmediateLoss()
        {
            var e = New();
            P(e, 7, 3); P(e, 0, 0); P(e, 7, 4); P(e, 0, 2); P(e, 7, 5); P(e, 14, 14); P(e, 7, 6); P(e, 14, 12);   // X four, O to move
            XOCellPos m = e.CalculateAIMove(CellPiece.O, AIDifficulty.Boss, Boss());
            Assert.IsTrue(m.x == 7 && (m.y == 2 || m.y == 7), $"got ({m.x},{m.y})");
        }

        [Test] public void BossAI_AlwaysLegal_OnEveryVariant()
        {
            foreach (XOVariantRule v in Enum.GetValues(typeof(XOVariantRule)))
            {
                var e = new XOCaroEngine(10, 5);
                e.Initialize(10, 5, spawnSpecialTiles: false, variant: v);
                e.SetRandomSeed(7);
                for (int i = 0; i < 40 && e.State == MatchState.Playing; i++)
                {
                    XOCellPos m = e.CalculateAIMove(e.CurrentTurn, AIDifficulty.Boss, Boss(2200));
                    Assert.IsTrue(m.IsValid, $"{v} ply {i}: invalid");
                    Assert.IsTrue(e.PlacePiece(m.x, m.y), $"{v} ply {i}: illegal ({m.x},{m.y})");
                }
            }
        }

        [Test] public void BossAI_BeatsHardAi_AsFirstMover()
        {
            int wins = 0;
            for (int g = 0; g < 4; g++)
            {
                var e = New();
                e.SetRandomSeed(100 + g);
                for (int i = 0; i < 225 && e.State == MatchState.Playing; i++)
                {
                    bool boss = e.CurrentTurn == CellPiece.X;
                    XOCellPos m = boss ? e.CalculateAIMove(CellPiece.X, AIDifficulty.Boss, Boss(2000)) : e.CalculateAIMove(CellPiece.O, AIDifficulty.Hard);
                    if (!m.IsValid || !e.PlacePiece(m.x, m.y)) break;
                }
                if (e.State == MatchState.XWon) wins++;
            }
            Assert.IsTrue(wins >= 3, $"boss won only {wins}/4 vs Hard AI");
        }

        [Test] public void BossKit_SkillCadence_AndDoubleMoves()
        {
            var b = new TowerFloorBlind { bossSkillMaxUses = 2, bossSkillCooldown = 8, botEquipment = BotEquipmentType.ThunderHammer, doubleMoveEvery = 6 };
            Assert.IsTrue(XOBossKit.CanUseSkill(b, 0, 0, 4));
            Assert.IsFalse(XOBossKit.CanUseSkill(b, 1, 10, 20), "cooldown is 8 boss moves = 16 plies");
            Assert.IsTrue(XOBossKit.CanUseSkill(b, 1, 10, 26));
            Assert.IsFalse(XOBossKit.CanUseSkill(b, 2, 10, 99), "max uses reached");
            Assert.IsTrue(XOBossKit.IsDoubleMoveDue(b, 12));
            Assert.IsFalse(XOBossKit.IsDoubleMoveDue(b, 8));
            Assert.AreEqual(2200, XOBossKit.MakeProfile(new TowerFloorBlind { bossElo = 2200, difficulty = AIDifficulty.Boss }).eloRating);
        }

        [Test] public void Counterstrike_TriggersOncePerMatch_OnlyForFours()
        {
            var e = New();
            e.IsTowerClimb = true;
            e.RelicQuery = t => t == CaroRelicType.Counterstrike;
            Assert.IsFalse(e.TryCheckCounterstrike(14000), "open three is not enough");
            Assert.IsTrue(e.TryCheckCounterstrike(22000));
            Assert.IsFalse(e.TryCheckCounterstrike(22000), "once per match");
        }

        [Test] public void EveryFloor_HasAHandLimit_SoTowerCannotDraw()
        {
            var run = new XORoguelikeRunState();
            foreach (var f in XORoguelikeCatalog.FloorBlinds)
            {
                int limit = run.GetMoveLimit(f);
                Assert.IsTrue(limit > 0 && limit < 100, $"floor {f.floorNumber} limit {limit}");
            }
            Assert.AreEqual(XORoguelikeCatalog.DefaultMoveLimit, run.GetMoveLimit(new TowerFloorBlind { maxPlayerMoves = 0 }));
            Assert.AreEqual(28, run.GetMoveLimit(new TowerFloorBlind { maxPlayerMoves = 28 }));
        }

        [Test] public void Omens_AreDeterministic_AndSkipEarlyFloors()
        {
            for (int seed = 10000; seed < 10200; seed++)
            {
                Assert.AreEqual(BossOmen.None, XOBossOmens.Roll(seed, 1, XORoguelikeCatalog.GetFloorBlind(1)));
                Assert.AreEqual(BossOmen.None, XOBossOmens.Roll(seed, 2, XORoguelikeCatalog.GetFloorBlind(2)));
                var b = XORoguelikeCatalog.GetFloorBlind(9);
                Assert.AreEqual(XOBossOmens.Roll(seed, 9, b), XOBossOmens.Roll(seed, 9, b));
            }
        }

        [Test] public void Omens_RespectApplicability_AndRateRisesWithDepth()
        {
            int lateHits = 0, earlyHits = 0, n = 600;
            for (int seed = 20000; seed < 20000 + n; seed++)
            {
                var noKit = new TowerFloorBlind { botEquipment = BotEquipmentType.None };
                BossOmen o = XOBossOmens.Roll(seed, 8, noKit);
                Assert.IsTrue(o != BossOmen.Relentless && o != BossOmen.Hasty, "kit-less boss got a kit omen");

                var frost = new TowerFloorBlind { botEquipment = BotEquipmentType.FrostStaff };
                Assert.IsTrue(XOBossOmens.Roll(seed, 8, frost) != BossOmen.Hasty, "Hasty needs double moves");

                if (XOBossOmens.Roll(seed, 15, XORoguelikeCatalog.GetFloorBlind(15)) != BossOmen.None) lateHits++;
                if (XOBossOmens.Roll(seed, 3, XORoguelikeCatalog.GetFloorBlind(3)) != BossOmen.None) earlyHits++;
            }
            Assert.IsTrue(lateHits > earlyHits * 1.5f, $"late {lateHits} vs early {earlyHits}");
            Assert.IsTrue(lateHits > n * 0.7f, $"floor 15 omen rate {lateHits}/{n}");
        }

        [Test] public void Omens_ModifyACopy_NeverTheCatalog()
        {
            var cat = XORoguelikeCatalog.GetFloorBlind(10);
            int elo = cat.bossElo, coins = cat.coinReward, uses = cat.bossSkillMaxUses;

            var elite = XOBossOmens.Apply(cat, BossOmen.Elite);
            Assert.AreEqual(elo + 200, elite.bossElo);
            Assert.AreEqual(coins * 3 / 2, elite.coinReward);
            Assert.AreEqual(uses + 1, XOBossOmens.Apply(cat, BossOmen.Relentless).bossSkillMaxUses);
            Assert.AreEqual(cat.doubleMoveEvery - 2, XOBossOmens.Apply(cat, BossOmen.Hasty).doubleMoveEvery);
            Assert.AreEqual(cat.maxPlayerMoves - 6, XOBossOmens.Apply(cat, BossOmen.ShortHands).maxPlayerMoves);
            Assert.AreEqual(coins * 2, XOBossOmens.Apply(cat, BossOmen.Wealthy).coinReward);

            Assert.AreEqual(elo, cat.bossElo);
            Assert.AreEqual(coins, cat.coinReward);
            Assert.AreEqual(uses, cat.bossSkillMaxUses);
            Assert.IsTrue(ReferenceEquals(cat, XOBossOmens.Apply(cat, BossOmen.None)));
        }

        [Test] public void ShortHands_NeverDropsBelowSixteen_AndUsesDefaultWhenUnset()
        {
            var b = new TowerFloorBlind { maxPlayerMoves = 0 };
            Assert.AreEqual(XORoguelikeCatalog.DefaultMoveLimit - 6, XOBossOmens.Apply(b, BossOmen.ShortHands).maxPlayerMoves);
            var tight = new TowerFloorBlind { maxPlayerMoves = 18 };
            Assert.AreEqual(16, XOBossOmens.Apply(tight, BossOmen.ShortHands).maxPlayerMoves);
        }

        [Test] public void RunState_ActiveBlind_IsCachedPerFloor_AndCarriesOmenText()
        {
            var run = new XORoguelikeRunState();
            run.StartNewRun(54321);
            run.CurrentFloor = 12;
            var a = run.GetActiveBlind();
            Assert.IsTrue(ReferenceEquals(a, run.GetActiveBlind()));
            Assert.AreEqual(a.blindName.Contains("Omen"), run.GetActiveOmen() != BossOmen.None);
            Assert.AreEqual(12, a.floorNumber);
        }

        [Test] public void BossTuning_GrowsMonotonicallyWithElo()
        {
            XOBossAI.Tuning prev = XOBossAI.TuneForElo(1000);
            for (int elo = 1100; elo <= 2800; elo += 100)
            {
                XOBossAI.Tuning t = XOBossAI.TuneForElo(elo);
                Assert.IsTrue(t.VcfDepth >= prev.VcfDepth && t.Breadth >= prev.Breadth && t.MoveNodes >= prev.MoveNodes, $"elo {elo}");
                Assert.IsTrue(t.Blunder <= prev.Blunder, $"elo {elo} blunder rises");
                prev = t;
            }
            Assert.IsTrue(XOBossAI.TuneForElo(1250).Blunder > 0.2f);
            Assert.AreEqual(0f, XOBossAI.TuneForElo(2450).Blunder);
            Assert.IsFalse(XOBossAI.TuneForElo(1250).DefendVcf);
            Assert.IsTrue(XOBossAI.TuneForElo(2000).ThreatSpace);
        }

        [Test] public void WeakBoss_NeverMissesAWinOrABlock_EvenWhenSlipping()
        {
            for (int seed = 1; seed <= 40; seed++)
            {
                var win = New();
                win.SetRandomSeed(seed);
                P(win, 0, 0); P(win, 7, 3); P(win, 0, 2); P(win, 7, 4); P(win, 0, 4); P(win, 7, 5); P(win, 14, 14); P(win, 7, 6); P(win, 14, 12);
                XOCellPos m = win.CalculateAIMove(CellPiece.O, AIDifficulty.Boss, Boss(1250));
                Assert.IsTrue(m.x == 7 && (m.y == 2 || m.y == 7), $"seed {seed}: no win ({m.x},{m.y})");

                var block = New();
                block.SetRandomSeed(seed);
                P(block, 7, 3); P(block, 0, 0); P(block, 7, 4); P(block, 0, 2); P(block, 7, 5); P(block, 14, 14); P(block, 7, 6); P(block, 14, 12);
                XOCellPos b = block.CalculateAIMove(CellPiece.O, AIDifficulty.Boss, Boss(1250));
                Assert.IsTrue(b.x == 7 && (b.y == 2 || b.y == 7), $"seed {seed}: no block ({b.x},{b.y})");
            }
        }

        [Test] public void GravityFloors_HaveRoomToWin()
        {
            foreach (var f in XORoguelikeCatalog.FloorBlinds)
                if (f.variantRule == XOVariantRule.GravityDrop)
                    Assert.IsTrue(XORoguelikeRunState.ComputeMoveLimit(f, false) >= 60, $"floor {f.floorNumber} gravity needs >= 60 hands");
        }

        [Test] public void FreezeCellFor_BlocksOnlyTheNamedSide_AndThaws()
        {
            var e = New();
            Assert.IsTrue(e.FreezeCellFor(7, 7, 2, CellPiece.X));
            Assert.IsFalse(e.CanPlaceForPiece(7, 7, CellPiece.X));
            Assert.IsTrue(e.CanPlaceForPiece(7, 7, CellPiece.O));
            P(e, 0, 0); P(e, 0, 2);   // two placements tick the freeze down
            Assert.IsTrue(e.CanPlaceForPiece(7, 7, CellPiece.X), "thawed");
        }

        private static XOCaroEngine OpenThreeForX(bool thaw)
        {
            var e = New();
            e.IsTowerClimb = true;
            e.RelicQuery = r => thaw && r == CaroRelicType.ThawCharm;
            P(e, 7, 6); P(e, 0, 0); P(e, 7, 7); P(e, 0, 2); P(e, 7, 5); // X open three, O to move
            return e;
        }

        private static XOCellPos FindFrozen(XOCaroEngine e)
        {
            for (int r = 0; r < e.BoardSize; r++)
                for (int c = 0; c < e.BoardSize; c++)
                    if (e.FrozenTurns[r, c] > 0) return new XOCellPos(r, c);
            return XOCellPos.Invalid;
        }

        [Test] public void BossFreeze_Classic_LocksBothSides()
        {
            var e = OpenThreeForX(false);
            Assert.IsTrue(e.TryExecuteBotCounterFreeze(new XOBotProfile { counterSkill = BotCounterSkill.CryoFreeze }, out XOCellPos cell));
            Assert.IsTrue(cell.IsValid);
            Assert.AreEqual(XOCaroSkillsAndRelics.BossFreezeTurns, e.FrozenTurns[cell.x, cell.y]);
            Assert.IsFalse(e.CanPlaceForPiece(cell.x, cell.y, CellPiece.X));
            Assert.IsFalse(e.CanPlaceForPiece(cell.x, cell.y, CellPiece.O));
        }

        [Test] public void BossFreeze_WithThawCharm_LocksOnlyTheBoss()
        {
            var e = OpenThreeForX(true);
            Assert.IsTrue(e.TryExecuteBotCounterFreeze(new XOBotProfile { counterSkill = BotCounterSkill.CryoFreeze }, out XOCellPos cell));
            Assert.IsTrue(e.CanPlaceForPiece(cell.x, cell.y, CellPiece.X), "player may still play the frozen cell");
            Assert.IsFalse(e.CanPlaceForPiece(cell.x, cell.y, CellPiece.O), "boss is locked out");
        }


        private static XOCaroEngine TowerEngine(params CaroRelicType[] relics)
        {
            var set = new HashSet<CaroRelicType>(relics);
            var e = new XOCaroEngine(15, 5);
            e.IsTowerClimb = true;
            e.RelicQuery = r => set.Contains(r);
            e.RelicConsumed = r => set.Remove(r);
            e.Initialize(15, 5, spawnSpecialTiles: false, variant: XOVariantRule.ClassicCaro, isTowerClimb: true);
            return e;
        }

        private static void BossWinsRow(XOCaroEngine e)
        {
            P(e, 0, 0); P(e, 7, 3); P(e, 0, 2); P(e, 7, 4); P(e, 0, 4); P(e, 7, 5); P(e, 14, 14); P(e, 7, 6); P(e, 14, 12);
            P(e, 7, 7);
        }

        [Test] public void RedTalisman_ErasesTheWinningLine_AndIsSpent()
        {
            int spent = 0;
            var e = TowerEngine(CaroRelicType.RedTalisman);
            e.RelicConsumed = r => spent++;
            BossWinsRow(e);
            Assert.AreEqual(MatchState.OWon, e.State);

            Assert.IsTrue(e.TryExecuteRedTalisman(out List<XOCellPos> erased));
            Assert.AreEqual(5, erased.Count);
            Assert.AreEqual(MatchState.Playing, e.State);
            Assert.AreEqual(CellPiece.X, e.CurrentTurn);
            foreach (XOCellPos c in erased) Assert.AreEqual(CellPiece.Empty, e.Board[c.x, c.y]);
            Assert.AreEqual(1, spent);
            Assert.IsFalse(e.TryExecuteRedTalisman(out _), "match is no longer lost");
        }

        [Test] public void RedTalisman_DoesNothingWithoutTheRelicOrWithoutALoss()
        {
            var e = TowerEngine();
            BossWinsRow(e);
            Assert.IsFalse(e.TryExecuteRedTalisman(out _));
            Assert.AreEqual(MatchState.OWon, e.State);

            var f = TowerEngine(CaroRelicType.RedTalisman);
            P(f, 7, 7);
            Assert.IsFalse(f.TryExecuteRedTalisman(out _));
        }

        // Places X stones on scattered cells (never in a line) with O answering in between, calling `after` once per X stone.
        private static void ScatterX(XOCaroEngine e, int stones, System.Action<int> afterXStone)
        {
            for (int i = 0; i < stones; i++)
            {
                P(e, i, (4 * i) % 15);
                afterXStone(i + 1);
                if (e.CurrentTurn == CellPiece.O) P(e, i, (4 * i + 2) % 15);
            }
        }

        [Test] public void OpeningGambit_GrantsOneExtraTurnOnTheThirdStone()
        {
            var e = TowerEngine(CaroRelicType.OpeningGambit);
            var turnAfter = new List<CellPiece>();
            ScatterX(e, 5, n => turnAfter.Add(e.CurrentTurn));
            Assert.AreEqual(CellPiece.O, turnAfter[0]);
            Assert.AreEqual(CellPiece.O, turnAfter[1]);
            Assert.AreEqual(CellPiece.X, turnAfter[2], "3rd stone: extra turn");
            Assert.AreEqual(CellPiece.O, turnAfter[3], "only once per match");
            Assert.AreEqual(CellPiece.O, turnAfter[4]);
        }

        [Test] public void GlassCannon_EveryTwelfthStoneGrantsAnExtraTurn()
        {
            var e = TowerEngine(CaroRelicType.GlassCannon);
            var turnAfter = new List<CellPiece>();
            ScatterX(e, 12, n => turnAfter.Add(e.CurrentTurn));
            for (int n = 1; n < 12; n++) Assert.AreEqual(CellPiece.O, turnAfter[n - 1], $"stone {n}");
            Assert.AreEqual(CellPiece.X, turnAfter[11], "12th stone grants the extra turn");
        }

        [Test] public void FateDice_AddsThreeGoldTilesNearTheCenter()
        {
            int Gold(XOCaroEngine e)
            {
                int n = 0, mid = e.BoardSize / 2;
                for (int r = mid - 3; r <= mid + 3; r++)
                    for (int c = mid - 3; c <= mid + 3; c++)
                        if (e.SpecialTiles[r, c] == SpecialTileType.GoldBonus) n++;
                return n;
            }
            XOCaroEngine Make(bool dice)
            {
                var e = new XOCaroEngine(15, 5);
                e.IsTowerClimb = true;
                e.RelicQuery = r => dice && r == CaroRelicType.FateDice;
                e.Initialize(15, 5, spawnSpecialTiles: true, variant: XOVariantRule.ClassicCaro, specialTilesSeed: 321, floorNumber: 4, isTowerClimb: true);
                return e;
            }
            Assert.AreEqual(Gold(Make(false)) + 3, Gold(Make(true)));
        }

        [Test] public void SiegeRam_ShattersOneRockPerMatch_AndOnlyForThePlayer()
        {
            var e = TowerEngine(CaroRelicType.SiegeRam);
            e.SpecialTiles[7, 7] = SpecialTileType.RockObstacle;
            e.SpecialTiles[9, 9] = SpecialTileType.RockObstacle;
            Assert.IsTrue(e.CanPlaceForPiece(7, 7, CellPiece.X));
            Assert.IsFalse(e.CanPlaceForPiece(7, 7, CellPiece.O), "boss cannot use the ram");
            P(e, 7, 7);
            Assert.AreEqual(CellPiece.X, e.Board[7, 7]);
            Assert.AreEqual(SpecialTileType.None, e.SpecialTiles[7, 7]);
            Assert.IsFalse(e.CanPlaceForPiece(9, 9, CellPiece.X), "once per match");

            var plain = TowerEngine();
            plain.SpecialTiles[7, 7] = SpecialTileType.RockObstacle;
            Assert.IsFalse(plain.CanPlaceForPiece(7, 7, CellPiece.X));
        }

        [Test] public void SingleUseRelics_AreSpentAndModifyTheRun()
        {
            var run = new XORoguelikeRunState();
            run.StartNewRun(11111);
            foreach (var r in new[] { CaroRelicType.CrimsonWard, CaroRelicType.SecondWind, CaroRelicType.FortuneScroll })
                Assert.IsTrue(run.EquipRelic(r), r.ToString());

            var blind = new TowerFloorBlind { maxPlayerMoves = 30, coinReward = 100 };
            Assert.AreEqual(30, run.GetMoveLimit(blind));
            Assert.IsTrue(run.TryUseSecondWind());
            Assert.AreEqual(40, run.GetMoveLimit(blind));
            Assert.IsFalse(run.TryUseSecondWind(), "spent");

            Assert.AreEqual(300, run.GetFloorReward(blind));
            Assert.AreEqual(100, run.GetFloorReward(blind), "scroll spent");

            int lives = run.Lives;
            Assert.IsFalse(run.LoseHeart());
            Assert.AreEqual(lives, run.Lives, "Crimson Ward absorbed the loss");
            Assert.IsTrue(run.ConsumeHeartWardNotice());
            Assert.IsFalse(run.ConsumeHeartWardNotice(), "notice shows once");
            run.LoseHeart();
            Assert.AreEqual(lives - 1, run.Lives, "ward is gone");

            run.ResetMatchState();
            Assert.AreEqual(30, run.GetMoveLimit(blind), "bonus hands last one match");
        }

        [Test] public void Haggler_CursedCoin_GlassCannon_ChangeNumbers()
        {
            var run = new XORoguelikeRunState();
            run.StartNewRun(22222);
            var relic = new CaroRelic { cost = 50 };
            Assert.AreEqual(50, run.GetRelicPrice(relic));
            run.EquipRelic(CaroRelicType.Haggler);
            Assert.AreEqual(40, run.GetRelicPrice(relic));

            run.CurrentFloor = 6;
            TowerFloorBlind before = run.GetActiveBlind();
            int elo = before.bossElo, coins = before.coinReward;
            run.EquipRelic(CaroRelicType.CursedCoin);
            TowerFloorBlind after = run.GetActiveBlind();
            Assert.AreEqual(elo + 150, after.bossElo);
            Assert.AreEqual(coins * 3 / 2, after.coinReward);

            Assert.AreEqual(37, XORoguelikeRunState.ComputeMoveLimit(new TowerFloorBlind(), false, true));
            Assert.AreEqual(12, XORoguelikeRunState.ComputeMoveLimit(new TowerFloorBlind { maxPlayerMoves = 16 }, false, true), "never below 12");
        }

        [Test] public void NewRelics_AreInTheCatalog_WithSingleUseMarked()
        {
            foreach (var t in new[] { CaroRelicType.RedTalisman, CaroRelicType.CrimsonWard, CaroRelicType.SecondWind, CaroRelicType.FortuneScroll })
                Assert.IsTrue(XORoguelikeCatalog.GetRelicData(t).description.StartsWith("SINGLE USE"), t.ToString());
            Assert.IsTrue(Enum.GetValues(typeof(CaroRelicType)).Length >= 25);
        }

        private static System.Func<CaroRelicType, bool> Has(params CaroRelicType[] r)
        {
            var set = new HashSet<CaroRelicType>(r);
            return set.Contains;
        }

        [Test] public void Archetypes_EveryRelicBelongsToOne_AndEachGroupHasDepth()
        {
            var counts = new Dictionary<RelicArchetype, int>();
            foreach (CaroRelicType t in Enum.GetValues(typeof(CaroRelicType)))
            {
                RelicArchetype a = XORelicArchetypes.Of(t);
                Assert.IsTrue(a != RelicArchetype.None, t + " has no archetype");
                counts[a] = counts.ContainsKey(a) ? counts[a] + 1 : 1;
            }
            foreach (RelicArchetype a in new[] { RelicArchetype.Attack, RelicArchetype.Defence, RelicArchetype.Economy, RelicArchetype.Terrain })
                Assert.IsTrue(counts[a] >= 5, a + " has only " + counts[a]);
        }

        [Test] public void Archetypes_SetBonusesNeedThreeOfAKind()
        {
            Assert.IsFalse(XORelicArchetypes.HasSet(Has(CaroRelicType.TwinThreat, CaroRelicType.Counterstrike), RelicArchetype.Attack));
            Assert.IsTrue(XORelicArchetypes.HasSet(Has(CaroRelicType.TwinThreat, CaroRelicType.Counterstrike, CaroRelicType.GlassCannon), RelicArchetype.Attack));
            Assert.AreEqual(0, XORelicArchetypes.HandsBonus(Has(CaroRelicType.TwinThreat, CaroRelicType.Counterstrike), 0));
            Assert.AreEqual(4, XORelicArchetypes.HandsBonus(Has(CaroRelicType.TwinThreat, CaroRelicType.Counterstrike, CaroRelicType.GlassCannon), 0));
        }

        [Test] public void Archetypes_ThemeRelicsScaleWithTheirGroup()
        {
            Assert.AreEqual(2, XORelicArchetypes.HandsBonus(Has(CaroRelicType.WarDrum), 0));
            // War Drum + Hourglass + Twin Threat: 3 Attack relics -> set (+4) and +2 each (+6)
            Assert.AreEqual(10, XORelicArchetypes.HandsBonus(Has(CaroRelicType.WarDrum, CaroRelicType.Hourglass, CaroRelicType.TwinThreat), 0));
            Assert.AreEqual(8, XORelicArchetypes.HandsBonus(Has(CaroRelicType.EmberBrand), 99), "Ember is capped at 8");

            Assert.AreEqual(-40, XORelicArchetypes.BossEloDelta(Has(CaroRelicType.BulwarkIdol)));
            Assert.AreEqual(-220, XORelicArchetypes.BossEloDelta(Has(CaroRelicType.BulwarkIdol, CaroRelicType.ThawCharm, CaroRelicType.CrownOfOmni)));
            Assert.AreEqual(150, XORelicArchetypes.BossEloDelta(Has(CaroRelicType.CursedCoin)));

            Assert.AreEqual(10, XORelicArchetypes.RewardPercent(Has(CaroRelicType.MerchantCrown)));
            Assert.AreEqual(55, XORelicArchetypes.RewardPercent(Has(CaroRelicType.MerchantCrown, CaroRelicType.KnightStep, CaroRelicType.AlchemistStone)));

            Assert.AreEqual(1, XORelicArchetypes.BonusGoldTiles(Has(CaroRelicType.CartographerEye)));
            Assert.AreEqual(8, XORelicArchetypes.BonusGoldTiles(Has(CaroRelicType.CartographerEye, CaroRelicType.FateDice, CaroRelicType.MinePlanter)));
        }

        [Test] public void ModifyBlind_CopiesAndClampsElo()
        {
            var b = new TowerFloorBlind { bossElo = 650, coinReward = 100 };
            Assert.IsTrue(ReferenceEquals(b, XORelicArchetypes.ModifyBlind(b, Has())), "no relics: same object");
            var m = XORelicArchetypes.ModifyBlind(b, Has(CaroRelicType.BulwarkIdol, CaroRelicType.ThawCharm, CaroRelicType.CrownOfOmni));
            Assert.AreEqual(600, m.bossElo, "never below 600");
            Assert.AreEqual(650, b.bossElo);
        }

        [Test] public void RunState_ActiveBlindFollowsRelicSynergies()
        {
            var run = new XORoguelikeRunState();
            run.StartNewRun(33333);
            run.CurrentFloor = 8;
            int elo = run.GetActiveBlind().bossElo, coins = run.GetActiveBlind().coinReward;
            run.EquipRelic(CaroRelicType.MerchantCrown);
            run.EquipRelic(CaroRelicType.KnightStep);
            run.EquipRelic(CaroRelicType.AlchemistStone);
            Assert.AreEqual(coins * 155 / 100, run.GetActiveBlind().coinReward, "Economy set + Merchant's Crown");
            Assert.AreEqual(elo, run.GetActiveBlind().bossElo);
        }

        [Test] public void EmberBrand_GrowsPerFloor_AndIsSnuffedByALostHeart()
        {
            var run = new XORoguelikeRunState();
            run.StartNewRun(44444);
            run.EquipRelic(CaroRelicType.EmberBrand);
            run.CurrentFloor = 2;
            run.CurrentFloor = 3;
            run.CurrentFloor = 4;
            Assert.AreEqual(3, run.EmberGrowth);
            run.CurrentFloor = 4;
            Assert.AreEqual(3, run.EmberGrowth, "re-setting the same floor does not grow it");
            run.LoseHeart();
            Assert.AreEqual(0, run.EmberGrowth);

            var warded = new XORoguelikeRunState();
            warded.StartNewRun(55555);
            warded.EquipRelic(CaroRelicType.EmberBrand);
            warded.EquipRelic(CaroRelicType.CrimsonWard);
            warded.CurrentFloor = 2;
            warded.LoseHeart();
            Assert.AreEqual(1, warded.EmberGrowth, "the ward absorbed the loss, the brand survives");
        }

        [Test] public void Terrain_Cartographer_AddsGoldTilesAtMatchStart()
        {
            int Gold(XOCaroEngine e)
            {
                int n = 0, mid = e.BoardSize / 2;
                for (int r = mid - 3; r <= mid + 3; r++)
                    for (int c = mid - 3; c <= mid + 3; c++)
                        if (e.SpecialTiles[r, c] == SpecialTileType.GoldBonus) n++;
                return n;
            }
            XOCaroEngine Make(params CaroRelicType[] relics)
            {
                var set = new HashSet<CaroRelicType>(relics);
                var e = new XOCaroEngine(15, 5);
                e.IsTowerClimb = true;
                e.RelicQuery = set.Contains;
                e.Initialize(15, 5, spawnSpecialTiles: true, variant: XOVariantRule.ClassicCaro, specialTilesSeed: 777, floorNumber: 4, isTowerClimb: true);
                return e;
            }
            int baseline = Gold(Make());
            Assert.AreEqual(baseline + 1, Gold(Make(CaroRelicType.CartographerEye)));
            Assert.AreEqual(baseline + 8, Gold(Make(CaroRelicType.CartographerEye, CaroRelicType.FateDice, CaroRelicType.MinePlanter)));
        }

        [Test] public void RelicExtraTurns_ShareACapOfTwoPerMatch()
        {
            var e = TowerEngine(CaroRelicType.Counterstrike, CaroRelicType.OpeningGambit, CaroRelicType.GlassCannon);
            Assert.IsFalse(e.TryCheckCounterstrike(14000));
            Assert.IsTrue(e.TryCheckCounterstrike(22000), "1st relic extra turn");
            var f = TowerEngine(CaroRelicType.Counterstrike, CaroRelicType.OpeningGambit);
            P(f, 0, 0); P(f, 0, 2); P(f, 4, 4); P(f, 4, 6);
            P(f, 8, 8);
            Assert.AreEqual(CellPiece.X, f.CurrentTurn, "Opening Gambit on the 3rd stone");
            Assert.IsTrue(f.TryCheckCounterstrike(22000), "2nd relic extra turn");
            Assert.AreEqual(2, f.ActiveRunStateForTests.RelicExtraTurnsThisMatch);
            var g = TowerEngine(CaroRelicType.Counterstrike);
            g.ActiveRunStateForTests.RelicExtraTurnsThisMatch = XORoguelikeRunState.RelicExtraTurnCap;
            Assert.IsFalse(g.TryCheckCounterstrike(150000), "cap reached");
        }

        [Test] public void Counterstrike_NeedsTheRelic()
        {
            var e = New();
            e.IsTowerClimb = true;
            e.RelicQuery = t => false;
            Assert.IsFalse(e.TryCheckCounterstrike(150000));
        }
    }
}
