using IPA.Config.Data;
using IPA.Config.Stores;
using UnityEngine;

namespace CustomAvatar.Utilities
{
    internal class PoseJsonConverter : ValueConverter<Pose>
    {
        public override Value ToValue(Pose obj, object parent)
        {
            Map position = Value.Map();
            Map rotation = Value.Map();
            Map pose = Value.Map();

            position.Add("x", Value.Float((decimal) obj.position.x));
            position.Add("y", Value.Float((decimal) obj.position.y));
            position.Add("z", Value.Float((decimal) obj.position.z));
            
            rotation.Add("x", Value.Float((decimal) obj.rotation.x));
            rotation.Add("y", Value.Float((decimal) obj.rotation.y));
            rotation.Add("z", Value.Float((decimal) obj.rotation.z));
            rotation.Add("w", Value.Float((decimal) obj.rotation.w));

            pose.Add("position", position);
            pose.Add("rotation", rotation);

            return pose;
        }

        public override Pose FromValue(Value value, object parent)
        {
            Map pose = (Map) value;
            Map position = (Map) pose["position"];
            Map rotation = (Map) pose["rotation"];

            return new Pose(
                new Vector3((float)((FloatingPoint)position["x"]).Value, (float)((FloatingPoint)position["y"]).Value, (float)((FloatingPoint)position["z"]).Value),
                new Quaternion((float)((FloatingPoint)rotation["x"]).Value, (float)((FloatingPoint)rotation["y"]).Value, (float)((FloatingPoint)rotation["z"]).Value, (float)((FloatingPoint)rotation["w"]).Value)
            );
        }
    }
}
