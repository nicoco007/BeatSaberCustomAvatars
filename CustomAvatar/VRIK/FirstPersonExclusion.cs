using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

/*namespace AvatarScriptPack
{
    class FirstPersonExclusion : MonoBehaviour
    {
        public GameObject[] Exclude;

        void Start()
        {
            try
            {
                if (Plugin.fpsAvatar && Exclude != null)
                {
                    foreach (Transform child in gameObject.GetComponentsInChildren<Transform>())
                    {
                        if (child.gameObject.layer == 27 && Exclude.Contains(child.gameObject))
                            child.gameObject.layer = 26;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
            }
        }
    }
}*/
