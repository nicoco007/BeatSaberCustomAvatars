//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
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

using System.IO;
using System.Reflection;
using UnityEngine;

namespace CustomAvatar
{
    public partial class AvatarDescriptor
    {
        private Mesh _saberMesh;

        protected void OnDrawGizmos()
        {
            if (!isActiveAndEnabled) return;
            if (!_saberMesh) _saberMesh = LoadMesh(Assembly.GetExecutingAssembly().GetManifestResourceStream("CustomAvatar.Resources.saber.dat"));

            DrawSaber(transform.Find("LeftHand"), _saberMesh, new Color(0.78f, 0.08f, 0.08f));
            DrawSaber(transform.Find("RightHand"), _saberMesh, new Color(0, 0.46f, 0.82f));
        }

        private Mesh LoadMesh(Stream stream)
        {
            Mesh mesh = new();

            using (BinaryReader reader = new(stream))
            {
                int length = reader.ReadInt32();
                Vector3[] vertices = new Vector3[length];

                for (int i = 0; i < length; i++)
                {
                    vertices[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                }

                length = reader.ReadInt32();
                Vector3[] normals = new Vector3[length];

                for (int i = 0; i < length; i++)
                {
                    normals[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                }

                length = reader.ReadInt32();
                int[] triangles = new int[length];

                for (int i = 0; i < length; i++)
                {
                    triangles[i] = reader.ReadInt32();
                }

                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetTriangles(triangles, 0);
            }

            return mesh;
        }

        internal void SaveMesh(Mesh mesh)
        {
            using (BinaryWriter writer = new(File.OpenWrite("mesh.dat")))
            {
                writer.Write(mesh.vertices.Length);

                foreach (Vector3 vertex in mesh.vertices)
                {
                    writer.Write(vertex.x);
                    writer.Write(vertex.y);
                    writer.Write(vertex.z);
                }

                writer.Write(mesh.normals.Length);

                foreach (Vector3 normal in mesh.normals)
                {
                    writer.Write(normal.x);
                    writer.Write(normal.y);
                    writer.Write(normal.z);
                }

                writer.Write(mesh.triangles.Length);

                foreach (int triangle in mesh.triangles)
                {
                    writer.Write(triangle);
                }
            }
        }

        private void DrawSaber(Transform transform, Mesh mesh, Color color)
        {
            if (!transform) return;

            Color prev = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawMesh(mesh, transform.position, transform.rotation, Vector3.one);
            Gizmos.color = prev;
        }
    }
}
