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

namespace CustomAvatar.Logging
{
    internal class UnityDebugLogger<T> : ILogger<T>
    {
        private readonly string _name;

        public UnityDebugLogger(string name = null)
        {
            _name = name;
        }

        public void Trace(object message) { }

        public void Debug(object message)
        {
            UnityEngine.Debug.Log(FormatMessage("DEBUG", message));
        }

        public void Notice(object message)
        {
            UnityEngine.Debug.Log(FormatMessage("NOTICE", message));
        }

        public void Info(object message)
        {
            UnityEngine.Debug.Log(FormatMessage("INFO", message));
        }

        public void Warning(object message)
        {
            UnityEngine.Debug.LogWarning(FormatMessage("WARNING", message));
        }

        public void Error(object message)
        {
            UnityEngine.Debug.LogError(FormatMessage("ERROR", message));
        }

        public void Critical(object message)
        {
            UnityEngine.Debug.LogError(FormatMessage("CRITICAL", message));
        }

        private string FormatMessage(string level, object message)
        {
            if (string.IsNullOrEmpty(_name))
            {
                return $"{level} | [{typeof(T).Name}] {message}";
            }

            return $"{level} | [{typeof(T).Name}({_name})] {message}";
        }
    }
}
