# Caro XO Pro (Tower of Anomalies)

A modern, tactile, and highly competitive **Caro / Gomoku / Tic-Tac-Toe** game built in **Unity 6 (Universal RP)** with rich procedural audio/visuals, 7 game variants, and a dedicated **Roguelike Boss Blind ("Tower of Anomalies")** mode.

---

## 🎮 Key Features

- **Standard & Tactical Caro Rules**:
  - Classic 10x10 & 15x15 Gomoku (5-in-a-row to win).
  - Infinite 3x3 Vanishing Pieces (oldest piece fades on 4th move).
  - Gobblet Stack 15x15 ("Cá lớn nuốt cá bé" - Lv.1, Lv.2, Lv.3 piece stacking & devouring).
  - Gravity Drop 15x15 (Connect-4 style column falling mechanics).
  - Strict Caro (blocked on both ends does not win).
  - Chaos Minefield (Blast Mines & Warp Portals).

- **Tower of Anomalies (Roguelike Caro)**:
  - 10 Mutating Boss Blinds (The Needle, The Void, The Arm, Gravity Abyss, Gobblet Colosseum, Overlord Apex).
  - 12 Synergistic Caro Relics (Diagonal Prism, Twin Threat, Shatter Hammer, Phoenix Sigil, etc.).
  - 1-Life Sudden Death with seed tracking & caravan drafting.

- **Adaptive Responsive Design**:
  - Full support for both **16:9 PC Desktop (1920x1080)** and **9:16 Mobile Portrait (1080x1920)**.

- **Zero External File Footprint**:
  - 28 vector UI icons procedurally synthesized in RAM.
  - 18 sound effects and ambient synthwave loop synthesized in RAM.

- **Playable Ads & Multi-Platform Compliance**:
  - Full Google Playable / YouTube Playables / MRAID / ironSource / Meta bridge integration.

---

## 🚀 How to Run WebGL Preview

1. Open folder `Builds/WebGL_Playable/`.
2. Double-click `Run_Preview.bat`.
3. The preview server will start at `http://localhost:8088/index.html`.

---

## 🧪 Automated Testing

Run the automated QA test suite (38 test scenarios, 200 fuzzing matches):
```bash
dotnet run --project Tools/QARunner/QARunner.csproj
```

