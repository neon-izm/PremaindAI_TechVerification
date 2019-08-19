using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PreMaid
{

    /// <summary>
    /// 各ジョイントに貼り付けて
    /// 「どの軸で動くか」
    /// 「サーボ番号は何か」
    /// みたいなのを指定する感じ
    /// </summary>
    public class ModelJoint : MonoBehaviour
    {
        [Header("02とか1Cとか当てる")]
        public string ServoID;

        public enum Axis
        {
            X, Y, Z
        }

        [SerializeField]
        public bool isInverse = false;
        //もしかして取り付け軸向きのinverseもenum定義した方がいいかも？

        [SerializeField]
        public Axis targetAxis = Axis.X;

        /// <summary>
        /// サーボ可動範囲の最小値[deg]
        /// </summary>
        [SerializeField]
        public float minAngle = -135f; // 3500

        /// <summary>
        /// サーボ可動範囲の最大値[deg]
        /// </summary>
        [SerializeField]
        public float maxAngle = 135f; // 11500

        /// <summary>
        /// 最大速度[deg/s]。ゼロだと速度制限なしとする
        /// </summary>
        [SerializeField]
        public float maxSpeed = 180f;

        /// <summary>
        /// 現在の角度[deg]
        /// 参照専用
        /// </summary>
        [SerializeField]
        public float currentAngle = 0f;

        /// <summary>
        /// 現在の角度指令値[サーボ用単位]
        /// 参照専用
        /// </summary>
        public float currentServoValue = 0f;

        /// <summary>
        /// ホームポジションでの角度指令値
        /// </summary>
        [SerializeField]
        public float defaultServoPosition = 7500f;

        /// <summary>
        /// 目標とする角度[deg] maxSpeedを超えない範囲でcurrentAngleがこれに追従する
        /// </summary>
        private float targetAngle = 0f;

        // 初期ローカル姿勢
        Quaternion initialLocalRotation = Quaternion.identity;

        // ローカル座標系でのサーボ回転軸
        Vector3 localServoAxis = Vector3.forward;

        public Quaternion normalizedRotation
        {
            get
            {
                return transform.rotation * Quaternion.Inverse(initialLocalRotation);
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            initialLocalRotation = transform.localRotation;

            // サーボ回転軸を求めておく
            Vector3 axis;
            switch (targetAxis)
            {
                case Axis.X:
                    axis = Vector3.right;
                    break;
                case Axis.Y:
                    axis = Vector3.up;
                    break;
                case Axis.Z:
                    axis = Vector3.forward;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            if (isInverse)
            {
                axis *= -1f;
            }

            // 元のローカル姿勢が正規化されていなくても対応するため、ルートの姿勢を基準にする
            Transform rootTransform = GetModelRootTransform();
            localServoAxis = Quaternion.Inverse(transform.rotation) * (rootTransform.rotation * axis);

            //// 可動範囲測定
            //minAngle = -0f;
            //maxAngle = 0f;
        }

        private void FixedUpdate()
        {
            // maxSpeedが0でなければ、ここでサーボ角を更新
            if (maxSpeed > 0f) UpdateServo();
        }

        /// <summary>
        /// モデルのルートには Animator があると仮定して、ルートのTransformを探す
        /// </summary>
        /// <returns></returns>
        private Transform GetModelRootTransform()
        {
            Transform t = transform;
            while (true)
            {
                if (t.GetComponent<Animator>()) break;  // Animator があればモデルのルートとみなす
                if (!t.parent) break;   // これ以上親がなければ、ルートとみなす
                t = t.parent;
            }
            return t;
        }

        //private void OnDestroy()
        //{
        //    Debug.Log(string.Format("{0} - Min: {1} Max: {2}", name, minAngle, maxAngle));
        //}

        /// <summary>
        /// 外部からサーボ角度[deg]を指定する
        /// </summary>
        /// <param name="angleEulerDegree"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetServoValue(float angleEulerDegree)
        {
            float angle = angleEulerDegree % 360f;
            if (angle > 180f) angle -= 360f;
            if (angle < -180f) angle += 360f;

            angle = Mathf.Clamp(angle, minAngle, maxAngle);
            targetAngle = angle;

            // maxSpeedが0なら（念のため負でも）ここで瞬間的に目標角にする
            if (maxSpeed <= 0f)
            {
                currentAngle = targetAngle;
                UpdateCurrentServoTransform();
            }
        }

        /// <summary>
        /// 実際にTranformにcurrentAngleを反映させる
        /// また、currentServoValueもここで更新
        /// ※ 本来は currentAngle をプロパティにして set メソッドで行えると良い内容
        /// </summary>
        private void UpdateCurrentServoTransform()
        {
            currentServoValue = Mathf.Round(currentAngle * 29.6296296296f + defaultServoPosition); //29.6296296296 = 4000/135
            transform.localRotation = initialLocalRotation * Quaternion.AngleAxis(currentAngle, localServoAxis);
        }

        /// <summary>
        /// 最高速度内でサーボ角を更新
        /// </summary>
        void UpdateServo()
        {
            if (!Mathf.Approximately(targetAngle, currentAngle))
            {
                float angle = targetAngle - currentAngle;
                float speed = Mathf.Abs(angle) / Time.deltaTime;
                if (speed > maxSpeed)
                {
                    angle *= maxSpeed / speed;
                }
                currentAngle += angle;
                UpdateCurrentServoTransform();
            }
        }

        /// <summary>
        /// 外部からサーボ指令値を指定する
        /// </summary>
        /// <param name="servoValue"></param>
        public void SetServoPosition(float servoValue)
        {
            float angle = (servoValue - defaultServoPosition) * 135f / 4000f;
            SetServoValue(angle);
        }


        /// <summary>
        /// 指定IDのModelJointを取得
        /// </summary>
        /// <param name="servoId">"0C"などのサーボID</param>
        /// <returns></returns>
        public static ModelJoint GetJointById(string servoId, ref ModelJoint[] joints)
        {
            foreach (var joint in joints)
            {
                if (joint.ServoID.Equals(servoId)) return joint;
            }
            return null;
        }

        /// <summary>
        /// 指定IDのModelJointを取得
        /// </summary>
        /// <param name="servoNo">0x0C などのサーボID</param>
        /// <returns></returns>
        public static ModelJoint GetJointById(int servoNo, ref ModelJoint[] joints)
        {
            string servoId = servoNo.ToString("X2");
            return GetJointById(servoId, ref joints);
        }
    }
}