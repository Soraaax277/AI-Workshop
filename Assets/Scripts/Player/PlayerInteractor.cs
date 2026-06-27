using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] Transform rayOrigin;
    [SerializeField] float interactRange = 5f;
    [SerializeField] float interactRadius = 0.5f;
    [SerializeField] LayerMask interactMask = ~0;

    [Header("Input")]
    [SerializeField] InputActionAsset inputActions;

    InputAction interactAction;
    InputActionMap playerActionMap;
    IInteractable currentTarget;

    public IInteractable CurrentTarget => currentTarget;
    public event System.Action<IInteractable> OnTargetChanged;

    void Awake()
    {
        ResolveRayOrigin();
        ResolveInputActions();
    }

    void ResolveRayOrigin()
    {
        if (rayOrigin != null)
            return;

        var camera = GetComponentInChildren<Camera>();
        if (camera != null)
            rayOrigin = camera.transform;
        else if (Camera.main != null)
            rayOrigin = Camera.main.transform;
    }

    void ResolveInputActions()
    {
        TryAssignInputActions();

        if (inputActions == null)
        {
            var assets = Resources.FindObjectsOfTypeAll<InputActionAsset>();
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i].name == "InputSystem_Actions")
                {
                    inputActions = assets[i];
                    break;
                }
            }
        }

        if (inputActions == null)
        {
            Debug.LogError("PlayerInteractor: Assign InputSystem_Actions to the Input Actions field.", this);
            return;
        }

        playerActionMap = inputActions.FindActionMap("Player", true);
        playerActionMap.Enable();
        interactAction = playerActionMap.FindAction("Interact", true);
        interactAction.Enable();
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

    void OnEnable()
    {
        playerActionMap?.Enable();
        interactAction?.Enable();
    }

    void OnDisable()
    {
        interactAction?.Disable();
    }

    void Update()
    {
        UpdateTarget();

        if (DialogueManager.Instance != null && DialogueManager.Instance.IsPlaying)
            return;

        if (currentTarget == null)
            return;

        if (!WasInteractPressedThisFrame())
            return;

        if (currentTarget.CanInteract(gameObject))
            currentTarget.Interact(gameObject);
    }

    bool WasInteractPressedThisFrame()
    {
        if (interactAction != null)
        {
            if (interactAction.triggered || interactAction.WasPressedThisFrame() || interactAction.WasPerformedThisFrame())
                return true;
        }

        return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
    }

    void UpdateTarget()
    {
        IInteractable found = FindTargetFromView();
        if (found == null)
            found = FindNearestInteractable();

        if (found != currentTarget)
        {
            currentTarget = found;
            OnTargetChanged?.Invoke(currentTarget);
        }
    }

    IInteractable FindTargetFromView()
    {
        if (rayOrigin == null)
            return null;

        Vector3 origin = rayOrigin.position;
        Vector3 direction = rayOrigin.forward;

        if (Physics.SphereCast(origin, interactRadius, direction, out RaycastHit hit, interactRange, interactMask, QueryTriggerInteraction.Collide))
        {
            IInteractable target = GetInteractableFromHit(hit.collider);
            if (target != null)
                return target;
        }

        if (Physics.Raycast(origin, direction, out hit, interactRange, interactMask, QueryTriggerInteraction.Collide))
        {
            IInteractable target = GetInteractableFromHit(hit.collider);
            if (target != null)
                return target;
        }

        return FindInteractableInFront(origin, direction);
    }

    IInteractable FindInteractableInFront(Vector3 origin, Vector3 direction)
    {
        Vector3 samplePoint = origin + direction * (interactRange * 0.65f);
        Collider[] overlaps = Physics.OverlapSphere(samplePoint, interactRadius * 2f, interactMask, QueryTriggerInteraction.Collide);

        IInteractable best = null;
        float bestScore = float.MaxValue;

        for (int i = 0; i < overlaps.Length; i++)
        {
            IInteractable candidate = InteractableUtility.GetFromCollider(overlaps[i]);
            if (candidate == null || IsSelfCollider(overlaps[i]))
                continue;

            Vector3 toTarget = overlaps[i].bounds.center - origin;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.001f)
                continue;

            float angle = Vector3.Angle(direction, toTarget.normalized);
            if (angle > 70f)
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

    IInteractable FindNearestInteractable()
    {
        Collider[] overlaps = Physics.OverlapSphere(transform.position, interactRange, interactMask, QueryTriggerInteraction.Collide);

        IInteractable best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < overlaps.Length; i++)
        {
            if (IsSelfCollider(overlaps[i]))
                continue;

            IInteractable candidate = InteractableUtility.GetFromCollider(overlaps[i]);
            if (candidate == null || !candidate.CanInteract(gameObject))
                continue;

            float distance = Vector3.Distance(transform.position, overlaps[i].bounds.center);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return best;
    }

    IInteractable GetInteractableFromHit(Collider collider)
    {
        if (collider == null || IsSelfCollider(collider))
            return null;

        return InteractableUtility.GetFromCollider(collider);
    }

    bool IsSelfCollider(Collider collider)
    {
        return collider.transform == transform || collider.transform.IsChildOf(transform);
    }

    void OnDrawGizmosSelected()
    {
        if (rayOrigin == null)
            return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(rayOrigin.position, rayOrigin.position + rayOrigin.forward * interactRange);
        Gizmos.DrawWireSphere(rayOrigin.position + rayOrigin.forward * interactRange, interactRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
