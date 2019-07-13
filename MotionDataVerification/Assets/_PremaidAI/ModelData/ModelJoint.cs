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

        // 初期ローカル姿勢
        Quaternion initialLocalRotation = Quaternion.identity;

        // ローカル座標系でのサーボ回転軸
        Vector3 localServoAxis = Vector3.forward;

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

        /// <summary>
        /// 外部からサーボ角度[deg]を指定する
        /// </summary>
        /// <param name="angleEulerDegree"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetServoValue(float angleEulerDegree)
        {
            transform.localRotation = initialLocalRotation * Quaternion.AngleAxis(angleEulerDegree, localServoAxis);
        }
    }
}