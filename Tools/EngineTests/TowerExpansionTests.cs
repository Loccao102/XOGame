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

        [Test] public void Counterstrike_NeedsTheRelic()
        {
            var e = New();
            e.IsTowerClimb = true;
            e.RelicQuery = t => false;
            Assert.IsFalse(e.TryCheckCounterstrike(150000));
        }
    }
}
