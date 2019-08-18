using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PreMaid.IKSample
{
    /// <summary>
    /// 両脚IKの目標を自動的に床に設定する例です
    /// </summary>
    public class PreMaidIKFloor : MonoBehaviour
    {
        [Tooltip("モデルを指定してください")]
        public Transform robotRoot;

        private Transform floorTransform;

        // Start is called before the first frame update
        void Awake()
        {
            // 床にこのスクリプトをアタッチしているものとする
            floorTransform = transform;

            if (robotRoot) {
                PreMaidIKController ikController = robotRoot.GetComponent<PreMaidIKController>();

                if (ikController)
                {
                    ModelJoint[] joints = robotRoot.GetComponentsInChildren<ModelJoint>();

                    Transform leftFoot = null;
                    Transform rightFoot = null;

                    foreach (var joint in joints)
                    {
                        if (joint.ServoID.Equals("1C"))
                        {
                            // 左足関節が見つかった
                            leftFoot = joint.transform;
                        }
                        else if (joint.ServoID.Equals("1A"))
                        {
                            rightFoot = joint.transform;
                        }
                    }


                    if (leftFoot)
                    {
                        var pos = leftFoot.position;
                        pos.y = floorTransform.position.y;

                        var obj = new GameObject("LeftFootTarget");
                        obj.transform.position = pos;
                        //obj.transform.rotation = leftFootEnd.rotation;
                        obj.transform.rotation = robotRoot.rotation;
                        obj.transform.parent = floorTransform;

                        ikController.leftFootTarget = obj.transform;
                    }

                    if (rightFoot)
                    {
                        var pos = rightFoot.position;
                        pos.y = floorTransform.position.y;

                        var obj = new GameObject("RightFootTarget");
                        obj.transform.position = pos;
                        //obj.transform.rotation = rightFootEnd.rotation;
                        obj.transform.rotation = robotRoot.rotation;
                        obj.transform.parent = floorTransform;

                        ikController.rightFootTarget = obj.transform;
                    }

                }
            }
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
