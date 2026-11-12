using UnityEngine;
using UnityEngine.UI;
using XOPlayable.Core;
using XOPlayable.Visuals;

namespace XOPlayable.UI
{
    /// <summary>
    /// Small self-playing 7x7 board shown on the lobby: nine scripted moves, a pulsing winning diagonal, a fade, then loop.
    /// Purely decorative (no raycasts) and driven by unscaled time so it keeps running while modals pause the game.
    /// </summary>
    public sealed class XOLobbyBoardDemo : MonoBehaviour
    {
        private const int N = 7;
        private const float MoveInterval = 0.55f;
        private const float PopTime = 0.28f;
        private const float WinHold = 1.5f;
        private const float FadeTime = 0.5f;

        // (row, col, isX). The X diagonal (1,1)..(5,5) wins on the last move.
        private static readonly int[,] Script =
        {
            { 3, 3, 1 }, { 3, 1, 0 }, { 2, 2, 1 }, { 2, 3, 0 }, { 4, 4, 1 },
            { 4, 2, 0 }, { 1, 1, 1 }, { 1, 4, 0 }, { 5, 5, 1 },
        };

        private Image[] _cells;
        private Image[] _pieces;
        private Image _frame;
        private float _time;
        private Color _cellA, _cellB, _win, _xCol, _oCol;

        /// <summary>Builds the frame and cells at <paramref name="size"/> pixels square inside <paramref name="root"/>.</summary>
        public void Build(RectTransform root, float size)
        {
            _frame = root.gameObject.GetComponent<Image>();
            if (_frame == null) _frame = root.gameObject.AddComponent<Image>();
            _frame.sprite = XOProceduralAssets.CardPanelSprite;
            _frame.type = Image.Type.Sliced;
            _frame.raycastTarget = false;

            float pad = size * 0.035f;
            float cell = (size - pad * 2f) / N;
            _cells = new Image[N * N];
            _pieces = new Image[N * N];

            for (int r = 0; r < N; r++)
            {
                for (int c = 0; c < N; c++)
                {
                    int i = r * N + c;
                    GameObject cellObj = XOUIBuilder.CreateUIObject($"Cell_{r}_{c}", root);
                    RectTransform rt = cellObj.GetComponent<RectTransform>();
                    rt.SetAnchor(new Vector2(0f, 1f), new Vector2(0f, 1f));
                    rt.sizeDelta = new Vector2(cell - 4f, cell - 4f);
                    rt.anchoredPosition = new Vector2(pad + cell * (c + 0.5f), -(pad + cell * (r + 0.5f)));

                    Image bg = cellObj.AddComponent<Image>();
                    bg.sprite = XOProceduralAssets.SoftCellSprite;
                    bg.type = Image.Type.Sliced;
                    bg.raycastTarget = false;
                    _cells[i] = bg;

                    Image piece = XOUIBuilder.CreateIconImage(rt, null, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(cell * 0.72f, cell * 0.72f));
                    piece.enabled = false;
                    _pieces[i] = piece;
                }
            }
        }

        public void ApplyTheme(XOThemePalette theme, string skinId)
        {
            _cellA = theme.cellNormalColor.ToUnityColor();
            _cellB = theme.cellAlternateColor.ToUnityColor();
            _win = theme.cellWinHighlightColor.ToUnityColor();
            _xCol = theme.xColor.ToUnityColor();
            _oCol = theme.oColor.ToUnityColor();
            if (_frame != null) _frame.color = theme.boardFrameColor.ToUnityColor();

            Sprite xs = XOProceduralAssets.GetPieceSkinSprite(skinId, true);
            Sprite os = XOProceduralAssets.GetPieceSkinSprite(skinId, false);
            for (int m = 0; m < Script.GetLength(0); m++)
            {
                Image p = _pieces[Script[m, 0] * N + Script[m, 1]];
                if (p != null) p.sprite = Script[m, 2] == 1 ? xs : os;
            }
        }

        private void Update()
        {
            if (_cells == null) return;
            _time += Time.unscaledDeltaTime;

            int moves = Script.GetLength(0);
            float winStart = moves * MoveInterval;
            float fadeStart = winStart + WinHold;
            float total = fadeStart + FadeTime + 0.3f;
            if (_time >= total) _time = 0f;

            float fade = _time > fadeStart ? 1f - Mathf.Clamp01((_time - fadeStart) / FadeTime) : 1f;
            float pulse = 0.5f + 0.5f * Mathf.Sin((_time - winStart) * 9f);

            for (int i = 0; i < _cells.Length; i++)
            {
                int r = i / N, c = i % N;
                _cells[i].color = ((r + c) & 1) == 0 ? _cellA : _cellB;
                _pieces[i].enabled = false;
            }

            for (int m = 0; m < moves; m++)
            {
                float age = _time - m * MoveInterval;
                if (age < 0f) break;

                int idx = Script[m, 0] * N + Script[m, 1];
                bool isX = Script[m, 2] == 1;
                Image p = _pieces[idx];
                p.enabled = true;

                Color col = isX ? _xCol : _oCol;
                col.a = fade;
                p.color = col;

                float k = Mathf.Clamp01(age / PopTime);
                p.rectTransform.localScale = Vector3.one * EaseOutBack(k);

                bool onWinLine = isX && _time >= winStart;
                if (onWinLine)
                    _cells[idx].color = Color.Lerp(_cells[idx].color, _win, (0.5f + 0.5f * pulse) * fade);
            }
        }

        private static float EaseOutBack(float k)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float x = k - 1f;
            return 1f + c3 * x * x * x + c1 * x * x;
        }
    }
}
