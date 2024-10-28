//  Beat Saber Custom Avatars - Custom player models for body presence in Beat Saber.
//  Copyright © 2018-2024  Nicolas Gnyra and Beat Saber Custom Avatars Contributors
//
//  This library is free software: you can redistribute it and/or
//  modify it under the terms of the GNU Lesser General Public
//  License as published by the Free Software Foundation, either
//  version 3 of the License, or (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
//
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

using UnityEditor;
using UnityEngine;

namespace CustomAvatar
{
    public partial class VRIKManager
    {
        private GUIStyle _redLabelStyle;
        private GUIStyle _greenLabelStyle;
        private GUIStyle _blueLabelStyle;

        protected void OnDrawGizmosSelected()
        {
            if (_redLabelStyle == null)
            {
                _redLabelStyle = new GUIStyle(EditorStyles.label);
                _redLabelStyle.normal.textColor = Color.red;
            }

            if (_greenLabelStyle == null)
            {
                _greenLabelStyle = new GUIStyle(EditorStyles.label);
                _greenLabelStyle.normal.textColor = Color.green;
            }

            if (_blueLabelStyle == null)
            {
                _blueLabelStyle = new GUIStyle(EditorStyles.label);
                _blueLabelStyle.normal.textColor = Color.blue;
            }

            DrawHandAxes(references_leftHand, solver_leftArm_wristToPalmAxis, solver_leftArm_palmToThumbAxis, true);
            DrawHandAxes(references_rightHand, solver_rightArm_wristToPalmAxis, solver_rightArm_palmToThumbAxis, false);
        }

        private void DrawHandAxes(Transform reference, Vector3 wristToPalmAxis, Vector3 palmToThumbAxis, bool invertNormal)
        {
            if (!reference)
            {
                return;
            }

            Vector3 wristToPalmVector = default;
            Vector3 palmToThumbVector = default;

            if (wristToPalmAxis.sqrMagnitude > 0)
            {
                wristToPalmVector = reference.rotation * wristToPalmAxis.normalized;

                Handles.color = Color.green;
                Handles.ArrowHandleCap(0, reference.position, Quaternion.LookRotation(wristToPalmVector), 0.1f, EventType.Repaint);
                Handles.Label(reference.position + wristToPalmVector * 0.12f, "Wrist to Palm Axis", _greenLabelStyle);
            }

            if (palmToThumbAxis.sqrMagnitude > 0)
            {
                palmToThumbVector = reference.rotation * palmToThumbAxis.normalized;

                Handles.color = Color.red;
                Handles.ArrowHandleCap(0, reference.position, Quaternion.LookRotation(palmToThumbVector), 0.1f, EventType.Repaint);
                Handles.Label(reference.position + palmToThumbVector * 0.12f, "Palm to Thumb Axis", _redLabelStyle);
            }

            if (wristToPalmAxis.sqrMagnitude > 0 && palmToThumbAxis.sqrMagnitude > 0)
            {
                Vector3 planeNormal = new Plane(reference.position, reference.position + wristToPalmVector, reference.position + palmToThumbVector).normal;

                if (invertNormal)
                {
                    planeNormal = -planeNormal;
                }

                Handles.color = Color.blue;
                Handles.ArrowHandleCap(0, reference.position, Quaternion.LookRotation(planeNormal), 0.1f, EventType.Repaint);
                Handles.Label(reference.position + planeNormal * 0.12f, "Palm Inside Axis", _blueLabelStyle);
            }
        }
    }
}
