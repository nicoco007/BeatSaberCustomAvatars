using UnityEngine;

namespace CustomAvatar
{
    // ReSharper disable ConvertToAutoProperty
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AvatarDescriptor : MonoBehaviour, ISerializationCallbackReceiver
    {
        public new string name;
        public string author;
        public bool allowHeightCalibration = true;
        public Sprite cover;

        // Legacy stuff
        // ReSharper disable InconsistentNaming
        #pragma warning disable 649
        [SerializeField] [HideInInspector] private string AvatarName;
        [SerializeField] [HideInInspector] private string AuthorName;
        [SerializeField] [HideInInspector] private Sprite CoverImage;
        [SerializeField] [HideInInspector] private string Name;
        [SerializeField] [HideInInspector] private string Author;
        [SerializeField] [HideInInspector] private Sprite Cover;
        #pragma warning restore 649
        // ReSharper restore InconsistentNaming

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            name = name ?? Name ?? AvatarName;
            author = author ?? Author ?? AuthorName;
            cover = cover ?? Cover ?? CoverImage;
        }
    }
}
