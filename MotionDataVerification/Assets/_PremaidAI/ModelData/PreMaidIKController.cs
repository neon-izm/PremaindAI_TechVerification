using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PreMaid
{
    public class PreMaidIKController : MonoBehaviour
    {
        [SerializeField] private Transform premaidRoot;

        private ModelJoint[] _joints;


        public class Arm
        {
            private Transform baseTransform;
            public ModelJoint shoulderPitch;
            public ModelJoint upperArmRoll;
            public ModelJoint upperArmPitch;
            public ModelJoint lowerArmRoll;
            public ModelJoint handPitch;

            public Transform elbowTarget;
            public Transform wristTarget;
            public float handAngle;

            /// <summary>
            /// 右手なら true、左手なら false にしておく
            /// </summary>
            public bool isRightSide = false;

            public PriorJoint priorJoint = PriorJoint.Elbow;

            public enum PriorJoint
            {
                None,
                Elbow,
                Wrist
            }

            float lengthShoulder;
            float lengthUpperArm;
            float lengthLowerArm;

            public void Initialize()
            {
                lengthShoulder = (upperArmRoll.transform.position - shoulderPitch.transform.position).magnitude;
                lengthUpperArm = (lowerArmRoll.transform.position - upperArmRoll.transform.position).magnitude;
                lengthLowerArm = (handPitch.transform.position - lowerArmRoll.transform.position).magnitude;

                baseTransform = shoulderPitch.transform.parent;
                
                // 肘の目標点が無ければ自動生成
                if (!elbowTarget)
                {
                    var obj = new GameObject((isRightSide ? "Right" : "Left") + "ElbowTarget");
                    elbowTarget = obj.transform;
                    elbowTarget.parent = baseTransform;
                    elbowTarget.position = lowerArmRoll.transform.position;
                }

                // 手首の目標点が無ければ自動生成
                if (!wristTarget)
                {
                    var obj = new GameObject((isRightSide ? "Right" : "Left") + "WristTarget");
                    wristTarget = obj.transform;
                    wristTarget.parent = baseTransform;
                    wristTarget.position = handPitch.transform.position;
                }
            }

            public void ApplyIK()
            {
                if (!elbowTarget || !wristTarget) return;

                if (priorJoint == PriorJoint.Elbow)
                {
                    ApplyIK_ElbowFirst();
                }
                else
                {
                    ApplyIK_WristFirst();
                }
            }

            private void ApplyIK_ElbowFirst()
            {
                // これ以下に肩に近づきすぎた肘目標点は無視する閾値
                const float sqrMinDistance = 0.0001f;   // [m^2]

                if ((elbowTarget.position - shoulderPitch.transform.position).sqrMagnitude < sqrMinDistance)
                {
                    ApplyIK_WristFirst();
                    return;
                }

                float sign = (isRightSide ? -1f : 1f);

                Quaternion invBaseRotation = Quaternion.Inverse(baseTransform.rotation);
                //Debug.Log(invBaseRotation.eulerAngles);

                Vector3 x0 = shoulderPitch.transform.position;
                Vector3 x0e = invBaseRotation * (elbowTarget.position - x0);

                float a0 = Mathf.Atan2(sign * x0e.z, -x0e.y) * Mathf.Rad2Deg; // 肩のX軸周り回転[deg]
                shoulderPitch.SetServoValue(a0);

                Vector3 x1 = upperArmRoll.transform.position;
                Quaternion invShoulderRotation = Quaternion.Inverse(shoulderPitch.normalizedRotation);
                Vector3 x1e = invShoulderRotation * (elbowTarget.position - x1);
                float a1 = Mathf.Atan2(sign * x1e.y, sign * -x1e.x) * Mathf.Rad2Deg; // 上腕のZ軸周り回転[deg]
                upperArmRoll.SetServoValue(a1);

                Vector3 x3 = lowerArmRoll.transform.position;
                Quaternion invUpperArmRotation = Quaternion.Inverse(upperArmRoll.normalizedRotation);
                Vector3 x3w = invUpperArmRotation * (wristTarget.position - x3);

                if (x3w.sqrMagnitude < sqrMinDistance)
                {
                    return;
                }

                float a2 = Mathf.Atan2(-x3w.x, -x3w.z) * Mathf.Rad2Deg; // 上腕のX軸周り回転[deg]
                upperArmPitch.SetServoValue(a2);

                Vector3 x4 = handPitch.transform.position;
                float l3w = Mathf.Sqrt(x3w.z * x3w.z + x3w.x * x3w.x);
                float a3 = Mathf.Atan2(sign * -l3w, x3w.y) * Mathf.Rad2Deg; // 前腕のZ軸周り回転[deg]
                lowerArmRoll.SetServoValue(a3);
            }

            private void ApplyIK_WristFirst()
            {

            }

            public void DrawGizmos()
            {
                float gizmoRadius = 0.01f;

                if (priorJoint == Arm.PriorJoint.Elbow)
                {
                    Gizmos.color = Color.red;
                }
                else
                {
                    Gizmos.color = Color.yellow;
                }

                if (elbowTarget)
                {
                    Gizmos.DrawLine(lowerArmRoll.transform.position, elbowTarget.position);
                    Gizmos.DrawSphere(elbowTarget.position, gizmoRadius);
                }

                if (priorJoint == Arm.PriorJoint.Elbow)
                {
                    Gizmos.color = Color.yellow;
                }
                else
                {
                    Gizmos.color = Color.red;
                }

                if (wristTarget)
                {
                    Gizmos.DrawLine(handPitch.transform.position, wristTarget.position);
                    Gizmos.DrawSphere(wristTarget.position, gizmoRadius);
                }

            }
        }


        private enum JointName
        {
            ShoulderPitch,
            ShoulderRoll,


        }

        public Transform leftElbowTarget;
        public Transform rightElbowTarget;
        public Transform leftWristTarget;
        public Transform rightWristTarget;

        private Arm leftArm;
        private Arm rightArm;

        public Arm.PriorJoint priorJoint = Arm.PriorJoint.Elbow;

        // Start is called before the first frame update
        void Start()
        {
            if (!premaidRoot)
            {
                // 未指定ならモデルのルートにこのスクリプトがアタッチされているものとする
                premaidRoot = transform;
            }

            if (premaidRoot != null)
            {
                _joints = premaidRoot.GetComponentsInChildren<ModelJoint>();
            }

            leftArm = new Arm();
            leftArm.isRightSide = false;
            leftArm.shoulderPitch = GetJointById("04");
            leftArm.upperArmRoll = GetJointById("0B");
            leftArm.upperArmPitch = GetJointById("0F");
            leftArm.lowerArmRoll = GetJointById("13");
            leftArm.handPitch = GetJointById("17");
            leftArm.Initialize();

            leftElbowTarget = leftArm.elbowTarget;
            leftWristTarget = leftArm.wristTarget;

            rightArm = new Arm();
            rightArm.isRightSide = true;
            rightArm.shoulderPitch = GetJointById("02");
            rightArm.upperArmRoll = GetJointById("09");
            rightArm.upperArmPitch = GetJointById("0D");
            rightArm.lowerArmRoll = GetJointById("11");
            rightArm.handPitch = GetJointById("15");
            rightArm.Initialize();

            rightElbowTarget = rightArm.elbowTarget;
            rightWristTarget = rightArm.wristTarget;

        }

        ModelJoint GetJointById(string servoId)
        {
            foreach (var joint in _joints)
            {
                if (joint.ServoID.Equals(servoId)) return joint;
            }
            return null;
        }


        // Update is called once per frame
        void Update()
        {
            leftArm.priorJoint = priorJoint;
            leftArm.ApplyIK();

            rightArm.priorJoint = priorJoint;
            rightArm.ApplyIK();

        }

        private void OnDrawGizmos()
        {
            if (leftArm != null) leftArm.DrawGizmos();
            if (rightArm != null) rightArm.DrawGizmos();
        }
    }
}
