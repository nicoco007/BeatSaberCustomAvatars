using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal class QuaternionExtensions
    {
        /*
         * https://gist.github.com/maxattack/4c7b4de00f5c1b95a33b
         * Copyright 2016 Max Kaufmann (max.kaufmann@gmail.com)
         * Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
         * The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
         * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
         */
        internal static Quaternion SmoothDamp(Quaternion current, Quaternion target, ref Vector4 derivative, float smoothTime, float maxSpeed, float deltaTime)
        {
            // account for double-cover
            float multiplier = Quaternion.Dot(current, target) > 0f ? 1f : -1f;
            target.x *= multiplier;
            target.y *= multiplier;
            target.z *= multiplier;
            target.w *= multiplier;

            // smooth damp (nlerp approx)
            Vector4 result = new Vector4(
                Mathf.SmoothDamp(current.x, target.x, ref derivative.x, smoothTime, maxSpeed, deltaTime),
                Mathf.SmoothDamp(current.y, target.y, ref derivative.y, smoothTime, maxSpeed, deltaTime),
                Mathf.SmoothDamp(current.z, target.z, ref derivative.z, smoothTime, maxSpeed, deltaTime),
                Mathf.SmoothDamp(current.w, target.w, ref derivative.w, smoothTime, maxSpeed, deltaTime)
            ).normalized;

            // ensure derivative is tangent
            Vector4 error = Vector4.Project(derivative, result);
            derivative.x -= error.x;
            derivative.y -= error.y;
            derivative.z -= error.z;
            derivative.w -= error.w;

            return new Quaternion(result.x, result.y, result.z, result.w);
        }
    }
}
