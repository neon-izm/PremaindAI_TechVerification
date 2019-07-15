using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif


namespace PreMaid.RemoteController
{
    /// <summary>
    /// PreMaidPoseControllerのベースになるクラス
    /// 命令を送る、受ける、という部分に特化しています
    /// </summary>
    public class PreMaidController : MonoBehaviour
    {
        [SerializeField] List<PreMaidServo> _servos = new List<PreMaidServo>();

        private string portName = "COM7";
        private const int BaudRate = 115200;
        private SerialPort _serialPort;

        [SerializeField] private bool _serialPortOpen = false;

        public bool SerialPortOpen
        {
            get { return _serialPortOpen; }
        }

        /// <summary>
        /// シリアルポート経由で受信した命令を受けるアクション
        /// </summary>
        public Action<string> OnReceivedFromPreMaidAI;

        public Action OnInitializeServoDefines = null;


        public List<PreMaidServo> Servos
        {
            get { return _servos; }
            set { _servos = value; }
        }


        /// <summary>
        /// プリメイドAIに命令を送るときはここにEnqueueする
        /// スペース区切りの命令文字列を入れることを想定しています
        /// 例："50 18 00 06 02 00 00 03 00 00 04 00 00 05 00 00 06 00 00 07 00 00 08 00 00 09 00 00 0A 00 00 0B 00 00 0C 00 00 0D 00 00 0E 00 00 0F 00 00 10 00 00 11 00 00 12 00 00 13 00 00 14 00 00 15 00 00 16 00 00 17 00 00 18 00 00 1A 00 00 1C 00 00 FF";
        /// </summary>
        ConcurrentQueue<string> sendingQueue = new ConcurrentQueue<string>();

        /// <summary>
        /// プリメイドAIから受け取った応答はここに入る
        /// 直接使わないでOnReceivedFromPreMaidAI 経由で受け取って欲しい
        /// </summary>
        ConcurrentQueue<string> receivedQueue = new ConcurrentQueue<string>();

        /// <summary>
        /// シリアルポートスレッド内から受け取ったデバッグログエラーをここに差し込んでメインスレッドで表示する
        /// </summary>
        ConcurrentQueue<string> errorQueue = new ConcurrentQueue<string>();


        /// <summary>
        /// シリアルポート用のスレッド
        /// </summary>
        private Thread _serialPortThread;

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
                _serialPort.ReadTimeout = 1; //これを明示的に指定してTimeout例外を握りつぶすのがUnity Monoの悲しいお作法っぽい…
                Debug.Log("シリアルポート:" + portName + " 接続成功");
                _serialPortOpen = true;
                _serialPortThread = new Thread(ReadAndWriteThreadFunc)
                {
                    IsBackground = true
                };
                _serialPortThread.Start();

                InvokeRepeating(nameof(RequestBatteryRemain), 2f, 10f);

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

            if (_serialPortThread != null && _serialPortThread.IsAlive)
            {
                _serialPortThread.Join();
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


        /// <summary>
        /// シリアルポート読み取り書き込みスレッド
        /// 読みと書きを別スレッドにするより、まとめて1スレッドにしたほうがシリアルポートのlockが走らない分だけ早い
        /// </summary>
        private void ReadAndWriteThreadFunc()
        {
            Debug.LogWarning("シリアルポート送信スレッド起動");

            var readBuffer = new byte[256 * 3];
            var readCount = 0;
            while (SerialPortOpen && _serialPort != null && _serialPort.IsOpen)
            {
                //PCから送る予定のキューが入っているかチェック
                if (sendingQueue.IsEmpty == false)
                {
                    var willSendString = string.Empty;
                    if (sendingQueue.TryDequeue(out willSendString))
                    {
                        byte[] willSendBytes =
                            PreMaidUtility.BuildByteDataFromStringOrder(willSendString);

                        _serialPort.Write(willSendBytes, 0, willSendBytes.Length);
                    }
                }


                //プリメイドAIからの受信チェック
                try
                {
                    readCount = _serialPort.Read(readBuffer, 0, readBuffer.Length);

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

                Thread.Sleep(1);
            }

            Debug.LogWarning("exit thread");
        }

        /// <summary>
        /// 現在のサーボ値を適用する1フレームだけのモーションを送る
        /// </summary>
        /// <returns></returns>
        string BuildPoseStringAll(int speed = 10)
        {
            speed = Mathf.Clamp(speed, 1, 255);

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
        /// 現在のサーボ値を適用する1フレームだけのモーションを送る
        /// </summary>
        /// <returns></returns>
        string BuildPoseString(int speed = 10)
        {
            speed = Mathf.Clamp(speed, 1, 255);

            //決め打ちのポーズ命令+スピード(小さい方が速くて、255が最大に遅い)
            string ret = "08 18 00 " + speed.ToString("X2");
            //そして各サーボぼ値を入れる

            var index = _servos.FindIndex(x => x.GetServoId() == 2);

            ret += " " + _servos[index].GetServoIdAndValueString();


            ret += " FF"; //パリティビットを仮で挿入する;

            //パリティビットを計算し直した値にして、文字列を返す
            return PreMaidUtility.RewriteXorString(ret);
        }


        /// <summary>
        /// 現在のサーボ値を適用してシリアル通信でプリメイドAI実機に送る
        /// </summary>
        public void ApplyPose()
        {
            if (SerialPortOpen == false)
            {
                Debug.LogWarning("ポーズ指定されたときにシリアルポートが開いていません");
                return;
            }

            sendingQueue.Enqueue(BuildPoseString(10)); //対象のモーション、今回は1個だけ;
        }

        /// <summary>
        /// 現在のサーボ値を適用してシリアル通信でプリメイドAI実機に送る
        /// </summary>
        public void ApplyPoseAllServos()
        {
            if (SerialPortOpen == false)
            {
                Debug.LogWarning("ポーズ指定されたときにシリアルポートが開いていません");
                return;
            }

            sendingQueue.Enqueue(BuildPoseStringAll(10)); //対象のモーション、今回は1個だけ;
        }


        /// <summary>
        /// 指定されたサーボ値を適用してシリアル通信でプリメイドAI実機に送る
        /// </summary>
        public void ApplyPoseFromServos(IEnumerable<PreMaidServo> servos, int speed = 10)
        {
            if (SerialPortOpen == false)
            {
                Debug.LogWarning("ポーズ指定されたときにシリアルポートが開いていません");
                return;
            }

            speed = Mathf.Clamp(speed, 1, 255);

            int servoNum = servos.Count();
            int orderLen = servoNum * 3 + 5; //命令長はサーボ個数が1個だったら0x08, サーボ個数が25個だったら0x50(80)になる

            //決め打ちのポーズ命令+スピード(小さい方が速くて、255が最大に遅い)
            string ret = orderLen.ToString("X2") + " 18 00 " + speed.ToString("X2");
            //そして各サーボぼ値を入れる
            foreach (var VARIABLE in servos)
            {
                ret += " " + VARIABLE.GetServoIdAndValueString();
            }

            ret += " FF"; //パリティビットを仮で挿入する;

            //パリティビットを計算し直した値にして、文字列を返す
            ret = PreMaidUtility.RewriteXorString(ret);

            sendingQueue.Enqueue(ret); //対象のモーション、今回は1個だけ;
        }


        /// <summary>
        /// 全サーボの強制脱力命令
        /// </summary>
        public void ForceAllServoStop(bool disconnect = true)
        {
            //ここで連続送信モードを停止しないと、脱力後の急なサーボ命令で一気にプリメイドAIが暴れて死ぬ
            //なので、普段はついでにシリアルポートも切る

            string allStop =
                "50 18 00 06 02 00 00 03 00 00 04 00 00 05 00 00 06 00 00 07 00 00 08 00 00 09 00 00 0A 00 00 0B 00 00 0C 00 00 0D 00 00 0E 00 00 0F 00 00 10 00 00 11 00 00 12 00 00 13 00 00 14 00 00 15 00 00 16 00 00 17 00 00 18 00 00 1A 00 00 1C 00 00 FF";
            sendingQueue.Enqueue(PreMaidUtility.RewriteXorString(allStop)); //ストップ命令を送る

            if (disconnect)
            {
                CloseSerialPort();
            }
        }


        /// <summary>
        /// 全サーボのストレッチパラメータ指定命令
        /// 18 19 10 10 3C 18 3C 1C 3C 14 3C 0C 3C 0E 3C 16 3C 1A 3C 12 3C 0A 3C 07
        /// </summary>
        public void ForceAllServoStretchProperty(int stretch)
        {
            var targetStretch = Mathf.Clamp(stretch, 1, 127);

            string stretchProp = string.Format("{0:X2}", targetStretch);

            //0x36=54個なので
            //最初の3個+ 25サーボ*2パラメータ+ チェックバイト
            string allServo =
                "36 19 10";

            foreach (var VARIABLE in Servos)
            {
                allServo += " " + VARIABLE.GetServoIdString() + " " + stretchProp;
            }

            allServo += "FF";
            sendingQueue.Enqueue(PreMaidUtility.RewriteXorString(allServo)); //ストレッチ命令を送る
        }

        /// <summary>
        /// 全サーボのスピードパラメータ指定命令
        /// 18 19 10 10 3C 18 3C 1C 3C 14 3C 0C 3C 0E 3C 16 3C 1A 3C 12 3C 0A 3C 07
        /// </summary>
        public void ForceAllServoSpeedProperty(int speed)
        {
            var targetSpeed = Mathf.Clamp(speed, 1, 127);

            string speedProp = string.Format("{0:X2}", targetSpeed);

            //0x36=54個なので
            //最初の3個+ 25サーボ*2パラメータ+ チェックバイト
            string allServo =
                "36 19 00";

            foreach (var VARIABLE in Servos)
            {
                allServo += " " + VARIABLE.GetServoIdString() + " " + speedProp;
            }

            allServo += "FF";
            sendingQueue.Enqueue(PreMaidUtility.RewriteXorString(allServo)); //ストレッチ命令を送る
        }

        /// <summary>
        /// バッテリー残量の問い合わせ、ハンドリングはPreMaidReceiver.csで行っています
        /// </summary>
        public void RequestBatteryRemain()
        {
            string batteryRequestOrder = "07 01 00 02 00 02 06";
            //Debug.Log("リクエスト:"+ batteryRequestOrder);
            sendingQueue.Enqueue(PreMaidUtility.RewriteXorString(batteryRequestOrder)); //バッテリー残量を教えてもらう
        }

        /// <summary>
        /// たぶんこれでFLASHのダンプが返ってくる
        /// </summary>
        /// <param name="page"></param>
        public void RequestFlashRomDump(int page)
        {
            string flashDump = "05 1C 00 " + string.Format("{0:X2}", page) + " FF";
            Debug.Log("リクエスト:" + flashDump);
            sendingQueue.Enqueue(PreMaidUtility.RewriteXorString(flashDump)); //FLASHの中身を教えてもらう？
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

            if (SerialPortOpen == false)
            {
                return;
            }

            //受信バッファ、バイナリで届くので区切りをどうしようか悩み中
            //一旦、素朴に先頭に命令長が来るでしょう、というつもりで書きます。
            if (receivedQueue.IsEmpty == false)
            {
                var receivedString = string.Empty;
                if (receivedQueue.TryDequeue(out receivedString))
                {
                    bufferedString += receivedString;

                    if (bufferedString.Length < 2)
                    {
                        return;
                    }

                    //異様にバッファが溜まったら捨てる
                    if (bufferedString.Length > 100)
                    {
                        Debug.Log("破棄します:" + bufferedString);
                        bufferedString = string.Empty;
                        return;
                    }

                    int orderLength = PreMaidUtility.HexStringToInt(bufferedString.Substring(0, 2));

                    //先頭0だったら命令ではないと判断して2文字読み捨て
                    //なぜなら0004051Fみたいな文字列が入っているので
                    if (orderLength == 0)
                    {
                        bufferedString = bufferedString.Substring(2);
                    }
                    //命令長が足りないので待つ
                    else if (orderLength > bufferedString.Length * 2)
                    {
                        return;
                    }
                    else if (bufferedString.Length >= orderLength * 2)
                    {
                        var targetOrder = bufferedString.Substring(0, orderLength * 2);
                        if (OnReceivedFromPreMaidAI != null)
                        {
                            OnReceivedFromPreMaidAI.Invoke(targetOrder);
                        }
                        else
                        {
                            Debug.Log(targetOrder);
                        }

                        //まだ余りバッファが有るならツメます
                        if (orderLength * 2 < bufferedString.Length)
                        {
                            bufferedString = bufferedString.Substring(orderLength * 2 + 1);
                        }
                        else
                        {
                            bufferedString = string.Empty;
                        }
                    }
                }
            }
        }
    }
}