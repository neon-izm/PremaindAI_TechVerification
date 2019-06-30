using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PreMaid
{
    /// <summary>
    /// とりあえずベタ書きでモーションの解析をします。
    /// </summary>
    public class MotionDataLoader : MonoBehaviour
    {
        [System.Serializable]
        public class Servo
        {
            public string id;
            public string lb; //先頭2文字
            public string hb; //末尾2文字
            public int servoValue; //3500から11500まで、7500がセンター
            public float eulerAngle; //角度を出します。7500が0で3500が-135度、11500が135度
        }

        [System.Serializable]
        //1フレームのポーズ
        public class PoseFrame
        {
            public List<Servo> servos = new List<Servo>();
        }

        [SerializeField] List<PoseFrame> _frames = new List<PoseFrame>();

        // Start is called before the first frame update
        void Start()
        {
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                Debug.Log(ServoStringToValue("1D", "4C"));
            }
        }

        /// <summary>
        /// モーションファイルを開く
        /// </summary>
        public void OpenFileButton()
        {
            var willOpenPath = VRM.FileDialogForWindows.FileDialog("Open Premaid Motion File", "*.pma");

            if (string.IsNullOrEmpty(willOpenPath))
            {
                return;
            }

            LoadPma(willOpenPath);
        }


        /// <summary>
        /// pmaファイル読み込み
        /// </summary>
        /// <param name="fullPath"></param>
        public void LoadPma(string fullPath)
        {
            StreamReader sr = new StreamReader(
                fullPath);

            string text = sr.ReadToEnd();

            sr.Close();
            //Debug.Log(text);
            ParsePma(text);
            return;
        }


        /// <summary>
        /// パースします
        /// </summary>
        /// <param name="fileContents"></param>
        void ParsePma(string fileContents)
        {
            var indexDataOffset = fileContents.IndexOf("データ=");
            if (indexDataOffset < 0)
            {
                Debug.LogError("想定外のデータのためクローズ");
                return;
            }

            var contenText = fileContents.Substring(indexDataOffset + "データ=".Length);
            Debug.Log("解析対象のデータは:" + contenText);
            //ゴミデータ、かどうか分からないけど解析できないデータ群と、単純なモーション群に分けてパースしていきたい。

            //モーションファイルは順に追いかけて行って "02"から4文字空けたら"03"があって、また4文字空けたら"04"があったらモーションデータ、と仮定する素朴な実装で一旦やってみます。
            //まずは半角スペースで分割した文字列を得ます。
            var hexByteArray = contenText.Split(' ');

            var tailIndex = hexByteArray.Length;
            var headIndex = 0;
            var seekIndex = headIndex;
            Debug.Log(tailIndex + "個のhexがあります");
            int frameCounter = 0;

            //ここからhex文字列の配列からパースしていきます。
            while (seekIndex < tailIndex)
            {
                if (hexByteArray[seekIndex] == "02" && (seekIndex + 6 < tailIndex) &&
                    hexByteArray[seekIndex + 3] == "03" &&
                    hexByteArray[seekIndex + 6] == "04")
                {
                    //ガガッとフレームパースしますよ～～
                    //なんも考えずにspanでarray渡そうかな～ 末尾に50 18 が大体ついてそう　25サーボ*3データで75個分を抜き出すと良い？
                    string[] servoArray = hexByteArray.Skip(seekIndex).Take(25 * 3).ToArray();
                    var parsedFrame = ParseOneFrame(servoArray);
                    if (parsedFrame != null)
                    {
                        _frames.Add(parsedFrame);
                    }

                    frameCounter++;
                }

                seekIndex++;
            }

            Debug.Log("合計:" + frameCounter + "個のモーションフレームがありました");
        }


        /// <summary>
        /// 75個のデータを受けて、サーボ情報を調べます。
        /// </summary>
        /// <param name="servoStrings"></param>
        /// <returns></returns>
        PoseFrame ParseOneFrame(string[] servoStrings)
        {
            if (servoStrings.Length != 75)
            {
                Debug.LogError("不正なフレームです");
            }

            PoseFrame ret = new PoseFrame();
            //25軸だと信じていますよ
            for (int i = 0; i < 25; i++)
            {
                Servo tmp = new Servo();
                tmp.id = servoStrings[i * 3];
                tmp.hb = servoStrings[i * 3 + 1];
                tmp.lb = servoStrings[i * 3 + 2];
                tmp.servoValue = ServoStringToValue(servoStrings[i * 3 + 1], servoStrings[i * 3 + 2]);
                tmp.eulerAngle = (tmp.servoValue - 7500) * 0.03375f;//0.03375= 135/4000
                ret.servos.Add(tmp);
            }

            return ret;
        }

        /// <summary>
        /// lbとhbに16進数の値を入れると、intの数値が返ってきます～
        /// </summary>
        /// <param name="hb">serveID+1 "4C"</param>
        /// <param name="lb">servoID+2 "1D"</param>
        /// <returns>7500</returns>
        private static int ServoStringToValue(string hb, string lb)
        {
            return Int32.Parse(lb + hb, NumberStyles.AllowHexSpecifier);
        }
    }
}