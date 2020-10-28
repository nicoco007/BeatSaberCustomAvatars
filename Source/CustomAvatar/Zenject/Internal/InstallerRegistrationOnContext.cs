using ModestTree;
using Zenject;

namespace CustomAvatar.Zenject.Internal
{
    internal class InstallerRegistrationOnContext : InstallerRegistrationOnTarget
    {
        private string _sceneName;
        private string _contextName;

        internal InstallerRegistrationOnContext(string sceneName, string contextName)
        {
            Assert.IsNotEmpty(sceneName);
            Assert.IsNotEmpty(contextName);

            _sceneName = sceneName;
            _contextName = contextName;
        }

        internal override bool ShouldInstall(Context context)
        {
            return context.name == _contextName && context.gameObject.scene.name == _sceneName;
        }
    }
}
