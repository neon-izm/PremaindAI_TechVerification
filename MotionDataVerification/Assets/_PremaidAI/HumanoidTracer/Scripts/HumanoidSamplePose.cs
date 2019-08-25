using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PreMaid.HumanoidTracer
{
    public class HumanoidSamplePose : MonoBehaviour
    {
        Animator targetAnimator;

        // Start is called before the first frame update
        void Start()
        {
            if (!targetAnimator) targetAnimator = GetComponent<Animator>();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                targetAnimator.SetTrigger("TestMotion");
            }
        }
    }
}
