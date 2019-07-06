using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using TMPro;
using UnityEngine;

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


        private bool _continuousMode = false;


        //何秒ごとにポーズ指定するか
        private float _poseProcessDelay = 0.9f;

        private float _timer = 0.0f;


        [SerializeField] private TMPro.TMP_Dropdown _dropdown = null;

        [SerializeField] private ServoUguiController _uguiController = null;

        // Start is called before the first frame update
        void Start()
        {
            _servos.Clear();
            PreMaidServo.AllServoPositionDump();
            foreach (PreMaidServo.ServoPosition item in Enum.GetValues(typeof(PreMaidServo.ServoPosition)))
            {
                PreMaidServo servo = new PreMaidServo(item);

                _servos.Add(servo);
            }

            //一覧を出す
            foreach (var VARIABLE in _servos)
            {
                Debug.Log(VARIABLE.GetServoIdString() + "   " + VARIABLE.GetServoId() + "  サーボ数値変換" +
                          VARIABLE.GetServoIdAndValueString());
            }

            _uguiController.Initialize(_servos);
            _uguiController.OnChangeValue+= OnChangeValue;
            
            Debug.Log(BuildPoseString());
            var portNames = SerialPort.GetPortNames();

            if (_dropdown == null)
            {
                Debug.LogError("シリアルポートを選択するDropDownが指定されていません");
                return;
            }

            List<TMP_Dropdown.OptionData> serialPortNamesList = new List<TMP_Dropdown.OptionData>();

            foreach (var VARIABLE in portNames)
            {
                TMP_Dropdown.OptionData optionData = new TMP_Dropdown.OptionData(VARIABLE);
                serialPortNamesList.Add(optionData);

                Debug.Log(VARIABLE);
            }

            _dropdown.ClearOptions();
            _dropdown.AddOptions(serialPortNamesList);
        }

        private void OnChangeValue()
        {
            //Debug.Log("値の変更");
            //Refreshします～
            var latestValues = _uguiController.GetCurrenSliderValues();

            for (int i = 0; i < _servos.Count; i++)
            {
                _servos[i].SetServoValueSafeClamp((int)latestValues[i]);
            }
        }


        public void SetContinuousMode(bool newValue)
        {
            _continuousMode = newValue;
        }


        public void OpenSerialPort()
        {
            Debug.Log(_dropdown.options[_dropdown.value].text + "を開きます");

            _serialPortOpen = SerialPortOpen(_dropdown.options[_dropdown.value].text);
        }

        /// <summary>
        /// シリアルポートを開ける
        /// </summary>
        /// <returns></returns>
        bool SerialPortOpen(string portName)
        {
            try
            {
                _serialPort = new SerialPort(portName, BaudRate, Parity.None, 8, StopBits.One);
                _serialPort.Open();
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


        /// <summary>
        /// 現在のサーボ値を適用する1フレームだけのモーションを送る
        /// </summary>
        /// <returns></returns>
        string BuildPoseString(int speed = 20)
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
            foreach (var VARIABLE in _servos)
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
            float waitSec = 0.04f; //0.03だと送信失敗することがある

            byte[] data1 = PreMaidUtility.BuildByteDataFromStringOrder("07 01 00 02 00 02 06");
            _serialPort.Write(data1, 0, data1.Length);
            yield return new WaitForSeconds(waitSec);

            byte[] data2 = PreMaidUtility.BuildByteDataFromStringOrder("07 01 00 08 00 02 0C");
            _serialPort.Write(data2, 0, data2.Length);
            yield return new WaitForSeconds(waitSec);

            byte[] data3 = PreMaidUtility.BuildByteDataFromStringOrder("08 02 00 08 00 FF FF 02");
            _serialPort.Write(data3, 0, data3.Length);
            yield return new WaitForSeconds(waitSec);

            byte[] data4 = PreMaidUtility.BuildByteDataFromStringOrder("04 04 00 00"); //フラッシュのライトプロテクト解除？
            _serialPort.Write(data4, 0, data4.Length);
            yield return new WaitForSeconds(waitSec);

            byte[] data5 = PreMaidUtility.BuildByteDataFromStringOrder("5c 1d 00 00 00"); //転送コマンド？
            _serialPort.Write(data5, 0, data5.Length);
            yield return new WaitForSeconds(waitSec);

            //ここでポーズ情報を取得する
            byte[] data6 =
                PreMaidUtility.BuildByteDataFromStringOrder(
                    BuildPoseString()); //対象のモーション、今回は1個だけ
            _serialPort.Write(data6, 0, data6.Length);
            yield return new WaitForSeconds(waitSec * 2);


            byte[] data7 = PreMaidUtility.BuildByteDataFromStringOrder("04 17 00 13 ff ff 41"); //不明
            _serialPort.Write(data7, 0, data7.Length);
            yield return new WaitForSeconds(waitSec);

            byte[] data8 = PreMaidUtility.BuildByteDataFromStringOrder("05 1E 00 01 1A");
            _serialPort.Write(data8, 0, data8.Length);
            yield return new WaitForSeconds(waitSec);


            byte[] data9 = PreMaidUtility.BuildByteDataFromStringOrder("05 1C 00 01 18"); //ベリファイダンプ要請
            _serialPort.Write(data9, 0, data9.Length);
            yield return new WaitForSeconds(waitSec);


            byte[] data10 = PreMaidUtility.BuildByteDataFromStringOrder("08 02 00 08 00 08 00 0A"); //モーションデータ転送終了
            _serialPort.Write(data10, 0, data10.Length);
            yield return new WaitForSeconds(waitSec);

            byte[] data11 = PreMaidUtility.BuildByteDataFromStringOrder("04 04 00 00"); //フラッシュのライトプロテクトを掛ける？
            _serialPort.Write(data11, 0, data11.Length);
            yield return new WaitForSeconds(waitSec);

            byte[] data12 = PreMaidUtility.BuildByteDataFromStringOrder("05 1F 00 01 1B"); //01番モーション再生
            _serialPort.Write(data12, 0, data12.Length);
            yield return new WaitForSeconds(waitSec);
        }


        // Update is called once per frame
        void Update()
        {
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