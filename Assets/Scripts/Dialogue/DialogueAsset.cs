using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogue", menuName = "Game/Dialogue Asset")]
public class DialogueAsset : ScriptableObject
{
    public string npcDisplayName = "Villager";
    public DialogueStage[] stages;
}
