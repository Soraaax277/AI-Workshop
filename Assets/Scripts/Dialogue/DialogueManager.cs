using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    enum DialogueMode
    {
        NpcLine,
        WaitingForChoice,
        ChoiceResponse
    }

    public static DialogueManager Instance { get; private set; }

    [SerializeField] DialogueUI dialogueUI;
    [SerializeField] InputActionAsset inputActions;

    InputAction submitAction;
    InputAction clickAction;
    InputAction jumpAction;
    InputAction attackAction;

    DialogueStage activeStage;
    string activeSpeakerName;
    int nodeIndex;
    int responseLineIndex;
    string[] activeResponseLines;
    DialogueMode mode;
    bool isPlaying;
    Action onDialogueComplete;

    public bool IsPlaying => isPlaying;
    public bool IsWaitingForChoice => isPlaying && mode == DialogueMode.WaitingForChoice;
    public event Action OnDialogueEnded;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dialogueUI == null)
            dialogueUI = FindFirstObjectByType<DialogueUI>();

        if (dialogueUI == null)
        {
            var uiObject = new GameObject("DialogueUI");
            uiObject.transform.SetParent(transform);
            dialogueUI = uiObject.AddComponent<DialogueUI>();
        }

        dialogueUI.Hide();
        dialogueUI.OnChoiceSelected += HandleChoiceSelected;
        TryAssignInputActions();
        BindInputActions();
    }

    void TryAssignInputActions()
    {
        if (inputActions != null)
            return;

        var assets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
        for (int i = 0; i < assets.Length; i++)
        {
            if (assets[i].name == "InputSystem_Actions")
            {
                inputActions = assets[i];
                return;
            }
        }
    }

    void OnDestroy()
    {
        if (dialogueUI != null)
            dialogueUI.OnChoiceSelected -= HandleChoiceSelected;
    }

    void OnEnable()
    {
        submitAction?.Enable();
        clickAction?.Enable();
        jumpAction?.Enable();
        attackAction?.Enable();
    }

    void OnDisable()
    {
        submitAction?.Disable();
        clickAction?.Disable();
        jumpAction?.Disable();
        attackAction?.Disable();
    }

    void BindInputActions()
    {
        if (inputActions == null)
            return;

        var uiMap = inputActions.FindActionMap("UI", true);
        submitAction = uiMap.FindAction("Submit", true);
        clickAction = uiMap.FindAction("Click", true);

        var playerMap = inputActions.FindActionMap("Player", true);
        jumpAction = playerMap.FindAction("Jump", true);
        attackAction = playerMap.FindAction("Attack", true);
    }

    void Update()
    {
        if (!isPlaying)
            return;

        if (mode == DialogueMode.WaitingForChoice)
            return;

        if (WasAdvancePressed())
            Advance();
    }

    bool WasAdvancePressed()
    {
        if (submitAction != null && submitAction.WasPerformedThisFrame())
            return true;

        if (clickAction != null && clickAction.WasPerformedThisFrame())
            return true;

        if (jumpAction != null && jumpAction.WasPerformedThisFrame())
            return true;

        if (attackAction != null && attackAction.WasPerformedThisFrame())
            return true;

        if (inputActions != null)
            return false;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
                return true;
        }

        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    }

    public bool StartDialogue(string speakerName, DialogueStage stage, Action onComplete = null)
    {
        if (stage == null || stage.nodes == null || stage.nodes.Length == 0)
        {
            onComplete?.Invoke();
            return false;
        }

        activeStage = stage;
        activeSpeakerName = speakerName;
        nodeIndex = 0;
        responseLineIndex = 0;
        activeResponseLines = null;
        onDialogueComplete = onComplete;
        isPlaying = true;

        SetPlayerControl(false);
        ShowCurrentNode();
        return true;
    }

    void ShowCurrentNode()
    {
        if (activeStage == null || nodeIndex >= activeStage.nodes.Length)
        {
            EndDialogue();
            return;
        }

        DialogueNode node = activeStage.nodes[nodeIndex];

        if (node.nodeType == DialogueNodeType.PlayerChoice)
        {
            if (node.choices == null || node.choices.Length == 0)
            {
                nodeIndex++;
                ShowCurrentNode();
                return;
            }

            mode = DialogueMode.WaitingForChoice;
            dialogueUI.ShowChoices(activeSpeakerName, node.choices);
            return;
        }

        mode = DialogueMode.NpcLine;
        dialogueUI.ShowNpcLine(activeSpeakerName, node.text);
    }

    void HandleChoiceSelected(int choiceIndex)
    {
        if (!isPlaying || mode != DialogueMode.WaitingForChoice || activeStage == null)
            return;

        DialogueNode node = activeStage.nodes[nodeIndex];
        if (node.choices == null || choiceIndex < 0 || choiceIndex >= node.choices.Length)
            return;

        DialogueChoice choice = node.choices[choiceIndex];
        activeResponseLines = choice.npcResponses;
        responseLineIndex = 0;

        if (activeResponseLines == null || activeResponseLines.Length == 0)
        {
            nodeIndex++;
            ShowCurrentNode();
            return;
        }

        mode = DialogueMode.ChoiceResponse;
        responseLineIndex = 0;
        dialogueUI.ShowPlayerLine(choice.playerText);
    }

    public void Advance()
    {
        if (!isPlaying || activeStage == null)
            return;

        if (mode == DialogueMode.NpcLine)
        {
            nodeIndex++;
            ShowCurrentNode();
            return;
        }

        if (mode == DialogueMode.ChoiceResponse)
        {
            if (activeResponseLines == null || responseLineIndex >= activeResponseLines.Length)
            {
                nodeIndex++;
                activeResponseLines = null;
                responseLineIndex = 0;
                ShowCurrentNode();
                return;
            }

            dialogueUI.ShowNpcLine(activeSpeakerName, activeResponseLines[responseLineIndex]);
            responseLineIndex++;
        }
    }

    public void EndDialogue()
    {
        if (!isPlaying)
            return;

        isPlaying = false;
        activeStage = null;
        activeResponseLines = null;
        dialogueUI.Hide();
        SetPlayerControl(true);

        var callback = onDialogueComplete;
        onDialogueComplete = null;
        callback?.Invoke();
        OnDialogueEnded?.Invoke();
    }

    void SetPlayerControl(bool enabled)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            return;

        var controller = player.GetComponent<PlayerController>();
        if (controller != null)
            controller.SetInputEnabled(enabled);
    }
}
