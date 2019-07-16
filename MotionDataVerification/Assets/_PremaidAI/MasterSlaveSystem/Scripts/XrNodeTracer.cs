using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace PreMaid.MasterSlaveSystem
{
    /// <summary>
    /// HMDを使ってマスタースレーブシステムを行う為に頭と両手をトラッキングする
    /// </summary>
    public class XrNodeTracer : MonoBehaviour
    {
        [SerializeField] private Transform headTarget;
        [SerializeField] private Transform rightHandTarget;
        [SerializeField] private Transform leftHandTarget;

        // Start is called before the first frame update
        void Start()
        {

            Debug.Log("<color=orange>[MasterSlaveSystem]XRDevice.model = " + XRDevice.model + "</color>");
            if (string.IsNullOrEmpty(XRDevice.model) || XRDevice.isPresent == false)
            {
                Debug.Log("<color=orange>[MasterSlaveSystem]HMDが見つからなかった、もしくはXR Settingsが無効です。IKノードを無効化します </color>");
                enabled = false;
            }
            XRDevice.SetTrackingSpaceType(TrackingSpaceType.RoomScale);
            
        }


        void Recenter()
        {
            var headPosition = InputTracking.GetLocalPosition(XRNode.Head);
            var currentPosition = transform.position;
            currentPosition.x = -headPosition.x*0.5f;
            currentPosition.z = -headPosition.z*0.5f;
            transform.position = currentPosition;
            var headRot= InputTracking.GetLocalRotation(XRNode.Head).eulerAngles;

            var currentRot = transform.rotation.eulerAngles;
            currentRot.y = -headRot.y;
            transform.rotation= Quaternion.Euler(currentRot);
        }
        // Update is called once per frame
        void Update()
        {

            //位置の初期化
            if (Input.GetKeyDown(KeyCode.T))
            {
                Invoke(nameof(Recenter),3f);
            }
            headTarget.localPosition = InputTracking.GetLocalPosition(XRNode.Head);
            headTarget.localRotation = InputTracking.GetLocalRotation(XRNode.Head);

            rightHandTarget.localPosition = InputTracking.GetLocalPosition(XRNode.RightHand);
            rightHandTarget.localRotation = InputTracking.GetLocalRotation(XRNode.RightHand);
            
            leftHandTarget.localPosition = InputTracking.GetLocalPosition(XRNode.LeftHand);
            leftHandTarget.localRotation = InputTracking.GetLocalRotation(XRNode.LeftHand);
            
            
        }
    }
}