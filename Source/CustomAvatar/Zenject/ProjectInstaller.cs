//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2026  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using System;
using System.Reflection;
using CustomAvatar.Logging;
using IPA.Loader;
using IPA.Logging;
using Zenject;

namespace CustomAvatar.Zenject
{
    internal class ProjectInstaller : Installer
    {
        private static readonly MethodInfo kCreateLoggerMethod = typeof(ILoggerFactory).GetMethod(nameof(ILoggerFactory.CreateLogger), BindingFlags.Public | BindingFlags.Instance);
        private static readonly Assembly kAssembly = Assembly.GetExecutingAssembly();

        private readonly Logger _ipaLogger;
        private readonly ILogger<CustomAvatarsInstaller> _logger;
        private readonly PluginMetadata _pluginMetadata;

        public ProjectInstaller(Logger ipaLogger, PluginMetadata pluginMetadata)
        {
            _ipaLogger = ipaLogger;
            _logger = new IPALogger<CustomAvatarsInstaller>(ipaLogger);
            _pluginMetadata = pluginMetadata;
        }

        public override void InstallBindings()
        {
            Container.Bind(typeof(ILoggerFactory)).To<IPALoggerFactory>().AsTransient().WithArguments(_ipaLogger);
            Container.Bind(typeof(ILogger<>)).FromMethodUntyped(CreateLogger).AsTransient();

            Container.Bind<PluginMetadata>().FromInstance(_pluginMetadata).When(InjectedIntoThisAssembly);
        }
        private object CreateLogger(InjectContext context)
        {
            Type genericType = context.MemberType.GenericTypeArguments[0];

            if (!genericType.IsAssignableFrom(context.ObjectType))
            {
                throw new InvalidOperationException($"Cannot create logger with generic type '{genericType}' for type '{context.ObjectType}'");
            }

            ILoggerFactory instance = context.Container.Resolve<ILoggerFactory>();

            return kCreateLoggerMethod.MakeGenericMethod(context.ObjectType).Invoke(instance, new object[] { null });
        }

        private bool InjectedIntoThisAssembly(InjectContext context)
        {
            return context.ObjectType.Assembly == kAssembly;
        }
    }
}
