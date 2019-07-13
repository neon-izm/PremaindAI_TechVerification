using System;
using System.Collections;
using System.Collections.Generic;
using PreMaid.RemoteController;
using UnityEngine;

namespace PreMaid.HumanoidTracer
{
    /// <summary>
    /// 各ジョイントにアタッチして、ローカル回転角を元にサーボ数値を求める仕組み
    /// </summary>
    public class HumanoidModelJoint : MonoBehaviour
    {
        [SerializeField] private PreMaidServo.ServoPosition targetServo;

        public PreMaidServo.ServoPosition TargetServo
        {
            get => targetServo;
            set => targetServo = value;
        }

        [SerializeField] private float currentServoValue;

        [SerializeField] private ModelJoint.Axis _axis = ModelJoint.Axis.X;

        [SerializeField] private bool inverse = false;

        [SerializeField] private float defaultAngleDegree = 0;

        [SerializeField] private float defaultServoPosition;


        public float CurrentServoValue()
        {
            var floatAngleDegree = transform.localEulerAngles.x;
            switch (_axis)
            {
                case ModelJoint.Axis.X:
                    floatAngleDegree = transform.localEulerAngles.x;
                    break;
                case ModelJoint.Axis.Y:
                    floatAngleDegree = transform.localEulerAngles.y;
                    break;
                case ModelJoint.Axis.Z:
                    floatAngleDegree = transform.localEulerAngles.z;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }


            if (TargetServo == PreMaidServo.ServoPosition.LeftShoulderRoll)
            {
                floatAngleDegree -= 66;
            }

            if (TargetServo == PreMaidServo.ServoPosition.RightShoulderRoll)
            {
                floatAngleDegree += 66;
            }

            //180以上（つまり、350度とか）だったら-10度にして返したいので360引く
            if (floatAngleDegree > 180f)
            {
                floatAngleDegree -= 360f;
            }

            if (inverse)
            {
                floatAngleDegree *= -1f;
            }

            var eulerAngle = (floatAngleDegree) * 29.6296296296f + defaultServoPosition; //29.6296296296 = 4000/135
            /*
            if (TargetServo == PreMaidServo.ServoPosition.LeftShoulderRoll)
            {
                return 5500;
            }
            if (TargetServo == PreMaidServo.ServoPosition.RightShoulderRoll)
            {
                return 9500;
            }*/

            currentServoValue = eulerAngle;
            return eulerAngle;
        }

        private void Awake()
        {
            defaultAngleDegree = 0;
            if (targetServo == PreMaidServo.ServoPosition.RightShoulderRoll)
            {
                defaultServoPosition = 9500;
            }
            else if (targetServo == PreMaidServo.ServoPosition.LeftShoulderRoll)
            {
                defaultServoPosition = 5500;
            }
            else
            {
                defaultServoPosition = 7500;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
        }
    }
}