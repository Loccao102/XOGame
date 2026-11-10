using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using XOPlayable.Core;
using XOPlayable.Visuals;

namespace XOPlayable.UI
{
    public class XOCellView : MonoBehaviour
    {
        public int Row { get; private set; }
        public int Col { get; private set; }

        private Button _button;
        private Image _bgImage;
        private Image _specialTileImage;
        private Image _pieceImage;
        private Image _hintOverlayImage;
        private Image _lastMoveDotImage;
        private Image _freezeIconImage;
        private Text _freezeText;

        private CellPiece _currentPiece = CellPiece.Empty;
        private SpecialTileType _specialTile = SpecialTileType.None;
        private int _frozenTurns = 0;
        private bool _isLastMove;
        private bool _isWinningCell;
        private bool _isHintCell;
        private bool _isBombTargetMode;
        private bool _isSwapTargetMode;
        private bool _isVanishingNext;
        private int _gobbleSize;
        private XOThemePalette _theme;
        private XOPieceSkin _pieceSkin;

        private Coroutine _animCoroutine;

        public void Initialize(int row, int col, Font font, Action<int, int> onCellClicked)
        {
            Row = row;
            Col = col;

            _bgImage = gameObject.GetComponent<Image>();
            _button = gameObject.GetComponent<Button>();

            // Special Tile Icon (Gold Coin, Extra-Turn Lightning, or Rock Obstacle)
            GameObject specialObj = new GameObject("SpecialTileIcon", typeof(RectTransform), typeof(Image));
            specialObj.transform.SetParent(transform, false);
            RectTransform specRT = specialObj.GetComponent<RectTransform>();
            specRT.anchorMin = new Vector2(0.16f, 0.16f);
            specRT.anchorMax = new Vector2(0.84f, 0.84f);
            specRT.offsetMin = Vector2.zero;
            specRT.offsetMax = Vector2.zero;
            _specialTileImage = specialObj.GetComponent<Image>();
            _specialTileImage.preserveAspect = true;
            _specialTileImage.raycastTarget = false;
            _specialTileImage.enabled = false;

            // Piece Sprite Icon (Supports both Neon X/O and Custom Illustrated Piece Skins!)
            GameObject pieceObj = new GameObject("PieceIcon", typeof(RectTransform), typeof(Image));
            pieceObj.transform.SetParent(transform, false);
            RectTransform pieceRT = pieceObj.GetComponent<RectTransform>();
            pieceRT.anchorMin = new Vector2(0.11f, 0.11f);
            pieceRT.anchorMax = new Vector2(0.89f, 0.89f);
            pieceRT.offsetMin = Vector2.zero;
            pieceRT.offsetMax = Vector2.zero;
            _pieceImage = pieceObj.GetComponent<Image>();
            _pieceImage.raycastTarget = false;
            _pieceImage.enabled = false;

            // Freeze Snowflake Icon
            GameObject freezeIconObj = new GameObject("FreezeIcon", typeof(RectTransform), typeof(Image));
            freezeIconObj.transform.SetParent(transform, false);
            RectTransform fiRT = freezeIconObj.GetComponent<RectTransform>();
            fiRT.anchorMin = new Vector2(0.14f, 0.14f);
            fiRT.anchorMax = new Vector2(0.86f, 0.86f);
            fiRT.offsetMin = Vector2.zero;
            fiRT.offsetMax = Vector2.zero;
            _freezeIconImage = freezeIconObj.GetComponent<Image>();
            _freezeIconImage.sprite = XOProceduralAssets.IconFreeze;
            _freezeIconImage.raycastTarget = false;
            _freezeIconImage.enabled = false;

            // Freeze Turn Counter Text
            GameObject freezeObj = new GameObject("FreezeLabel", typeof(RectTransform), typeof(Text));
            freezeObj.transform.SetParent(transform, false);
            RectTransform freezeRT = freezeObj.GetComponent<RectTransform>();
            freezeRT.anchorMin = new Vector2(0.55f, 0f);
            freezeRT.anchorMax = new Vector2(0.95f, 0.45f);
            freezeRT.offsetMin = Vector2.zero;
            freezeRT.offsetMax = Vector2.zero;
            _freezeText = freezeObj.GetComponent<Text>();
            _freezeText.font = font;
            _freezeText.fontSize = 18;
            _freezeText.fontStyle = FontStyle.Bold;
            _freezeText.alignment = TextAnchor.LowerRight;
            _freezeText.color = Color.white;
            _freezeText.raycastTarget = false;
            _freezeText.enabled = false;

            // Hint Target Ring
            GameObject hintObj = new GameObject("HintRing", typeof(RectTransform), typeof(Image));
            hintObj.transform.SetParent(transform, false);
            RectTransform hintRT = hintObj.GetComponent<RectTransform>();
            hintRT.anchorMin = new Vector2(0.06f, 0.06f);
            hintRT.anchorMax = new Vector2(0.94f, 0.94f);
            hintRT.offsetMin = Vector2.zero;
            hintRT.offsetMax = Vector2.zero;
            _hintOverlayImage = hintObj.GetComponent<Image>();
            _hintOverlayImage.sprite = XOProceduralAssets.HintTargetSprite;
            _hintOverlayImage.raycastTarget = false;
            _hintOverlayImage.enabled = false;

            // Last Move Corner Dot
            GameObject dotObj = new GameObject("LastMoveDot", typeof(RectTransform), typeof(Image));
            dotObj.transform.SetParent(transform, false);
            RectTransform dotRT = dotObj.GetComponent<RectTransform>();
            dotRT.anchorMin = new Vector2(0.73f, 0.73f);
            dotRT.anchorMax = new Vector2(0.91f, 0.91f);
            dotRT.offsetMin = Vector2.zero;
            dotRT.offsetMax = Vector2.zero;
            _lastMoveDotImage = dotObj.GetComponent<Image>();
            _lastMoveDotImage.sprite = XOProceduralAssets.CircleSprite;
            _lastMoveDotImage.raycastTarget = false;
            _lastMoveDotImage.enabled = false;

            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(() => onCellClicked?.Invoke(Row, Col));
        }

        public void SetState(
            CellPiece piece,
            SpecialTileType specialTile,
            int frozenTurns,
            XOThemePalette theme,
            XOPieceSkin pieceSkin,
            bool isLastMove,
            bool isWinCell,
            bool isHint,
            bool isBombTargetMode,
            bool isSwapTargetMode,
            bool isVanishingNext = false,
            int gobbleSize = 0,
            bool animatePop = false)
        {
            _currentPiece = piece;
            _specialTile = specialTile;
            _frozenTurns = frozenTurns;
            _theme = theme;
            _pieceSkin = pieceSkin;
            _isLastMove = isLastMove;
            _isWinningCell = isWinCell;
            _isHintCell = isHint;
            _isBombTargetMode = isBombTargetMode;
            _isSwapTargetMode = isSwapTargetMode;
            _isVanishingNext = isVanishingNext;
            _gobbleSize = gobbleSize;

            RefreshVisuals();

            float baseScale = (_gobbleSize >= 1)
                ? (_gobbleSize == 1 ? 0.72f : (_gobbleSize == 2 ? 0.88f : 1.05f))
                : 1.0f;

            if (animatePop && piece != CellPiece.Empty && gameObject.activeInHierarchy)
            {
                if (_animCoroutine != null) StopCoroutine(_animCoroutine);
                _animCoroutine = StartCoroutine(PopInAnimation(baseScale));
            }
            else if (_pieceImage != null)
            {
                _pieceImage.rectTransform.localScale = new Vector3(baseScale, baseScale, 1f);
            }
        }

        public void RefreshVisuals()
        {
            if (_theme == null || _bgImage == null) return;

            bool isAlt = ((Row + Col) % 2 == 1);
            Color baseCellColor = isAlt ? _theme.cellAlternateColor : _theme.cellNormalColor;

            if (_isWinningCell)
            {
                _bgImage.color = _theme.cellWinHighlightColor;
            }
            else if (_specialTile == SpecialTileType.RockObstacle && _currentPiece == CellPiece.Empty)
            {
                _bgImage.color = XOThemeDatabase.Hex("#1E293B");
            }
            else if (_frozenTurns > 0)
            {
                _bgImage.color = XOThemeDatabase.Hex("#0284C7");
            }
            else if (_isBombTargetMode && _currentPiece == CellPiece.O)
            {
                _bgImage.color = Color.Lerp(baseCellColor, XOThemeDatabase.Hex("#EF4444"), 0.50f);
            }
            else if (_isSwapTargetMode && _currentPiece == CellPiece.O)
            {
                _bgImage.color = Color.Lerp(baseCellColor, XOThemeDatabase.Hex("#A855F7"), 0.55f);
            }
            else if (_isVanishingNext && _currentPiece != CellPiece.Empty)
            {
                _bgImage.color = Color.Lerp(baseCellColor, XOThemeDatabase.Hex("#F97316"), 0.36f);
            }
            else if (_isLastMove)
            {
                _bgImage.color = _theme.cellLastMoveColor;
            }
            else if (_isHintCell)
            {
                _bgImage.color = Color.Lerp(baseCellColor, _theme.goldAdColor, 0.35f);
            }
            else if (_currentPiece == CellPiece.Empty && _specialTile == SpecialTileType.ExtraTurnLightning)
            {
                _bgImage.color = Color.Lerp(baseCellColor, XOThemeDatabase.Hex("#F59E0B"), 0.25f);
            }
            else if (_currentPiece == CellPiece.Empty && _specialTile == SpecialTileType.GoldBonus)
            {
                _bgImage.color = Color.Lerp(baseCellColor, XOThemeDatabase.Hex("#FBBF24"), 0.18f);
            }
            else if (_currentPiece == CellPiece.Empty && _specialTile == SpecialTileType.HiddenMine)
            {
                bool revealMine = XORoguelikeRunManager.HasRelic(CaroRelicType.Telescope) || _isHintCell;
                _bgImage.color = revealMine ? Color.Lerp(baseCellColor, XOThemeDatabase.Hex("#EF4444"), 0.22f) : baseCellColor;
            }
            else if (_currentPiece == CellPiece.Empty && _specialTile == SpecialTileType.WarpPortal)
            {
                _bgImage.color = Color.Lerp(baseCellColor, XOThemeDatabase.Hex("#A855F7"), 0.26f);
            }
            else
            {
                _bgImage.color = baseCellColor;
            }

            // Special Tile Icon Rendering (only when cell is Empty and not Frozen)
            if (_specialTileImage != null)
            {
                bool isRevealedMine = (_specialTile == SpecialTileType.HiddenMine) && (XORoguelikeRunManager.HasRelic(CaroRelicType.Telescope) || _isHintCell);
                bool showSpecial = (_currentPiece == CellPiece.Empty && _frozenTurns <= 0 && _specialTile != SpecialTileType.None && (_specialTile != SpecialTileType.HiddenMine || isRevealedMine));
                _specialTileImage.enabled = showSpecial;
                if (showSpecial)
                {
                    _specialTileImage.sprite = _specialTile switch
                    {
                        SpecialTileType.GoldBonus          => XOProceduralAssets.IconCoin,
                        SpecialTileType.ExtraTurnLightning => XOProceduralAssets.IconLightning,
                        SpecialTileType.RockObstacle       => XOProceduralAssets.IconRock,
                        SpecialTileType.HiddenMine         => XOProceduralAssets.IconBomb,
                        SpecialTileType.WarpPortal         => XOProceduralAssets.IconSwap,
                        _                                  => null
                    };
                    _specialTileImage.color = (_specialTile == SpecialTileType.RockObstacle)
                        ? new Color(1f, 1f, 1f, 0.95f)
                        : new Color(1f, 1f, 1f, 0.88f);
                }
            }

            // Freeze Status or Infinite3x3 Vanishing-Next Warning Badge
            bool showFreeze = (_frozenTurns > 0 && _currentPiece == CellPiece.Empty);
            if (_freezeIconImage != null) _freezeIconImage.enabled = showFreeze;
            if (_freezeText != null)
            {
                if (showFreeze)
                {
                    _freezeText.enabled = true;
                    _freezeText.text = _frozenTurns.ToString();
                    _freezeText.color = Color.white;
                }
                else if (_isVanishingNext && _currentPiece != CellPiece.Empty)
                {
                    _freezeText.enabled = true;
                    _freezeText.text = "FADE";
                    _freezeText.color = XOThemeDatabase.Hex("#FDE047");
                }
                else if (_gobbleSize >= 1 && _currentPiece != CellPiece.Empty)
                {
                    _freezeText.enabled = true;
                    _freezeText.text = (_gobbleSize >= 3) ? "LV.3*" : $"LV.{_gobbleSize}";
                    _freezeText.color = (_gobbleSize >= 3) ? XOThemeDatabase.Hex("#FBBF24") : XOThemeDatabase.Hex("#E2E8F0");
                }
                else
                {
                    _freezeText.enabled = false;
                }
            }

            // Piece Rendering (Classic X/O or Custom Illustrated Piece Skin)
            if (_currentPiece == CellPiece.Empty)
            {
                _pieceImage.enabled = false;
            }
            else
            {
                _pieceImage.enabled = true;
                string skinId = (_pieceSkin != null) ? _pieceSkin.id : "classic_xo";
                bool isX = (_currentPiece == CellPiece.X);
                _pieceImage.sprite = XOProceduralAssets.GetPieceSkinSprite(skinId, isX);

                if (skinId == "classic_xo")
                {
                    if (isX)
                        _pieceImage.color = _isWinningCell ? XOThemeDatabase.Hex("#0F172A") : _theme.xColor;
                    else
                        _pieceImage.color = _isWinningCell ? XOThemeDatabase.Hex("#7F1D1D") : _theme.oColor;
                }
                else
                {
                    _pieceImage.color = Color.white;
                }
            }

            // Last Move Dot
            _lastMoveDotImage.enabled = _isLastMove && !_isWinningCell && _currentPiece != CellPiece.Empty;
            if (_lastMoveDotImage.enabled)
            {
                _lastMoveDotImage.color = (_currentPiece == CellPiece.X) ? _theme.xColor : _theme.oColor;
            }

            // Hint Ring
            _hintOverlayImage.enabled = _isHintCell && _currentPiece == CellPiece.Empty && _frozenTurns <= 0;
            if (_hintOverlayImage.enabled)
            {
                _hintOverlayImage.color = _theme.goldAdColor;
            }
        }

        private void Update()
        {
            if (_isHintCell && _hintOverlayImage != null && _hintOverlayImage.enabled)
            {
                float pulse = 0.88f + 0.16f * Mathf.Sin(Time.unscaledTime * 7f);
                _hintOverlayImage.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
                _hintOverlayImage.rectTransform.Rotate(0f, 0f, -45f * Time.unscaledDeltaTime);
            }
            else if ((_isBombTargetMode || _isSwapTargetMode) && _currentPiece == CellPiece.O && _pieceImage != null)
            {
                float pulse = 0.94f + 0.15f * Mathf.Sin(Time.unscaledTime * 10f);
                _pieceImage.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
            }
            else if (_isWinningCell && _pieceImage != null)
            {
                float pulse = 1.0f + 0.12f * Mathf.Sin(Time.unscaledTime * 8f + (Row + Col) * 0.5f);
                _pieceImage.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
            }
            else if (_isVanishingNext && _currentPiece != CellPiece.Empty && _pieceImage != null)
            {
                float pulse = 0.82f + 0.14f * Mathf.Sin(Time.unscaledTime * 8f);
                _pieceImage.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
            }
            else if (_currentPiece == CellPiece.Empty && (_specialTile == SpecialTileType.ExtraTurnLightning || _specialTile == SpecialTileType.WarpPortal) && _specialTileImage != null && _specialTileImage.enabled)
            {
                float pulse = 0.90f + 0.14f * Mathf.Sin(Time.unscaledTime * 6f + Row);
                _specialTileImage.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
            }
        }

        private IEnumerator PopInAnimation(float targetScale = 1.0f)
        {
            float duration = 0.18f;
            float t = 0f;

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / duration);
                float scale = (p < 0.65f)
                    ? Mathf.Lerp(0.15f, targetScale * 1.22f, p / 0.65f)
                    : Mathf.Lerp(targetScale * 1.22f, targetScale, (p - 0.65f) / 0.35f);
                if (_pieceImage != null) _pieceImage.rectTransform.localScale = new Vector3(scale, scale, 1f);
                yield return null;
            }

            if (_pieceImage != null) _pieceImage.rectTransform.localScale = new Vector3(targetScale, targetScale, 1f);
        }
    }
}
