using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace CustomAvatar.UI
{
    class AvatarPreviewRotation : MonoBehaviour
    {
        public static bool rotatePreview = false;
        private float rotationSpeed = 20f;

        void Awake()
        {
            Plugin.Log("AvatarPreviewRotation is awake");
        }

        void Update()
        {
            if (rotatePreview)
            {
                transform.Rotate(Vector3.up * Time.deltaTime * rotationSpeed);
            }
        }
    }
}
