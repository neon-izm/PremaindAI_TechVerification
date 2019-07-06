using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PreMaid.RemoteController
{
    /// <summary>
    /// 個々のサーボの位置と角度を指定できるやつ
    /// </summary>
    [System.Serializable]
    public class PreMaidServo
    {
        private const int MaxServoValue = 11500;
        private const int MinServoValue = 3500;

        //for debug purpose
        [SerializeField]
        private string servoName;
        
        [SerializeField] private ServoPosition _servoPosition = ServoPosition.RightShoulderPitch;

        [SerializeField] private int _servoValue = 7500;
        private int _defaultServoValue = 7500;

        string standPose=   "50 18 00 06 02 4C 1D 03 4C 1D 04 " + " 4C 1D "+
        "05 4C 1D 06 4C 1D 07 4C 1D 08 4C 1D 09 1C 25 0A 4C 1D 0B 7C 15 0C 4C 1D 0D 4C 1D 0E 4C 1D 0F 4C 1D 10 4C 1D 11 4C 1D 12 4C 1D 13 4C 1D 14 4C 1D 15 4C 1D 16 4C 1D 17 4C 1D 18 4C 1D 1A 4C 1D 1C 4C 1D D9";

        
        /// <summary>
        /// サーボの取り付け位置
        /// </summary>
        public enum ServoPosition
        {
            RightShoulderPitch = 0x02, //肩ピッチR
            HeadPitch = 0x03, //頭ピッチ
            LeftShoulderPitch = 0x04, //肩ピッチL
            HeadYaw = 0x05, //頭ヨー
            RightHipYaw = 0x06, //ヒップヨーR
            HeadRoll = 0x07 /*萌え軸*/,
            LeftHipYaw = 0x08, //ヒップヨーL
            RightShoulderRoll = 0x09, //肩ロールR
            RightHipRoll = 0x0A, //ヒップロールR
            LeftShoulderRoll = 0x0B, //肩ロールL
            LeftHipRoll = 0x0C, //ヒップロールL
            RightUpperArmYaw = 0x0D, //上腕ヨーR
            RightUpperLegPitch = 0x0E, //腿ピッチR
            LeftUpperArmYaw = 0x0F, //上腕ヨーL
            LeftUpperLegPitch = 0x10, //腿ピッチL
            RightLowerArmPitch = 0x11, //肘ピッチR
            RightLowerLegPitch = 0x12, //膝ピッチR
            LeftLowerArmPitch = 0x13, //肘ピッチL
            LeftLowerLegPitch = 0x14, //肘ピッチL
            RightHandYaw = 0x15, //手首ヨーR
            RightFootPitch = 0x16, //足首ピッチR
            LeftHandYaw = 0x17, //手首ヨーL
            LeftFootPitch = 0x18, //足首ピッチL
            RightFootRoll = 0x1A, //足首ロールR
            LeftFootRoll = 0x1C, //足首ロールL
        }

        //初期値指定なしのコンストラクタは禁止します
        private PreMaidServo()
        {
        }

        public PreMaidServo(ServoPosition servoPosition)
        {
            _defaultServoValue = 7500;
            _servoPosition = servoPosition;
            servoName = servoPosition.ToString();
            
            switch (_servoPosition)
            {
                case ServoPosition.RightShoulderPitch:
                    break;
                case ServoPosition.HeadPitch:
                    break;
                case ServoPosition.LeftShoulderPitch:
                    break;
                case ServoPosition.HeadYaw:
                    break;
                case ServoPosition.RightHipYaw:
                    break;
                case ServoPosition.HeadRoll:
                    break;
                case ServoPosition.LeftHipYaw:
                    break;
                case ServoPosition.RightShoulderRoll:
                    //9500
                    SetServoValue("1C 25");
                    break;
                case ServoPosition.RightHipRoll:
                    break;
                case ServoPosition.LeftShoulderRoll:
                    //5500
                    SetServoValue("7C 15");
                    break;
                case ServoPosition.LeftHipRoll:
                    break;
                case ServoPosition.RightUpperArmYaw:
                    break;
                case ServoPosition.RightUpperLegPitch:
                    break;
                case ServoPosition.LeftUpperArmYaw:
                    break;
                case ServoPosition.LeftUpperLegPitch:
                    break;
                case ServoPosition.RightLowerArmPitch:
                    break;
                case ServoPosition.RightLowerLegPitch:
                    break;
                case ServoPosition.LeftLowerArmPitch:
                    break;
                case ServoPosition.LeftLowerLegPitch:
                    break;
                case ServoPosition.RightHandYaw:
                    break;
                case ServoPosition.RightFootPitch:
                    break;
                case ServoPosition.LeftHandYaw:
                    break;
                case ServoPosition.LeftFootPitch:
                    break;
                case ServoPosition.RightFootRoll:
                    break;
                case ServoPosition.LeftFootRoll:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(servoPosition), servoPosition, null);
            }
        }

        /// <summary>
        /// "1C" とか "0F"とかのサーボID文字列を返す
        /// </summary>
        /// <returns></returns>
        public string GetServoIdString()
        {
            return ((int) _servoPosition).ToString("X2");
        }

        /// <summary>
        /// 28とか2とか6とかのサーボIDのint値を返す
        /// </summary>
        /// <returns></returns>
        public int GetServoId()
        {
            return (int) _servoPosition;
        }

        /// <summary>
        /// 現在のサーボ値を返す 7500とか
        /// </summary>
        /// <returns></returns>
        public int GetServoValue()
        {
            return _servoValue;
        }

        /// <summary>
        /// 7500だったら"4C 1D"が返ってくる
        /// </summary>
        /// <returns></returns>
        public string GetServoValueString()
        {
            var tmp = ConvertEndian(_servoValue.ToString("X1"));

            return $"{tmp[0]}{tmp[1]} {tmp[2]}{tmp[3]}";
        }


        /// <summary>
        /// プリメイドのシリアル経由で送る時の値を返す
        /// 0E 4C 1D(0E番のサーボを7500に)みたいな文字列が出力される
        /// </summary>
        /// <returns></returns>
        public string GetServoIdAndValueString()
        {
            return $"{GetServoIdString()} {GetServoValueString()}";
        }

        /// <summary>
        /// 外部からサーボの値を入れる3500から11500の間の値
        /// </summary>
        /// <param name="newValue"></param>
        public void SetServoValue(int newValue)
        {
            if (newValue > MaxServoValue || newValue < MinServoValue)
            {
                throw new ArgumentOutOfRangeException($"サーボの値は{MinServoValue}から{MaxServoValue}の間だけです{newValue}はダメです");
                return;
            }

            _servoValue = newValue;
        }

        /// <summary>
        /// "4C 1D"などの外部からサーボの値を入れる
        /// ちなみに4C 1Dが7500です
        /// </summary>
        /// <param name="newValue">"4C 1D"</param>
        public void SetServoValue(string spaceSplitedByteString)
        {
            var aaa = ConvertEndian(PreMaidUtility.RemoveWhitespace(spaceSplitedByteString));
            int intValue = int.Parse(aaa, System.Globalization.NumberStyles.HexNumber);
            Debug.Log($"{spaceSplitedByteString} は {intValue} ");

            SetServoValue(intValue);
        }


    
        //サーボ種類一覧をダンプします
        public static void AllServoPositionDump()
        {
            foreach (var item in Enum.GetValues(typeof(PreMaidServo.ServoPosition)))
            {
                string name = Enum.GetName(typeof(PreMaidServo.ServoPosition), item);

                Debug.Log($"{name} val: {(int) item}  raw val:{((int) item).ToString("X2")}");
            }
        }

        /// <summary>
        /// 4C1Dを入れたら1D4Cが返ってくる
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static string ConvertEndian(string data)
        {
            int sValueAsInt = int.Parse(data, System.Globalization.NumberStyles.HexNumber);
            byte[] bytes = BitConverter.GetBytes(sValueAsInt);
            string retval = "";
            foreach (byte b in bytes)
                retval += b.ToString("X2");
            return retval.Substring(0, 4);
        }
    }
}