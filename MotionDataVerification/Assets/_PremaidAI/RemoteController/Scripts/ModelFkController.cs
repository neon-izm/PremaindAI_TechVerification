using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PreMaid.RemoteController
{
    public class ModelFkController : MonoBehaviour
    {
        [Tooltip("ロボットモデルです。それ自体にアタッチされていれば未指定で構いません")]
        public Transform premaidRoot;

        [Tooltip("未指定の場合は自動で検索します")]
        public PreMaidController controller;

        private ModelJoint[] _joints;

        // Start is called before the first frame update
        void Start()
        {
            if (!premaidRoot)
            {
                // 未指定ならモデルのルートにこのスクリプトがアタッチされているものとする
                premaidRoot = transform;
            }

            if (premaidRoot != null)
            {
                _joints = premaidRoot.GetComponentsInChildren<ModelJoint>();
            }

            // PreMaidControllerが未指定ならシーンに存在するものを検索
            if (!controller)
            {
                controller = GameObject.FindObjectOfType<PreMaidController>();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (controller)
            {
                foreach (var servo in controller.Servos)
                {
                    string servoId = servo.GetServoIdString();
                    ModelJoint joint = GetJointById(servoId);

                    if (joint != null)
                    {
                        joint.SetServoPosition(servo.GetServoValue());
                    }
                }
            }
        }

        /// <summary>
        /// 指定IDのModelJointを取得
        /// </summary>
        /// <param name="servoId"></param>
        /// <returns></returns>
        ModelJoint GetJointById(string servoId)
        {
            foreach (var joint in _joints)
            {
                if (joint.ServoID.Equals(servoId)) return joint;
            }
            return null;
        }
    }
}
