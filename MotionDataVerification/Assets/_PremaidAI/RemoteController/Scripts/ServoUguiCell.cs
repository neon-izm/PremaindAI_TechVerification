using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PreMaid.RemoteController
{

    public class ServoUguiCell : MonoBehaviour
    {
        [SerializeField] public Slider slider;
        [SerializeField] private Text _servoName;
        [SerializeField] private Text _servoValueLabel;

        public float ServoValue()
        {
            return slider.value;
        }
        
        public void Initialize(string servoName, int minValue, int maxValue, int currentValue )
        {
            // スライダーの現在値が表示されるようにする。下の currentValue 代入時も反映されるよう、最初に準備
            slider.onValueChanged.AddListener(OnSliderValueChanged);

            _servoName.text = servoName;
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = currentValue;
        }

        /// <summary>
        /// スライダーの値を画面にも表示させる
        /// </summary>
        /// <param name="value"></param>
        private void OnSliderValueChanged(float value)
        {
            _servoValueLabel.text = value.ToString("0");
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