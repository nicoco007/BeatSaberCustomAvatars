//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Beat Saber Custom Avatars Contributors
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using IPA.Logging;

namespace CustomAvatar.Logging
{
    internal class IPALogger<T> : ILogger<T>
    {
        private readonly string _name;
        private readonly Logger _logger;

        public IPALogger(Logger logger, string name = null)
        {
            _logger = logger;
            _name = name;
        }

        public void Trace(object message)
        {
            _logger.Trace(FormatMessage(message));
        }

        public void Debug(object message)
        {
            _logger.Debug(FormatMessage(message));
        }

        public void Notice(object message)
        {
            _logger.Notice(FormatMessage(message));
        }

        public void Info(object message)
        {
            _logger.Info(FormatMessage(message));
        }

        public void Warning(object message)
        {
            _logger.Warn(FormatMessage(message));
        }

        public void Error(object message)
        {
            _logger.Error(FormatMessage(message));
        }

        public void Critical(object message)
        {
            _logger.Critical(FormatMessage(message));
        }

        private string FormatMessage(object message)
        {
            if (string.IsNullOrEmpty(_name))
            {
                return $"[{typeof(T).Name}] {message}";
            }

            return $"[{typeof(T).Name}({_name})] {message}";
        }
    }
}
