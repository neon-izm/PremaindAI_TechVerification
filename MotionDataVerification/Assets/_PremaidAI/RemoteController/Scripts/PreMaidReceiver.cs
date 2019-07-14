using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PreMaid.RemoteController
{
    /// <summary>
    /// プリメイドAIからの受信命令を解析するスクリプト
    /// </summary>
    [RequireComponent(typeof(PreMaidPoseController))]
    public class PreMaidReceiver : MonoBehaviour
    {
        private PreMaidPoseController _preMaidPoseController = null;
        // Start is called before the first frame update
        void Start()
        {
            _preMaidPoseController = GetComponent<PreMaidPoseController>();
            _preMaidPoseController.OnReceivedFromPreMaidAI+= OnReceivedFromPreMaidAi;
        }

        /// <summary>
        /// 受信時の処理
        /// </summary>
        /// <param name="receivedString"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void OnReceivedFromPreMaidAi(string receivedString)
        {
            //4文字以下なら不正
            if (receivedString.Length < 4)
            {
                return;
            }
            //3-4文字目が命令種類
            string orderKind = receivedString.Substring(2, 2);
            //Debug.Log("orderKind:"+ orderKind);
            switch (orderKind)
            {
                //バッテリー残量
                case "01":
                    if (receivedString.Length >= 10)
                    {
                        int rawValtageValue =
                            PreMaidUtility.HexStringToInt(PreMaidUtility.ConvertEndian(receivedString.Substring(6, 4)));
                        Debug.Log($"バッテリー残量{rawValtageValue} で電圧は{rawValtageValue / 216f} V");

                        if (rawValtageValue / 216.0f < 9f)
                        {
                            Debug.LogError("バッテリー残量が9V以下です！！！！");
                        }
                    }

                    break;
                default:
                    Debug.Log(receivedString);
                    break;
            }
            
        }

        
    }
}