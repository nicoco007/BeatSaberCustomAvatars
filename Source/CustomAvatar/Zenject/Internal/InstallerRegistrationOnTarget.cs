using Zenject;

namespace CustomAvatar.Zenject.Internal
{
    internal abstract class InstallerRegistrationOnTarget
    {
        internal abstract bool ShouldInstall(Context context);
    }
}
