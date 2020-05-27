using UnityEngine;

namespace CustomAvatar
{
    public class FirstPersonExclusion : MonoBehaviour
    {
        [HideInInspector] public GameObject[] Exclude;
        public GameObject[] exclude;

        public void Awake()
        {
            if (Exclude != null && Exclude.Length > 0) exclude = Exclude;
        }
    }
}
