#if UNITY_EDITOR
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public static class GameSetupMenu
{
    const string InputActionsPath = "Assets/InputSystem_Actions.inputactions";

    [MenuItem("Game/Setup Demo Scene")]
    static void SetupDemoScene()
    {
        EnsureTags();
        EnsureLayers();
        ApplyLineOfSightLayers();
        var ground = CreateGround();
        CreatePlayer();
        var wall = CreateCoverWall();
        CreateEnemy();
        CreateNpc();
        CreateManagers();
        BakeNavMesh(ground, wall);

        Debug.Log("Demo scene setup complete.");
    }

    [MenuItem("Game/Fix Line Of Sight Layers")]
    static void FixLineOfSightLayers()
    {
        EnsureLayers();
        ApplyLineOfSightLayers();
        Debug.Log("Line of sight layers applied. Player and Enemy are ignored by obstruction checks; walls on Default still block vision.");
    }

    [MenuItem("Game/Create Cover Wall")]
    static void CreateCoverWallMenuItem()
    {
        EnsureLayers();
        ApplyLineOfSightLayers();
        var wall = CreateCoverWall();
        var ground = GameObject.Find("Ground");
        BakeNavMesh(ground, wall);
        Selection.activeGameObject = wall;
        Debug.Log("Cover wall created. Hide behind it to break enemy line of sight.");
    }

    [MenuItem("Game/Create Sample NPC Dialogue Asset")]
    static void CreateSampleDialogueAsset()
    {
        const string path = "Assets/Data/Dialogue/ElderMaraDialogue.asset";
        System.IO.Directory.CreateDirectory("Assets/Data/Dialogue");

        var asset = ElderMaraDialogueBuilder.CreateAsset();
        var existing = AssetDatabase.LoadAssetAtPath<DialogueAsset>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);

        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = asset;
        Debug.Log($"Created expanded branching dialogue asset at {path}");
    }

    static void EnsureTags()
    {
        AddTagIfMissing("Enemy");
        AddTagIfMissing("NPC");
    }

    static void AddTagIfMissing(string tag)
    {
        var tags = UnityEditorInternal.InternalEditorUtility.tags;
        for (int i = 0; i < tags.Length; i++)
        {
            if (tags[i] == tag)
                return;
        }

        UnityEditorInternal.InternalEditorUtility.AddTag(tag);
    }

    static void EnsureLayers()
    {
        EnsureLayer(6, "Player");
        EnsureLayer(7, "Enemy");
    }

    static void EnsureLayer(int index, string layerName)
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layersProp = tagManager.FindProperty("layers");
        var layer = layersProp.GetArrayElementAtIndex(index);
        if (string.IsNullOrEmpty(layer.stringValue))
        {
            layer.stringValue = layerName;
            tagManager.ApplyModifiedProperties();
        }
    }

    static void ApplyLineOfSightLayers()
    {
        int playerLayer = LayerMask.NameToLayer("Player");
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && playerLayer >= 0)
            SetLayerRecursively(player, playerLayer);

        var enemy = GameObject.FindGameObjectWithTag("Enemy");
        if (enemy != null && enemyLayer >= 0)
            SetLayerRecursively(enemy, enemyLayer);
    }

    static void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    static GameObject CreateGround()
    {
        if (GameObject.Find("Ground") != null)
            return GameObject.Find("Ground");

        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(4f, 1f, 4f);
        ground.isStatic = true;

        if (ground.GetComponent<NavMeshSurface>() == null)
            ground.AddComponent<NavMeshSurface>();

        return ground;
    }

    static void BakeNavMesh(GameObject ground, GameObject wall)
    {
        if (ground == null)
            return;

        var surface = ground.GetComponent<NavMeshSurface>();
        if (surface == null)
            return;

        if (wall != null)
        {
            var obstacle = wall.GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if (obstacle != null)
                obstacle.carving = true;
        }

        surface.BuildNavMesh();
    }

    static GameObject CreateCoverWall()
    {
        if (GameObject.Find("CoverWall") != null)
            return GameObject.Find("CoverWall");

        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "CoverWall";
        wall.transform.position = new Vector3(2.5f, 1.5f, 0.5f);
        wall.transform.localScale = new Vector3(1.2f, 3f, 8f);
        wall.isStatic = true;
        wall.layer = LayerMask.NameToLayer("Default");

        var renderer = wall.GetComponent<Renderer>();
        if (renderer != null)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.color = new Color(0.45f, 0.42f, 0.38f);
            renderer.sharedMaterial = material;
        }

        var obstacle = wall.AddComponent<UnityEngine.AI.NavMeshObstacle>();
        obstacle.carving = true;
        obstacle.shape = UnityEngine.AI.NavMeshObstacleShape.Box;
        obstacle.center = Vector3.zero;
        obstacle.size = Vector3.one;

        return wall;
    }

    static void CreatePlayer()
    {
        if (GameObject.FindGameObjectWithTag("Player") != null)
            return;

        var existingCamera = GameObject.Find("Main Camera");
        if (existingCamera != null)
            existingCamera.SetActive(false);

        var player = new GameObject("Player");
        player.tag = "Player";
        player.layer = LayerMask.NameToLayer("Player");
        player.transform.position = new Vector3(0f, 1f, -4f);

        var controller = player.AddComponent<CharacterController>();
        controller.height = 2f;
        controller.center = new Vector3(0f, 1f, 0f);

        var cameraPivot = new GameObject("CameraPivot").transform;
        cameraPivot.SetParent(player.transform);
        cameraPivot.localPosition = new Vector3(0f, 1.6f, 0f);

        var cameraObject = new GameObject("PlayerCamera");
        cameraObject.transform.SetParent(cameraPivot);
        cameraObject.transform.localPosition = Vector3.zero;
        var camera = cameraObject.AddComponent<Camera>();
        camera.tag = "MainCamera";
        cameraObject.AddComponent<AudioListener>();

        var inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputActionsPath);

        var playerController = player.AddComponent<PlayerController>();
        SetPrivateField(playerController, "cameraPivot", cameraPivot);
        SetPrivateField(playerController, "inputActions", inputAsset);

        var interactor = player.AddComponent<PlayerInteractor>();
        SetPrivateField(interactor, "rayOrigin", cameraObject.transform);
        SetPrivateField(interactor, "inputActions", inputAsset);

        player.AddComponent<PlayerHealth>();
    }

    static void CreateEnemy()
    {
        if (GameObject.Find("Enemy") != null)
            return;

        var enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        enemy.name = "Enemy";
        enemy.tag = "Enemy";
        enemy.layer = LayerMask.NameToLayer("Enemy");
        enemy.transform.position = new Vector3(6f, 1f, 4f);

        Object.DestroyImmediate(enemy.GetComponent<CapsuleCollider>());
        var collider = enemy.AddComponent<CapsuleCollider>();
        collider.height = 2f;
        collider.center = new Vector3(0f, 1f, 0f);

        enemy.AddComponent<UnityEngine.AI.NavMeshAgent>();
        enemy.AddComponent<EnemyHealth>();
        enemy.AddComponent<EnemyController>();

        var patrolA = new GameObject("PatrolPoint_A").transform;
        patrolA.position = new Vector3(4f, 0f, 6f);
        var patrolB = new GameObject("PatrolPoint_B").transform;
        patrolB.position = new Vector3(8f, 0f, 2f);

        var enemyController = enemy.GetComponent<EnemyController>();
        SetPrivateField(enemyController, "patrolPoints", new[] { patrolA, patrolB });
    }

    static void CreateNpc()
    {
        const string dialoguePath = "Assets/Data/Dialogue/ElderMaraDialogue.asset";
        if (!System.IO.File.Exists(dialoguePath))
            CreateSampleDialogueAsset();

        var dialogueAsset = AssetDatabase.LoadAssetAtPath<DialogueAsset>(dialoguePath);
        var existingNpc = GameObject.Find("NPC_ElderMara");

        if (existingNpc != null)
        {
            var existingDialogue = existingNpc.GetComponent<NPCDialogue>();
            if (existingDialogue == null)
                existingDialogue = existingNpc.AddComponent<NPCDialogue>();

            SetPrivateField(existingDialogue, "npcName", "Elder Mara");
            SetPrivateField(existingDialogue, "dialogueAsset", dialogueAsset);
            return;
        }

        var npc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        npc.name = "NPC_ElderMara";
        npc.tag = "NPC";
        npc.transform.position = new Vector3(-3f, 1f, 2f);

        Object.DestroyImmediate(npc.GetComponent<CapsuleCollider>());
        var collider = npc.AddComponent<CapsuleCollider>();
        collider.height = 2f;
        collider.center = new Vector3(0f, 1f, 0f);
        collider.isTrigger = false;

        var dialogue = npc.AddComponent<NPCDialogue>();
        SetPrivateField(dialogue, "npcName", "Elder Mara");
        SetPrivateField(dialogue, "dialogueAsset", dialogueAsset);
    }

    static void CreateManagers()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            var uiModule = eventSystemObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            uiModule.actionsAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputActionsPath);
        }

        if (Object.FindFirstObjectByType<DialogueManager>() == null)
        {
            var manager = new GameObject("DialogueManager");
            var dialogueManager = manager.AddComponent<DialogueManager>();
            var inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputActionsPath);
            SetPrivateField(dialogueManager, "inputActions", inputAsset);
        }

        if (Object.FindFirstObjectByType<InteractionPromptUI>() == null)
        {
            var prompt = new GameObject("InteractionPromptUI");
            prompt.AddComponent<InteractionPromptUI>();
        }
    }

    static void SetPrivateField(Object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(target, value);
    }
}
#endif
