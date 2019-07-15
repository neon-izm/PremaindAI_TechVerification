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

        [SerializeField] private bool isInverse = false;
        //もしかして取り付け軸向きのinverseもenum定義した方がいいかも？

        [SerializeField] private Axis targetAxis = Axis.X;

        /// <summary>
        /// サーボ可動範囲の最小値[deg]
        /// </summary>
        [SerializeField]
        private float minAngle = -135f; // 3500

        /// <summary>
        /// サーボ可動範囲の最大値[deg]
        /// </summary>
        [SerializeField]
        private float maxAngle = 135f; // 11500

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
            float angle = Mathf.Clamp(angleEulerDegree, minAngle, maxAngle);

            // 可動範囲オーバー時にデバッグ出力
            if (angle != angleEulerDegree)
            {
                ////if (ServoID.Equals("02") || ServoID.Equals("04") || ServoID.Equals("15") || ServoID.Equals("17"))
                //if (true)
                //{
                //    if (angleEulerDegree < minAngle)
                //    {
                //        minAngle = angleEulerDegree;
                //        //Debug.Log(string.Format("{0} - Angle: {1} Min: {2}", name, angleEulerDegree, minAngle));
                //    }
                //    else if (angleEulerDegree > maxAngle)
                //    {
                //        maxAngle = angleEulerDegree;
                //        //Debug.Log(string.Format("{0} - Angle: {1} Max: {2}", name, angleEulerDegree, maxAngle));
                //    }
                //    angle = angleEulerDegree;
                //}
                //else
                //{
                //    Debug.Log(string.Format("{0} - Angle: {1} Min: {2} Max: {3}", name, angleEulerDegree, minAngle, maxAngle));
                //}
            }

            transform.localRotation = initialLocalRotation * Quaternion.AngleAxis(angle, localServoAxis);
        }
    }
}