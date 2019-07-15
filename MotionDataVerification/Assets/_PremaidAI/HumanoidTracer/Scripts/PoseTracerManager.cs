using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using PreMaid.RemoteController;
using TMPro;
using UnityEngine;

namespace PreMaid.HumanoidTracer
{
    /// <summary>
    /// MecanimというかHumanoidのアバターからモーションを実機に反映するスクリプト
    /// 仮説として、全サーボ情報を送ると結構応答性が悪い（10FPS程度）ので、
    /// キーフレームを1秒ごと+差分フレームを送る
    /// これで1個や2個しかサーボが動かないモーションだと応答性がよくなる、はず
    /// </summary>
    [RequireComponent(typeof(PreMaid.RemoteController.PreMaidController))]
    [DefaultExecutionOrder(11001)] //after VRIK calclate
    public class PoseTracerManager : MonoBehaviour
    {
        private PreMaid.RemoteController.PreMaidController _controller;

        [SerializeField] private Animator target;

        [SerializeField] private HumanoidModelJoint[] _joints;

        [SerializeField] private TMPro.TMP_Dropdown _serialPortsDropdown = null;

        private bool _initialized = false;

        //差分フレームタイマー
        private float coolTime = 0f;

        //キーフレームは1秒ごとに打つ
        private float keyFrameTimer = 0f;

        List<PreMaidServo> latestServos = new List<PreMaidServo>();


        [SerializeField] private int currentFPS = 0;
        
        // Start is called before the first frame update
        void Start()
        {
            _controller = GetComponent<PreMaid.RemoteController.PreMaidController>();
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
                StartCoroutine(PreMaidParamInitilize());
            }
        }


        IEnumerator PreMaidParamInitilize()
        {
            yield return new WaitForSeconds(1f);
            //ここらへんでサーボパラメータ入れたりする
            Invoke(nameof(ApplyMecanimPoseWithDiff), 3f);
        }

        /// <summary>
        /// 現在のAnimatorについているサーボの値を参照しながら差分だけ送る
        /// </summary>
        void ApplyMecanimPoseWithDiff()
        {
            var servos = _controller.Servos;

            List<PreMaidServo> orders = new List<PreMaidServo>();

            foreach (var VARIABLE in _joints)
            {
                var mecanimServoValue = VARIABLE.CurrentServoValue();

                var servo = servos.Find(x => x.ServoPositionEnum == VARIABLE.TargetServo);

                int premaidServoValue = servo.GetServoValue();

                //20以上サーボの値が変わってたら命令とする
                //50とかでもいいかも
                if (Mathf.Abs(mecanimServoValue - premaidServoValue) > 40)
                {
                    servo.SetServoValueSafeClamp((int) mecanimServoValue);
                    PreMaidServo tmp = new PreMaidServo(VARIABLE.TargetServo);
                    tmp.SetServoValueSafeClamp((int) mecanimServoValue);
                    orders.Add(tmp);
                }
            }

            //ここでordersに差分だけ送れます
            coolTime = orders.Count * 0.005f; //25個あると0.08くらい、1個だと0.01くらいのクールタイムが良い

            if (orders.Count > 0)
            {
                currentFPS++;
                //Debug.Log("Servo Num:" + orders.Count);
                _controller.ApplyPoseFromServos(orders, Mathf.Clamp(orders.Count*2,10,40));
            }

            if (_initialized == false)
            {
                _initialized = true;
            }
        }

        /// <summary>
        /// 現在のAnimatorについているサーボの値を参照しながら全て送る
        /// </summary>
        void ApplyMecanimPoseAll()
        {
            var servos = _controller.Servos;

            List<PreMaidServo> orders = new List<PreMaidServo>();

            foreach (var VARIABLE in _joints)
            {
                var mecanimServoValue = VARIABLE.CurrentServoValue();

                var servo = servos.Find(x => x.ServoPositionEnum == VARIABLE.TargetServo);

                int premaidServoValue = servo.GetServoValue();


                servo.SetServoValueSafeClamp((int) mecanimServoValue);
                PreMaidServo tmp = new PreMaidServo(VARIABLE.TargetServo);
                tmp.SetServoValueSafeClamp((int) mecanimServoValue);
                orders.Add(tmp);
            }

            //ここでordersに差分だけ送れます
            coolTime = 0.09f; //25個あると0.08くらい、1個だと0.01くらいのクールタイムが良い

            keyFrameTimer = 1f;
            Debug.Log("全フレーム転送 :" + orders.Count+" FPS:"+currentFPS);
            _controller.ApplyPoseFromServos(orders, 40);

            currentFPS = 0;

            if (_initialized == false)
            {
                _initialized = true;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                target.SetTrigger("TestMotion");
            }
        }

        void LateUpdate()
        {
            if (_initialized == false)
            {
                return;
            }

            coolTime -= Time.deltaTime;
            keyFrameTimer -= Time.deltaTime;
            if (coolTime <= 0)
            {
                
                if (keyFrameTimer <= 0)
                {
                    ApplyMecanimPoseAll();
                }
                else
                {
                    ApplyMecanimPoseWithDiff();
                }
            }
        }
    }
}