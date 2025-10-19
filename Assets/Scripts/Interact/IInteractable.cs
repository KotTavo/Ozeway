// IInteractable.cs
using UnityEngine;

public interface IInteractable
{
    string GetInteractionText();
    void OnInteract(GameObject interactor);
    bool CanInteract(GameObject interactor);
    void OnHighlightStart(GameObject interactor);
    void OnHighlightEnd(GameObject interactor);
}