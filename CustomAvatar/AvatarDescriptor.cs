using UnityEngine;

namespace CustomAvatar
{
    public class AvatarDescriptor : MonoBehaviour
    {
        //For some reason, FormerlySerializedAs attribute doesn't work here, so I have to keep the names the same even though they're now private fields.

        // ReSharper disable once InconsistentNaming
        [SerializeField] private string AvatarName;

        // ReSharper disable once InconsistentNaming
        [SerializeField] private string AuthorName;

        //[SerializeField] private Transform _viewPoint;

        [SerializeField] private bool _allowHeightCalibration = true;

        [SerializeField] private Sprite CoverImage;
        
        public string Name
        {
            get => AvatarName;
            set => AvatarName = value;
        }
        public string Author
        {
            get => AuthorName;
            set => AuthorName = value;
        }
        //public Transform ViewPoint => _viewPoint;
        public bool AllowHeightCalibration => _allowHeightCalibration;
        public Sprite Cover => CoverImage;
    }
}
