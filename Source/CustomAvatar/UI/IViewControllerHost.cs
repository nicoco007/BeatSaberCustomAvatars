namespace CustomAvatar.UI
{
    internal interface IViewControllerHost
    {
        void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling);
        void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling);
    }
}
