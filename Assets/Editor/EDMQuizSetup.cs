using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using EDMQuiz;

/// <summary>EDMQuiz セットアップ用エディタスクリプト</summary>
public static class EDMQuizSetup
{
    private const string QuestionsDir = "Assets/_EDMQuiz/ScriptableObjects/Questions";
    private const string SoDir        = "Assets/_EDMQuiz/ScriptableObjects";
    private const string ScenesDir    = "Assets/_EDMQuiz/Scenes";
    private const string UxmlBase     = "Assets/_EDMQuiz/UI/Layouts";

    // ─── Step 1: QuizQuestion 作成 ───────────────────────────────
    [MenuItem("EDMQuiz/Setup/1. Create QuizQuestions")]
    public static void CreateQuizQuestions()
    {
        var questions = new[]
        {
            new QuizData(
                "Q01_Mix",
                "DJが2曲をなめらかにつなぎ合わせる技術は？",
                "みっくす",
                new[]{"み","っ","く","す","た","ら","ほ","で"}),
            new QuizData(
                "Q02_Drop",
                "EDMで音楽が爆発的に盛り上がるピークのことは？",
                "どろっぷ",
                new[]{"ど","ろ","っ","ぷ","み","な","つ","か"}),
            new QuizData(
                "Q03_Phrase",
                "繰り返し使われる印象的なメロディの断片のことは？",
                "ふれーず",
                new[]{"ふ","れ","ー","ず","み","く","た","の"}),
            new QuizData(
                "Q04_Beat",
                "音楽における規則正しいリズムの基本単位のことは？",
                "びょうし",
                new[]{"び","ょ","う","し","た","く","ら","も"}),
            new QuizData(
                "Q05_Stage",
                "DJやアーティストが演奏・パフォーマンスする場所は？",
                "すてーじ",
                new[]{"す","て","ー","じ","く","み","ら","ほ"}),
        };

        foreach (var q in questions)
        {
            string path = $"{QuestionsDir}/{q.fileName}.asset";
            if (AssetDatabase.LoadAssetAtPath<QuizQuestion>(path) != null)
            {
                Debug.Log($"[Setup] Skip (exists): {path}");
                continue;
            }
            var so = ScriptableObject.CreateInstance<QuizQuestion>();
            so.questionText     = q.question;
            so.correctAnswer    = q.answer;
            so.hiraganaOptions  = q.options;
            AssetDatabase.CreateAsset(so, path);
            Debug.Log($"[Setup] Created: {path}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Setup] Step 1 Done: QuizQuestions created.");
    }

    // ─── Step 2: QuizDatabase 作成 & 問題を登録 ──────────────────
    [MenuItem("EDMQuiz/Setup/2. Create QuizDatabase")]
    public static void CreateQuizDatabase()
    {
        string dbPath = $"{SoDir}/QuizDatabase.asset";
        var db = AssetDatabase.LoadAssetAtPath<QuizDatabase>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<QuizDatabase>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        var list = new List<QuizQuestion>();
        for (int i = 1; i <= 5; i++)
        {
            string[] names = { "Q01_Mix", "Q02_Drop", "Q03_Phrase", "Q04_Beat", "Q05_Stage" };
            string qPath = $"{QuestionsDir}/{names[i-1]}.asset";
            var q = AssetDatabase.LoadAssetAtPath<QuizQuestion>(qPath);
            if (q == null) Debug.LogWarning($"[Setup] Not found: {qPath} — run Step 1 first.");
            else list.Add(q);
        }

        var so = new SerializedObject(db);
        var prop = so.FindProperty("questions");
        prop.arraySize = list.Count;
        for (int i = 0; i < list.Count; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Setup] Step 2 Done: QuizDatabase created at {dbPath} ({list.Count} questions).");
    }

    // ─── Step 3: TitleScene 作成 ─────────────────────────────────
    [MenuItem("EDMQuiz/Setup/3. Create TitleScene")]
    public static void CreateTitleScene()
    {
        string scenePath = $"{ScenesDir}/TitleScene.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Main Camera
        var camObj = new GameObject("Main Camera");
        var cam = camObj.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
        camObj.tag = "MainCamera";

        // AudioManager (DontDestroyOnLoad)
        var amObj = new GameObject("AudioManager");
        amObj.AddComponent<AudioManager>();

        // UI
        var uiObj  = new GameObject("TitleUI");
        var uidoc  = uiObj.AddComponent<UIDocument>();
        var uxml   = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{UxmlBase}/title-panel.uxml");
        if (uxml != null) uidoc.visualTreeAsset = uxml;
        else Debug.LogWarning("[Setup] title-panel.uxml not found.");

        var ts = uiObj.AddComponent<TitleScreen>();
        var so = new SerializedObject(ts);
        so.FindProperty("_uiDocument").objectReferenceValue = uidoc;
        so.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"[Setup] Step 3 Done: TitleScene saved at {scenePath}.");
    }

    // ─── Step 4: GameScene 作成 ──────────────────────────────────
    [MenuItem("EDMQuiz/Setup/4. Create GameScene")]
    public static void CreateGameScene()
    {
        string scenePath = $"{ScenesDir}/GameScene.unity";
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Main Camera
        var camObj = new GameObject("Main Camera");
        var cam = camObj.AddComponent<Camera>();
        cam.clearFlags      = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.1f);
        camObj.tag = "MainCamera";

        // ── AudioManager (DontDestroyOnLoad なので TitleScene 経由でも直テストでも動く) ──
        var amObj = new GameObject("AudioManager");
        amObj.AddComponent<AudioManager>();

        // ── Managers 親 ──
        var managers = new GameObject("Managers");

        var gfmObj = new GameObject("GameFlowManager");
        gfmObj.transform.SetParent(managers.transform);
        var gfm = gfmObj.AddComponent<GameFlowManager>();

        var smObj = new GameObject("ScoreManager");
        smObj.transform.SetParent(managers.transform);
        smObj.AddComponent<ScoreManager>();

        var bpmObj = new GameObject("BpmClock");
        bpmObj.transform.SetParent(managers.transform);
        bpmObj.AddComponent<BpmClock>();

        // QuizDatabase 参照
        var db = AssetDatabase.LoadAssetAtPath<QuizDatabase>($"{SoDir}/QuizDatabase.asset");
        if (db != null)
        {
            var gfmSo = new SerializedObject(gfm);
            gfmSo.FindProperty("_quizDatabase").objectReferenceValue = db;
            gfmSo.ApplyModifiedProperties();
        }
        else Debug.LogWarning("[Setup] QuizDatabase not found — run Steps 1&2 first.");

        // ── UI ──
        var uiParent = new GameObject("UI");
        var gameUiObj = new GameObject("GameUI");
        gameUiObj.transform.SetParent(uiParent.transform);

        var uidoc = gameUiObj.AddComponent<UIDocument>();
        var uxml  = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>($"{UxmlBase}/game-panel.uxml");
        if (uxml != null) uidoc.visualTreeAsset = uxml;
        else Debug.LogWarning("[Setup] game-panel.uxml not found.");

        // HiraganaInputUI
        var hiragana = gameUiObj.AddComponent<HiraganaInputUI>();
        var hiSo = new SerializedObject(hiragana);
        hiSo.FindProperty("_uiDocument").objectReferenceValue = uidoc;
        hiSo.ApplyModifiedProperties();

        // ResultScreen
        var result = gameUiObj.AddComponent<ResultScreen>();
        var rsSo = new SerializedObject(result);
        rsSo.FindProperty("_uiDocument").objectReferenceValue = uidoc;
        rsSo.ApplyModifiedProperties();

        // VFXDirector
        var vfx = gameUiObj.AddComponent<VFXDirector>();
        var vfxSo = new SerializedObject(vfx);
        vfxSo.FindProperty("_uiDocument").objectReferenceValue = uidoc;
        vfxSo.ApplyModifiedProperties();

        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"[Setup] Step 4 Done: GameScene saved at {scenePath}.");
    }

    // ─── Step 5: Build Settings に両シーンを追加 ─────────────────
    [MenuItem("EDMQuiz/Setup/5. Add Scenes to Build")]
    public static void AddScenesToBuild()
    {
        var scenes = new[]
        {
            new EditorBuildSettingsScene($"{ScenesDir}/TitleScene.unity", true),
            new EditorBuildSettingsScene($"{ScenesDir}/GameScene.unity",  true),
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("[Setup] Step 5 Done: TitleScene (0) + GameScene (1) added to Build Settings.");
    }

    // ─── 全ステップ一括実行 ─────────────────────────────────────
    [MenuItem("EDMQuiz/Setup/Run All Steps")]
    public static void RunAll()
    {
        CreateQuizQuestions();
        CreateQuizDatabase();
        CreateTitleScene();
        CreateGameScene();
        AddScenesToBuild();
        Debug.Log("[Setup] All steps complete!");
    }

    private struct QuizData
    {
        public string   fileName;
        public string   question;
        public string   answer;
        public string[] options;
        public QuizData(string f, string q, string a, string[] o)
        { fileName=f; question=q; answer=a; options=o; }
    }
}
