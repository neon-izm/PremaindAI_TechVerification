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

        [SerializeField]
        private int currentFrame = 0;

        [System.Serializable]
        //1フレームのポーズ
        public class PoseFrame
        {
            public int frameNumber;

            public string commandLength;//"50"で固定？
            public string command;//"18"で固定？
            public string commandPadding;//"00"で固定
            public string frameWait;//"FF"のことが多い
            public List<Servo> servos = new List<Servo>();
            public string checkByte;//ここまでの値をxorしたもの

            public int wait;    // frameWaitの値
        }

        /// <summary>
        /// データ中のループ情報を保持する構造体
        /// </summary>
        private struct Loop
        {
            public int startKeyFrameIndex;
            public int iteration;
        }

        [SerializeField] private Transform premaidRoot;


        [SerializeField] List<PoseFrame> _frames = new List<PoseFrame>();

        [SerializeField]
        private ModelJoint[] _joints;

        /// <summary>
        /// 再生時のFPS（komas per second）
        /// </summary>
        [SerializeField]
        private float fps = 66.67f;

        /// <summary>
        /// モーション再生中は true
        /// </summary>
        private bool isPlaying = false;

        /// <summary>
        /// モーション再生開始時刻
        /// </summary>
        private float startedTime = 0f;

        /// <summary>
        /// 現在のコマ。再生中は時刻に合わせて増える
        /// </summary>
        [SerializeField]
        private int currentKoma = 0;

        /// <summary>
        /// 全コマ数
        /// </summary>
        private int totalKomas = 0;

        /// <summary>
        /// 再生開始をこのコマからとする。負の値も可
        /// 動画との時刻合わせ用
        /// </summary>
        [SerializeField]
        private int firstKoma = 0;

        private UnityEngine.Video.VideoPlayer videoPlayer;
        private UnityEngine.UI.Slider motionSeekSlider;
        private bool isSliderDraggable = true;


        void ApplyPose(int frameNumber)
        {
            if (_frames.Count <= frameNumber)
            {
                return;
            }

            var targetFrame = _frames[frameNumber];

            foreach (var VARIABLE in targetFrame.servos)
            {

                try
                {
                    var modelJoint = _joints.First(joint => joint.ServoID == VARIABLE.id);
                    if (modelJoint != null)
                    {
                        modelJoint.SetServoValue(VARIABLE.eulerAngle);
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("some exeption:" + e);
                }

            }

        }

        void ApplyPose(PoseFrame prevFrame, PoseFrame nextFrame, float weight)
        {
            foreach (var prevServo in prevFrame.servos)
            {
                float angle = prevServo.eulerAngle;

                string id = prevServo.id;
                var nextServo = nextFrame.servos.First(servo => servo.id.Equals(id));
                if (nextServo != null)
                {
                    // -135～135 [deg] ということだとLeapAngle()は一旦 +180 してから -180 とかしないとダメかも
                    //angle = Mathf.LerpAngle(prevServo.eulerAngle, nextServo.eulerAngle, weight);

                    // サーボが 360 [deg] 回らないため、単にLerp()で良いのでは
                    angle = Mathf.Lerp(prevServo.eulerAngle, nextServo.eulerAngle, weight);
                }

                try
                {
                    var modelJoint = _joints.FirstOrDefault(joint => joint.ServoID == prevServo.id);
                    if (modelJoint != null)
                    {
                        modelJoint.SetServoValue(angle);
                    }
                }
                catch (Exception e)
                {
                    Debug.Log("some exeption:" + e);
                }

            }

        }

        /// <summary>
        /// 指定コマ（本当はフレーム）に該当する姿勢を前後の姿勢から補間して返す
        /// </summary>
        /// <param name="koma"></param>
        void ApplyPoseByKoma(int koma)
        {
            if (_frames.Count < 1)
            {
                // キーフレームが1つもなければ何もしない
                return;
            }
            else if ((_frames.Count == 1) || (koma < _frames[0].wait))
            {
                // キーフレームが1つしかないか、指定コマが負なら先頭の姿勢を単にとる
                currentFrame = 0;
                ApplyPose(currentFrame);
                return;
            }

            PoseFrame prevFrame = _frames[0];
            PoseFrame nextFrame = null;
            currentFrame = 0;

            int elapsedKoma = prevFrame.wait;
            float weight = 0f;
            for (int frameNumber = 1; frameNumber < _frames.Count; frameNumber++)
            {
                nextFrame = _frames[frameNumber];

                // 次のフレームで指定時刻以上になるなら、ここが求めたいタイミングである
                if ((elapsedKoma + nextFrame.wait) >= koma)
                {
                    // 2つのコマ間の重みを0～1で求める
                    weight = Mathf.Clamp01((float)(koma - elapsedKoma) / nextFrame.wait);
                    break;
                }

                elapsedKoma += nextFrame.wait;
                prevFrame = nextFrame;

                currentFrame = frameNumber;
            }

            // 最後まで指定タイミングが見つからなければ、最終姿勢をとらせる
            if (prevFrame == nextFrame)
            {
                currentFrame = _frames.Count - 1;
                ApplyPose(currentFrame);
                return;
            }

            // 2つのキーフレームの間の姿勢をとらせる
            ApplyPose(prevFrame, nextFrame, weight);
        }

        // Start is called before the first frame update
        void Start()
        {
            if (premaidRoot != null)
            {
                _joints = premaidRoot.GetComponentsInChildren<ModelJoint>();
            }

            videoPlayer = FindObjectOfType<UnityEngine.Video.VideoPlayer>();
            motionSeekSlider = FindObjectOfType<UnityEngine.UI.Slider>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.K))
            {
                Debug.Log(ServoStringToValue("1D", "4C"));
            }


            if (isPlaying)
            {
                currentKoma = (int)((Time.time - startedTime) * fps);
                ApplyPoseByKoma(currentKoma);

                // スライダーを進ませる
                if (motionSeekSlider && (totalKomas != 0) && (currentKoma <= totalKomas))
                {
                    isSliderDraggable = false;
                    motionSeekSlider.value = (float)currentKoma / (float)totalKomas;
                    isSliderDraggable = true;
                }
            }
        }

        /// <summary>
        /// 動作を再生／停止
        /// </summary>
        public void PlayButton()
        {
            if (isPlaying)
            {
                // 停止

                if (videoPlayer) videoPlayer.Stop();
                //if (videoPlayer) videoPlayer.Pause();

                isPlaying = false;
                currentKoma = 0;
                ApplyPose(currentFrame);
            }
            else
            {
                // 再生開始

                if (videoPlayer) videoPlayer.Play();

                currentKoma = firstKoma;
                startedTime = Time.time - ((float)firstKoma / fps);
                isPlaying = true;
            }
        }

        /// <summary>
        /// 指定のコマに移動
        /// </summary>
        /// <param name="koma"></param>
        public void JumpKoma(float normalizedKoma)
        {
            if (isSliderDraggable)
            {
                currentKoma = (int)(normalizedKoma * totalKomas);
                startedTime = Time.time - currentKoma / fps;
                ApplyPoseByKoma(currentKoma);
            }
        }

        /// <summary>
        /// モーションファイルを開く
        /// </summary>
        public void OpenFileButton()
        {
            var willOpenPath = VRM.FileDialogForWindows.FileDialog("Open Premaid Motion File", ".pma", ".mp4", ".mov");

            if (string.IsNullOrEmpty(willOpenPath))
            {
                return;
            }

            string ext = Path.GetExtension(willOpenPath).ToLower();

            if (ext == ".mp4" || ext == ".mov")
            {
                LoadVideo(willOpenPath);
            }
            else
            {
                LoadPma(willOpenPath);

                // 同名の動画があるか検索

                var dir = Path.GetDirectoryName(willOpenPath);
                var file = Path.GetFileNameWithoutExtension(willOpenPath);
                var path = Path.Combine(dir, file);
                if (File.Exists(path + ".mp4"))
                {
                    LoadVideo(path + ".mp4");
                }
                else if (File.Exists(path + ".mov"))
                {
                    LoadVideo(path + ".mov");
                }
            }
        }

        /// <summary>
        /// 動画読み込み
        /// </summary>
        /// <param name="fullPath"></param>
        private void LoadVideo(string fullPath)
        {
            if (!videoPlayer) return;

            videoPlayer.Stop();
            videoPlayer.url = "file://" + fullPath;
            videoPlayer.Play();
            videoPlayer.Pause();
        }


        /// <summary>
        /// pmaファイル読み込み
        /// </summary>
        /// <param name="fullPath"></param>
        public void LoadPma(string fullPath)
        {
            StreamReader sr = new StreamReader(
                fullPath,
                new UTF8Encoding(true));

            string text = sr.ReadToEnd();

            sr.Close();
            //Debug.Log(text.Length);
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
            totalKomas = 0;

            var loops = new Stack<Loop>();   // ループ情報。読み込み中に都度都度増減する

            _frames.Clear();

            //ここからhex文字列の配列からパースしていきます。
            while (seekIndex < tailIndex)
            {

                //50 18 から始まるのがモーションデータ作法
                if (hexByteArray[seekIndex] == "50" && (seekIndex + 6 < tailIndex) &&
                    hexByteArray[seekIndex + 1] == "18")
                {
                    //ガガッとフレームパースしますよ～～
                    //なんも考えずにspanでarray渡そうかな～ 末尾に50 18 が大体ついてそう　25サーボ*3データで75個分を抜き出すと良い？
                    string[] servoArray = hexByteArray.Skip(seekIndex).Take(0x50).ToArray();
                    var parsedFrame = ParseOneFrame(servoArray);
                    if (parsedFrame != null)
                    {
                        parsedFrame.frameNumber = _frames.Count;
                        _frames.Add(parsedFrame);
                        totalKomas += parsedFrame.wait;
                        //Debug.Log(frameCounter + " : " + parsedFrame.frameWait + " : " + parsedFrame.wait);
                    }

                    frameCounter++;
                    seekIndex += 0x50;
                }
                // ループ始点
                else if (hexByteArray[seekIndex] == "08" && (seekIndex + 7 < tailIndex) &&
                    hexByteArray[seekIndex + 1] == "02")
                {
                    string[] commandArray = hexByteArray.Skip(seekIndex).Take(0x08).ToArray();

                    Loop loop = new Loop();
                    loop.iteration = HexToInt(commandArray[5]);  // ここが繰り返し回数？
                    loop.startKeyFrameIndex = _frames.Count - 1;    // ループ始点となるキーフレーム番号

                    loops.Push(loop);

                    seekIndex += 0x08;
                }
                // ループ終点
                else if (hexByteArray[seekIndex] == "08" && (seekIndex + 7 < tailIndex) &&
                    hexByteArray[seekIndex + 1] == "07")
                {
                    string[] commandArray = hexByteArray.Skip(seekIndex).Take(0x08).ToArray();

                    InterpretLoop(loops.Pop());

                    seekIndex += 0x08;
                }
                else
                {
                    seekIndex++;
                }
            }

            Debug.Log("合計:" + frameCounter + "個のキーフレームがありました");
            Debug.Log("合計:" + totalKomas + "個のフレームがありました");
        }

        private void InterpretLoop(Loop loop)
        {
            int startIndex = loop.startKeyFrameIndex;
            int endIndex = _frames.Count - 1;
            for (int i = 0; i < loop.iteration - 1; i++)    // 1回は元のデータであるので繰り返し回数-1
            {
                for (int j = startIndex; j <= endIndex; j++)
                {
                    _frames.Add(_frames[j]);
                    totalKomas += _frames[j].wait;
                }
            }
        }

        /// <summary>
        /// 75個のデータを受けて、サーボ情報を調べます。
        /// </summary>
        /// <param name="servoStrings"></param>
        /// <returns></returns>
        PoseFrame ParseOneFrame(string[] servoStrings)
        {
            if (servoStrings.Length != 80)
            {
                Debug.LogError("不正なフレームです");
            }

            PoseFrame ret = new PoseFrame();
            ret.commandLength = servoStrings[0];
            ret.command = servoStrings[1];
            ret.commandPadding = servoStrings[2];
            ret.frameWait = servoStrings[3];

            ret.wait = HexToInt(ret.frameWait);

            //25軸だと信じていますよ
            for (int i = 0; i < 25; i++)
            {
                Servo tmp = new Servo();
                tmp.id = servoStrings[4 + i * 3];
                tmp.hb = servoStrings[4 + i * 3 + 1];
                tmp.lb = servoStrings[4 + i * 3 + 2];
                tmp.servoValue = ServoStringToValue(servoStrings[4 + (i * 3) + 1], servoStrings[4 + (i * 3) + 2]);
                tmp.eulerAngle = (tmp.servoValue - 7500) * 0.03375f;//0.03375= 135/4000
                ret.servos.Add(tmp);
            }
            //checkbyte
            ret.checkByte = servoStrings[79];
            return ret;
        }

        private static int HexToInt(string hex)
        {
            return int.Parse(hex, NumberStyles.AllowHexSpecifier);
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
