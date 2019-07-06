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

        public float ServoValue()
        {
            return slider.value;
        }
        
        public void Initialize(string servoName, int minValue, int maxValue, int currentValue )
        {
            _servoName.text = servoName;
            slider.minValue = minValue;
            slider.maxValue = maxValue;
            slider.value = currentValue;
            
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