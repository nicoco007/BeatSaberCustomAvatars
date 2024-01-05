//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2024  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

namespace CustomAvatar.Configuration
{
    internal class ObservableValue<T>
    {
        public event Action<T> changed;

        public T value
        {
            get => _value;
            set
            {
                _value = value;
                changed?.Invoke(value);
            }
        }

        private T _value;

        public ObservableValue() { }

        public ObservableValue(T value)
        {
            _value = value;
        }

        public static implicit operator T(ObservableValue<T> ov)
        {
            return ov != null ? ov.value : default;
        }

        public override string ToString()
        {
            return _value?.ToString();
        }
    }
}
