//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2021  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
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

using CustomAvatar.Logging;
using UnityEngine;

#if UNITY_EDITOR
using CustomAvatar.Utilities;
using System.IO;
using System.Reflection;
#else
using Zenject;
#endif

namespace CustomAvatar
{
    /// <summary>
    /// Container for an avatar's name and other information configured before exportation.
    /// </summary>
    public class AvatarDescriptor : MonoBehaviour, ISerializationCallbackReceiver
    {
        /// <summary>
        /// Avatar's name.
        /// </summary>
        [Tooltip("Avatar's name.")]
        public new string name;

        /// <summary>
        /// Avatar creator's name.
        /// </summary>
        [Tooltip("Avatar creator's name.")]
        public string author;

        /// <summary>
        /// Whether or not to allow height calibration for this avatar.
        /// </summary>
        [Tooltip("Whether or not to allow height calibration for this avatar.")]
        public bool allowHeightCalibration = true;

        /// <summary>
        /// Whether or not this avatar supports automatic calibration. Note that this requires specific setup of the waist and feet trackers.
        /// </summary>
        [Tooltip("Whether or not this avatar supports automatic calibration. Note that this requires specific setup of the waist and feet trackers.")]
        public bool supportsAutomaticCalibration = false;

        /// <summary>
        /// The image shown in the in-game avatars list.
        /// </summary>
        [Tooltip("The image shown in the in-game avatars list.")]
        public Sprite cover;

        // Legacy stuff
#pragma warning disable CS0649, IDE0044, IDE1006, IDE0055
        [SerializeField] [HideInInspector] private string AvatarName;
        [SerializeField] [HideInInspector] private string AuthorName;
        [SerializeField] [HideInInspector] private Sprite CoverImage;
        [SerializeField] [HideInInspector] private string Name;
        [SerializeField] [HideInInspector] private string Author;
        [SerializeField] [HideInInspector] private Sprite Cover;
#pragma warning restore CS0649, IDE0044, IDE1006, IDE0055

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            name = name ?? Name ?? AvatarName;
            author = author ?? Author ?? AuthorName;
            cover = cover ?? Cover ?? CoverImage;
        }

#if UNITY_EDITOR
        private Mesh _saberMesh;

        public void Start()
        {
            var ikHelper = new IKHelper(new UnityDebugLogger<IKHelper>());
            ikHelper.InitializeVRIK(transform.GetComponentInChildren<VRIKManager>(), transform);
        }

        internal void OnDrawGizmos()
        {
            if (!isActiveAndEnabled) return;
            if (!_saberMesh) _saberMesh = LoadMesh(Assembly.GetExecutingAssembly().GetManifestResourceStream("CustomAvatar.Resources.saber.dat"));

            DrawSaber(transform.Find("LeftHand"), _saberMesh, new Color(0.78f, 0.08f, 0.08f));
            DrawSaber(transform.Find("RightHand"), _saberMesh, new Color(0, 0.46f, 0.82f));
        }

        private Mesh LoadMesh(Stream stream)
        {
            var mesh = new Mesh();

            using (var reader = new BinaryReader(stream))
            {
                int length = reader.ReadInt32();
                var vertices = new Vector3[length];

                for (int i = 0; i < length; i++)
                {
                    vertices[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                }

                length = reader.ReadInt32();
                var normals = new Vector3[length];

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
            using (var writer = new BinaryWriter(File.OpenWrite("mesh.dat")))
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
#else
        [Inject]
        internal void Construct(ILogger<AvatarDescriptor> logger)
        {
            logger.name = name;

            if (!string.IsNullOrEmpty(AvatarName) ||
                !string.IsNullOrEmpty(Name) ||
                !string.IsNullOrEmpty(AuthorName) ||
                !string.IsNullOrEmpty(Author) ||
                CoverImage ||
                Cover)
            {
                logger.Warning("Avatar is using a deprecated field; please re-export this avatar using the latest version of Custom Avatars");
            }
        }
#endif
    }
}
