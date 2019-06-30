using System.Collections;
using System.Collections.Generic;
using System.IO;
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
            public int id;
            public string lb; //先頭2文字
            public string hb; //末尾2文字
            public int servoValue; //3500から11500まで、7500がセンター
            public float eulerAngle; //角度を出します。
        }

        // Start is called before the first frame update
        void Start()
        {
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
            Debug.Log(tailIndex+"個のhexがあります");
            //ここからパースしていきます。
            
        }

        // Update is called once per frame
        void Update()
        {
        }
    }
}