using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] Text speakerText;
    [SerializeField] Text lineText;
    [SerializeField] Text continueHintText;
    [SerializeField] RectTransform choicesContainer;

    readonly System.Collections.Generic.List<Button> choiceButtons = new();
    Canvas canvas;

    public event Action<int> OnChoiceSelected;

    void Awake()
    {
        EnsureUI();
        Hide();
    }

    public void ShowNpcLine(string speaker, string line)
    {
        EnsureUI();
        panel.SetActive(true);
        HideChoiceButtons();
        speakerText.text = speaker;
        lineText.text = line;
        continueHintText.gameObject.SetActive(true);
        continueHintText.text = "Space / Click to continue";
    }

    public void ShowPlayerLine(string line)
    {
        EnsureUI();
        panel.SetActive(true);
        HideChoiceButtons();
        speakerText.text = "You";
        lineText.text = line;
        continueHintText.gameObject.SetActive(true);
        continueHintText.text = "Space / Click to continue";
    }

    public void ShowChoices(string npcName, DialogueChoice[] choices)
    {
        EnsureUI();
        panel.SetActive(true);
        speakerText.text = npcName;
        lineText.text = "What will you say?";
        continueHintText.gameObject.SetActive(true);
        continueHintText.text = "Click a choice below";

        EnsureChoiceButtons(choices.Length);

        for (int i = 0; i < choices.Length; i++)
        {
            int capturedIndex = i;
            var button = choiceButtons[i];
            button.gameObject.SetActive(true);

            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = choices[i].playerText;
                label.raycastTarget = false;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnChoiceSelected?.Invoke(capturedIndex));
        }

        for (int i = choices.Length; i < choiceButtons.Count; i++)
            choiceButtons[i].gameObject.SetActive(false);
    }

    public void Hide()
    {
        HideChoiceButtons();

        if (panel != null)
            panel.SetActive(false);
    }

    void HideChoiceButtons()
    {
        foreach (var button in choiceButtons)
        {
            if (button != null)
                button.gameObject.SetActive(false);
        }
    }

    void EnsureChoiceButtons(int requiredCount)
    {
        EnsureUI();

        while (choiceButtons.Count < requiredCount)
        {
            var buttonObject = CreateChoiceButton(choicesContainer);
            choiceButtons.Add(buttonObject.GetComponent<Button>());
        }
    }

    void EnsureUI()
    {
        if (panel != null && speakerText != null && lineText != null && choicesContainer != null)
            return;

        canvas = gameObject.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.AddComponent<GraphicRaycaster>();
        }

        panel = CreatePanel("DialoguePanel", transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(920f, 320f), new Vector2(0f, 36f));
        var panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.82f);

        speakerText = CreateText("SpeakerText", panel.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -18f), new Vector2(-24f, -52f), 22, FontStyle.Bold, TextAnchor.UpperLeft);
        lineText = CreateText("LineText", panel.transform, new Vector2(0f, 0.35f), new Vector2(1f, 1f), new Vector2(24f, 0f), new Vector2(-24f, -58f), 18, FontStyle.Normal, TextAnchor.UpperLeft);
        continueHintText = CreateText("ContinueHint", panel.transform, new Vector2(0f, 0f), new Vector2(1f, 0.32f), new Vector2(24f, 8f), new Vector2(-24f, -8f), 14, FontStyle.Italic, TextAnchor.LowerRight);

        var choicesObject = new GameObject("ChoicesContainer", typeof(RectTransform));
        choicesObject.transform.SetParent(panel.transform, false);
        choicesContainer = choicesObject.GetComponent<RectTransform>();
        choicesContainer.anchorMin = new Vector2(0f, 0f);
        choicesContainer.anchorMax = new Vector2(1f, 0.35f);
        choicesContainer.offsetMin = new Vector2(20f, 16f);
        choicesContainer.offsetMax = new Vector2(-20f, 0f);

        var layout = choicesObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 8f;
        layout.childControlHeight = true;
        layout.childControlWidth = true;
        layout.childForceExpandHeight = false;
        layout.childForceExpandWidth = true;

        EnsureEventSystem();
    }

    static void EnsureEventSystem()
    {
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            var eventSystemObject = new GameObject("EventSystem");
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
        }

        var legacyModule = eventSystem.GetComponent<StandaloneInputModule>();
        if (legacyModule != null)
            Destroy(legacyModule);

        var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
        if (inputModule == null)
            inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();

        if (inputModule.actionsAsset == null)
        {
            var assets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i].name == "InputSystem_Actions")
                {
                    inputModule.actionsAsset = assets[i];
                    break;
                }
            }
        }
    }

    static GameObject CreateChoiceButton(Transform parent)
    {
        var buttonObject = new GameObject("ChoiceButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(parent, false);

        var layoutElement = buttonObject.GetComponent<LayoutElement>();
        layoutElement.minHeight = 34f;
        layoutElement.preferredHeight = 38f;

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.18f, 0.2f, 0.28f, 0.95f);
        image.raycastTarget = true;

        var button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        var colors = button.colors;
        colors.normalColor = new Color(0.18f, 0.2f, 0.28f, 0.95f);
        colors.highlightedColor = new Color(0.28f, 0.34f, 0.48f, 1f);
        colors.pressedColor = new Color(0.12f, 0.14f, 0.2f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        var label = CreateText("Label", buttonObject.transform, Vector2.zero, Vector2.one, new Vector2(12f, 4f), new Vector2(-12f, -4f), 16, FontStyle.Normal, TextAnchor.MiddleLeft);
        label.horizontalOverflow = HorizontalWrapMode.Wrap;

        return buttonObject;
    }

    static GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPosition)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;

        return go;
    }

    static Text CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, int fontSize, FontStyle fontStyle, TextAnchor alignment)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);

        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        var text = go.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.alignment = alignment;
        text.color = Color.white;
        text.supportRichText = true;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;

        return text;
    }
}
