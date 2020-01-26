using UnityEngine;

namespace CustomAvatar
{
    public class FirstPersonExclusion : MonoBehaviour
    {
        [HideInInspector] public GameObject[] Exclude;
        public GameObject[] exclude;

        public void Awake()
        {
            exclude = Exclude ?? exclude;
        }
    }
}