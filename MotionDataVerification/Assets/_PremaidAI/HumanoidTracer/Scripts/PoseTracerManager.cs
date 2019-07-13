using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using PreMaid.HumanoidTracer;
using PreMaid.RemoteController;
using TMPro;
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

        [SerializeField] private TMPro.TMP_Dropdown _serialPortsDropdown = null;


        private bool initialized = false;

        //何秒ごとにポーズ指定するか
        private float _poseProcessDelay = 0.1f;

        private float _timer = 0.0f;


        // Start is called before the first frame update
        void Start()
        {
            _controller = GetComponent<PreMaid.RemoteController.PreMaidPoseController>();
            List<TMP_Dropdown.OptionData> serialPortNamesList = new List<TMP_Dropdown.OptionData>();

            var portNames = SerialPort.GetPortNames();


            foreach (var VARIABLE in portNames)
            {
                TMP_Dropdown.OptionData optionData = new TMP_Dropdown.OptionData(VARIABLE);
                serialPortNamesList.Add(optionData);

                Debug.Log(VARIABLE);
            }

            _serialPortsDropdown.ClearOptions();
            _serialPortsDropdown.AddOptions(serialPortNamesList);


            //対象のAnimatorにBoneにHumanoidModelJoint.csのアタッチ漏れがあるかもしれない
            //なので、一旦全部検索して、見つからなかったサーボ情報はspineに全部動的にアタッチする
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
        }


        /// <summary>
        /// UGUIのOpenボタンを押したときの処理
        /// </summary>
        public void Open()
        {
            var willOpenSerialPortName = _serialPortsDropdown.options[_serialPortsDropdown.value].text;
            Debug.Log(willOpenSerialPortName + "を開きます");
            var openSuccess = _controller.OpenSerialPort(willOpenSerialPortName);
            if (openSuccess)
            {
                _controller.SetContinuousMode(true);
                Invoke(nameof(Apply), 2f);
            }
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
                target.SetBool("TestMotion", true);
            }

            if (Input.GetKeyDown(KeyCode.B))
            {
                target.SetBool("TestMotion", false);
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