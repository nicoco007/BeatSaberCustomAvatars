using System.Runtime.CompilerServices;
using IPA.Config.Stores;
using IPA.Config.Stores.Attributes;
using IPA.Config.Stores.Converters;
using UnityEngine;

[assembly: InternalsVisibleTo(GeneratedStore.AssemblyVisibilityTarget)]

namespace CustomAvatar.Utilities
{
    // ReSharper disable ClassNeverInstantiated.Global
    // ReSharper disable ClassWithVirtualMembersNeverInherited.Global
    // ReSharper disable RedundantDefaultMemberInitializer
    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
    // ReSharper disable UnusedMember.Global
    internal class Settings
    {
        public virtual bool isAvatarVisibleInFirstPerson { get; set; } = true;
        [UseConverter(typeof(EnumConverter<AvatarResizeMode>))] public virtual AvatarResizeMode resizeMode { get; set; } = AvatarResizeMode.Height;
        public virtual bool enableFloorAdjust { get; set; } = false;
        public virtual bool moveFloorWithRoomAdjust { get; set; } = false;
        public virtual string previousAvatarPath { get; set; } = null;
        public virtual float playerArmSpan { get; set; } = 1.7f;
        public virtual bool useAutomaticFullBodyCalibration { get; set; } = false;
        public virtual bool calibrateFullBodyTrackingOnStart { get; set; } = false;
        public virtual float cameraNearClipPlane { get; set; } = 0.1f;
        [UseConverter(typeof(Vector2ValueConverter))] public virtual Vector2 mirrorSize { get; set; } = new Vector2(5f, 2.5f);
        public virtual float mirrorRenderScale { get; set; } = 1.0f;
        public virtual FullBodyMotionSmoothing fullBodyMotionSmoothing { get; set; } = new FullBodyMotionSmoothing();
        public virtual FullBodyCalibration fullBodyCalibration { get; set; } = new FullBodyCalibration();

        public class FullBodyMotionSmoothing
        {
            public virtual TrackedPointSmoothing waist { get; set; } = new TrackedPointSmoothing { position = 15, rotation = 10 };
            public virtual TrackedPointSmoothing feet { get; set; } = new TrackedPointSmoothing { position = 13, rotation = 17 };
        }

        public class TrackedPointSmoothing
        {
            public virtual float position { get; set; }
            public virtual float rotation { get; set; }
        }

        public class FullBodyCalibration
        {
            [UseConverter(typeof(PoseValueConverter))] public virtual Pose leftLeg { get; set; } = Pose.identity;
            [UseConverter(typeof(PoseValueConverter))] public virtual Pose rightLeg { get; set; } = Pose.identity;
            [UseConverter(typeof(PoseValueConverter))] public virtual Pose pelvis { get; set; } = Pose.identity;
        }

        public virtual void Changed()
        {
            Plugin.logger.Debug("Settings changed");

            CheckAllValuesValid();
        }

        public virtual void OnReload()
        {
            Plugin.logger.Debug("Settings reloaded");

            CheckAllValuesValid();
        }
        
        private void CheckAllValuesValid()
        {
            if (fullBodyCalibration.pelvis.rotation.x == 0 && fullBodyCalibration.pelvis.rotation.y == 0 &&
                fullBodyCalibration.pelvis.rotation.z == 0 && fullBodyCalibration.pelvis.rotation.w == 0)
            {
                fullBodyCalibration.pelvis = Pose.identity;
            }

            if (fullBodyCalibration.leftLeg.rotation.x == 0 && fullBodyCalibration.leftLeg.rotation.y == 0 &&
                fullBodyCalibration.leftLeg.rotation.z == 0 && fullBodyCalibration.leftLeg.rotation.w == 0)
            {
                fullBodyCalibration.leftLeg = Pose.identity;
            }

            if (fullBodyCalibration.rightLeg.rotation.x == 0 && fullBodyCalibration.rightLeg.rotation.y == 0 &&
                fullBodyCalibration.rightLeg.rotation.z == 0 && fullBodyCalibration.rightLeg.rotation.w == 0)
            {
                fullBodyCalibration.rightLeg = Pose.identity;
            }
        }
    }
}
