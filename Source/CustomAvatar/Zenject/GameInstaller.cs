﻿//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2025  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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
using CustomAvatar.Avatar;
using CustomAvatar.Player;
using CustomAvatar.Replays;
using CustomAvatar.Utilities;
using Hive.Versioning;
using Zenject;

namespace CustomAvatar.Zenject
{
    internal class GameInstaller : BaseInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind(typeof(IInitializable), typeof(IDisposable)).To<AvatarGameplayEventsPlayer>().AsSingle().NonLazy();
            Container.Bind(typeof(IInitializable)).To<GameEnvironmentObjectManager>().AsSingle().NonLazy();

            Container.Bind(typeof(BeatmapObjectEventFilter), typeof(IInitializable), typeof(IDisposable)).To<BeatmapObjectEventFilter>().AsSingle();

            Container.BindExecutionOrder<GameEnvironmentObjectManager>(1000);

            if (IsPluginLoadedAndMatchesVersion("ScoreSaber", new VersionRange("^3.0.0")))
            {
                Container.Bind(typeof(IInitializable)).To<ScoreSaberReplayHandler>().AsSingle();
                Container.BindInitializableExecutionOrder<ScoreSaberReplayHandler>(1000);
            }

            if (IsPluginLoadedAndMatchesVersion("BeatLeader", new VersionRange(">= 0.9.0 < 0.11.0")))
            {
                Container.Bind(typeof(IInitializable)).To<BeatLeaderReplayHandler>().AsSingle();
                Container.BindInitializableExecutionOrder<BeatLeaderReplayHandler>(1000);
            }
        }
    }
}
