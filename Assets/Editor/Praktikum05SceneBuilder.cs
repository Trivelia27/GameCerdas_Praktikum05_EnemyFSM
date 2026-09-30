using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

// Membangun scene Praktikum05_FSM secara otomatis:
// Ground, Obstacle (layer Obstacle), Player, Enemy, PatrolPoints, SafePoint, NavMesh (bake),
// serta mengisi semua reference di Inspector.
// Jalankan lewat menu: Praktikum 05 > Build FSM Scene
public static class Praktikum05SceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Praktikum05_FSM.unity";
    private const string NavMeshFolder = "Assets/Scenes/Praktikum05_FSM";
    private const string ObstacleLayerName = "Obstacle";

    [MenuItem("Praktikum 05/Build FSM Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets", "Scenes");

        int obstacleLayer = EnsureLayer(ObstacleLayerName);

        Material groundMat = GetMaterial("GroundMaterial", new Color(0.55f, 0.55f, 0.55f));
        Material obstacleMat = GetMaterial("ObstacleMaterial", new Color(0.45f, 0.3f, 0.2f));
        Material playerMat = GetMaterial("PlayerMaterial", new Color(0.2f, 0.5f, 1f));
        Material enemyMat = GetMaterial("EnemyMaterial", new Color(0.2f, 0.8f, 0.3f));
        Material noseMat = GetMaterial("MarkerMaterial", Color.white);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ---------- Kamera (top-down miring) ----------
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(0f, 16f, -26f);
            cam.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
        }

        // ---------- Environment ----------
        GameObject environment = new GameObject("Environment");

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(environment.transform);
        ground.transform.position = Vector3.zero;
        ground.transform.localScale = new Vector3(3f, 1f, 3f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        // Tinggi obstacle 3 m agar Raycast eye-level Enemy pasti terhalang.
        CreateObstacle("Obstacle01", environment.transform, new Vector3(0f, 1.5f, 6f), new Vector3(8f, 3f, 2f), obstacleLayer, obstacleMat);
        CreateObstacle("Obstacle02", environment.transform, new Vector3(0f, 1.5f, -6f), new Vector3(10f, 3f, 3f), obstacleLayer, obstacleMat);
        CreateObstacle("Obstacle03", environment.transform, new Vector3(-8f, 1.5f, 0f), new Vector3(2f, 3f, 6f), obstacleLayer, obstacleMat);

        // ---------- Navigation ----------
        GameObject navigation = new GameObject("Navigation");
        NavMeshSurface surface = navigation.AddComponent<NavMeshSurface>();

        // ---------- Patrol Points & SafePoint ----------
        GameObject patrolRoot = new GameObject("PatrolPoints");
        Transform w1 = CreateEmpty("W1", patrolRoot.transform, new Vector3(-10f, 0f, 10f));
        Transform w2 = CreateEmpty("W2", patrolRoot.transform, new Vector3(10f, 0f, 10f));
        Transform w3 = CreateEmpty("W3", patrolRoot.transform, new Vector3(10f, 0f, -10f));
        Transform w4 = CreateEmpty("W4", patrolRoot.transform, new Vector3(-10f, 0f, -10f));

        Transform safePoint = CreateEmpty("SafePoint", null, new Vector3(12f, 0f, -12f));

        // ---------- Player ----------
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        player.transform.position = new Vector3(0f, 1f, 0f);
        player.GetComponent<Renderer>().sharedMaterial = playerMat;
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>()); // digantikan CharacterController
        player.AddComponent<CharacterController>();
        player.AddComponent<SimplePlayerController>();
        player.AddComponent<PlayerHealth>();
        PlayerAttackTest attackTest = player.AddComponent<PlayerAttackTest>();
        IgnoreInNavMeshBuild(player);
        CreateMarker(player.transform, noseMat);

        // ---------- Enemy ----------
        GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemy.name = "Enemy";
        enemy.transform.position = new Vector3(8f, 1f, 5f);
        enemy.GetComponent<Renderer>().sharedMaterial = enemyMat;

        NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
        agent.speed = 2f;
        agent.angularSpeed = 360f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 0f;
        agent.baseOffset = 1f; // pivot kapsul di tengah, NavMesh di lantai

        EnemyHealth enemyHealth = enemy.AddComponent<EnemyHealth>();
        EnemyPerception perception = enemy.AddComponent<EnemyPerception>();
        EnemyFSM fsm = enemy.AddComponent<EnemyFSM>();
        EnemyStateUI stateUI = enemy.AddComponent<EnemyStateUI>();
        IgnoreInNavMeshBuild(enemy);
        CreateMarker(enemy.transform, noseMat);

        // ---------- Wiring Inspector ----------
        SetRef(perception, "player", player.transform);
        SetLayerMask(perception, "obstacleMask", 1 << obstacleLayer);

        SetRef(fsm, "player", player.transform);
        SetRef(fsm, "agent", agent);
        SetRef(fsm, "perception", perception);
        SetRef(fsm, "health", enemyHealth);
        SetRefArray(fsm, "patrolPoints", new Object[] { w1, w2, w3, w4 });
        SetRef(fsm, "safePoint", safePoint);

        SetRef(attackTest, "enemy", enemyHealth);

        SetRef(stateUI, "fsm", fsm);
        SetRef(stateUI, "bodyRenderer", enemy.GetComponent<Renderer>());

        // ---------- HUD (dibangun lewat kode saat Play) ----------
        new GameObject("GameHUD").AddComponent<GameHUDManager>();

        // ---------- Bake NavMesh ----------
        surface.BuildNavMesh();

        if (surface.navMeshData == null)
        {
            Debug.LogError("[Praktikum05] NavMesh gagal di-bake. Bake manual lewat NavMeshSurface > Bake.");
        }
        else
        {
            EnsureFolder("Assets/Scenes", "Praktikum05_FSM");
            string navPath = NavMeshFolder + "/NavMesh-Navigation.asset";
            AssetDatabase.DeleteAsset(navPath); // timpa hasil bake lama bila ada
            AssetDatabase.CreateAsset(surface.navMeshData, navPath);
            EditorUtility.SetDirty(surface);
        }

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeGameObject = enemy;

        Debug.Log("[Praktikum05] Scene dibuat: " + ScenePath + ". Tekan Play untuk menguji.");
    }

    // Bake ulang NavMesh pada scene yang sedang terbuka (mis. setelah memindahkan obstacle).
    [MenuItem("Praktikum 05/Rebake NavMesh")]
    public static void Rebake()
    {
        NavMeshSurface surface = Object.FindFirstObjectByType<NavMeshSurface>();

        if (surface == null)
        {
            Debug.LogError("[Praktikum05] NavMeshSurface tidak ditemukan di scene ini.");
            return;
        }

        string oldPath = surface.navMeshData != null
            ? AssetDatabase.GetAssetPath(surface.navMeshData)
            : "";

        surface.BuildNavMesh();

        if (surface.navMeshData == null)
        {
            Debug.LogError("[Praktikum05] Bake gagal.");
            return;
        }

        string newPath = string.IsNullOrEmpty(oldPath)
            ? NavMeshFolder + "/NavMesh-Navigation.asset"
            : oldPath;

        if (string.IsNullOrEmpty(oldPath))
            EnsureFolder("Assets/Scenes", "Praktikum05_FSM");
        else
            AssetDatabase.DeleteAsset(oldPath);

        AssetDatabase.CreateAsset(surface.navMeshData, newPath);
        EditorUtility.SetDirty(surface);

        Scene scene = surface.gameObject.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("[Praktikum05] NavMesh di-bake ulang dan scene disimpan.");
    }

    // Tombol Restart (R) memuat ulang scene lewat nama, sehingga scene harus ada di Build Settings.
    [InitializeOnLoadMethod]
    private static void EnsureSceneInBuildSettings()
    {
        if (!System.IO.File.Exists(ScenePath))
            return;

        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

        foreach (EditorBuildSettingsScene existing in scenes)
        {
            if (existing.path == ScenePath)
                return;
        }

        EditorBuildSettingsScene[] updated = new EditorBuildSettingsScene[scenes.Length + 1];
        System.Array.Copy(scenes, updated, scenes.Length);
        updated[scenes.Length] = new EditorBuildSettingsScene(ScenePath, true);
        EditorBuildSettings.scenes = updated;
    }

    // ------------------------------------------------------------------

    private static void CreateObstacle(string name, Transform parent, Vector3 position, Vector3 scale, int layer, Material mat)
    {
        GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstacle.name = name;
        obstacle.transform.SetParent(parent);
        obstacle.transform.position = position;
        obstacle.transform.localScale = scale;
        obstacle.layer = layer;
        obstacle.GetComponent<Renderer>().sharedMaterial = mat;
    }

    private static Transform CreateEmpty(string name, Transform parent, Vector3 position)
    {
        GameObject go = new GameObject(name);
        if (parent != null)
            go.transform.SetParent(parent);
        go.transform.position = position;
        return go.transform;
    }

    // Kubus kecil di depan kapsul agar arah hadap (forward) terlihat saat demo FOV.
    private static void CreateMarker(Transform parent, Material mat)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "FacingMarker";
        Object.DestroyImmediate(marker.GetComponent<BoxCollider>());
        marker.transform.SetParent(parent, false);
        marker.transform.localPosition = new Vector3(0f, 0.5f, 0.5f);
        marker.transform.localScale = new Vector3(0.3f, 0.3f, 0.5f);
        marker.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // Player/Enemy jangan ikut di-bake menjadi bagian NavMesh.
    private static void IgnoreInNavMeshBuild(GameObject go)
    {
        NavMeshModifier modifier = go.AddComponent<NavMeshModifier>();
        modifier.ignoreFromBuild = true;
    }

    private static Material GetMaterial(string name, Color color)
    {
        string path = "Assets/Materials/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        Material mat = new Material(shader);
        mat.color = color;
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);
        if (existing >= 0)
            return existing;

        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty slot = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(slot.stringValue))
            {
                slot.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return i;
            }
        }

        Debug.LogError("[Praktikum05] Tidak ada slot Layer kosong untuk '" + layerName + "'.");
        return 0;
    }

    private static void SetRef(Object target, string field, Object value)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogError("[Praktikum05] Field tidak ditemukan: " + target.GetType().Name + "." + field);
            return;
        }
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetRefArray(Object target, string field, Object[] values)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogError("[Praktikum05] Field tidak ditemukan: " + target.GetType().Name + "." + field);
            return;
        }
        prop.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetLayerMask(Object target, string field, int mask)
    {
        SerializedObject so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(field);
        if (prop == null)
        {
            Debug.LogError("[Praktikum05] Field tidak ditemukan: " + target.GetType().Name + "." + field);
            return;
        }
        prop.intValue = mask;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
