using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PreMaid
{
    public class MathfUtility
    {
        /// <summary>
        /// 回転順序
        /// </summary>
        public enum RotationOrder
        {
            XYZ,
            YXZ,
            ZXY,     // この順序だとUnity標準と同じ
            XZY,
            ZYX,
            YZX,
        };

        /// <summary>
        /// 指定した順序で3軸を回転させたクォータニオンを返します
        /// </summary>
        /// <param name="euler"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        static public Quaternion EulerToQuaternion(Vector3 euler, RotationOrder order)
        {
            Quaternion qX = Quaternion.AngleAxis(euler.x, Vector3.right);
            Quaternion qY = Quaternion.AngleAxis(euler.y, Vector3.up);
            Quaternion qZ = Quaternion.AngleAxis(euler.z, Vector3.forward);

            // 「*」演算子は (後の回転) * (先の回転) となる
            switch (order)
            {
                case RotationOrder.ZYX:
                    return qX * qY * qZ;

                case RotationOrder.YZX:
                    return qX * qZ * qY;

                case RotationOrder.ZXY:     // Unity標準はこれ
                    return qY * qX * qZ;

                case RotationOrder.XZY:
                    return qY * qZ * qX;

                case RotationOrder.YXZ:
                    return qZ * qX * qY;

                case RotationOrder.XYZ:
                    return qZ * qY * qX;
            }
            Debug.LogError("Wrong order : " + order);
            return Quaternion.identity;
        }

        /// <summary>
        /// 指定クォータニオンは指定した順序で各軸周りに回転が行われたものであるとして、そのオイラー角を返します
        /// </summary>
        /// <param name="q"></param>
        /// <param name="order"></param>
        /// <seealso href="https://forum.unity.com/threads/rotation-order.13469/">参考</seealso>
        /// <returns></returns>
        static public Vector3 QuaternionToEuler(Quaternion q, RotationOrder order)
        {
            float[] ret;
            switch (order)
            {
                case RotationOrder.XYZ:
                    ret = CalcAngles(2f * (q.x * q.y + q.w * q.z),
                        q.w * q.w + q.x * q.x - q.y * q.y - q.z * q.z,
                        -2f * (q.x * q.z - q.w * q.y),
                        2f * (q.y * q.z + q.w * q.x),
                        q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z);
                    return new Vector3(ret[0], ret[1], ret[2]);


                case RotationOrder.YXZ:
                    ret = CalcAngles(-2f * (q.x * q.y - q.w * q.z),
                        q.w * q.w - q.x * q.x + q.y * q.y - q.z * q.z,
                        2f * (q.y * q.z + q.w * q.x),
                        -2f * (q.x * q.z - q.w * q.y),
                        q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z);
                    return new Vector3(ret[1], ret[0], ret[2]);


                case RotationOrder.ZXY:
                    ret = CalcAngles(2f * (q.x * q.z + q.w * q.y),
                        q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z,
                        -2f * (q.y * q.z - q.w * q.x),
                        2f * (q.x * q.y + q.w * q.z),
                        q.w * q.w - q.x * q.x + q.y * q.y - q.z * q.z);
                    return new Vector3(ret[1], ret[2], ret[0]);

                case RotationOrder.XZY:
                    ret = CalcAngles(-2f * (q.x * q.z - q.w * q.y),
                        q.w * q.w + q.x * q.x - q.y * q.y - q.z * q.z,
                        2f * (q.x * q.y + q.w * q.z),
                        -2f * (q.y * q.z - q.w * q.x),
                        q.w * q.w - q.x * q.x + q.y * q.y - q.z * q.z);
                    return new Vector3(ret[0], ret[2], ret[1]);

                case RotationOrder.ZYX:
                    ret = CalcAngles(-2f * (q.y * q.z - q.w * q.x),
                        q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z,
                        2f * (q.x * q.z + q.w * q.y),
                        -2f * (q.x * q.y - q.w * q.z),
                        q.w * q.w + q.x * q.x - q.y * q.y - q.z * q.z);
                    return new Vector3(ret[2], ret[1], ret[0]);

                case RotationOrder.YZX:
                    ret = CalcAngles(2f * (q.y * q.z + q.w * q.x),
                        q.w * q.w - q.x * q.x + q.y * q.y - q.z * q.z,
                        -2f * (q.x * q.y - q.w * q.z),
                        2f * (q.x * q.z + q.w * q.y),
                        q.w * q.w + q.x * q.x - q.y * q.y - q.z * q.z);
                    return new Vector3(ret[2], ret[0], ret[1]);
            }
            Debug.LogError("Wrong order : " + order);
            return Vector3.zero;
        }

        static float[] CalcAngles(float r11, float r12, float r21, float r31, float r32)
        {
            float[] ret = new float[3];
            ret[0] = Mathf.Atan2(r31, r32) * Mathf.Rad2Deg;
            ret[1] = Mathf.Asin(r21) * Mathf.Rad2Deg;
            ret[2] = Mathf.Atan2(r11, r12) * Mathf.Rad2Deg;
            return ret;
        }
    }
}
