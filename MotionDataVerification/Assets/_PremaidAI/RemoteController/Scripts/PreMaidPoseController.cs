using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace PreMaid.RemoteController
{
    /// <summary>
    /// 普通のラジコンぽく動かすサンプルスクリプト
    /// </summary>
    public class PreMaidPoseController : MonoBehaviour
    {
        [SerializeField] List<PreMaidServo> _servos = new List<PreMaidServo>();

        private string portName = "COM7";
        private const int BaudRate = 115200;
        private SerialPort _serialPort;


        [SerializeField] private bool _serialPortOpen = false;

        /// <summary>
        /// 連続送信モードフラグ、初期値はfalse
        /// </summary>
        private bool _continuousMode = false;


        //何秒ごとにポーズ指定するか
        private float _poseProcessDelay = 0.25f;

        private float _timer = 0.0f;


        public Action<string> OnReceivedFromPreMaidAI;

        public Action OnInitializeServoDefines = null;

        /// <summary>
        /// 連続送信モードが切り替わったときに呼ばれる
        /// </summary>
        public Action<bool> OnContinuousModeChange;

        public List<PreMaidServo> Servos
        {
            get { return _servos; }
            set { _servos = value; }
        }


        ConcurrentQueue<string> sendingQueue = new ConcurrentQueue<string>();

        ConcurrentQueue<string> receivedQueue = new ConcurrentQueue<string>();

        ConcurrentQueue<string> errorQueue = new ConcurrentQueue<string>();


        private Thread _writeThread;
        private Thread _readThread;

        object lockObject = new object();

        /// <summary>
        /// エディタ再生終了時にシリアルポートの明示的開放をする為のキャンセル用
        /// </summary>
        private bool ShouldNotExit = false;

        // Start is called before the first frame update
        void Start()
        {
            Servos.Clear();
            //PreMaidServo.AllServoPositionDump();
            foreach (PreMaidServo.ServoPosition item in Enum.GetValues(typeof(PreMaidServo.ServoPosition)))
            {
                PreMaidServo servo = new PreMaidServo(item);

                Servos.Add(servo);
            }

            //一覧を出す
            foreach (var VARIABLE in Servos)
            {
                Debug.Log(VARIABLE.GetServoIdString() + "   " + VARIABLE.GetServoId() + "  サーボ数値変換" +
                          VARIABLE.GetServoIdAndValueString());
            }


            Debug.Log(BuildPoseString());
            OnInitializeServoDefines?.Invoke();
#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += OnChangedPlayMode;

#endif
        }

#if UNITY_EDITOR
        //プレイモードが変更された
        private void OnChangedPlayMode(PlayModeStateChange state)
        {
            //シリアルポートスレッド起動中にエディタ再生停止をしようとしたら、一旦キャンセルしつつシリアルポートスレッドを開放する
            if (state == PlayModeStateChange.ExitingPlayMode && ShouldNotExit)
            {
                EditorApplication.isPlaying = true;
                Debug.Log("シリアルポートを明示的にクローズします！OK呼ばれた");
                CloseSerialPort();

                ShouldNotExit = false;
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                Debug.Log("停止状態になった！");
            }
        }
#endif
        /// <summary>
        /// 連続送信モードを変更する
        /// 内部的に_continuousModeのboolを直接書き換えるのはこの関数経由にしてください
        /// </summary>
        /// <param name="newValue"></param>
        public void SetContinuousMode(bool newValue)
        {
            Debug.Log("連続送信モード切替 次の値は:" + newValue);
            _continuousMode = newValue;
            OnContinuousModeChange?.Invoke(_continuousMode);

            if (newValue)
            {
                _timer = 0;
            }
        }


        /// <summary>
        /// シリアルポートを開く
        /// </summary>
        /// <param name="portName">"COM4"とか</param>
        /// <returns></returns>
        public bool OpenSerialPort(string portName)
        {
            try
            {
                _serialPort = new SerialPort(portName, BaudRate, Parity.None, 8, StopBits.One);
                _serialPort.Open();
                _serialPort.ReadTimeout = 1;
                Debug.Log("シリアルポート:" + portName + " 接続成功");
                _serialPortOpen = true;
                _writeThread = new Thread(WriteThreadFunc)
                {
                    IsBackground = true
                };
                _writeThread.Start();

                _readThread = new Thread(ReadThreadFunc)
                {
                    IsBackground = true
                };
                _readThread.Start();
                ShouldNotExit = true;
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("シリアルポートOpen失敗しました、ペアリング済みか、プリメイドAIのポートか確認してください");
                Console.WriteLine(e);
                return false;
            }


            Debug.LogWarning($"指定された{portName}がありません。portNameを書き換えてください");
            return false;
        }


        private void OnApplicationQuit()
        {
            CloseSerialPort();
        }

        public void CloseSerialPort()
        {
            Debug.Log("シリアルポートをクローズします");
            _serialPortOpen = false;

            if (_writeThread != null && _writeThread.IsAlive)
            {
                _writeThread.Join();
            }


            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
            }

            if (_serialPort != null)
            {
                _serialPort.Dispose();
            }
        }

        private void ReadThreadFunc()
        {
            Debug.LogWarning("シリアルポート受信スレッド起動");

            var readBuffer = new byte[256 * 3];
            var readCount = 0;
            while (_serialPortOpen && _serialPort != null && _serialPort.IsOpen)
            {
                try
                {
                    lock (lockObject)
                    {
                        readCount = _serialPort.Read(readBuffer, 0, readBuffer.Length);
                    }

                    if (readCount > 0)
                    {
                        receivedQueue.Enqueue(PreMaidUtility.DumpBytesToHexString(readBuffer, readCount));
                    }
                }
                catch (TimeoutException tEx)
                {
                    //errorQueue.Enqueue("TimeOut Exception:" + tEx.Message);
                    //Thread.Sleep(1);
                    continue;
                }
                catch (System.Exception e)
                {
                    errorQueue.Enqueue(e.Message);
                    //Debug.LogWarning(e.Message);
                }

                Thread.Sleep(2);
            }

            errorQueue.Enqueue("受信スレッド終了");
        }


        private void WriteThreadFunc()
        {
            Debug.LogWarning("シリアルポート送信スレッド起動");

            while (_serialPortOpen && _serialPort != null && _serialPort.IsOpen)
            {
                //PCから送る予定のキューが入っているかチェック
                if (sendingQueue.IsEmpty == false)
                {
                    var willSendString = string.Empty;
                    if (sendingQueue.TryDequeue(out willSendString))
                    {
                        byte[] willSendBytes =
                            PreMaidUtility.BuildByteDataFromStringOrder(willSendString);
                        lock (lockObject)
                        {
                            _serialPort.Write(willSendBytes, 0, willSendBytes.Length);
                        }
                    }
                }

                Thread.Sleep(1);
            }

            Debug.LogWarning("exit thread");
        }

        /// <summary>
        /// 現在のサーボ値を適用する1フレームだけのモーションを送る
        /// </summary>
        /// <returns></returns>
        string BuildPoseString(int speed = 50)
        {
            if (speed > 255)
            {
                speed = 255;
            }

            if (speed < 1)
            {
                speed = 1;
            }

            //決め打ちのポーズ命令+スピード(小さい方が速くて、255が最大に遅い)
            string ret = "50 18 00 " + speed.ToString("X2");
            //そして各サーボぼ値を入れる
            foreach (var VARIABLE in Servos)
            {
                ret += " " + VARIABLE.GetServoIdAndValueString();
            }

            ret += " FF"; //パリティビットを仮で挿入する;

            //パリティビットを計算し直した値にして、文字列を返す
            return PreMaidUtility.RewriteXorString(ret);
        }


        /// <summary>
        /// 現在のサーボ値を適用してシリアル通信でプリメイドAI実機に送る
        /// </summary>
        public void ApplyPose()
        {
            if (_serialPortOpen == false)
            {
                Debug.LogWarning("ポーズ指定されたときにシリアルポートが開いていません");
                return;
            }

            StartCoroutine(ApplyPoseCoroutine());
        }


        /// <summary>
        /// たぶんあとで非同期待ち受けつかう
        /// </summary>
        /// <returns></returns>
        IEnumerator ApplyPoseCoroutine()
        {
            float waitSec = 0.06f; //0.03だと送信失敗することがある

            sendingQueue.Enqueue(BuildPoseString(80)); //対象のモーション、今回は1個だけ;
            yield return new WaitForSeconds(waitSec);
        }


        /// <summary>
        /// 全サーボの強制脱力命令
        /// </summary>
        public void ForceAllServoStop()
        {
            //ここで連続送信モードを停止しないと、脱力後の急なサーボ命令で一気にプリメイドAIが暴れて死ぬ
            SetContinuousMode(false);

            string allStop =
                "50 18 00 06 02 00 00 03 00 00 04 00 00 05 00 00 06 00 00 07 00 00 08 00 00 09 00 00 0A 00 00 0B 00 00 0C 00 00 0D 00 00 0E 00 00 0F 00 00 10 00 00 11 00 00 12 00 00 13 00 00 14 00 00 15 00 00 16 00 00 17 00 00 18 00 00 1A 00 00 1C 00 00 FF";
            sendingQueue.Enqueue(PreMaidUtility.RewriteXorString(allStop)); //ストップ命令を送る
        }

        private string bufferedString = string.Empty;


        // Update is called once per frame
        void Update()
        {
            if (errorQueue.IsEmpty == false)
            {
                var errorString = string.Empty;
                if (errorQueue.TryDequeue(out errorString))
                {
                    Debug.LogError(errorString);
                }
            }

            //受信バッファ、バイナリで届くので区切りをどうしようか悩み中
            //一旦、受信始めてから4文字以上で区切り、とします
            if (receivedQueue.IsEmpty == false)
            {
                var receivedString = string.Empty;
                if (receivedQueue.TryDequeue(out receivedString))
                {
                    bufferedString += receivedString;

                    if (bufferedString.Length > 4)
                    {
                        if (OnReceivedFromPreMaidAI != null)
                        {
                            OnReceivedFromPreMaidAI.Invoke(bufferedString);
                        }
                        else
                        {
                            Debug.Log(bufferedString);
                        }

                        bufferedString = string.Empty;
                    }
                }
            }

            if (_serialPortOpen == false)
            {
                return;
            }

            if (_continuousMode == false)
            {
                return;
            }

            _timer += Time.deltaTime;
            if (_timer > _poseProcessDelay)
            {
                ApplyPose();
                _timer -= _poseProcessDelay;
            }
        }
    }
}