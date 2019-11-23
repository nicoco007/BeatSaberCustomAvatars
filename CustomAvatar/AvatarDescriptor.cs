using UnityEngine;

namespace CustomAvatar
{
    // ReSharper disable ConvertToAutoProperty
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AvatarDescriptor : MonoBehaviour
    {
        // Legacy stuff
        // ReSharper disable InconsistentNaming
        #pragma warning disable 649
        [SerializeField] private string AvatarName;
        [SerializeField] private string AuthorName;
        [SerializeField] private Sprite CoverImage;
        #pragma warning restore 649
        // ReSharper enable InconsistentNaming

        [SerializeField] private string _name;
        [SerializeField] private string _author;
        [SerializeField] private bool _allowHeightCalibration = true;
        [SerializeField] private Sprite _cover;

        public new string name
        {
            get => _name ?? AvatarName;
            set => _name = value;
        }

        public string author
        {
            get => _author ?? AuthorName;
            set => _author = value;
        }

        public bool allowHeightCalibration
        {
            get => _allowHeightCalibration;
            set => _allowHeightCalibration = value;
        }

        public Sprite cover
        {
            get => _cover ?? CoverImage;
            set => _cover = value;
        }
    }
}
