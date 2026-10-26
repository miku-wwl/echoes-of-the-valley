using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Echoes.EditorTools
{
    /// <summary>Title screen: a sunset diorama of the ruined valley behind the menu, with drifting fireflies.</summary>
    internal static class MainMenuSceneBuilder
    {
        public static void Build(MaterialLibrary m, PrefabSet prefabs, AudioSet audio)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SceneKit.CreateSun(Quaternion.Euler(14f, 150f, 0f), new Color(1f, 0.74f, 0.52f), 1.25f);
            SceneKit.ConfigureEnvironment(m["SkyMenu"], new Color(0.55f, 0.50f, 0.64f), new Color(0.80f, 0.60f, 0.52f),
                new Color(0.34f, 0.32f, 0.32f), new Color(0.88f, 0.66f, 0.56f), 0.010f);

            GameObject backdrop = new GameObject("Backdrop");
            Transform root = backdrop.transform;
            Pieces.Prim(root, PrimitiveType.Cylinder, "Ground", new Vector3(0f, -0.3f, 40f), new Vector3(180f, 0.3f, 180f), m["GroundMenu"]);
            Vector3[] hills = { new Vector3(-40f, -6f, 70f), new Vector3(10f, -9f, 90f), new Vector3(55f, -7f, 66f), new Vector3(-8f, -4f, 52f) };
            Vector3[] hillSizes = { new Vector3(70f, 26f, 44f), new Vector3(90f, 34f, 52f), new Vector3(64f, 24f, 40f), new Vector3(46f, 16f, 30f) };
            for (int i = 0; i < hills.Length; i++)
                Pieces.Prim(root, PrimitiveType.Sphere, "Hill" + i, hills[i], hillSizes[i], m[i % 2 == 0 ? "HillMenu" : "GroundMenu"]);

            HeartParts heart = Landmarks.Heart(root, new Vector3(5f, 0f, 22f), m);
            Landmarks.Cottage(root, new Vector3(-15f, 0f, 26f), 40f, m, dusk: true);

            Vector3[] treeSpots =
            {
                new Vector3(-9f, 0f, 14f), new Vector3(-22f, 0f, 18f), new Vector3(16f, 0f, 16f), new Vector3(24f, 0f, 26f),
                new Vector3(-30f, 0f, 34f), new Vector3(30f, 0f, 40f), new Vector3(-4f, 0f, 38f), new Vector3(12f, 0f, 48f),
                new Vector3(-18f, 0f, 46f), new Vector3(6f, 0f, 9f), new Vector3(-14f, 0f, 8f)
            };
            GameObject[] variants = { prefabs.TreeRound, prefabs.TreeTall, prefabs.TreeBlossom };
            for (int i = 0; i < treeSpots.Length; i++)
            {
                GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(variants[i % variants.Length], root);
                tree.transform.position = treeSpots[i];
                tree.transform.rotation = Quaternion.Euler(0f, i * 47f, 0f);
                tree.transform.localScale = Vector3.one * (1.1f + 0.12f * (i % 4));
            }
            Pieces.MakeStatic(backdrop);
            Pieces.MakeDynamic(heart.CrystalPivot.gameObject);

            BuildFireflies();
            SceneKit.CreateVolume("PostProcessing", AssetPaths.Settings + "/MenuPostProcessing.asset", 0.42f, 0.78f, 0.30f, 8f);
            AudioWiring.Add(audio, false);

            Camera camera = SceneKit.CreateCamera(new Vector3(0f, 2.4f, -6f), Quaternion.Euler(-5f, 4f, 0f), 52f);
            SceneKit.CreateEventSystem();
            BuildMenuUi(camera.transform);

            if (!EditorSceneManager.SaveScene(scene, AssetPaths.MainMenuScene))
                throw new System.InvalidOperationException("Could not save " + AssetPaths.MainMenuScene);
            Debug.Log("[Echoes] Generated " + AssetPaths.MainMenuScene);
        }

        static void BuildFireflies()
        {
            GameObject go = new GameObject("Fireflies");
            go.transform.position = new Vector3(0f, 2.2f, 12f);
            ParticleSystem system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.prewarm = true;
            main.playOnAwake = true;
            main.duration = 10f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.18f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.5f, 1f), new Color(0.72f, 1f, 0.6f, 1f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 70;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.rateOverTime = 8f;

            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(34f, 3f, 30f);

            ParticleSystem.NoiseModule noise = system.noise;
            noise.enabled = true;
            noise.strength = 0.8f;
            noise.frequency = 0.25f;

            ParticleSystem.ColorOverLifetimeModule fade = system.colorOverLifetime;
            fade.enabled = true;
            fade.color = new ParticleSystem.MinMaxGradient(SceneKit.FadeInOut(Color.white, 1f));

            go.GetComponent<ParticleSystemRenderer>().sharedMaterial = SceneKit.GlowMaterial();
        }

        static void BuildMenuUi(Transform cameraTransform)
        {
            Canvas canvas = UiKit.CreateCanvas("MenuCanvas", 0);
            Transform root = canvas.transform;
            MainMenuController controller = canvas.gameObject.AddComponent<MainMenuController>();

            // A soft dusk-coloured plate keeps the title readable over the brightest part of the sky.
            Image plate = UiKit.Panel(root, "TitlePlate", new Color(0.12f, 0.09f, 0.22f, 0.42f));
            plate.rectTransform.Place(UiKit.Center, UiKit.Center, new Vector2(0f, 200f), new Vector2(1700f, 330f));

            Text title = UiKit.Label(root, "Title", "Echoes of the Valley", 112, UiKit.Cream, TextAnchor.MiddleCenter, FontStyle.Bold, false);
            title.rectTransform.Place(UiKit.Center, UiKit.Center, new Vector2(0f, 250f), new Vector2(1600f, 150f));
            Outline outline = title.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.22f, 0.12f, 0.20f, 0.85f);
            outline.effectDistance = new Vector2(3f, -3f);

            Text subtitle = UiKit.Label(root, "Subtitle", "Silence   ·   Awakening   ·   Resonance   ·   Rest", 36, UiKit.Gold, TextAnchor.MiddleCenter, FontStyle.Bold);
            subtitle.rectTransform.Place(UiKit.Center, UiKit.Center, new Vector2(0f, 150f), new Vector2(1400f, 56f));

            Button continueGame = UiKit.MakeButton(root, "ContinueButton", "Continue", UiKit.Center, new Vector2(0f, -10f), new Vector2(380f, 76f));
            Button play = UiKit.MakeButton(root, "PlayButton", "New Game", UiKit.Center, new Vector2(0f, -100f), new Vector2(380f, 76f));
            Button credits = UiKit.MakeButton(root, "CreditsButton", "Credits", UiKit.Center, new Vector2(0f, -190f), new Vector2(380f, 76f));
            Button quit = UiKit.MakeButton(root, "QuitButton", "Quit", UiKit.Center, new Vector2(0f, -280f), new Vector2(380f, 76f));
            Text saveStatus = UiKit.Label(root, "SaveStatus", "No saved game yet.", 26, new Color(1f, 0.95f, 0.84f, 0.95f), TextAnchor.MiddleCenter);
            saveStatus.rectTransform.Place(UiKit.Center, UiKit.Center, new Vector2(0f, -356f), new Vector2(680f, 40f));

            Text footer = UiKit.Label(root, "Footer", "A quiet valley is waiting to be heard   |   thatgamejam #02", 24,
                new Color(1f, 0.95f, 0.84f, 0.85f), TextAnchor.LowerCenter);
            footer.rectTransform.Place(UiKit.BottomCenter, UiKit.BottomCenter, new Vector2(0f, 26f), new Vector2(1200f, 40f));
            Text version = UiKit.Label(root, "Version", "v0", 22, new Color(1f, 0.95f, 0.84f, 0.7f), TextAnchor.LowerLeft);
            version.rectTransform.Place(UiKit.BottomLeft, UiKit.BottomLeft, new Vector2(32f, 26f), new Vector2(300f, 34f));

            // Music and sound volume, bottom-right
            Image volumePanel = UiKit.Panel(root, "VolumePanel", UiKit.PanelColor, true, true);
            volumePanel.rectTransform.Place(UiKit.BottomRight, UiKit.BottomRight, new Vector2(-32f, 84f), new Vector2(580f, 150f));
            Text volumeTitle = UiKit.Label(volumePanel.transform, "Title", "Volume", 26, UiKit.Gold, TextAnchor.UpperLeft, FontStyle.Bold);
            volumeTitle.rectTransform.Place(UiKit.TopLeft, UiKit.TopLeft, new Vector2(30f, -10f), new Vector2(300f, 34f));
            UiKit.VolumeSliders(volumePanel.transform, UiKit.Center, new Vector2(0f, -14f));

            // The credits: what is AI-made and what is original (text comes from GameCredits at run time)
            Image creditsDim = UiKit.Panel(root, "CreditsPanel", new Color(0.04f, 0.03f, 0.08f, 0.78f), false);
            creditsDim.rectTransform.Fill();
            creditsDim.raycastTarget = true;
            Image creditsBox = UiKit.Panel(creditsDim.transform, "Box", UiKit.ModalColor, true, true);
            creditsBox.rectTransform.Place(UiKit.Center, UiKit.Center, Vector2.zero, new Vector2(1280f, 900f));
            Text creditsTitle = UiKit.Label(creditsBox.transform, "Title", "Credits", 66, UiKit.Gold, TextAnchor.UpperCenter, FontStyle.Bold);
            creditsTitle.rectTransform.Place(UiKit.TopCenter, UiKit.TopCenter, new Vector2(0f, -30f), new Vector2(1100f, 84f));
            Text creditsBody = UiKit.Label(creditsBox.transform, "Body", GameCredits.Text, 28, UiKit.Cream, TextAnchor.UpperCenter);
            creditsBody.rectTransform.Place(UiKit.TopCenter, UiKit.TopCenter, new Vector2(0f, -130f), new Vector2(1120f, 620f));
            Button creditsClose = UiKit.MakeButton(creditsBox.transform, "CloseButton", "Close", UiKit.BottomCenter, new Vector2(0f, 98f), new Vector2(320f, 76f));
            Text creditsHint = UiKit.Label(creditsBox.transform, "Hint", "Esc  Close", 22, new Color(1f, 0.95f, 0.84f, 0.75f), TextAnchor.LowerCenter);
            creditsHint.rectTransform.Place(UiKit.BottomCenter, UiKit.BottomCenter, new Vector2(0f, 14f), new Vector2(400f, 30f));
            creditsDim.gameObject.SetActive(false);

            // "Start a new game?" - shown only when a save exists; the save is not touched by asking
            Image dim = UiKit.Panel(root, "ConfirmPanel", new Color(0.04f, 0.03f, 0.08f, 0.72f), false);
            dim.rectTransform.Fill();
            dim.raycastTarget = true;
            Image box = UiKit.Panel(dim.transform, "Box", UiKit.ModalColor, true, true);
            box.rectTransform.Place(UiKit.Center, UiKit.Center, Vector2.zero, new Vector2(1180f, 520f));
            Text question = UiKit.Label(box.transform, "Question", "Start a new game?", 66, UiKit.Gold, TextAnchor.UpperCenter, FontStyle.Bold);
            question.rectTransform.Place(UiKit.TopCenter, UiKit.TopCenter, new Vector2(0f, -36f), new Vector2(1080f, 88f));
            Text message = UiKit.Label(box.transform, "Message", "You have a saved game.", 34, UiKit.Cream, TextAnchor.UpperCenter);
            message.rectTransform.Place(UiKit.TopCenter, UiKit.TopCenter, new Vector2(0f, -144f), new Vector2(1020f, 190f));
            Button startNew = UiKit.MakeButton(box.transform, "ConfirmButton", "Start New Game", UiKit.BottomCenter, new Vector2(-270f, 84f), new Vector2(470f, 84f));
            Button cancel = UiKit.MakeButton(box.transform, "CancelButton", "Cancel", UiKit.BottomCenter, new Vector2(270f, 84f), new Vector2(380f, 84f));

            Wire.Set(controller, "confirmPanel", dim.gameObject);
            Wire.Set(controller, "confirmText", message);
            Wire.Set(controller, "confirmButton", startNew);
            Wire.Set(controller, "cancelButton", cancel);
            dim.gameObject.SetActive(false);

            Wire.Set(controller, "creditsButton", credits);
            Wire.Set(controller, "creditsPanel", creditsDim.gameObject);
            Wire.Set(controller, "creditsText", creditsBody);
            Wire.Set(controller, "creditsCloseButton", creditsClose);
            Wire.Set(controller, "versionText", version);

            Wire.Set(controller, "continueButton", continueGame);
            Wire.Set(controller, "playButton", play);
            Wire.Set(controller, "quitButton", quit);
            Wire.Set(controller, "saveStatusText", saveStatus);
            Wire.Set(controller, "cameraRig", cameraTransform);
        }
    }
}
