using System.IO;
using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal static class BinaryWriterExtensions
    {
        public static void Write(this BinaryWriter writer, Pose pose)
        {
            writer.Write(pose.position);
            writer.Write(pose.rotation);
        }

        public static void Write(this BinaryWriter writer, Vector3 vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
            writer.Write(vector.z);
        }

        public static void Write(this BinaryWriter writer, Quaternion quaternion)
        {
            writer.Write(quaternion.x);
            writer.Write(quaternion.y);
            writer.Write(quaternion.z);
            writer.Write(quaternion.w);
        }
    }
}
