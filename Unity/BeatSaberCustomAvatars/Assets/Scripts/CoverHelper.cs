using CustomAvatar;
using System.Collections.Specialized;
using System.Runtime.Versioning;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CoverHelper : MonoBehaviour
{
    public AvatarDescriptor avatar;
    public int imageSize = 1024;

    [Tooltip("Disabling this can cause issues with bloom materials")]
    public bool enforceOpacity = true;

    private void Start()
    {
        transform.position = new Vector3(0, 1.45f, 2.3f);

        Transform head = avatar.transform.Find("Head");
        Transform body = avatar.transform.Find("Body");
        Transform leftHand = avatar.transform.Find("LeftHand");
        Transform rightHand = avatar.transform.Find("RightHand");

        head.position = new Vector3(0, 1.7f, 0);
        body.position = new Vector3(0, 1.7f, 0);

        leftHand.position = new Vector3(-0.3f, 1.2f, 0.1f);
        leftHand.rotation = Quaternion.Euler(-45, 0, 0);

        rightHand.position = new Vector3(0.3f, 1.2f, 0.1f);
        rightHand.rotation = Quaternion.Euler(-45, 0, 0);
    }
}