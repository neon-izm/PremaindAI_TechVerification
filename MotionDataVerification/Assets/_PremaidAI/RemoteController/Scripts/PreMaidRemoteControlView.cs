using System;
using System.Collections.Generic;
using System.IO.Ports;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PreMaid.RemoteController
{
    /// <summary>
    /// GUIボタン系からの操作をControllerに伝える
    /// 逆に言うと、コントローラはGUIを触らないようにしたい
    /// </summary>
    [RequireComponent(typeof(PreMaidController))]
    public class PreMaidRemoteControlView : MonoBehaviour
    {
        private PreMaidController _preMaidController = null;

        private PreMaidPoseController _preMaidPoseController = null;

        [SerializeField] private TMP_Dropdown dropdown = null;

        [SerializeField] private ServoUguiController uguiController = null;

        [SerializeField] private Toggle continuousToggle;


        private void OnEnable()
        {
            if (_preMaidController == null)
            {
                _preMaidController = GetComponent<PreMaidController>();
                _preMaidPoseController = GetComponent<PreMaidPoseController>();
            }
        }

        // Start is called before the first frame update
        void Awake()
        {
            if (_preMaidController == null)
            {
                _preMaidController = GetComponent<PreMaidController>();
                _preMaidPoseController = GetComponent<PreMaidPoseController>();
            }
            //コントローラの初期化の後に、こちらのGUIを初期化する、という初期化順制御です
            _preMaidController.OnInitializeServoDefines+= OnInitializeServoDefines;
           
        }

        /// <summary>
        /// 実質的な初期化関数
        /// </summary>
        private void OnInitializeServoDefines()
        {
            uguiController.Initialize(_preMaidController.Servos);
            uguiController.OnChangeValue += OnUguiSliderValueChange;

            if (dropdown == null)
            {
                Debug.LogError("シリアルポートを選択するDropDownが指定されていません");
                return;
            }


            List<TMP_Dropdown.OptionData> serialPortNamesList = new List<TMP_Dropdown.OptionData>();

            var portNames = SerialPort.GetPortNames();


            foreach (var VARIABLE in portNames)
            {
                TMP_Dropdown.OptionData optionData = new TMP_Dropdown.OptionData(VARIABLE);
                serialPortNamesList.Add(optionData);

                Debug.Log(VARIABLE);
            }

            dropdown.ClearOptions();
            dropdown.AddOptions(serialPortNamesList);
            
            _preMaidPoseController.OnContinuousModeChange+= OnContinuousModeChange;
            
            continuousToggle.onValueChanged.AddListener(SetContinuousModeFromGui);
        }


        /// <summary>
        /// GUIからトグルボタンで連続送信モードを切り替えたときに呼ぶ
        /// </summary>
        /// <param name="newValue"></param>
        public void SetContinuousModeFromGui(bool newValue)
        {
            _preMaidPoseController.SetContinuousMode(newValue);
        }

        /// <summary>
        /// 連続送信モードの切り替えを検知したアクション
        /// 要するに、システム側から連続送信モードを切ったときにGUIのトグルボタンに反映したい、という処理
        /// </summary>
        /// <param name="obj"></param>
        private void OnContinuousModeChange(bool obj)
        {
            continuousToggle.isOn = obj;
        }


        /// <summary>
        /// シリアルポートを開く、ボタンを押した時の処理
        /// </summary>
        public void OnClickSerialPortOpenButton()
        {
            var willOpenSerialPortName = dropdown.options[dropdown.value].text;
            Debug.Log(willOpenSerialPortName + "を開きます");
            _preMaidController.OpenSerialPort(willOpenSerialPortName);
        }

        private void OnUguiSliderValueChange()
        {
            //Debug.Log("値の変更");
            //sliderの各値を取得して
            var latestValues = uguiController.GetCurrenSliderValues();

            //サーボコントローラにスライダの値を反映させる
            //いつプリメイドAIの実機に転送するかは、このGUIは関知しない
            for (int i = 0; i < _preMaidController.Servos.Count; i++)
            {
                _preMaidController.Servos[i].SetServoValueSafeClamp((int) latestValues[i]);
            }
        }

        // Update is called once per frame
        void Update()
        {
        }
    }
}