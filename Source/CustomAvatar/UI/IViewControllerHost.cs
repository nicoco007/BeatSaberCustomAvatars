using CustomAvatar.Avatar;

namespace CustomAvatar.UI
{
    internal interface IViewControllerHost
    {
        void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling);
        void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling);
        void UpdateUI(SpawnedAvatar avatar);
    }
}
