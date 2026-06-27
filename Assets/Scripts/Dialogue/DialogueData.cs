using System;
using UnityEngine;

[Serializable]
public class DialogueChoice
{
    [TextArea(2, 4)]
    public string playerText;

    [TextArea(2, 5)]
    public string[] npcResponses;
}

public enum DialogueNodeType
{
    NpcLine,
    PlayerChoice
}

[Serializable]
public class DialogueNode
{
    public DialogueNodeType nodeType = DialogueNodeType.NpcLine;

    [TextArea(2, 6)]
    public string text;

    public DialogueChoice[] choices;
}

[Serializable]
public class DialogueStage
{
    public string stageName = "Greeting";
    [TextArea(1, 3)]
    public string stageDescription;
    public DialogueNode[] nodes;
}
