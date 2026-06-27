using UnityEngine;
using UnityEngine.InputSystem;

public static class GameRuntimeSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void ConfigureScene()
    {
        ConfigurePlayer();
        EnsureManagers();
    }

    static void ConfigurePlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        if (player.GetComponent<PlayerInteractor>() == null)
            player.AddComponent<PlayerInteractor>();

        if (player.GetComponent<PlayerController>() == null)
            player.AddComponent<PlayerController>();
    }

    static void EnsureManagers()
    {
        if (Object.FindFirstObjectByType<DialogueManager>() == null)
        {
            var managerObject = new GameObject("DialogueManager");
            var manager = managerObject.AddComponent<DialogueManager>();

            InputActionAsset inputActions = null;
            var assets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i].name == "InputSystem_Actions")
                {
                    inputActions = assets[i];
                    break;
                }
            }

            if (inputActions != null)
            {
                var field = typeof(DialogueManager).GetField("inputActions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(manager, inputActions);
            }
        }

        if (Object.FindFirstObjectByType<InteractionPromptUI>() == null)
            new GameObject("InteractionPromptUI").AddComponent<InteractionPromptUI>();
    }
}
