using UnityEngine;
using UnityEngine.UI;

public class InteractionPromptUI : MonoBehaviour
{
    [SerializeField] PlayerInteractor playerInteractor;
    [SerializeField] Text promptText;

    Canvas canvas;
    bool dialogueEventsSubscribed;

    void Awake()
    {
        if (playerInteractor == null)
            playerInteractor = FindFirstObjectByType<PlayerInteractor>();

        EnsureUI();
        SetPrompt(null);
    }

    void Start()
    {
        SubscribeToDialogueManager();
    }

    void Update()
    {
        TrySubscribeToDialogueEvents();

        if (playerInteractor != null)
            RefreshPrompt();
    }

    void TrySubscribeToDialogueEvents()
    {
        if (dialogueEventsSubscribed || DialogueManager.Instance == null)
            return;

        DialogueManager.Instance.OnDialogueEnded += RefreshPrompt;
        dialogueEventsSubscribed = true;
    }

    void SubscribeToDialogueManager()
    {
        TrySubscribeToDialogueEvents();
    }

    void OnEnable()
    {
        if (playerInteractor != null)
            playerInteractor.OnTargetChanged += HandleTargetChanged;
    }

    void OnDisable()
    {
        if (playerInteractor != null)
            playerInteractor.OnTargetChanged -= HandleTargetChanged;

        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnDialogueEnded -= RefreshPrompt;

        dialogueEventsSubscribed = false;
    }

    void HandleTargetChanged(IInteractable target)
    {
        RefreshPrompt();
    }

    void RefreshPrompt()
    {
        if (playerInteractor == null)
            return;

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsPlaying)
        {
            SetPrompt(null);
            return;
        }

        var target = playerInteractor.CurrentTarget;
        SetPrompt(target != null && target.CanInteract(playerInteractor.gameObject)
            ? target.GetInteractionPrompt()
            : null);
    }

    void SetPrompt(string message)
    {
        EnsureUI();

        bool hasMessage = !string.IsNullOrWhiteSpace(message);
        promptText.gameObject.SetActive(hasMessage);
        if (hasMessage)
            promptText.text = message;
    }

    void EnsureUI()
    {
        if (promptText != null)
            return;

        canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();
        }

        var textObject = new GameObject("PromptText", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(transform, false);

        var rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(600f, 40f);
        rect.anchoredPosition = new Vector2(0f, 24f);

        promptText = textObject.GetComponent<Text>();
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptText.fontSize = 18;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = Color.white;
    }
}
