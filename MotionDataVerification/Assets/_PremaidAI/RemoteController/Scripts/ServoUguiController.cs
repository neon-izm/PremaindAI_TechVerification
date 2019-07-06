using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PreMaid.RemoteController
{
    /// <summary>
    /// スライダーで動かせるようにするやつ
    /// </summary>
    public class ServoUguiController : MonoBehaviour
    {
        [SerializeField] private GameObject _servoPrefab = null;

        [SerializeField] private Transform _parent;
        
        List<ServoUguiCell> _cells= new List<ServoUguiCell>();

        public Action OnChangeValue;
        
        public void Initialize(List<PreMaidServo> input)
        {
            foreach (var VARIABLE in input)
            {
                var cell = GameObject.Instantiate(_servoPrefab, _parent);
                var cellScript = cell.GetComponent<ServoUguiCell>();
                cellScript.Initialize(VARIABLE.GetServoName(), VARIABLE.MinServoValue, VARIABLE.MaxServoValue,
                    VARIABLE.GetServoValue());
                cellScript.slider.onValueChanged.AddListener(Call);
                _cells.Add(cellScript);
            }
        }

        public List<float> GetCurrenSliderValues()
        {
            List<float> ret = new List<float>();
            foreach (var VARIABLE in _cells)
            {
                ret.Add(VARIABLE.slider.value);
            }

            return ret;
        }
        
        private void Call(float arg0)
        {
            OnChangeValue?.Invoke();
        }
    }
}