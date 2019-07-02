using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO.Ports;

namespace PreMaid
{
    /// <summary>
    /// モーションデータをベタで送るテスト
    /// </summary>
    public class MotionDataWriterSample : MonoBehaviour
    {
        /// <summary>
        /// ポーズ終了（ゆっくりTポーズを取り、待機ポーズになる）
        /// </summary>
        private string forceTpose =
            "04 05 00 01";

        /// <summary>
        /// 「どうぞ」というポーズを取る命令
        /// </summary>
        private string poseDouzo =
            "05 1F 00 3E 24";
        
        /// <summary>
        /// 格納しているダンスを踊る命令
        /// </summary>
        private string startDance =
            "05 1F 00 01 1B";

        private string portName = "COM7";
        private int baudRate    = 115200;
        private SerialPort serialPort_;
     
        
        
        //適当に抜き出したポーズ
        //まだ動作確認出来ていない
        private string testPose =
            "50 18 00 0B 02 70 11 03 4C 1D 04 58 1B 05 4C 1D 06 4C 1D 07 4C 1D 08 4C 1D 09 68 22 0A 9B 1D 0B 96 15 0C CE 1D 0D 64 1D 0E 7A 1C 0F FB 28 10 B4 1E 11 5C 2B 12 A8 1B 13 85 11 14 1C 20 15 8B 25 16 7A 1C 17 D9 14 18 B4 1E 1A 24 1D 1C 9A 1A AF FF FF";

        
        // Start is called before the first frame update
        void Start()
        {
            serialPort_ = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
            serialPort_.Open();
        }

        // Update is called once per frame
        void Update()
        {

            //動作確認にもTポーズというかポーズ終了を送るのは良いこと
            if (Input.GetKeyDown(KeyCode.A))
            {
                byte[] data = BuildByteDataFromStringOrder(forceTpose);
                serialPort_.Write(data, 0, data.Length);
                
                DumpDebugLogToHex(data);
                
            }

            //どうぞ、のポーズを取る
            if (Input.GetKeyDown(KeyCode.B))
            {
                byte[] data = BuildByteDataFromStringOrder(poseDouzo);
                serialPort_.Write(data, 0, data.Length);
                
                DumpDebugLogToHex(data);
            }

            //格納しているダンスモーションを再生する
            if (Input.GetKeyDown(KeyCode.C))
            {
                byte[] data = BuildByteDataFromStringOrder(startDance);
                serialPort_.Write(data, 0, data.Length);
                
                DumpDebugLogToHex(data);
            }

            //まだ動作していない、任意サーボ情報を送るポーズ指定
            if (Input.GetKeyDown(KeyCode.D))
            {
                byte[] data = BuildByteDataFromStringOrder(testPose);
                serialPort_.Write(data, 0, data.Length);

                DumpDebugLogToHex(data);
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                serialPort_.Close();
            }
            
        }

        /// <summary>
        /// byte[]の中身を直接文字列としてDebug.Logに出す
        /// 0xff0x63の場合は、FF63と表示される
        /// </summary>
        /// <param name="hex"></param>
        private static void DumpDebugLogToHex(byte[] hex)
        {
            string str = "";
            for (int i = 0; i < hex.Length; i++) {
                str += string.Format("{0:X2}", hex[i]);
            }
            Debug.Log(str);
        }
        
        /// <summary>
        /// スペース区切りの文字列からbyte配列を作る。このコードを読む人はこれだけ使ってもらえれば！
        /// </summary>
        /// <param name="spaceSplitedByteString"></param>
        /// <returns></returns>
        byte[] BuildByteDataFromStringOrder(string spaceSplitedByteString)
        {
            var hexString = RemoveWhitespace(spaceSplitedByteString);
            
            return HexStringToByteArray(hexString) ;
        }
        
        /// <summary>
        /// "FF0063"みたいな文字列を0xff,0x00,0x63みたいなbyte配列にする
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static byte[] HexStringToByteArray(string hex)
        {
            int numberChars = hex.Length;
            byte[] bytes = new byte[numberChars / 2];
            for (int i = 0; i < numberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
        
        /// <summary>
        /// スペース区切りのモーション文字列をスペース消して返す
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string RemoveWhitespace( string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }
        
       
    }
}