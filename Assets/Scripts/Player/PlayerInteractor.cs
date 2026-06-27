using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] Transform rayOrigin;
    [SerializeField] float interactRange = 4f;
    [SerializeField] float interactRadius = 0.35f;
    [SerializeField] LayerMask interactMask = ~0;

    [Header("Input")]
    [SerializeField] InputActionAsset inputActions;

    InputAction interactAction;
    IInteractable currentTarget;

    public IInteractable CurrentTarget => currentTarget;
    public event System.Action<IInteractable> OnTargetChanged;

    void Awake()
    {
        TryAssignInputActions();

        if (inputActions == null)
        {
            Debug.LogError("PlayerInteractor: Assign InputSystem_Actions to the Input Actions field.", this);
            return;
        }

        interactAction = inputActions.FindActionMap("Player", true).FindAction("Interact", true);
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

    void OnEnable() => interactAction?.Enable();
    void OnDisable() => interactAction?.Disable();

    void Update()
    {
        UpdateTarget();

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsPlaying)
            return;

        if (interactAction == null || currentTarget == null)
            return;

        if (!WasInteractPressedThisFrame())
            return;

        if (currentTarget.CanInteract(gameObject))
            currentTarget.Interact(gameObject);
    }

    bool WasInteractPressedThisFrame()
    {
        return interactAction.WasPressedThisFrame() || interactAction.WasPerformedThisFrame();
    }

    void UpdateTarget()
    {
        IInteractable found = null;

        if (rayOrigin != null)
        {
            Vector3 origin = rayOrigin.position;
            Vector3 direction = rayOrigin.forward;

            if (Physics.SphereCast(origin, interactRadius, direction, out RaycastHit hit, interactRange, interactMask, QueryTriggerInteraction.Collide))
                found = InteractableUtility.GetFromCollider(hit.collider);

            if (found == null && Physics.Raycast(origin, direction, out hit, interactRange, interactMask, QueryTriggerInteraction.Collide))
                found = InteractableUtility.GetFromCollider(hit.collider);

            if (found == null)
                found = FindInteractableInFront(origin, direction);
        }

        if (found != currentTarget)
        {
            currentTarget = found;
            OnTargetChanged?.Invoke(currentTarget);
        }
    }

    IInteractable FindInteractableInFront(Vector3 origin, Vector3 direction)
    {
        Vector3 samplePoint = origin + direction * (interactRange * 0.65f);
        Collider[] overlaps = Physics.OverlapSphere(samplePoint, interactRadius * 1.75f, interactMask, QueryTriggerInteraction.Collide);

        IInteractable best = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < overlaps.Length; i++)
        {
            IInteractable candidate = InteractableUtility.GetFromCollider(overlaps[i]);
            if (candidate == null)
                continue;

            Vector3 toTarget = overlaps[i].transform.position - origin;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.001f)
                continue;

            float angle = Vector3.Angle(direction, toTarget.normalized);
            if (angle > 55f)
                continue;

            float score = angle + toTarget.magnitude * 0.15f;
            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        return best;
    }

    void OnDrawGizmosSelected()
    {
        if (rayOrigin == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(rayOrigin.position, rayOrigin.position + rayOrigin.forward * interactRange);
        Gizmos.DrawWireSphere(rayOrigin.position + rayOrigin.forward * interactRange, interactRadius);
    }
}
