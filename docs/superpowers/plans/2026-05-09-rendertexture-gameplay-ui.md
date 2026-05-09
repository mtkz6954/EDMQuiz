# RenderTexture Gameplay UI Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 既存の UI Toolkit gameplay UI を RenderTexture 経由で world-space 表示し、`Light2D` の見た目が gameplay UI に反映されるようにする。

**Architecture:** 既存の overlay `UIDocument` は入力と結果表示の土台として維持しつつ、gameplay 表示は display A / display B の 2 つの UI Toolkit パネルを RenderTexture に描画し、world-space の受光面へ貼る。`HiraganaInputUI` は複数 `UIDocument` へ同じ状態を配信し、`VFXDirector` は overlay 専用演出と gameplay 表示演出の参照先を分離する。

**Tech Stack:** Unity 6, UI Toolkit, URP 2D, Light2D, RenderTexture, NUnit EditMode tests, uloop

---

### Task 1: Red test for new gameplay UI asset split

**Files:**
- Modify: `Assets/_EDMQuiz/Tests/EditMode/Editor/GamePresentationPolishTests.cs`
- Create: `Assets/_EDMQuiz/UI/Layouts/game-panel-display-a.uxml`
- Create: `Assets/_EDMQuiz/UI/Layouts/game-panel-display-b.uxml`
- Create: `Assets/_EDMQuiz/UI/Styles/game-panel-display-a.uss`
- Create: `Assets/_EDMQuiz/UI/Styles/game-panel-display-b.uss`

- [ ] **Step 1: Write the failing tests**
- [ ] **Step 2: Run EditMode tests to verify they fail**
  Run: `uloop.cmd run-tests --mode EditMode --filter GamePresentationPolishTests`
- [ ] **Step 3: Add split gameplay UXML / USS assets**
- [ ] **Step 4: Run EditMode tests to verify the new asset expectations pass**
  Run: `uloop.cmd run-tests --mode EditMode --filter GamePresentationPolishTests`

### Task 2: Add RenderTexture display pipeline and scene references

**Files:**
- Create: `Assets/_EDMQuiz/Scripts/UI/GameplayRenderTextureMirror.cs`
- Modify: `Assets/_EDMQuiz/Scenes/GameScene.unity`
- Modify: `Assets/_EDMQuiz/MainGame.asset`
- Modify: `Assets/_EDMQuiz/Scripts/Core/GameConstants.cs`

- [ ] **Step 1: Write a failing test for RenderTexture mirror configuration**
- [ ] **Step 2: Run EditMode tests to verify it fails**
  Run: `uloop.cmd run-tests --mode EditMode --filter GamePresentationPolishTests`
- [ ] **Step 3: Implement a runtime mirror component that creates RenderTextures, binds panel target textures, and builds world-space lit display surfaces**
- [ ] **Step 4: Wire the mirror component and new display documents into `GameScene`**
- [ ] **Step 5: Run EditMode tests to verify configuration passes**
  Run: `uloop.cmd run-tests --mode EditMode --filter GamePresentationPolishTests`

### Task 3: Refactor gameplay UI scripts for multi-document output

**Files:**
- Modify: `Assets/_EDMQuiz/Scripts/UI/HiraganaInputUI.cs`
- Modify: `Assets/_EDMQuiz/Scripts/UI/ResultScreen.cs`
- Modify: `Assets/_EDMQuiz/Scripts/VFX/VFXDirector.cs`
- Modify: `Assets/_EDMQuiz/UI/Layouts/game-panel.uxml`

- [ ] **Step 1: Write a failing test for overlay/result separation and multi-layout names**
- [ ] **Step 2: Run EditMode tests to verify it fails**
  Run: `uloop.cmd run-tests --mode EditMode --filter GamePresentationPolishTests`
- [ ] **Step 3: Refactor `HiraganaInputUI` to update overlay input proxy plus display A / B documents**
- [ ] **Step 4: Refactor `VFXDirector` and `ResultScreen` to keep result UI on overlay while gameplay visuals route to the new mirror flow**
- [ ] **Step 5: Keep gameplay overlay controls invisible-but-interactive where needed so mouse input still works**
- [ ] **Step 6: Run EditMode tests to verify script/layout expectations pass**
  Run: `uloop.cmd run-tests --mode EditMode --filter GamePresentationPolishTests`

### Task 4: Compile and PlayMode verification

**Files:**
- Verify only

- [ ] **Step 1: Run compile verification**
  Run: `uloop.cmd compile --force-recompile false --wait-for-domain-reload true`
- [ ] **Step 2: Start PlayMode and capture gameplay screenshots**
  Run: `uloop.cmd control-play-mode --action Play`
- [ ] **Step 3: Capture rendering evidence during BuildUp / AnswerWindow**
  Run: `uloop.cmd screenshot --capture-mode rendering --output-directory .uloop/outputs/Screenshots`
- [ ] **Step 4: Stop PlayMode**
  Run: `uloop.cmd control-play-mode --action Stop`
- [ ] **Step 5: Review screenshots and logs for Light2D visibility, readability, and input proxy regressions**
