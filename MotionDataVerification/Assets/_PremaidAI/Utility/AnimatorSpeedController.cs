using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PreMaid
{
    /// <summary>
    /// Animatorを含むオブジェクトにアタッチすると、再生速度をインスペクタで変更できます
    /// </summary>
    public class AnimatorSpeedController : MonoBehaviour
    {
        /// <summary>
        /// アニメーション速度（倍率）
        /// </summary>
        [Range(0f, 4f), Tooltip("アニメーション速度（倍率）")]
        public float animationSpeed = 1f;

        private float lastAnimationSpeed;
        private Animator animator;


        // 再生開始時に設定
        void Start()
        {
            animator = GetComponent<Animator>();
            ApplyAnimationSpeed();
        }

        // 再生中は値の変化を監視し、変化があれば設定
        private void Update()
        {
            if (animationSpeed != lastAnimationSpeed)
            {
                ApplyAnimationSpeed();
            }
        }

        // 実際にAnimatorに設定
        private void ApplyAnimationSpeed()
        {
            if (animator)
            {
                animator.speed = animationSpeed;
            }
            lastAnimationSpeed = animationSpeed;
        }
    }
}
