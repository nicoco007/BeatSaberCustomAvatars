using System.IO;
using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal static class BinaryReaderExtensions
    {
        public static Pose ReadPose(this BinaryReader reader)
        {
            return new Pose(reader.ReadVector3(), reader.ReadQuaternion());
        }
        
        public static Vector3 ReadVector3(this BinaryReader reader)
        {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
        
        public static Quaternion ReadQuaternion(this BinaryReader reader)
        {
            return new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }
    }
}
