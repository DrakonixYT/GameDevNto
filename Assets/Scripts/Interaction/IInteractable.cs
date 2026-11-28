namespace HangarGame.Interaction
{
    public interface IInteractable
    {
        bool CanInteract();
        string GetPrompt();
        void OnHover(bool isHovered);
        void OnInteract();
        void OnInteractHold();
    }
}
