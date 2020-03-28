using IPA.Config.Data;
using IPA.Config.Stores;
using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal class Vector2ValueConverter : ValueConverter<Vector2>
    {
        public override Value ToValue(Vector2 obj, object parent)
        {
            Map vector2 = Value.Map();

            vector2.Add("x", Value.Float((decimal) obj.x));
            vector2.Add("y", Value.Float((decimal) obj.y));

            return vector2;
        }

        public override Vector2 FromValue(Value value, object parent)
        {
            Map vector2 = (Map) value;

            return new Vector2((float)((FloatingPoint)vector2["x"]).Value, (float)((FloatingPoint)vector2["y"]).Value);
        }
    }
}
