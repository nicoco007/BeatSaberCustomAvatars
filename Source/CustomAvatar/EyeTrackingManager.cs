using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CustomAvatar
{
    public class EyeTrackingManager : MonoBehaviour
    {
        public SkinnedMeshRenderer targetMesh;
        public int leftEyeWinkleBlendShapeIndex = 0;
        public int rightEyeWinkleBlendShapeIndex = 0;
        public float EyeMoveStrength = 1.0f;

        public bool winkleEnable = true;
    }
}
