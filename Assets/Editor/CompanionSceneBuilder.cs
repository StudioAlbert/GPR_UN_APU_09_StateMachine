using System;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UIE = UnityEngine.UIElements;   // Image et Button existent des deux cotes

namespace Companion.EditorTools
{
    /// <summary>
    /// Regénère les quatre scènes de démo depuis zéro. C'est le script qui a construit
    /// le companion : il documente le montage de chaque scène, et permet de repartir
    /// d'une base propre si une scène est cassée. Menu : Companion > Rebuild demo scenes.
    /// </summary>
    public static class CompanionSceneBuilder
    {
        const string Dir = "Assets/01 - Scenes/";
        const string MatDir = "Assets/06 - Rendering/Materials/";
        const string UiDir = "Assets/06 - Rendering/UI/";
        const string ThemePath = "Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss";

        /// <summary>Le fond uni des quatre scènes — #151515, celui du prototype.</summary>
        static readonly Color Background = new Color32(21, 21, 21, 255);

        [UnityEditor.MenuItem("Companion/Rebuild demo scenes")]
        public static void Rebuild() { UnityEngine.Debug.Log(Run()); }

        public static string Run()
        {
            var log = new System.Text.StringBuilder();
            AssetDatabase.StartAssetEditing();
            try { EnsureMaterials(); } finally { AssetDatabase.StopAssetEditing(); }
            AssetDatabase.Refresh();
            EnsureUiAssets();

            log.AppendLine(Hero());
            log.AppendLine(Guard());
            log.AppendLine(Qte());
            log.AppendLine(TurnBased());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return log.ToString();
        }

        // ------------------------------------------------------------------ helpers
        static Material _ground, _actor;
        static UIE.PanelSettings _panel;
        static UIE.VisualTreeAsset _hudTree, _turnBasedTree;

        static void EnsureMaterials()
        {
            if (!AssetDatabase.IsValidFolder("Assets/06 - Rendering"))
                AssetDatabase.CreateFolder("Assets", "06 - Rendering");
            if (!AssetDatabase.IsValidFolder("Assets/06 - Rendering/Materials"))
                AssetDatabase.CreateFolder("Assets/06 - Rendering", "Materials");
            if (!AssetDatabase.IsValidFolder("Assets/01 - Scenes"))
                AssetDatabase.CreateFolder("Assets", "01 - Scenes");

            _ground = Mat("Ground", new Color(0.20f, 0.21f, 0.24f));
            _actor = Mat("Actor", new Color(0.80f, 0.82f, 0.85f));
        }

        static Material Mat(string name, Color c)
        {
            string path = MatDir + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                m = new Material(s);
                AssetDatabase.CreateAsset(m, path);
            }
            m.color = c;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// Le thème runtime et le PanelSettings du HUD : Unity les crée normalement depuis
        /// le menu Create, on les fabrique ici pour qu'une régénération suffise.
        /// Les documents eux-mêmes (UXML, USS) sont écrits à la main, comme tout asset d'UI.
        /// </summary>
        static void EnsureUiAssets()
        {
            if (AssetDatabase.LoadAssetAtPath<UIE.ThemeStyleSheet>(ThemePath) == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ThemePath));
                File.WriteAllText(ThemePath, "@import url(\"unity-theme://default\");\n");
                AssetDatabase.ImportAsset(ThemePath);
            }

            string panelPath = UiDir + "StateMachineHudPanelSettings.asset";
            _panel = AssetDatabase.LoadAssetAtPath<UIE.PanelSettings>(panelPath);
            if (_panel == null)
            {
                _panel = ScriptableObject.CreateInstance<UIE.PanelSettings>();
                AssetDatabase.CreateAsset(_panel, panelPath);
            }

            // Même résolution de référence que les CanvasScaler déjà en place.
            _panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<UIE.ThemeStyleSheet>(ThemePath);
            _panel.scaleMode = UIE.PanelScaleMode.ScaleWithScreenSize;
            _panel.referenceResolution = new Vector2Int(1280, 720);
            _panel.screenMatchMode = UIE.PanelScreenMatchMode.MatchWidthOrHeight;
            _panel.match = 0f;
            _panel.sortingOrder = 1f;
            EditorUtility.SetDirty(_panel);

            _hudTree = AssetDatabase.LoadAssetAtPath<UIE.VisualTreeAsset>(UiDir + "StateMachineHud.uxml");
            _turnBasedTree = AssetDatabase.LoadAssetAtPath<UIE.VisualTreeAsset>(UiDir + "TurnBasedHud.uxml");

            if (_hudTree == null || _turnBasedTree == null)
                UnityEngine.Debug.LogError($"UXML manquant dans {UiDir} — le HUD sera vide.");
        }

        static Scene NewScene()
        {
            return EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        }

        static GameObject Prim(PrimitiveType t, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(t);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static void Cam(Vector3 pos, Vector3 euler)
        {
            var cam = GameObject.Find("Main Camera");
            if (cam == null) return;
            cam.transform.position = pos;
            cam.transform.rotation = Quaternion.Euler(euler);

            // Fond uni : pas de skybox derrière un companion de démo.
            var camera = cam.GetComponent<Camera>();
            if (camera == null) return;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Background;
        }

        /// <summary>Le panneau de debug d'une scène sans machine imbriquée.</summary>
        static Companion.StateMachineHud Hud(MonoBehaviour main, string title, string controls)
        {
            return Hud(_hudTree, main, title, controls);
        }

        /// <summary>
        /// Le panneau de debug : un UIDocument qui porte le document de la scène, et le
        /// présentateur qui l'alimente. <paramref name="main"/> est la machine affichée
        /// en grand, en haut à droite.
        /// </summary>
        static Companion.StateMachineHud Hud(UIE.VisualTreeAsset tree, MonoBehaviour main, string title, string controls)
        {
            var go = new GameObject("HUD");

            var document = go.AddComponent<UIE.UIDocument>();
            document.panelSettings = _panel;
            document.visualTreeAsset = tree;

            var hud = go.AddComponent<Companion.StateMachineHud>();
            var so = new SerializedObject(hud);
            so.FindProperty("sceneTitle").stringValue = title;
            so.FindProperty("controls").stringValue = controls;
            so.FindProperty("mainMachine").objectReferenceValue = main;
            so.ApplyModifiedPropertiesWithoutUndo();
            return hud;
        }

        /// <summary>
        /// Lie une machine imbriquée au panneau qui l'attend dans le document : le panneau
        /// existe déjà dans l'UXML, on dit seulement quelle machine il montre.
        /// </summary>
        static void Card(Companion.StateMachineHud hud, MonoBehaviour machine, string element)
        {
            var so = new SerializedObject(hud);
            SerializedProperty list = so.FindProperty("cards");

            int i = list.arraySize;
            list.arraySize = i + 1;
            SerializedProperty entry = list.GetArrayElementAtIndex(i);
            entry.FindPropertyRelative("machine").objectReferenceValue = machine;
            entry.FindPropertyRelative("element").stringValue = element;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Save(Scene scene, string name)
        {
            string path = Dir + name + ".unity";
            EditorSceneManager.SaveScene(scene, path);
            AddToBuild(path);
        }

        static void AddToBuild(string path)
        {
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            foreach (var s in list) if (s.path == path) return;
            list.Add(new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        static GameObject Canvas(out GameObject root)
        {
            var go = new GameObject("Canvas");
            var c = go.AddComponent<UnityEngine.Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            go.AddComponent<GraphicRaycaster>();
            if (GameObject.Find("EventSystem") == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
            root = go;
            return go;
        }

        static GameObject Box(GameObject parent, string name, Vector2 pos, Vector2 size, Color c)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var img = go.AddComponent<Image>();
            img.color = c;
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go;
        }

        static GameObject Label(GameObject parent, string text, int size, Color c)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent.transform, false);
            var t = go.AddComponent<TMPro.TextMeshProUGUI>();
            t.text = text;
            t.fontSize = size;
            t.color = c;
            t.alignment = TMPro.TextAlignmentOptions.Center;
            t.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return go;
        }

        static GameObject Btn(GameObject parent, string name, string text, Vector2 pos, Vector2 size)
        {
            var go = Box(parent, name, pos, size, new Color(0.92f, 0.92f, 0.94f));
            var b = go.AddComponent<Button>();
            b.targetGraphic = go.GetComponent<Image>();
            Label(go, text, 22, new Color(0.1f, 0.1f, 0.1f));
            return go;
        }

        // ------------------------------------------------------------------ 01 Hero
        static string Hero()
        {
            var scene = NewScene();
            Cam(new Vector3(0f, 2.2f, -7f), new Vector3(10f, 0f, 0f));
            Prim(PrimitiveType.Plane, "Ground", Vector3.zero, new Vector3(2f, 1f, 2f), _ground);
            var hero = Prim(PrimitiveType.Cube, "Hero", new Vector3(0f, 0.5f, 0f), Vector3.one, _actor);
            var heroMachine = hero.AddComponent<Companion.Hero.HeroStateMachine>();
            Hud(heroMachine,
                "01 — Héros : Grounded, Jumping, Landing",
                "Espace : sauter. Pendant Landing, la machine refuse le saut — il n'y a pas de transition.");
            Save(scene, "01 - Hero");
            return "01 - Hero ok";
        }

        // ------------------------------------------------------------------ 02 Guard
        static string Guard()
        {
            var scene = NewScene();
            Cam(new Vector3(0f, 4.5f, -9f), new Vector3(16f, 0f, 0f));
            Prim(PrimitiveType.Plane, "Ground", Vector3.zero, new Vector3(2f, 1f, 2f), _ground);

            var a = new GameObject("Point A");
            a.transform.position = new Vector3(-4f, 0.5f, 0f);
            var b = new GameObject("Point B");
            b.transform.position = new Vector3(4f, 0.5f, 0f);

            Prim(PrimitiveType.Cylinder, "Marker A", new Vector3(-4f, 0.05f, 0f), new Vector3(0.4f, 0.05f, 0.4f), _ground);
            Prim(PrimitiveType.Cylinder, "Marker B", new Vector3(4f, 0.05f, 0f), new Vector3(0.4f, 0.05f, 0.4f), _ground);

            var guard = Prim(PrimitiveType.Cube, "Guard", new Vector3(-4f, 0.5f, 0f), Vector3.one, null);
            guard.GetComponent<Renderer>().sharedMaterial = new Material(_actor);
            var g = guard.AddComponent<Companion.Guard.GuardStateMachine>();
            var so = new SerializedObject(g);
            so.FindProperty("pointA").objectReferenceValue = a.transform;
            so.FindProperty("pointB").objectReferenceValue = b.transform;
            so.ApplyModifiedPropertiesWithoutUndo();

            Hud(g,
                "02 — Garde : Idle, Patrolling, OnAlarm",
                "A : lever l'alarme (AddAnyTransition — elle coupe les deux autres états). R : l'annuler.");
            Save(scene, "02 - Guard");
            return "02 - Guard ok";
        }

        // ------------------------------------------------------------------ 03 QTE
        static string Qte()
        {
            var scene = NewScene();
            Cam(new Vector3(0f, 1.5f, -6f), new Vector3(8f, 0f, 0f));
            Prim(PrimitiveType.Plane, "Ground", Vector3.zero, new Vector3(2f, 1f, 2f), _ground);

            GameObject canvas;
            Canvas(out canvas);

            var host = new GameObject("QTE");
            var q = host.AddComponent<Companion.Qte.QteStateMachine>();

            var invite = Box(canvas, "Invite Panel", new Vector2(0f, -140f), new Vector2(560f, 170f), new Color(0.12f, 0.14f, 0.18f, 0.92f));
            Label(invite, "Une alarme sonne !", 26, Color.white).GetComponent<RectTransform>().offsetMin = new Vector2(0f, 70f);
            var bConfirm = Btn(invite, "Button Confirm", "Réagir", new Vector2(0f, -40f), new Vector2(240f, 56f));

            var confirm = Box(canvas, "Confirm Panel", new Vector2(0f, -140f), new Vector2(560f, 170f), new Color(0.12f, 0.14f, 0.18f, 0.92f));
            Label(confirm, "Couper le courant ?", 26, Color.white).GetComponent<RectTransform>().offsetMin = new Vector2(0f, 70f);
            var bOk = Btn(confirm, "Button Ok", "Oui", new Vector2(-130f, -40f), new Vector2(200f, 56f));
            var bCancel = Btn(confirm, "Button Cancel", "Non", new Vector2(130f, -40f), new Vector2(200f, 56f));

            var fail = Box(canvas, "Fail Panel", new Vector2(0f, -140f), new Vector2(560f, 120f), new Color(0.45f, 0.08f, 0.06f, 0.92f));
            Label(fail, "Raté", 34, Color.white);

            var success = Box(canvas, "Success Panel", new Vector2(0f, -140f), new Vector2(560f, 120f), new Color(0.06f, 0.35f, 0.18f, 0.92f));
            Label(success, "Réussi", 34, Color.white);

            var closing = Box(canvas, "Closing Panel", new Vector2(0f, -140f), new Vector2(560f, 120f), new Color(0.12f, 0.14f, 0.18f, 0.7f));
            Label(closing, "…", 34, Color.white);

            var so = new SerializedObject(q);
            so.FindProperty("invitePanel").objectReferenceValue = invite;
            so.FindProperty("confirmPanel").objectReferenceValue = confirm;
            so.FindProperty("failPanel").objectReferenceValue = fail;
            so.FindProperty("successPanel").objectReferenceValue = success;
            so.FindProperty("closingPanel").objectReferenceValue = closing;
            so.ApplyModifiedPropertiesWithoutUndo();

            Action<GameObject, string> wire = (go, arg) =>
            {
                var btn = go.GetComponent<Button>();
                UnityEventTools.AddStringPersistentListener(
                    btn.onClick,
                    new UnityEngine.Events.UnityAction<string>(q.Press),
                    arg);
            };
            wire(bConfirm, "confirm");
            wire(bOk, "ok");
            wire(bCancel, "cancel");

            Hud(q,
                "03 — QTE : Invite, Confirm, Fail, Success, Closing",
                "Un état = un panneau. OnEnter l'affiche, OnExit le cache : aucun SetActive ailleurs. Les boutons sont les transitions.");
            Save(scene, "03 - QTE");
            return "03 - QTE ok";
        }

        // ------------------------------------------------------------------ 04 TurnBased
        static string TurnBased()
        {
            var scene = NewScene();
            Cam(new Vector3(0f, 4f, -8.5f), new Vector3(16f, 0f, 0f));
            Prim(PrimitiveType.Plane, "Ground", Vector3.zero, new Vector3(2f, 1f, 2f), _ground);

            var playerGo = Prim(PrimitiveType.Cube, "Player Turret", new Vector3(-3f, 0.5f, 0f), Vector3.one, null);
            playerGo.GetComponent<Renderer>().sharedMaterial = new Material(_actor);
            var player = playerGo.AddComponent<Companion.TurnBased.Turret>();
            var pso = new SerializedObject(player);
            pso.FindProperty("label").stringValue = "Tourelle joueur";
            pso.ApplyModifiedPropertiesWithoutUndo();

            var aiGo = Prim(PrimitiveType.Cube, "AI Turret", new Vector3(3f, 0.5f, 0f), Vector3.one, null);
            aiGo.GetComponent<Renderer>().sharedMaterial = new Material(_actor);
            var ai = aiGo.AddComponent<Companion.TurnBased.Turret>();
            var aso = new SerializedObject(ai);
            aso.FindProperty("label").stringValue = "Tourelle IA";
            aso.ApplyModifiedPropertiesWithoutUndo();

            var host = new GameObject("Game");
            var game = host.AddComponent<Companion.TurnBased.TurnBasedStateMachine>();
            var gso = new SerializedObject(game);
            gso.FindProperty("playerTurret").objectReferenceValue = player;
            gso.FindProperty("aiTurret").objectReferenceValue = ai;
            gso.ApplyModifiedPropertiesWithoutUndo();

            GameObject canvas;
            Canvas(out canvas);
            var bFire = Btn(canvas, "Button Fire", "Tirer", new Vector2(-130f, -260f), new Vector2(220f, 60f));
            var bNext = Btn(canvas, "Button Next", "Tour suivant", new Vector2(130f, -260f), new Vector2(220f, 60f));

            UnityEventTools.AddVoidPersistentListener(
                bFire.GetComponent<Button>().onClick,
                new UnityEngine.Events.UnityAction(game.PressFire));
            UnityEventTools.AddVoidPersistentListener(
                bNext.GetComponent<Button>().onClick,
                new UnityEngine.Events.UnityAction(game.PressNext));

            var hud = Hud(_turnBasedTree, game,
                "04 — Tour par tour : deux machines emboîtées",
                "Tirer puis Tour suivant. L'IA tire jusqu'à être à sec : la transition du haut lit l'état du bas.");

            // Les deux panneaux existent déjà dans TurnBasedHud.uxml : on dit qui ils montrent.
            Card(hud, player, "card-player");
            Card(hud, ai, "card-ai");
            Save(scene, "04 - TurnBased");
            return "04 - TurnBased ok";
        }
    }
}
