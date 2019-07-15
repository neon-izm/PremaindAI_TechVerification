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

                    Transform leftFootEnd = null;
                    Transform rightFootEnd = null;

                    foreach (var joint in joints)
                    {
                        if (joint.ServoID.Equals("1C"))
                        {
                            // 左足関節が見つかった
                            if (joint.transform.childCount < 1)
                            {
                                leftFootEnd = joint.transform;
                            }
                            else
                            {
                                leftFootEnd = joint.transform.GetChild(0);
                            }
                        }
                        else if (joint.ServoID.Equals("1A"))
                        {
                            // 右足関節が見つかった
                            if (joint.transform.childCount < 1)
                            {
                                rightFootEnd = joint.transform;
                            }
                            else
                            {
                                rightFootEnd = joint.transform.GetChild(0);
                            }
                        }
                    }


                    if (leftFootEnd)
                    {
                        var obj = new GameObject("LeftFootTarget");
                        obj.transform.position = leftFootEnd.position;
                        //obj.transform.rotation = leftFootEnd.rotation;
                        obj.transform.rotation = robotRoot.rotation;
                        obj.transform.parent = floorTransform;

                        ikController.leftFootTarget = obj.transform;
                    }

                    if (rightFootEnd)
                    {
                        var obj = new GameObject("RightFootTarget");
                        obj.transform.position = rightFootEnd.position;
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
