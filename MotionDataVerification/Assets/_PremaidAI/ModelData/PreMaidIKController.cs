using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PreMaid
{
    public class PreMaidIKController : MonoBehaviour
    {
        private ModelJoint[] _joints;


        public class Arm
        {
            private Transform baseTransform;
            public ModelJoint shoulderPitch;
            public ModelJoint upperArmRoll;
            public ModelJoint upperArmPitch;
            public ModelJoint lowerArmRoll;
            public ModelJoint handPitch;
            private Transform handTip;

            public Transform elbowTarget;
            public Transform handTarget;
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
                Hand
            }

            float lengthShoulder;
            float lengthUpperArm;
            float lengthLowerArm;

            public void Initialize()
            {
                baseTransform = shoulderPitch.transform.parent;
                if (handPitch.transform.childCount < 1)
                {
                    handTip = handPitch.transform;  // 子が無かった場合はhandPitchを手先とする
                }
                else
                {
                    handTip = handPitch.transform.GetChild(0);  // 子があればそれを手先とする
                }

                lengthShoulder = (upperArmRoll.transform.position - shoulderPitch.transform.position).magnitude;
                lengthUpperArm = (lowerArmRoll.transform.position - upperArmRoll.transform.position).magnitude;
                lengthLowerArm = (handTip.position - lowerArmRoll.transform.position).magnitude;

                
                // 肘の目標点が無ければ自動生成
                if (!elbowTarget)
                {
                    var obj = new GameObject((isRightSide ? "Right" : "Left") + "ElbowTarget");
                    elbowTarget = obj.transform;
                    elbowTarget.parent = baseTransform;
                    elbowTarget.position = lowerArmRoll.transform.position;
                }

                // 手首の目標点が無ければ自動生成
                if (!handTarget)
                {
                    var obj = new GameObject((isRightSide ? "Right" : "Left") + "HandTarget");
                    handTarget = obj.transform;
                    handTarget.parent = baseTransform;
                    handTarget.position = handPitch.transform.position;
                }
            }

            public void ApplyIK()
            {
                if (!elbowTarget || !handTarget) return;

                switch (priorJoint)
                {
                    case PriorJoint.Elbow:
                        ApplyIK_ElbowFirst();
                        break;
                    case PriorJoint.Hand:
                        ApplyIK_HandFirst();
                        break;
                }
            }

            /// <summary>
            /// 肘座標優先モードで腕のIKを解き、順次サーボ角度を設定していく
            /// </summary>
            private void ApplyIK_ElbowFirst()
            {
                // これ以下に肩に近づきすぎた肘目標点は無視する閾値
                const float sqrMinDistance = 0.0001f;   // [m^2]

                float sign = (isRightSide ? -1f : 1f);  // 左右の腕による方向入れ替え用

                Vector3 x0 = shoulderPitch.transform.position;  // UpperArmJointの座標
                Quaternion invBaseRotation = Quaternion.Inverse(baseTransform.rotation);
                Vector3 x0e = invBaseRotation * (elbowTarget.position - x0);    // x0から肘目標点までのベクトル

                // 閾値より肘目標点が肩に近すぎる場合、手首優先モードのIKにする
                if (x0e.sqrMagnitude < sqrMinDistance)
                {
                    ApplyIK_HandFirst();
                    return;
                }

                float a0 = Mathf.Atan2(sign * x0e.z, -x0e.y) * Mathf.Rad2Deg; // 肩のX軸周り回転[deg]
                shoulderPitch.SetServoValue(a0);

                Vector3 x1 = upperArmRoll.transform.position;   // 上腕始点の座標
                Quaternion invShoulderRotation = Quaternion.Inverse(shoulderPitch.normalizedRotation);
                Vector3 x1e = invShoulderRotation * (elbowTarget.position - x1);
                float a1 = Mathf.Atan2(sign * x1e.y, sign * -x1e.x) * Mathf.Rad2Deg; // 上腕のZ軸周り回転[deg]
                upperArmRoll.SetServoValue(a1);

                Vector3 x3 = lowerArmRoll.transform.position;   // 肘座標
                Quaternion invUpperArmRotation = Quaternion.Inverse(upperArmRoll.normalizedRotation);
                Vector3 x3h = invUpperArmRotation * (handTarget.position - x3);    // 肘から手首目標点へ向かうベクトル

                // 閾値より手首目標点が肘に近すぎる場合は、肘から先は処理しない
                if (x3h.sqrMagnitude < sqrMinDistance)
                {
                    return;
                }

                float a2 = Mathf.Atan2(-x3h.x, -x3h.z) * Mathf.Rad2Deg; // 上腕のX軸周り回転[deg]
                upperArmPitch.SetServoValue(a2);

                Vector3 x4 = handPitch.transform.position;
                float l3h = Mathf.Sqrt(x3h.z * x3h.z + x3h.x * x3h.x);
                float a3 = Mathf.Atan2(sign * -l3h, x3h.y) * Mathf.Rad2Deg; // 前腕のZ軸周り回転[deg]
                lowerArmRoll.SetServoValue(a3);
            }

            /// <summary>
            /// 手先座標優先モードで腕のIKを解き、順次サーボ角度を設定していく
            /// </summary>
            private void ApplyIK_HandFirst()
            {
                // これ以下に肩に近づきすぎた肘目標点は無視する閾値
                const float sqrMinDistance = 0.000025f;   // [m^2]

                float sign = (isRightSide ? -1f : 1f);  // 左右の腕による方向入れ替え用

                Vector3 x0 = shoulderPitch.transform.position;  // UpperArmJointの座標
                Quaternion invBaseRotation = Quaternion.Inverse(baseTransform.rotation);
                Vector3 x0h = invBaseRotation * (handTarget.position - x0);    // x0から肘目標点までのベクトル

                if ((x0h.y * x0h.y + x0h.z * x0h.z) < sqrMinDistance)
                {
                    // 肩ピッチの特異点近傍であれば、回転させない
                }
                else
                {
                    // 特異点近傍でなければ、回転させる
                    float a0 = sign * Mathf.Atan2(x0h.z, -x0h.y) * Mathf.Rad2Deg; // 肩のX軸周り回転[deg]
                    shoulderPitch.SetServoValue(a0);
                }

                Vector3 x1 = upperArmRoll.transform.position;
                Quaternion invShoulderRotation = Quaternion.Inverse(shoulderPitch.normalizedRotation);
                Vector3 x1h = invShoulderRotation * (handTarget.position - x1);

                float a1 = 0f;
                float a2 = 0f;
                float a3 = 0f;

                float x1w_sqrlen = x1h.sqrMagnitude;

                if (x1w_sqrlen <= (lengthUpperArm - lengthLowerArm) * (lengthUpperArm - lengthLowerArm))
                {
                    // 上腕始点に手首目標点が近すぎて三角形にならない場合
                    a3 = sign * -135f;
                    a1 = sign * 0f;
                }
                else if (x1w_sqrlen >= (lengthUpperArm + lengthLowerArm) * (lengthUpperArm + lengthLowerArm))
                {
                    // 腕を伸ばした以上に手首目標点が遠くて三角形にならない場合
                    a3 = sign * 10f;   // 手を伸ばすときには、あえて過伸展
                    a1 = sign * Mathf.Atan2(x1h.y, sign * -x1h.x) * Mathf.Rad2Deg - a3;
                }
                else
                {
                    // 三角形になる場合、余弦定理により肘の角度を求める
                    float cosx3 = (lengthUpperArm * lengthUpperArm + lengthLowerArm * lengthLowerArm - x1w_sqrlen) / (2f * lengthUpperArm * lengthLowerArm);
                    a3 = sign * (Mathf.Acos(cosx3) * Mathf.Rad2Deg - 180f);
                    float cosa1d = (lengthUpperArm * lengthUpperArm + x1w_sqrlen - lengthLowerArm * lengthLowerArm) / (2f * lengthUpperArm * Mathf.Sqrt(x1w_sqrlen));
                    float a1sub = Mathf.Acos(cosa1d) * Mathf.Rad2Deg;
                    a1 = sign * (Mathf.Atan2(x1h.y, sign * -x1h.x) * Mathf.Rad2Deg + a1sub);
                }
                upperArmRoll.SetServoValue(a1);
                upperArmPitch.SetServoValue(a2);
                lowerArmRoll.SetServoValue(a3);

                //// 関節0に可動限界があるため、関節1を動かした後に関節2の回転で補う
                //x1 = upperArmRoll.transform.position;
                //Quaternion invUpperArmRotation = Quaternion.Inverse(upperArmRoll.normalizedRotation);

                //x1w = (invUpperArmRotation * (handTarget.position - x1));
                //Vector3 x13 = (invUpperArmRotation * (lowerArmRoll.transform.position - x1));
                //Vector3 x15 = (invUpperArmRotation * (handTip.position - x1));

                //float x13_sqrlen = x13.sqrMagnitude;
                //Vector3 x5n = (x15 - Vector3.Dot(x13, x15) / x13_sqrlen * x13).normalized;   // 今の手先の点に向かうx13の単位法線ベクトル
                //Vector3 xwn = (x1w - Vector3.Dot(x13, x1w) / x13_sqrlen * x13).normalized;   // 手先目標点に向かうx13の単位法線ベクトル

                //float cos5wn = Vector3.Dot(x5n, xwn);
                //if (cos5wn >= 0.98f)
                //{
                //}
                //else
                //{
                //    a2 = sign * (Mathf.Acos(cos5wn) * Mathf.Rad2Deg);
                //    if (a2 < 180f) a2 += 180f;
                //    if (a2 > 180f) a2 -= 180f;
                //    //if (float.IsNaN(a2)) Debug.Log("cos:" + cos5wn);
                //    //if (!isRightSide) Debug.Log(a2);

                //    upperArmPitch.SetServoValue(a2);
                //}

                
            }

            public void DrawGizmos()
            {
                float gizmoRadius = 0.01f;

                if (priorJoint == Arm.PriorJoint.Elbow)
                {
                    Gizmos.color = Color.red;

                    if (elbowTarget)
                    {
                        Gizmos.DrawLine(lowerArmRoll.transform.position, elbowTarget.position);
                        Gizmos.DrawSphere(elbowTarget.position, gizmoRadius);
                    }
                }

                if (priorJoint == Arm.PriorJoint.Elbow)
                {
                    Gizmos.color = Color.yellow;
                }
                else
                {
                    Gizmos.color = Color.red;
                }

                if (handTarget)
                {
                    Gizmos.DrawLine(handPitch.transform.position, handTarget.position);
                    Gizmos.DrawSphere(handTarget.position, gizmoRadius);
                }

            }
        }


        [Tooltip("ロボットモデルです。それ自体にアタッチされていれば未指定で構いません")]
        public Transform premaidRoot;

        [Tooltip("左手先目標です。未指定ならば自動生成します")]
        public Transform leftHandTarget;

        [Tooltip("右手先目標です。未指定ならば自動生成します")]
        public Transform rightHandTarget;

        [Tooltip("左手肘目標です。未指定ならば自動生成します")]
        public Transform leftElbowTarget;

        [Tooltip("右手肘目標です。未指定ならば自動生成します")]
        public Transform rightElbowTarget;

        private Arm leftArm;
        private Arm rightArm;

        [Tooltip("IKの基準を何に置くかです")]
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
            leftArm.handTarget = leftHandTarget;
            leftArm.elbowTarget = leftElbowTarget;
            leftArm.Initialize();

            leftElbowTarget = leftArm.elbowTarget;
            leftHandTarget = leftArm.handTarget;

            rightArm = new Arm();
            rightArm.isRightSide = true;
            rightArm.shoulderPitch = GetJointById("02");
            rightArm.upperArmRoll = GetJointById("09");
            rightArm.upperArmPitch = GetJointById("0D");
            rightArm.lowerArmRoll = GetJointById("11");
            rightArm.handPitch = GetJointById("15");
            rightArm.handTarget = rightHandTarget;
            rightArm.elbowTarget = rightElbowTarget;
            rightArm.Initialize();

            rightElbowTarget = rightArm.elbowTarget;
            rightHandTarget = rightArm.handTarget;

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
