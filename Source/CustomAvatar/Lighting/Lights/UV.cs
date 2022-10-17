using UnityEngine;

namespace CustomAvatar.Lighting.Lights
{
    internal struct UV
    {
        private readonly Triangle[] _triangles;

        internal UV(Triangle[] triangles)
        {
            _triangles = triangles;
        }

        public static UV Parse(Vector2[] uv, ushort[] tris)
        {
            var triangles = new Triangle[tris.Length / 3];

            for (int i = 0; i < tris.Length / 3; ++i)
            {
                triangles[i] = new Triangle(uv[tris[i * 3]], uv[tris[i * 3 + 1]], uv[tris[i * 3 + 2]]);
            }

            return new UV(triangles);
        }

        public bool ContainsPoint(Vector2 point)
        {
            foreach (Triangle triangle in _triangles)
            {
                if (triangle.ContainsPoint(point))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
