using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PreMaid.HumanoidTracer;
using PreMaid.RemoteController;
using UnityEngine;

namespace PreMaid.HumanoidTracer

{
    [RequireComponent(typeof(PreMaid.RemoteController.PreMaidPoseController))]
    [DefaultExecutionOrder(11001)] //after VRIK calclate
    public class PoseTracerManager : MonoBehaviour
    {
        private PreMaid.RemoteController.PreMaidPoseController _controller;

        [SerializeField] private Animator target;

        [SerializeField] private HumanoidModelJoint[] _joints;


        private bool initialized = false;

        //何秒ごとにポーズ指定するか
        private float _poseProcessDelay = 0.1f;

        private float _timer = 0.0f;


        // Start is called before the first frame update
        void Start()
        {
            _controller = GetComponent<PreMaid.RemoteController.PreMaidPoseController>();
            _controller.OpenSerialPort("COM7");


            Transform spineBone = target.GetBoneTransform(HumanBodyBones.Spine);
            //仮でspineにでも付けておこう
            if (target != null)
            {
                var joints = target.GetComponentsInChildren<HumanoidModelJoint>();

                foreach (PreMaidServo.ServoPosition item in Enum.GetValues(typeof(PreMaidServo.ServoPosition)))
                {
                    if (Array.FindIndex(joints, joint => joint.TargetServo == item) == -1)
                    {
                        var jointScript = spineBone.gameObject.AddComponent<HumanoidModelJoint>();
                        jointScript.TargetServo = item;
                    }
                }
            }

            _joints = target.GetComponentsInChildren<HumanoidModelJoint>();

            _controller.SetContinuousMode(true);
            Invoke(nameof(Apply), 2f);
        }


        void Apply()
        {
            var servos = _controller.Servos;

            foreach (var VARIABLE in _joints)
            {
                var servoValue = VARIABLE.CurrentServoValue();
                servos.Find(x => x.ServoPositionEnum == VARIABLE.TargetServo).SetServoValueSafeClamp((int) servoValue);
            }

            _controller.ApplyPose();
            if (initialized == false)
            {
                initialized = true;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                target.SetBool("TestMotion",true);
            }
            if (Input.GetKeyDown(KeyCode.B))
            {
                target.SetBool("TestMotion",false);
            }
        }

        void LateUpdate()
        {
            if (initialized == false)
            {
                return;
            }

            _timer += Time.deltaTime;
            if (_timer > _poseProcessDelay)
            {
                Apply();
                _timer -= _poseProcessDelay;
            }
        }
    }
}