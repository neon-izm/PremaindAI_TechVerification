using System;
using UnityEngine;

namespace PreMaid.RemoteController
{
    /// <summary>
    /// 普通のラジコンぽく動かすサンプルスクリプト
    /// </summary>
    [RequireComponent(typeof(PreMaidController))]
    public class PreMaidPoseController : MonoBehaviour
    {
        /// <summary>
        /// 連続送信モードフラグ、初期値はfalse
        /// </summary>
        private bool _continuousMode = false;


        //何秒ごとにポーズ指定するか
        private float _poseProcessDelay = 0.02f;

        private float _timer = 0.0f;


        /// <summary>
        /// 連続送信モードが切り替わったときに呼ばれる
        /// </summary>
        public Action<bool> OnContinuousModeChange;

        private PreMaidController _controller = null;


        // Start is called before the first frame update
        void Start()
        {
            _controller = GetComponent<PreMaidController>();
        }

        /// <summary>
        /// 連続送信モードを変更する
        /// 内部的に_continuousModeのboolを直接書き換えるのはこの関数経由にしてください
        /// </summary>
        /// <param name="newValue"></param>
        public void SetContinuousMode(bool newValue)
        {
            Debug.Log("連続送信モード切替 次の値は:" + newValue);
            _continuousMode = newValue;
            OnContinuousModeChange?.Invoke(_continuousMode);

            if (newValue)
            {
                _timer = 0;
            }
        }


        // Update is called once per frame
        void Update()
        {
            if (_continuousMode == true)
            {
                _timer += Time.deltaTime;
                if (_timer > _poseProcessDelay)
                {
                    _controller.ApplyPose();
                    _timer -= _poseProcessDelay;
                }
            }
        }
    }
}