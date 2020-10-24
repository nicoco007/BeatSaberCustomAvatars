using System;
using System.Linq;
using Zenject;

namespace CustomAvatar.Zenject.Internal
{
    internal class InstallerRegistrationOnMonoInstaller<T> : InstallerRegistrationOnTarget where T : MonoInstaller
    {
        private Type _targetType;

        internal InstallerRegistrationOnMonoInstaller()
        {
            _targetType = typeof(T);
        }

        internal override bool ShouldInstall(Context context)
        {
            return context.Installers.Any(i => i.GetType() == _targetType);
        }
    }
}
