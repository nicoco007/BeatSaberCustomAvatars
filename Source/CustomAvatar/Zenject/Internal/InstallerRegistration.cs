//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2023  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using ModestTree;
using System;
using System.Reflection;
using Zenject;

namespace CustomAvatar.Zenject.Internal
{
    internal class InstallerRegistration
    {
        private static readonly MethodInfo kInstallMethod = typeof(DiContainer).GetMethod("Install", BindingFlags.Public | BindingFlags.Instance, null, CallingConventions.Standard, new[] { typeof(object[]) }, null);

        public Type installer { get; }

        private readonly MethodInfo _installMethod;
        private object[] _extraArgs;
        private InstallerRegistrationOnTarget _target;

        public InstallerRegistration(Type installer)
        {
            Assert.DerivesFrom(installer, typeof(Installer));

            this.installer = installer;

            _installMethod = kInstallMethod.MakeGenericMethod(installer);
            _extraArgs = new object[0];
        }

        public InstallerRegistration WithArguments(params object[] extraArgs)
        {
            _extraArgs = extraArgs;
            return this;
        }

        public InstallerRegistrationOnContext OnContext(string sceneName, string contextName)
        {
            var target = new InstallerRegistrationOnContext(sceneName, contextName);

            _target = target;

            return target;
        }

        public InstallerRegistrationOnMonoInstaller<T> OnMonoInstaller<T>() where T : MonoInstaller
        {
            var target = new InstallerRegistrationOnMonoInstaller<T>();

            _target = target;

            return target;
        }

        public InstallerRegistrationOnDecoratorContext OnDecoratorContext(string decoratedContractName)
        {
            var target = new InstallerRegistrationOnDecoratorContext(decoratedContractName);

            _target = target;

            return target;
        }

        internal bool TryInstallInto(Context context)
        {
            if (!_target.ShouldInstall(context)) return false;

            _installMethod.Invoke(context.Container, new[] { _extraArgs });

            return true;
        }
    }
}
