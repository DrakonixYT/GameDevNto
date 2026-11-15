namespace HangarGame.Interaction
{
    public interface IInteractable
    {
        void OnHoverEnter();
        void OnHoverExit();
        void OnInteract();
        string GetInteractionPrompt();
    }
}
