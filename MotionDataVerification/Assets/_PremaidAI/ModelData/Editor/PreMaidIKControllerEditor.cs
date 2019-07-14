using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace PreMaid
{
    [CustomEditor(typeof(PreMaidIKController))]
    public class PreMaidIKControllerEditor : Editor
    {
        private void OnSceneGUI()
        {
            PreMaidIKController controller = (PreMaidIKController)target;


            if (controller.priorJoint == PreMaidIKController.Arm.PriorJoint.Elbow)
            {
                if (!controller.leftHandTarget || !controller.rightHandTarget || !controller.leftElbowTarget || !controller.rightElbowTarget) return;

                EditorGUI.BeginChangeCheck();

                Vector3 leftWristPos = Handles.PositionHandle(controller.leftHandTarget.position, Quaternion.identity);
                Vector3 rightWristPos = Handles.PositionHandle(controller.rightHandTarget.position, Quaternion.identity);
                Vector3 leftElbowPos = Handles.PositionHandle(controller.leftElbowTarget.position, Quaternion.identity);
                Vector3 rightElbowPos = Handles.PositionHandle(controller.rightElbowTarget.position, Quaternion.identity);

                if (EditorGUI.EndChangeCheck())
                {
                    controller.leftHandTarget.position = leftWristPos;
                    controller.rightHandTarget.position = rightWristPos;
                    controller.leftElbowTarget.position = leftElbowPos;
                    controller.rightElbowTarget.position = rightElbowPos;
                }
            }
            else
            {
                if (!controller.leftHandTarget || !controller.rightHandTarget) return;

                EditorGUI.BeginChangeCheck();
                Vector3 leftWristPos = Handles.PositionHandle(controller.leftHandTarget.position, Quaternion.identity);
                Vector3 rightWristPos = Handles.PositionHandle(controller.rightHandTarget.position, Quaternion.identity);

                if (EditorGUI.EndChangeCheck())
                {
                    controller.leftHandTarget.position = leftWristPos;
                    controller.rightHandTarget.position = rightWristPos;
                }
            }
        }
    }
}
