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

using IPA.Logging;

namespace CustomAvatar.Logging
{
    internal class IPALogger<T> : ILogger<T>
    {
        private readonly Logger _logger;

        public IPALogger(Logger logger, string name = null)
        {
            _logger = logger.GetChildLogger(typeof(T).Name);

            if (!string.IsNullOrEmpty(name))
            {
                _logger = _logger.GetChildLogger(name);
            }
        }

        public void LogTrace(object message)
        {
            _logger.Trace(message?.ToString());
        }

        public void LogDebug(object message)
        {
            _logger.Debug(message?.ToString());
        }

        public void LogNotice(object message)
        {
            _logger.Notice(message?.ToString());
        }

        public void LogInformation(object message)
        {
            _logger.Info(message?.ToString());
        }

        public void LogWarning(object message)
        {
            _logger.Warn(message?.ToString());
        }

        public void LogError(object message)
        {
            _logger.Error(message?.ToString());
        }

        public void LogCritical(object message)
        {
            _logger.Critical(message?.ToString());
        }
    }
}
