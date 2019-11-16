using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace CustomAvatar
{
    [JsonConverter(typeof(StringEnumConverter))]
    internal enum AvatarResizeMode
    {
        ArmSpan,
        Height,
        None
    }
}
