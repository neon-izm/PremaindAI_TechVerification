// 指定したHumanoidの動作をプリメイドAIモデルに真似させます
//
// 使い方
//  1. PremaidAIモデルにこのスクリプトをアタッチしておく
//  2. 動作元となるモデルを sourceAnimator に指定
//
// 制限事項
//  - 元となるモデルは Humanoid で、必要なボーンが揃っていること
//  - 元となるモデルは Start() 時点では Tポーズ をとっていること

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PreMaid.HumanoidTracer
{
    public class HumanoidMimicryController : MonoBehaviour
    {
        /// <summary>
        /// 元となる動きをするHumanoid
        /// </summary>
        [Tooltip("このモデルの動作を真似します")]
        public Animator sourceHumanoid;

        /// <summary>
        /// 動かす対象のモデル
        /// </summary>
        [Tooltip("反映先ロボットモデルです。それ自体にアタッチされていれば未指定で構いません")]
        private Transform premaidRoot;

        /// <summary>
        /// サーボの取り付け位置
        /// </summary>
        public enum ServoPosition
        {
            RightShoulderPitch = 0x02,  //肩ピッチR
            HeadPitch = 0x03,           //頭ピッチ
            LeftShoulderPitch = 0x04,   //肩ピッチL
            HeadYaw = 0x05,             //頭ヨー
            RightHipYaw = 0x06,         //ヒップヨーR
            HeadRoll = 0x07             /*萌え軸*/,
            LeftHipYaw = 0x08,          //ヒップヨーL
            RightShoulderRoll = 0x09,   //肩ロールR
            RightHipRoll = 0x0A,        //ヒップロールR
            LeftShoulderRoll = 0x0B,    //肩ロールL
            LeftHipRoll = 0x0C,         //ヒップロールL
            RightUpperArmYaw = 0x0D,    //上腕ヨーR
            RightUpperLegPitch = 0x0E,  //腿ピッチR
            LeftUpperArmYaw = 0x0F,     //上腕ヨーL
            LeftUpperLegPitch = 0x10,   //腿ピッチL
            RightLowerArmPitch = 0x11,  //肘ピッチR
            RightLowerLegPitch = 0x12,  //膝ピッチR
            LeftLowerArmPitch = 0x13,   //肘ピッチL
            LeftLowerLegPitch = 0x14,   //肘ピッチL
            RightHandYaw = 0x15,        //手首ヨーR
            RightFootPitch = 0x16,      //足首ピッチR
            LeftHandYaw = 0x17,         //手首ヨーL
            LeftFootPitch = 0x18,       //足首ピッチL
            RightFootRoll = 0x1A,       //足首ロールR
            LeftFootRoll = 0x1C,        //足首ロールL
        }

        /// <summary>
        /// プリメイドAIモデル中の全ModelJoint
        /// </summary>
        private ModelJoint[] _joints;

        /// <summary>
        /// 関節位置とModelJointの対応
        /// </summary>
        private Dictionary<ServoPosition, ModelJoint> servos = new Dictionary<ServoPosition, ModelJoint>();

        /// <summary>
        /// 元になるモデルの必要関節初期姿勢
        /// </summary>
        private Dictionary<HumanBodyBones, Quaternion> invOrgRotations = new Dictionary<HumanBodyBones, Quaternion>();

        // 上半身、下半身それぞれの根本となるボーン（モデルによってはChestが無かったりするため変数としておく）
        private HumanBodyBones upperBodyRootBone = HumanBodyBones.Chest;
        private HumanBodyBones lowerBodyRootBone = HumanBodyBones.Hips;


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

            // ボーンの対応を初期化
            InitializeServos();

            // 元モデルの初期姿勢を記憶
            InitializeOriginalRotations();
        }

        /// <summary>
        /// サーボ一覧を準備
        /// </summary>
        void InitializeServos()
        {
            servos.Clear();
            AddServo(ServoPosition.HeadYaw);             //頭ヨー
            AddServo(ServoPosition.HeadRoll);            /*萌え軸*/
            AddServo(ServoPosition.HeadPitch);           //頭ピッチ
            AddServo(ServoPosition.RightShoulderPitch);  //肩ピッチR
            AddServo(ServoPosition.RightShoulderRoll);   //肩ロールR
            AddServo(ServoPosition.RightUpperArmYaw);    //上腕ヨーR
            AddServo(ServoPosition.RightLowerArmPitch);  //肘ピッチR
            AddServo(ServoPosition.RightHandYaw);        //手首ヨーR
            AddServo(ServoPosition.LeftShoulderPitch);   //肩ピッチL
            AddServo(ServoPosition.LeftShoulderRoll);    //肩ロールL
            AddServo(ServoPosition.LeftUpperArmYaw);     //上腕ヨーL
            AddServo(ServoPosition.LeftLowerArmPitch);   //肘ピッチL
            AddServo(ServoPosition.LeftHandYaw);         //手首ヨーL
            AddServo(ServoPosition.RightHipYaw);         //ヒップヨーR
            AddServo(ServoPosition.RightHipRoll);        //ヒップロールR
            AddServo(ServoPosition.RightUpperLegPitch);  //腿ピッチR
            AddServo(ServoPosition.RightLowerLegPitch);  //膝ピッチR
            AddServo(ServoPosition.RightFootPitch);      //足首ピッチR
            AddServo(ServoPosition.RightFootRoll);       //足首ロールR
            AddServo(ServoPosition.LeftHipYaw);          //ヒップヨーL
            AddServo(ServoPosition.LeftHipRoll);         //ヒップロールL
            AddServo(ServoPosition.LeftUpperLegPitch);   //腿ピッチL
            AddServo(ServoPosition.LeftLowerLegPitch);   //肘ピッチL
            AddServo(ServoPosition.LeftFootPitch);       //足首ピッチL
            AddServo(ServoPosition.LeftFootRoll);        //足首ロールL
        }

        /// <summary>
        /// サーボ情報一つ分を記憶
        /// </summary>
        /// <param name="servoNo"></param>
        void AddServo(ServoPosition servoNo)
        {
            var joint = ModelJoint.GetJointById((int)servoNo, ref _joints);
            if (joint)
            {
                servos.Add(servoNo, joint);

                // 開発用に、いったん制限なしとする
                joint.minAngle = -179f;
                joint.maxAngle = 180f;
                if (joint.minAngle > 0f) joint.minAngle = 0f;   // 開発用。Tポーズだと上腕が0degのため
                if (joint.maxAngle < 0f) joint.maxAngle = 0f;   // 開発用。Tポーズだと上腕が0degのため
            }
            //Debug.Log(servoId + " " + joint.name);
        }

        /// <summary>
        /// 元モデルの初期姿勢を記憶
        /// </summary>
        void InitializeOriginalRotations()
        {
            invOrgRotations.Clear();

            if (!sourceHumanoid) return;

            Quaternion invRootRot, invParentRot, invRot;
            Transform parentTransform;

            invRootRot = Quaternion.Inverse(sourceHumanoid.transform.rotation);

            // 上半身の基部
            upperBodyRootBone = HumanBodyBones.Chest;
            parentTransform = sourceHumanoid.GetBoneTransform(upperBodyRootBone);
            if (!parentTransform)
            {
                upperBodyRootBone = HumanBodyBones.Spine;
                parentTransform = sourceHumanoid.GetBoneTransform(upperBodyRootBone);
            }
            if (!parentTransform)
            {
                upperBodyRootBone = HumanBodyBones.Hips;
                parentTransform = sourceHumanoid.GetBoneTransform(upperBodyRootBone);
            }
            AddInvOriginalRotation(upperBodyRootBone, invRootRot);
            invParentRot = Quaternion.Inverse(parentTransform.rotation);
            Debug.Log("InvRootRot: " + invRootRot + "  InvParentRot: " + invParentRot);

            // 頭部
            AddInvOriginalRotation(HumanBodyBones.Head, invParentRot);

            // 右腕部
            invRot = AddInvOriginalRotation(HumanBodyBones.RightUpperArm, invParentRot);
            invRot = AddInvOriginalRotation(HumanBodyBones.RightLowerArm, invParentRot);
            invRot = AddInvOriginalRotation(HumanBodyBones.RightHand, invRot);

            // 左腕部
            invRot = AddInvOriginalRotation(HumanBodyBones.LeftUpperArm, invParentRot);
            invRot = AddInvOriginalRotation(HumanBodyBones.LeftLowerArm, invParentRot);
            invRot = AddInvOriginalRotation(HumanBodyBones.LeftHand, invRot);

            // 下半身の基部
            lowerBodyRootBone = HumanBodyBones.Spine;
            parentTransform = sourceHumanoid.GetBoneTransform(lowerBodyRootBone);
            invParentRot = Quaternion.Inverse(parentTransform.rotation);
            //AddInvOriginalRotation(lowerBodyRootBone, invRootRot);

            // 右脚部
            invRot = AddInvOriginalRotation(HumanBodyBones.RightUpperLeg, invParentRot);
            invRot = AddInvOriginalRotation(HumanBodyBones.RightLowerLeg, invParentRot);
            invRot = AddInvOriginalRotation(HumanBodyBones.RightFoot, invParentRot);

            // 左脚部
            invRot = AddInvOriginalRotation(HumanBodyBones.LeftUpperLeg, invParentRot);
            invRot = AddInvOriginalRotation(HumanBodyBones.LeftLowerLeg, invParentRot);
            invRot = AddInvOriginalRotation(HumanBodyBones.LeftFoot, invParentRot);
        }

        /// <summary>
        /// サーボ情報一つ分を記憶
        /// </summary>
        /// <param name="servoNo"></param>
        Quaternion AddInvOriginalRotation(HumanBodyBones bone, Quaternion invParentRotation)
        {
            Transform tr = sourceHumanoid.GetBoneTransform(bone);
            Quaternion rot = invParentRotation * tr.rotation;
            //Quaternion rot = tr.rotation * invParentRotation;
            //Quaternion invRot = Quaternion.Inverse(invParentRotation) * Quaternion.Inverse(tr.rotation);
            Quaternion invRot = Quaternion.Inverse(rot);
            //invRot = Quaternion.Inverse(tr.rotation);
            //invRot = Quaternion.Inverse(invParentRotation) * tr.rotation;
            invOrgRotations.Add(bone, invRot);
            return Quaternion.Inverse(tr.rotation);
        }

        /// <summary>
        /// Quaternion で渡された姿勢のうち、X, Y, Z 軸いずれか周り成分を抽出してサーボ角に反映します
        /// </summary>
        /// <param name="rot">目標姿勢</param>
        /// <param name="joint">指定サーボ</param>
        /// <returns>回転させた軸成分を除いた残りの回転 Quaternion</returns>
        Quaternion ApplyPartialRotation(Quaternion rot, ModelJoint joint)
        {
            Quaternion q = rot;
            Vector3 axis = Vector3.right;
            float direction = (joint.isInverse ? -1f : 1f);     // 逆転なら-1
            switch (joint.targetAxis)
            {
                case ModelJoint.Axis.X:
                    q.y = q.z = 0;
                    if (q.x < 0) direction = -direction;
                    axis = Vector3.right;
                    break;
                case ModelJoint.Axis.Y:
                    q.x = q.z = 0;
                    if (q.y < 0) direction = -direction;
                    axis = Vector3.up;
                    break;
                case ModelJoint.Axis.Z:
                    q.x = q.y = 0;
                    if (q.z < 0) direction = -direction;
                    axis = Vector3.forward;
                    break;
            }
            if (q.w == 0 && q.x == 0 && q.y == 0 && q.z == 0)
            {
                Debug.Log("Joint: " + joint.name + " rotation N/A");
                q = Quaternion.identity;
            }
            q.Normalize();
            float angle = Mathf.Acos(q.w) * 2.0f * Mathf.Rad2Deg * direction;

            var actualAngle = joint.SetServoValue(angle);

            return rot * Quaternion.Inverse(q);
            //return rot * Quaternion.Inverse(Quaternion.AngleAxis(actualAngle, axis);
        }

        /// <summary>
        /// Quaternion で渡された姿勢のうち、X, Y, Z 軸いずれか周り成分を抽出してサーボ角に反映します
        /// </summary>
        /// <param name="rot">目標姿勢</param>
        /// <param name="joint">指定サーボ</param>
        /// <returns>回転させた軸成分を除いた残りの回転 Quaternion</returns>
        Quaternion ApplyPartialRotation(Quaternion rot, Quaternion invRot, ModelJoint joint)
        {
            Quaternion q = rot;
            Vector3 axis = Vector3.right;
            float direction = (joint.isInverse ? -1f : 1f);     // 逆転なら-1
            switch (joint.targetAxis)
            {
                case ModelJoint.Axis.X:
                    q.y = q.z = 0;
                    if (q.x < 0) direction = -direction;
                    axis = Vector3.right;
                    break;
                case ModelJoint.Axis.Y:
                    q.x = q.z = 0;
                    if (q.y < 0) direction = -direction;
                    axis = Vector3.up;
                    break;
                case ModelJoint.Axis.Z:
                    q.x = q.y = 0;
                    if (q.z < 0) direction = -direction;
                    axis = Vector3.forward;
                    break;
            }
            if (q.w == 0 && q.x == 0 && q.y == 0 && q.z == 0)
            {
                Debug.Log("Joint: " + joint.name + " rotation N/A");
                q = Quaternion.identity;
            }
            q.Normalize();
            float angle = Mathf.Acos(q.w) * 2.0f * Mathf.Rad2Deg * direction;

            var actualAngle = joint.SetServoValue(angle);

            return Quaternion.Inverse(Quaternion.AngleAxis(actualAngle / direction, axis)) * invRot;
        }

        /// <summary>
        /// Quaternion で渡された姿勢のうち、X, Y, Z 軸いずれか周り成分を抽出してサーボ角に反映します
        /// </summary>
        /// <param name="rot">目標姿勢</param>
        /// <param name="joint">指定サーボ</param>
        /// <returns>回転させた軸成分を除いた残りの回転 Quaternion</returns>
        Quaternion ApplyDirectRotation(Quaternion rot, float angle, ModelJoint joint)
        {
            Quaternion q;
            Vector3 axis = Vector3.right;
            float direction = (joint.isInverse ? -1f : 1f);     // 逆転なら-1
            switch (joint.targetAxis)
            {
                case ModelJoint.Axis.X:
                    axis = Vector3.right;
                    break;
                case ModelJoint.Axis.Y:
                    axis = Vector3.up;
                    break;
                case ModelJoint.Axis.Z:
                    axis = Vector3.forward;
                    break;
            }
            var actualAngle = joint.SetServoValue(angle * direction);

            //q = Quaternion.AngleAxis(angle, axis);
            q = Quaternion.AngleAxis(actualAngle / direction, axis);    // 角度制限を考慮
            return Quaternion.Inverse(q) * rot;
        }

        /// <summary>
        /// 指定角度がほぼゼロならばtrue
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        bool IsAngleApproximatelyZero(float angle)
        {
            const float threshold = 1.0f;   // これ未満ならほぼ0と見做す角度[deg]
            return (Mathf.Abs(angle) < threshold);
        }

        /// <summary>
        /// 姿勢を各サーボの角度に反映
        /// </summary>
        void LateUpdate()
        {
            if (!sourceHumanoid) return;

            HumanBodyBones bone;    // 対象ボーン
            Transform tr;           // 対象Transformの作業用変数
            Transform parentTransform;          // 対象の親Transform
            Quaternion rot;         // 相対姿勢の作業用変数

            var rootTr = sourceHumanoid.GetBoneTransform(HumanBodyBones.Hips);
            Quaternion invParentRot = Quaternion.Inverse(rootTr.rotation);
            Quaternion invRot;

            Vector3 euler;
            Vector3 xt;

            // 頭部と両腕の親は Chest とする
            parentTransform= sourceHumanoid.GetBoneTransform(upperBodyRootBone);
            invParentRot = Quaternion.Inverse(parentTransform.rotation);

            // 頭部姿勢を反映
            bone = HumanBodyBones.Head;
            tr = sourceHumanoid.GetBoneTransform(bone);
            //rot = invParentRot * tr.rotation * invOrgRotations[bone];
            rot = invOrgRotations[bone] * invParentRot * tr.rotation;
            rot = ApplyPartialRotation(rot, servos[ServoPosition.HeadYaw]);
            rot = ApplyPartialRotation(rot, servos[ServoPosition.HeadRoll]);
            rot = ApplyPartialRotation(rot, servos[ServoPosition.HeadPitch]);
            //euler = MathfUtility.QuaternionToEuler(rot, MathfUtility.RotationOrder.XZY);
            //rot = ApplyDirectRotation(rot, euler.y, servos[ServoPosition.HeadYaw]);
            //rot = ApplyDirectRotation(rot, euler.z, servos[ServoPosition.HeadRoll]);
            //rot = ApplyDirectRotation(rot, euler.x, servos[ServoPosition.HeadPitch]);

            // 右上腕姿勢を反映
            bone = HumanBodyBones.RightUpperArm;
            tr = sourceHumanoid.GetBoneTransform(bone);
            rot = invParentRot * tr.rotation * invOrgRotations[bone];
            xt = rot * Vector3.right;
            euler.x = -Mathf.Atan2(xt.z, -xt.y) * Mathf.Rad2Deg;
            euler.z = -Mathf.Atan2(Mathf.Sqrt(xt.y * xt.y + xt.z * xt.z), xt.x) * Mathf.Rad2Deg;
            euler.y = 0f;
            if (IsAngleApproximatelyZero(euler.z))
            {
                euler.x = servos[ServoPosition.RightShoulderPitch].currentAngle;    // 上腕が真横に伸びた状態ならば、肩は今の姿勢をキープ
            }
            invRot = Quaternion.identity;
            invRot = ApplyDirectRotation(invRot, euler.x, servos[ServoPosition.RightShoulderPitch]);
            invRot = ApplyDirectRotation(invRot, euler.z, servos[ServoPosition.RightShoulderRoll]);

            // 右前腕姿勢を反映
            bone = HumanBodyBones.RightLowerArm;
            tr = sourceHumanoid.GetBoneTransform(bone);
            rot = invRot * invParentRot * tr.rotation * invOrgRotations[bone];
            //rot = invParentRot * tr.rotation * invOrgRotations[bone];
            xt = rot * Vector3.right;
            euler.x = -Mathf.Atan2(xt.z, -xt.y) * Mathf.Rad2Deg;
            euler.z = -Mathf.Atan2(Mathf.Sqrt(xt.y * xt.y + xt.z * xt.z), xt.x) * Mathf.Rad2Deg;
            euler.y = 0f;
            //Debug.Log("Rot: " + rot.eulerAngles + "  pos: " + xt + " Euler: " + euler);
            if (IsAngleApproximatelyZero(euler.z))
            {
                //euler.x = Mathf.LerpAngle(servos[ServoPosition.RightShoulderPitch].currentAngle, 0f, 0.5f);    // 前腕がまっすぐに伸びた状態ならば、ひねりは今の姿勢をキープ
                euler.x = 0f;    // 前腕がまっすぐに伸びた状態ならば、ひねりはなしにする
            }
            //Debug.Log("Rot: " + rot + "  TmpPos: " + xt + " Euler: " + euler);
            invRot = ApplyDirectRotation(invRot, euler.x, servos[ServoPosition.RightUpperArmYaw]);
            invRot = ApplyDirectRotation(invRot, euler.z, servos[ServoPosition.RightLowerArmPitch]);

            // 右手首姿勢を反映
            bone = HumanBodyBones.RightHand;
            tr = sourceHumanoid.GetBoneTransform(bone);
            //rot = invRot * tr.rotation * invOrgRotations[bone];
            rot = invParentRot * tr.rotation * invOrgRotations[bone];
            //rot = invOrgRotations[bone] * invRot * tr.rotation;
            rot = ApplyPartialRotation(rot, servos[ServoPosition.RightHandYaw]);

            // 左上腕姿勢を反映
            bone = HumanBodyBones.LeftUpperArm;
            tr = sourceHumanoid.GetBoneTransform(bone);
            rot = invParentRot * tr.rotation * invOrgRotations[bone];
            xt = rot * Vector3.right;
            euler.x = Mathf.Atan2(xt.z, xt.y) * Mathf.Rad2Deg;
            euler.z = Mathf.Atan2(Mathf.Sqrt(xt.y * xt.y + xt.z * xt.z), xt.x) * Mathf.Rad2Deg;
            euler.y = 0f;
            //Debug.Log("Rot: " + rot + "  TmpPos: " + xt + " Euler: " + euler);
            if (IsAngleApproximatelyZero(euler.z))
            {
                euler.x = servos[ServoPosition.LeftShoulderPitch].currentAngle;    // 上腕が真横に伸びた状態ならば、肩は今の姿勢をキープ
            }
            invRot = Quaternion.identity;
            invRot = ApplyDirectRotation(invRot, euler.x, servos[ServoPosition.LeftShoulderPitch]);
            invRot = ApplyDirectRotation(invRot, euler.z, servos[ServoPosition.LeftShoulderRoll]);

            // 左前腕姿勢を反映
            bone = HumanBodyBones.LeftLowerArm;
            tr = sourceHumanoid.GetBoneTransform(bone);
            rot = invRot * invParentRot * tr.rotation * invOrgRotations[bone];
            xt = rot * Vector3.right;
            euler.x = Mathf.Atan2(xt.z, xt.y) * Mathf.Rad2Deg;
            euler.z = Mathf.Atan2(Mathf.Sqrt(xt.y * xt.y + xt.z * xt.z), xt.x) * Mathf.Rad2Deg;
            euler.y = 0f;
            //Debug.Log("Rot: " + rot + "  TmpPos: " + xt + " Euler: " + euler);
            if (IsAngleApproximatelyZero(euler.z))
            {
                euler.x = 0f;    // 前腕がまっすぐに伸びた状態ならば、ひねりはなしにする
            }
            invRot = ApplyDirectRotation(invRot, euler.x, servos[ServoPosition.LeftUpperArmYaw]);
            invRot = ApplyDirectRotation(invRot, euler.z, servos[ServoPosition.LeftLowerArmPitch]);

            // 左手首姿勢を反映
            bone = HumanBodyBones.LeftHand;
            tr = sourceHumanoid.GetBoneTransform(bone);
            //rot = invRot * tr.rotation * invOrgRotations[bone];
            //rot = invOrgRotations[bone] * invRot * tr.rotation;
            rot = invParentRot * tr.rotation * invOrgRotations[bone];
            rot = ApplyPartialRotation(rot, servos[ServoPosition.LeftHandYaw]);


            // 両脚の親は Hips とする
            parentTransform = sourceHumanoid.GetBoneTransform(lowerBodyRootBone);
            invParentRot = Quaternion.Inverse(parentTransform.rotation);
            
            // 右大腿姿勢を反映
            bone = HumanBodyBones.RightUpperLeg;
            tr = sourceHumanoid.GetBoneTransform(bone);
            rot = invParentRot * tr.rotation * invOrgRotations[bone];
            euler = MathfUtility.QuaternionToEuler(rot, MathfUtility.RotationOrder.YZX);
            invRot = Quaternion.identity;
            invRot = ApplyDirectRotation(invRot, euler.y, servos[ServoPosition.RightHipYaw]);
            invRot = ApplyDirectRotation(invRot, euler.z, servos[ServoPosition.RightHipRoll]);
            invRot = ApplyDirectRotation(invRot, euler.x, servos[ServoPosition.RightUpperLegPitch]);

            // 右脛姿勢を反映
            bone = HumanBodyBones.RightLowerLeg;
            tr = sourceHumanoid.GetBoneTransform(bone);
            rot = invRot * invParentRot * tr.rotation * invOrgRotations[bone];
            //rot = invOrgRotations[bone] * invRot * tr.rotation;
            invRot = ApplyPartialRotation(rot, invRot, servos[ServoPosition.RightLowerLegPitch]);

            // 右足姿勢を反映
            bone = HumanBodyBones.RightFoot;
            tr = sourceHumanoid.GetBoneTransform(bone);
            rot = invRot * invParentRot * tr.rotation * invOrgRotations[bone];
            //rot = invOrgRotations[bone] * invRot * tr.rotation;
            rot = ApplyPartialRotation(rot, servos[ServoPosition.RightFootPitch]);
            rot = ApplyPartialRotation(rot, servos[ServoPosition.RightFootRoll]);


            // 左大腿姿勢を反映
            bone = HumanBodyBones.LeftUpperLeg;
            tr = sourceHumanoid.GetBoneTransform(bone);
            rot = invParentRot * tr.rotation * invOrgRotations[bone];
            euler = MathfUtility.QuaternionToEuler(rot, MathfUtility.RotationOrder.YZX);
            invRot = Quaternion.identity;
            invRot = ApplyDirectRotation(invRot, euler.y, servos[ServoPosition.LeftHipYaw]);
            invRot = ApplyDirectRotation(invRot, euler.z, servos[ServoPosition.LeftHipRoll]);
            invRot = ApplyDirectRotation(invRot, euler.x, servos[ServoPosition.LeftUpperLegPitch]);

            // 左脛姿勢を反映
            bone = HumanBodyBones.LeftLowerLeg;
            tr = sourceHumanoid.GetBoneTransform(bone);
            rot = invRot * invParentRot * tr.rotation * invOrgRotations[bone];
            invRot = ApplyPartialRotation(rot, invRot, servos[ServoPosition.LeftLowerLegPitch]);

            // 左足姿勢を反映
            bone = HumanBodyBones.LeftFoot;
            tr = sourceHumanoid.GetBoneTransform(bone);
            rot = invRot * invParentRot * tr.rotation * invOrgRotations[bone];
            rot = ApplyPartialRotation(rot, servos[ServoPosition.LeftFootPitch]);
            rot = ApplyPartialRotation(rot, servos[ServoPosition.LeftFootRoll]);
        }
    }
}

