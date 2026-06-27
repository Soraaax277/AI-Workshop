using UnityEngine;

[RequireComponent(typeof(Collider))]
public class NPCDialogue : MonoBehaviour, IInteractable
{
    [Header("Identity")]
    [SerializeField] string npcName = "Villager";

    [Header("Progressive Dialogue")]
    [SerializeField] DialogueAsset dialogueAsset;
    [SerializeField] DialogueStage[] inlineStages;
    [SerializeField] bool repeatFinalStage = true;
    [SerializeField] bool facePlayerOnTalk = true;

    int conversationCount;
    bool isTalking;

    public int ConversationCount => conversationCount;

    public string GetInteractionPrompt()
    {
        return $"Press E to talk to {npcName}";
    }

    public bool CanInteract(GameObject interactor)
    {
        RecoverFromStuckState();

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsPlaying)
            return false;

        return !isTalking && StageHasContent(GetCurrentStage());
    }

    public void Interact(GameObject interactor)
    {
        RecoverFromStuckState();

        if (isTalking)
            return;

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsPlaying)
            return;

        DialogueStage stage = GetCurrentStage();
        if (!StageHasContent(stage))
        {
            Debug.LogWarning($"NPCDialogue on {name} has no valid dialogue nodes assigned.", this);
            return;
        }

        if (facePlayerOnTalk && interactor != null)
            FaceInteractor(interactor.transform);

        if (DialogueManager.Instance == null)
        {
            var managerObject = new GameObject("DialogueManager");
            managerObject.AddComponent<DialogueManager>();
        }

        if (!DialogueManager.Instance.StartDialogue(npcName, stage, OnDialogueFinished))
            return;

        isTalking = true;
    }

    DialogueStage GetCurrentStage()
    {
        DialogueStage[] stages = GetStages();
        if (stages == null || stages.Length == 0)
            return null;

        if (conversationCount < stages.Length)
            return stages[conversationCount];

        return repeatFinalStage ? stages[stages.Length - 1] : null;
    }

    DialogueStage[] GetStages()
    {
        if (dialogueAsset != null && dialogueAsset.stages != null && dialogueAsset.stages.Length > 0)
            return dialogueAsset.stages;

        return inlineStages;
    }

    static bool StageHasContent(DialogueStage stage)
    {
        return stage != null && stage.nodes != null && stage.nodes.Length > 0;
    }

    void OnDisable()
    {
        isTalking = false;
    }

    void RecoverFromStuckState()
    {
        if (!isTalking)
            return;

        if (DialogueManager.Instance == null || !DialogueManager.Instance.IsPlaying)
            isTalking = false;
    }

    void OnDialogueFinished()
    {
        isTalking = false;
        conversationCount++;
    }

    void FaceInteractor(Transform interactor)
    {
        Vector3 direction = interactor.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    public void ResetConversationProgress()
    {
        conversationCount = 0;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (dialogueAsset != null)
            npcName = dialogueAsset.npcDisplayName;
    }
#endif
}
