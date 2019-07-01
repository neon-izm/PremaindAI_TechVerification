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
            X,Y,Z
        }

        [SerializeField] private bool isInverse = false;
        //もしかして取り付け軸向きのinverseもenum定義した方がいいかも？
        
        [SerializeField] private Axis targetAxis= Axis.X;
        
        Vector3 initRotation= Vector3.zero;
        
        // Start is called before the first frame update
        void Start()
        {
            initRotation = transform.localEulerAngles;
        }

        /// <summary>
        /// 外部から指定する
        /// </summary>
        /// <param name="angleEulerDegree"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public void SetServoValue(float angleEulerDegree)
        {
            Vector3 targetLocalRotation = initRotation;
            switch (targetAxis)
            {
                case Axis.X:
                    if (isInverse)
                    {
                        targetLocalRotation.x += -angleEulerDegree;
                    }
                    else
                    {
                        targetLocalRotation.x += angleEulerDegree;
                    }

                    break;
                case Axis.Y:
                    if (isInverse)
                    {
                        targetLocalRotation.y += -angleEulerDegree;
                    }
                    else
                    {
                        targetLocalRotation.y += angleEulerDegree;
                    }
                    break;
                case Axis.Z:
                    if (isInverse)
                    {
                        targetLocalRotation.z += -angleEulerDegree;
                    }
                    else
                    {
                        targetLocalRotation.z += angleEulerDegree;
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            transform.localRotation= Quaternion.Euler(targetLocalRotation);
        }
    }
}