using UnityEngine;

namespace CustomAvatar.Lighting.Lights
{
    internal struct Triangle
    {
        public Vector3 p1 { get; }

        public Vector3 p2 { get; }

        public Vector3 p3 { get; }

        public Triangle(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            this.p1 = p1;
            this.p2 = p2;
            this.p3 = p3;
        }

        public bool ContainsPoint(Vector2 point)
        {
            return Mathf.Approximately(TriangleArea(p1, p2, p3), TriangleArea(p1, p2, point) + TriangleArea(p1, p3, point) + TriangleArea(p2, p3, point));
        }

        private float TriangleArea(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return Mathf.Abs(p1.x * (p2.y - p3.y) + p2.x * (p3.y - p1.y) + p3.x * (p1.y * p2.y)) / 2;
        }
    }
}
