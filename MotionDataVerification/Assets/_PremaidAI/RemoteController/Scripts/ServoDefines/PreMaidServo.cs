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
        public readonly int MaxServoValue = 11500;
        public readonly int MinServoValue = 3500;

        //for debug purpose
        [SerializeField] private string servoName;

        [SerializeField] private ServoPosition _servoPosition = ServoPosition.RightShoulderPitch;

        public ServoPosition ServoPositionEnum => _servoPosition;

        [SerializeField] private int _servoValue = 7500;
        private int _defaultServoValue = 7500;

        const string PreMaidAiDefaultStandPose = "50 18 00 06 02 4C 1D 03 4C 1D 04 4C 1D 05 4C 1D 06 4C 1D 07 4C 1D 08 4C 1D 09 1C 25 0A 4C 1D 0B 7C 15 0C 4C 1D 0D 4C 1D 0E 4C 1D 0F 4C 1D 10 4C 1D 11 4C 1D 12 4C 1D 13 4C 1D 14 4C 1D 15 4C 1D 16 4C 1D 17 4C 1D 18 4C 1D 1A 4C 1D 1C 4C 1D D9";


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

        /// <summary>
        /// サーボ一覧の表示順定義
        /// </summary>
        public static ServoPosition[] servoPositions =
        {
            ServoPosition.HeadYaw,              //頭ヨー
            ServoPosition.HeadRoll,             /*萌え軸*/
            ServoPosition.HeadPitch,            //頭ピッチ
            ServoPosition.RightShoulderPitch,   //肩ピッチR
            ServoPosition.RightShoulderRoll,    //肩ロールR
            ServoPosition.RightUpperArmYaw,     //上腕ヨーR
            ServoPosition.RightLowerArmPitch,   //肘ピッチR
            ServoPosition.RightHandYaw,         //手首ヨーR
            ServoPosition.LeftShoulderPitch,    //肩ピッチL
            ServoPosition.LeftShoulderRoll,     //肩ロールL
            ServoPosition.LeftUpperArmYaw,      //上腕ヨーL
            ServoPosition.LeftLowerArmPitch,    //肘ピッチL
            ServoPosition.LeftHandYaw,          //手首ヨーL
            ServoPosition.RightHipYaw,          //ヒップヨーR
            ServoPosition.RightHipRoll,         //ヒップロールR
            ServoPosition.RightUpperLegPitch,   //腿ピッチR
            ServoPosition.RightLowerLegPitch,   //膝ピッチR
            ServoPosition.RightFootPitch,       //足首ピッチR
            ServoPosition.RightFootRoll,        //足首ロールR
            ServoPosition.LeftHipYaw,           //ヒップヨーL
            ServoPosition.LeftHipRoll,          //ヒップロールL
            ServoPosition.LeftUpperLegPitch,    //腿ピッチL
            ServoPosition.LeftLowerLegPitch,    //肘ピッチL
            ServoPosition.LeftFootPitch,        //足首ピッチL
            ServoPosition.LeftFootRoll,         //足首ロールL
        };

        //初期値指定なしのコンストラクタは禁止します
        private PreMaidServo()
        {
        }

        public PreMaidServo(ServoPosition servoPosition)
        {
            _defaultServoValue = 7500;
            _servoPosition = servoPosition;
            servoName = servoPosition.ToString();

            //TODO:このServoのリミット設定は、大雑把に安全そうなリミットを設定しています。
            //なので、場合によってはこの値を緩められるか検討するのは可能です。
            //4000-11000
            switch (_servoPosition)
            {
                case ServoPosition.RightShoulderPitch:
                    MaxServoValue = 10000;//8700で後ろ手に組む
                    MinServoValue = 4000;//4500で正面
                    break;
                case ServoPosition.HeadPitch:
                    MaxServoValue = 8000; //うなづいた感じ
                    MinServoValue = 7300; //上向いた限界
                    break;
                case ServoPosition.LeftShoulderPitch:
                    MaxServoValue = 10500; //手が上向いたじょうたい
                    MinServoValue = 4000; //手を後ろ手に組む
                    break;
                case ServoPosition.HeadYaw:
                    MinServoValue = 6600; //左を向いてる
                    MaxServoValue = 8400; //右を向いてる
                    break;
                case ServoPosition.RightHipYaw:
                    MaxServoValue = 7900; //内また気味
                    MinServoValue = 7000; //外開き
                    break;
                case ServoPosition.HeadRoll:
                    MaxServoValue = 7800; //左に首をかしげる
                    MinServoValue = 7200; //右に首をかしげる
                    break;
                case ServoPosition.LeftHipYaw:
                    MinServoValue = 7000; //内また
                    MaxServoValue = 8000; //外開き
                    break;
                case ServoPosition.RightShoulderRoll:
                    SetServoValueSafeClamp("1C 25");//9500
                    MinServoValue = 7500; //Tポーズ
                    MaxServoValue = 9700; //気を付け
                    break;
                case ServoPosition.RightHipRoll:
                    MinServoValue = 7000; // やすめ、肩幅に開く
                    MaxServoValue = 7700; // 気を付け
                    break;
                case ServoPosition.LeftShoulderRoll:
                    SetServoValueSafeClamp("7C 15");//5500
                    MinServoValue = 5200; //気を付け
                    MaxServoValue = 7300; //Tポーズ

                    break;
                case ServoPosition.LeftHipRoll:
                    MaxServoValue = 8000; // やすめ、肩幅に開く
                    MinServoValue = 7300; // 気を付け
                    break;
                case ServoPosition.RightUpperArmYaw:
                    MinServoValue = 6000; //手の外ひねり
                    MaxServoValue = 9000; //手の内ひねり
                    break;
                case ServoPosition.RightUpperLegPitch:
                    //シビアですぐこけるよ！！
                    MaxServoValue = 9000; //のけぞる
                    MinServoValue = 6000; //前屈
                    break;
                case ServoPosition.LeftUpperArmYaw:
                    MinServoValue = 6000; //手の内ひねり
                    MaxServoValue = 9000; //手の外ひねり
                    break;
                case ServoPosition.LeftUpperLegPitch:
                    //シビアですぐこけるよ！！
                    MaxServoValue = 9000; //前屈
                    MinServoValue = 6000; //のけぞる
                    break;
                case ServoPosition.RightLowerArmPitch:
                    MaxServoValue = 11000; //肘を力こぶ出来る方に曲げる10000とかいけるけど、体に刺さらないように要調整（ほかの腕軸を見てね）
                    MinServoValue = 7400; //肘を外開き、無理させないでね、もげるよ
                    break;
                case ServoPosition.RightLowerLegPitch:
                    MinServoValue = 5000; //膝を曲げる。当然5000とかにするとコケる 
                    MaxServoValue = 7550; //膝をあり得ない方に曲げる。ほとんど曲がらない
                    break;
                case ServoPosition.LeftLowerArmPitch:
                    MaxServoValue = 7600; //肘を外開き、無理させないでね、もげるよ
                    MinServoValue = 4000; //肘を力こぶ出来る方に曲げる 5000とかいけるけど、体に刺さらないように要調整（ほかの腕軸を見てね）
                    break;
                case ServoPosition.LeftLowerLegPitch:
                    MinServoValue = 7450; //膝をあり得ない方に曲げる。ほとんど曲がらない
                    MaxServoValue = 9000; //膝を曲げる。当然10000とかにするとコケる
                    break;
                case ServoPosition.RightHandYaw:
                    MinServoValue = 6000; //手のひらを外回し
                    MaxServoValue = 9000; //手のひらを内回し
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
        /// LeftShoulderRollとかの文字列を返す
        /// </summary>
        /// <returns></returns>
        public string GetServoName()
        {
            string name = Enum.GetName(typeof(PreMaidServo.ServoPosition), _servoPosition);

            return name;
        }

        /// <summary>
        /// "1C" とか "0F"とかのサーボID文字列を返す
        /// </summary>
        /// <returns></returns>
        public string GetServoIdString()
        {
            return ((int)_servoPosition).ToString("X2");
        }

        /// <summary>
        /// 28とか2とか6とかのサーボIDのint値を返す
        /// </summary>
        /// <returns></returns>
        public int GetServoId()
        {
            return (int)_servoPosition;
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
            var tmp = PreMaidUtility.ConvertEndian(_servoValue.ToString("X2"));

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
        public void SetServoValueSafeClamp(int newValue)
        {
            if (newValue > MaxServoValue)
            {
                newValue = MaxServoValue;

            }
            if (newValue < MinServoValue)
            {
                newValue = MinServoValue;
            }

            _servoValue = newValue;
        }

        /// <summary>
        /// "4C 1D"などの外部からサーボの値を入れる
        /// ちなみに4C 1Dが7500です
        /// </summary>
        /// <param name="newValue">"4C 1D"</param>
        public void SetServoValueSafeClamp(string spaceSplitedByteString)
        {
            var aaa = PreMaidUtility.ConvertEndian(PreMaidUtility.RemoveWhitespace(spaceSplitedByteString));
            int intValue = int.Parse(aaa, System.Globalization.NumberStyles.HexNumber);
            //Debug.Log($"{spaceSplitedByteString} は {intValue} ");

            SetServoValueSafeClamp(intValue);
        }


        //サーボ種類一覧をダンプします
        public static void AllServoPositionDump()
        {
            foreach (var item in Enum.GetValues(typeof(PreMaidServo.ServoPosition)))
            {
                string name = Enum.GetName(typeof(PreMaidServo.ServoPosition), item);

                Debug.Log($"{name} val: {(int)item}  raw val:{((int)item).ToString("X2")}");
            }
        }


    }
}