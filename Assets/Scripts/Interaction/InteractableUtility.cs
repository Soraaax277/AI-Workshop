using UnityEngine;

public static class InteractableUtility
{
    public static IInteractable GetFromCollider(Collider collider)
    {
        if (collider == null)
            return null;

        var behaviours = collider.GetComponentsInParent<MonoBehaviour>();
        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IInteractable interactable)
                return interactable;
        }

        return null;
    }
}
