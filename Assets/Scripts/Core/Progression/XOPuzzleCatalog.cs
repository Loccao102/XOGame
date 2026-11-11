namespace XOPlayable.Core
{
    public static class XOPuzzleCatalog
    {
        public static XOPuzzleStage GenerateStage(int stageNumber, int boardSize)
        {
            int mid = boardSize / 2;
            int patternId = (stageNumber - 1) % 6;

            var stage = new XOPuzzleStage
            {
                stageNumber = stageNumber,
                rewardCoins = 60 + XOMath.Min(stageNumber * 10, 140)
            };

            void AddX(int dr, int dc) => stage.presetMoves.Add(
                new MoveRecord(XOMath.Clamp(mid + dr, 0, boardSize - 1), XOMath.Clamp(mid + dc, 0, boardSize - 1), CellPiece.X));
            void AddO(int dr, int dc) => stage.presetMoves.Add(
                new MoveRecord(XOMath.Clamp(mid + dr, 0, boardSize - 1), XOMath.Clamp(mid + dc, 0, boardSize - 1), CellPiece.O));

            switch (patternId)
            {
                case 0:
                    stage.title = $"Stage {stageNumber}: Twin Blades";
                    stage.objectiveHint = "Create a double-threat fork at the center to win in 3 moves!";
                    stage.maxPlayerMoves = 3;
                    AddX(0, -1); AddX(0, -2); AddX(0, 1);
                    AddX(-1, 0); AddX(1, 0);
                    AddO(0, -3); AddO(-2, -1); AddO(2, 1); AddO(1, -1); AddO(-1, 2);
                    break;

                case 1:
                    stage.title = $"Stage {stageNumber}: Counter Strike";
                    stage.objectiveHint = "Block O's threat while extending your diagonal line!";
                    stage.maxPlayerMoves = 4;
                    AddO(-1, -1); AddO(-1, 0); AddO(-1, 1);
                    AddX(0, -1); AddX(1, 0); AddX(2, 1);
                    AddX(-2, -2); AddO(2, -1);
                    break;

                case 2:
                    stage.title = $"Stage {stageNumber}: Crossfire Trap";
                    stage.objectiveHint = "Connect your diagonal branches into an unstoppable open-4!";
                    stage.maxPlayerMoves = 4;
                    AddX(-2, -2); AddX(-1, -1); AddX(1, 1);
                    AddX(0, -2); AddX(0, -1);
                    AddO(-3, -3); AddO(2, 2); AddO(-1, 1); AddO(1, -1); AddO(0, 2);
                    break;

                case 3:
                    stage.title = $"Stage {stageNumber}: Siege Breaker";
                    stage.objectiveHint = "O surrounds the center! Use Bomb/Swap or find the winning gap!";
                    stage.maxPlayerMoves = 5;
                    AddO(0, -1); AddO(0, 0); AddO(0, 1);
                    AddO(-1, 0); AddO(1, 0);
                    AddX(-2, -1); AddX(-2, 0); AddX(-2, 1);
                    AddX(-1, -2); AddX(0, -2); AddX(1, -2);
                    break;

                case 4:
                    stage.title = $"Stage {stageNumber}: Golden Point";
                    stage.objectiveHint = "Find the winning intersection between the row and diagonal!";
                    stage.maxPlayerMoves = 4;
                    AddX(1, -2); AddX(1, -1); AddX(1, 1);
                    AddX(-1, -1); AddX(0, 0);
                    AddO(1, -3); AddO(-2, -2); AddO(0, 1); AddO(-1, 1); AddO(2, 0);
                    break;

                default:
                    stage.title = $"Stage {stageNumber}: Grandmaster Clash";
                    stage.maxPlayerMoves = 6;
                    stage.objectiveHint = $"Fierce mid-game formation! Win within {stage.maxPlayerMoves} moves!";
                    AddX(0, 0); AddX(0, 1); AddX(1, -1); AddX(-1, -1); AddX(1, 1);
                    AddO(-1, 0); AddO(-1, 1); AddO(0, -1); AddO(1, 0); AddO(2, 2);
                    break;
            }

            return stage;
        }
    }
}

