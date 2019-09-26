using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PreMaid
{
    public class PreMaidIKController : MonoBehaviour
    {
        #region IK solver classes
        /// <summary>
        /// 頭部のIKソルバ
        /// </summary>
        public class HeadIK
        {
            private Transform baseTransform;
            public ModelJoint neckYaw;
            public ModelJoint headPitch;
            public ModelJoint headRoll;     // 所謂萌軸

            public Transform headTarget;

            public void Initialize()
            {
                baseTransform = neckYaw.transform.parent;

                // 目標点が無ければ自動生成
                if (!headTarget)
                {
                    const float distance = 0.3f;
                    var obj = new GameObject("HeadTarget");
                    headTarget = obj.transform;
                    headTarget.parent = baseTransform;
                    headTarget.position = neckYaw.transform.position + baseTransform.rotation * Vector3.forward * distance;
                }
            }

            public void ApplyIK()
            {
                if (!headTarget) return;

                Quaternion invBaseRotation = Quaternion.Inverse(baseTransform.rotation);
                Vector3 gazeVec = invBaseRotation * (headTarget.position - headRoll.transform.position);

                Quaternion lookAtRot = Quaternion.LookRotation(gazeVec);
                Vector3 eular = lookAtRot.eulerAngles;
                float yaw = eular.y - (eular.y > 180f ? 360f : 0f);
                float pitch = eular.x - (eular.x > 180f ? 360f : 0f);

                neckYaw.SetServoValue(yaw);
                headPitch.SetServoValue(pitch);
            }

            public void DrawGizmos()
            {
                const float gizmoRadius = 0.005f;

                Gizmos.color = Color.red;

                if (headTarget)
                {
                    Gizmos.DrawLine(headRoll.transform.position, headTarget.position);
                    Gizmos.DrawSphere(headTarget.position, gizmoRadius);
                }
            }
        }

        /// <summary>
        /// 腕のIKソルバ
        /// </summary>
        public class ArmIK
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

                if (priorJoint == ArmIK.PriorJoint.Elbow)
                {
                    Gizmos.color = Color.red;

                    if (elbowTarget)
                    {
                        Gizmos.DrawLine(lowerArmRoll.transform.position, elbowTarget.position);
                        Gizmos.DrawSphere(elbowTarget.position, gizmoRadius);
                    }
                }

                if (priorJoint == ArmIK.PriorJoint.Elbow)
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

        /// <summary>
        /// 胴体IKソルバ
        /// </summary>
        public class BodyIK
        {
            private Transform baseTransform;
            public ModelJoint upperLegYaw;
            public ModelJoint upperLegRoll;
            public ModelJoint upperLegPitch;
            public ModelJoint kneePitch;
            public ModelJoint anklePitch;
            public ModelJoint footRoll;
            private Transform footEnd;

            public Transform footTarget;

            /// <summary>
            /// 右脚なら true、左脚なら false にしておく
            /// </summary>
            public bool isRightSide = false;


            public void Initialize()
            {
                baseTransform = upperLegYaw.transform.parent;

                if (footRoll.transform.childCount > 0)
                {
                    footEnd = footRoll.transform.GetChild(0);   // footRollに子（Foot_endを期待）があればその位置を終端とする
                }
                else
                {
                    footEnd = footRoll.transform;   // footRollに子が無ければそれが終端とする
                }

                // 目標が無ければ自動生成
                if (!footTarget)
                {
                    var obj = new GameObject((isRightSide ? "Right" : "Left") + "FootTarget");
                    footTarget = obj.transform;
                    footTarget.parent = baseTransform;
                    footTarget.position = footEnd.position;
                    footTarget.rotation = baseTransform.rotation;
                }
            }

            public void ApplyIK()
            {
                if (!footTarget) return;

                Quaternion invBaseRotation = Quaternion.Inverse(baseTransform.rotation);

                Quaternion targetRotation = invBaseRotation * footTarget.rotation;


                Vector3 rt = targetRotation.eulerAngles;

                float a0 = rt.y;
                upperLegYaw.SetServoValue(a0);

            }

            public void DrawGizmos()
            {
                Vector3 gizmoSize = new Vector3(0.02f, 0.002f, 0.05f);

                Gizmos.color = Color.red;

                if (footTarget)
                {
                    Gizmos.DrawLine(footEnd.position, footTarget.position);
                    Gizmos.DrawCube(footTarget.position, gizmoSize);
                }
            }
        }

        /// <summary>
        /// 脚IKソルバ
        /// </summary>
        public class LegIK
        {
            private Transform baseTransform;
            public ModelJoint upperLegYaw;      // x0
            public ModelJoint upperLegRoll;     // x1
            public ModelJoint upperLegPitch;    // x2
            public ModelJoint kneePitch;        // x3
            public ModelJoint anklePitch;       // x4
            public ModelJoint footRoll;         // x5
            private Vector3 footEndPos;         // x6

            public Transform footTarget;

            /// <summary>
            /// 右脚なら true、左脚なら false にしておく
            /// </summary>
            public bool isRightSide = false;

            private Vector3 xo01, xo12, xo23, xo34, xo45, xo15, xo06;

            public void Initialize()
            {
                baseTransform = upperLegYaw.transform.parent;

                footEndPos = footRoll.transform.position;
                footEndPos.y = baseTransform.position.y;        // 初期状態は水平であるとして、足裏はbaseTransformの高さだとする

                // 目標が無ければ自動生成
                if (!footTarget)
                {
                    var obj = new GameObject((isRightSide ? "Right" : "Left") + "FootTarget");
                    footTarget = obj.transform;
                    footTarget.parent = baseTransform;
                    footTarget.position = footEndPos;
                    footTarget.rotation = baseTransform.rotation;
                }

                // ※これが呼ばれる時点（初期状態）ではモデルは T-Pose であること
                Quaternion invBaseRotation = Quaternion.Inverse(baseTransform.rotation);
                xo12 = invBaseRotation * (upperLegPitch.transform.position - upperLegRoll.transform.position);
                xo23 = invBaseRotation * (kneePitch.transform.position - upperLegPitch.transform.position);
                xo34 = invBaseRotation * (anklePitch.transform.position - kneePitch.transform.position);
                xo45 = invBaseRotation * (footRoll.transform.position - anklePitch.transform.position);
                xo15 = invBaseRotation * (footRoll.transform.position - upperLegRoll.transform.position);
                xo06 = invBaseRotation * (footEndPos - upperLegYaw.transform.position);
            }

            public void ApplyIK()
            {
                if (!footTarget) return;

                // これ以下に元位置に近づきすぎた目標は無視する閾値
                const float sqrMinDistance = 0.000025f;   // [m^2]
                const float a1Limit = 0.1f;               // [deg]

                float sign = (isRightSide ? -1f : 1f);  // 左右の腕による方向入れ替え用

                Quaternion invBaseRotation = Quaternion.Inverse(baseTransform.rotation);

                Quaternion targetRotation = invBaseRotation * footTarget.rotation;

                // Unityでは Z,Y,X の順番なので、ロボットとは一致しないはず
                Vector3 rt = targetRotation.eulerAngles;
                float yaw = -rt.y;
                float pitch = -rt.x;
                float roll = -rt.z;

                float a0, a1, a2, a3, a4, a5;
                a0 = yaw;

                Vector3 dx = invBaseRotation * (upperLegYaw.transform.position + (baseTransform.rotation * xo06) - footTarget.position);    // 初期姿勢時足元に対する足目標の変位
                dx = Quaternion.AngleAxis(-a0, Vector3.up) * dx;     // ヨーがある場合は目標変位も座標変換

                dx.z = 0f;  // ひとまず Z は無視してのIKを実装

                if (dx.sqrMagnitude < sqrMinDistance)
                {
                    // 目標が直立姿勢に近ければ、指令値はゼロとする
                    a1 = a2 = a3 = a4 = a5 = 0f;
                }
                else
                {
                    a1 = Mathf.Atan2(dx.x, -xo15.y + dx.y) * Mathf.Rad2Deg;
                    a5 = -a1;

                    //float len15 = dx.x / Mathf.Sin(a1 * Mathf.Deg2Rad); // 屈伸した状態での x1-x5 間長さ // sinだと a1==0 のとき失敗する
                    float len15 = (-xo15.y + dx.y) / Mathf.Cos(a1 * Mathf.Deg2Rad); // 屈伸した状態での x1-x5 間長さ
                    float cosa2 = (len15 + xo12.y + xo45.y) / (-xo23.y - xo34.y);
                    if (cosa2 >= 1f)
                    {
                        // 可動範囲以上に伸ばされそうな場合
                        a2 = 0f;
                    }
                    else if (cosa2 <= 0f)
                    {
                        // 完全に屈曲よりもさらに下げられそうな場合
                        a2 = sign * 90f;
                    }
                    else
                    {
                        // 屈伸の形に収まりそうな場合
                        a2 = sign * Mathf.Acos(cosa2) * Mathf.Rad2Deg;
                    }
                    a4 = a2;  // ひとまず、 a4 は a2 と同じとなる動作のみ可
                    a3 = a2 + a4;
                }

                upperLegYaw.SetServoValue(a0);
                upperLegRoll.SetServoValue(a1);
                upperLegPitch.SetServoValue(a2);
                kneePitch.SetServoValue(a3);
                anklePitch.SetServoValue(a4);
                footRoll.SetServoValue(a5);

            }

            public void DrawGizmos()
            {
                Vector3 gizmoSize = new Vector3(0.02f, 0.002f, 0.05f);

                Gizmos.color = Color.red;

                if (footTarget)
                {
                    Gizmos.DrawLine(footEndPos, footTarget.position);
                    //Gizmos.DrawCube(footTarget.position, gizmoSize);

                    var matrix = Gizmos.matrix;
                    Gizmos.matrix = Matrix4x4.TRS(footTarget.position, footTarget.rotation, Vector3.one);
                    Gizmos.DrawCube(Vector3.zero, gizmoSize);
                    Gizmos.matrix = matrix;
                }
            }
        }
        #endregion

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

        [Tooltip("頭部目標です。未指定ならば自動生成します")]
        public Transform headTarget;

        [Tooltip("左脚目標です。未指定ならば自動生成します")]
        public Transform leftFootTarget;

        [Tooltip("右脚目標です。未指定ならば自動生成します")]
        public Transform rightFootTarget;

        private ArmIK leftArmIK;
        private ArmIK rightArmIK;
        private HeadIK headIK;
        private LegIK leftLegIK;
        private LegIK rightLegIK;

        private ModelJoint[] _joints;

        [Tooltip("腕IKの基準を何に置くかです")]
        public ArmIK.PriorJoint priorJoint = ArmIK.PriorJoint.Elbow;

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

            // 左腕IKソルバを準備
            leftArmIK = new ArmIK();
            leftArmIK.isRightSide = false;
            leftArmIK.shoulderPitch = GetJointById("04");
            leftArmIK.upperArmRoll = GetJointById("0B");
            leftArmIK.upperArmPitch = GetJointById("0F");
            leftArmIK.lowerArmRoll = GetJointById("13");
            leftArmIK.handPitch = GetJointById("17");
            leftArmIK.handTarget = leftHandTarget;
            leftArmIK.elbowTarget = leftElbowTarget;
            leftArmIK.Initialize();
            leftElbowTarget = leftArmIK.elbowTarget;  // 自動生成されていたら、controller側に代入
            leftHandTarget = leftArmIK.handTarget;    // 自動生成されていたら、controller側に代入

            // 右腕IKソルバを準備
            rightArmIK = new ArmIK();
            rightArmIK.isRightSide = true;
            rightArmIK.shoulderPitch = GetJointById("02");
            rightArmIK.upperArmRoll = GetJointById("09");
            rightArmIK.upperArmPitch = GetJointById("0D");
            rightArmIK.lowerArmRoll = GetJointById("11");
            rightArmIK.handPitch = GetJointById("15");
            rightArmIK.handTarget = rightHandTarget;
            rightArmIK.elbowTarget = rightElbowTarget;
            rightArmIK.Initialize();
            rightElbowTarget = rightArmIK.elbowTarget;    // 自動生成されていたら、controller側に代入
            rightHandTarget = rightArmIK.handTarget;      // 自動生成されていたら、controller側に代入

            // 頭部IKソルバを準備
            headIK = new HeadIK();
            headIK.neckYaw = GetJointById("05");
            headIK.headPitch = GetJointById("03");
            headIK.headRoll = GetJointById("07");
            headIK.headTarget = headTarget;
            headIK.Initialize();
            headTarget = headIK.headTarget;         // 自動生成されていたら、controller側に代入

            // 左脚IKソルバを準備
            leftLegIK = new LegIK();
            leftLegIK.isRightSide = false;
            leftLegIK.upperLegYaw = GetJointById("08");
            leftLegIK.upperLegRoll = GetJointById("0C");
            leftLegIK.upperLegPitch = GetJointById("10");
            leftLegIK.kneePitch = GetJointById("14");
            leftLegIK.anklePitch = GetJointById("18");
            leftLegIK.footRoll = GetJointById("1C");
            leftLegIK.footTarget = leftFootTarget;
            leftLegIK.Initialize();
            leftFootTarget = leftLegIK.footTarget;

            // 右脚IKソルバを準備
            rightLegIK = new LegIK();
            rightLegIK.isRightSide = true;
            rightLegIK.upperLegYaw = GetJointById("06");
            rightLegIK.upperLegRoll = GetJointById("0A");
            rightLegIK.upperLegPitch = GetJointById("0E");
            rightLegIK.kneePitch = GetJointById("12");
            rightLegIK.anklePitch = GetJointById("16");
            rightLegIK.footRoll = GetJointById("1A");
            rightLegIK.footTarget = rightFootTarget;
            rightLegIK.Initialize();
            rightFootTarget = rightLegIK.footTarget;
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
        void LateUpdate()
        {
            leftLegIK.ApplyIK();
            rightLegIK.ApplyIK();

            leftArmIK.priorJoint = priorJoint;
            leftArmIK.ApplyIK();

            rightArmIK.priorJoint = priorJoint;
            rightArmIK.ApplyIK();

            headIK.ApplyIK();
        }

        private void OnDrawGizmos()
        {
            if (leftLegIK != null) leftLegIK.DrawGizmos();
            if (rightLegIK != null) rightLegIK.DrawGizmos();
            if (leftArmIK != null) leftArmIK.DrawGizmos();
            if (rightArmIK != null) rightArmIK.DrawGizmos();
            if (headIK != null) headIK.DrawGizmos();
        }
    }
}
