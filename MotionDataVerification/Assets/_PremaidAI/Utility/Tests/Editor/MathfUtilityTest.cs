using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests
{
    public class MathfUtilityTest
    {
        // A Test behaves as an ordinary method
        [Test]
        public void GetEulerTest()
        {
            // Use the Assert class to test conditions
            float x, y, z;
            float min = -180f;
            float max = 180f;

            for (int i = 0; i < 20; i++)
            {
                x = Random.Range(min, max);
                y = Random.Range(min, max);
                z = Random.Range(min, max);
                Vector3 orgEuler = new Vector3(x, y, z);
                Debug.Log(orgEuler);

                foreach (RotationOrder order in System.Enum.GetValues(typeof(RotationOrder)))
                {
                    //if (order != RotationOrder.ZXY) continue;

                    Debug.Log(order);
                    Quaternion q = EulerToQuaternion(orgEuler, order);

                    Vector3 euler = QuaternionToEuler(q, order);

                    // ほぼ等しければ、同一にしておく
                    if (ApproximatelyEuler(orgEuler, euler))
                    {
                        orgEuler = euler;
                    }
                    else
                    {
                        Quaternion qNew = EulerToQuaternion(euler, order);
                        Debug.Log(q + " -> " + qNew + " " + Quaternion.Dot(q, qNew));

                        // 符号の入れ替わりはそろえる
                        if (q.w < 0f)
                        {
                            q.w = -q.w;
                            q.x = -q.x;
                            q.y = -q.y;
                            q.z = -q.z;
                        }
                        if (qNew.w < 0f)
                        {
                            qNew.w = -qNew.w;
                            qNew.x = -qNew.x;
                            qNew.y = -qNew.y;
                            qNew.z = -qNew.z;
                        }

                        if (q == qNew) orgEuler = euler;
                    }

                    Assert.AreEqual(orgEuler, euler, "Unity euler: " + q.eulerAngles.ToString() + "\n" + "Order: " + order);
                }
            }
        }

        /// <summary>
        /// オイラー角について、要素ごとにほぼ等しいか確認
        /// </summary>
        /// <param name="v1"></param>
        /// <param name="v2"></param>
        /// <returns>ほぼ等しいベクトルならばtrue</returns>
        static bool ApproximatelyEuler(Vector3 v1, Vector3 v2)
        {
            const float threshold = 0.1f;
            float dx = (Mathf.Abs(v1.x - v2.x) - 180f) % 360f + 180f;
            float dy = (Mathf.Abs(v1.y - v2.y) - 180f) % 360f + 180f;
            float dz = (Mathf.Abs(v1.z - v2.z) - 180f) % 360f + 180f;
            //float dx = Mathf.Abs(v1.x - v2.x);
            //float dy = Mathf.Abs(v1.y - v2.y);
            //float dz = Mathf.Abs(v1.z - v2.z);

            if (dx >= threshold) return false;
            if (dy >= threshold) return false;
            if (dz >= threshold) return false;
            return true;
        }

        ///// <summary>
        ///// クォータニオンについて、ほぼ等しいか確認
        ///// ※逆転でも等しいとします
        ///// </summary>
        ///// <param name="q1"></param>
        ///// <param name="q2"></param>
        ///// <returns></returns>
        //static bool ApproximatelyQuaternion(Quaternion q1, Quaternion q2)
        //{
        //    // 結果が遠い回転になるようならば、逆回転にする
        //    if (q1.w < 0f)
        //    {
        //        q1.w = -q1.w;
        //        q1.x = -q1.x;
        //        q1.y = -q1.y;
        //        q1.z = -q1.z;
        //    }

        //}

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

                case RotationOrder.ZXY:
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


        static public Vector3 GetEulerZXY(Quaternion q)
        {
            return q.eulerAngles;
        }

        public enum RotationOrder
        {
            XYZ,
            YXZ,
            ZXY,
            XZY,
            ZYX,
            YZX,
        };

        static float[] threeaxisrot(float r11, float r12, float r21, float r31, float r32)
        {
            float[] ret = new float[3];
            ret[0] = Mathf.Atan2(r31, r32) * Mathf.Rad2Deg;
            ret[1] = Mathf.Asin(r21) * Mathf.Rad2Deg;
            ret[2] = Mathf.Atan2(r11, r12) * Mathf.Rad2Deg;
            return ret;
        }

        // Reference https://forum.unity.com/threads/rotation-order.13469/
        static public Vector3 QuaternionToEuler(Quaternion q, RotationOrder order)
        {
            float[] ret;
            switch (order)
            {
                case RotationOrder.XYZ:
                    ret = threeaxisrot(2f * (q.x * q.y + q.w * q.z),
                        q.w * q.w + q.x * q.x - q.y * q.y - q.z * q.z,
                        -2f * (q.x * q.z - q.w * q.y),
                        2f * (q.y * q.z + q.w * q.x),
                        q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z);
                    return new Vector3(ret[0], ret[1], ret[2]);


                case RotationOrder.YXZ:
                    ret = threeaxisrot(-2f * (q.x * q.y - q.w * q.z),
                        q.w * q.w - q.x * q.x + q.y * q.y - q.z * q.z,
                        2f * (q.y * q.z + q.w * q.x),
                        -2f * (q.x * q.z - q.w * q.y),
                        q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z);
                    return new Vector3(ret[1], ret[0], ret[2]);


                case RotationOrder.ZXY:
                    ret = threeaxisrot(2f * (q.x * q.z + q.w * q.y),
                        q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z,
                        -2f * (q.y * q.z - q.w * q.x),
                        2f * (q.x * q.y + q.w * q.z),
                        q.w * q.w - q.x * q.x + q.y * q.y - q.z * q.z);
                    return new Vector3(ret[1], ret[2], ret[0]);

                case RotationOrder.XZY:
                    ret = threeaxisrot(-2f * (q.x * q.z - q.w * q.y),
                        q.w * q.w + q.x * q.x - q.y * q.y - q.z * q.z,
                        2f * (q.x * q.y + q.w * q.z),
                        -2f * (q.y * q.z - q.w * q.x),
                        q.w * q.w - q.x * q.x + q.y * q.y - q.z * q.z);
                    return new Vector3(ret[0], ret[2], ret[1]);

                case RotationOrder.ZYX:
                    ret = threeaxisrot(-2f * (q.y * q.z - q.w * q.x),
                        q.w * q.w - q.x * q.x - q.y * q.y + q.z * q.z,
                        2f * (q.x * q.z + q.w * q.y),
                        -2f * (q.x * q.y - q.w * q.z),
                        q.w * q.w + q.x * q.x - q.y * q.y - q.z * q.z);
                    return new Vector3(ret[2], ret[1], ret[0]);

                case RotationOrder.YZX:
                    ret = threeaxisrot(2f * (q.y * q.z + q.w * q.x),
                        q.w * q.w - q.x * q.x + q.y * q.y - q.z * q.z,
                        -2f * (q.x * q.y - q.w * q.z),
                        2f * (q.x * q.z + q.w * q.y),
                        q.w * q.w + q.x * q.x - q.y * q.y - q.z * q.z);
                    return new Vector3(ret[2], ret[0], ret[1]);
            }
            Debug.LogError("Wrong order : " + order);
            return Vector3.zero;
        }
    }
}
