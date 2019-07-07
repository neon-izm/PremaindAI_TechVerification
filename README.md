# PremaindAI_TechVerification
プリメイドAIのダンスモーションデータの解析プロジェクトです。
総サーボ数（自由度）　25軸のヒューマノイドロボットが、値下げによって2万円で手に入るようになった為、とても魅力的なおもちゃです。

http://robots.dmm.com/robot/premaidai

モーションデータ形式は *.pma という拡張子でAndroidアプリ上でダンスモーションデータをダウンロードすると  
Android\data\com.robotyuenchi.premaidai\  
以下に保存されています。  

# 現在できる事
## モーションデータの読み込みとプレビュー
MotionDataLoadTestWithModel.unity を開いて*.pmaファイルを読み込み、Play/Stopボタンを押すことでモーションを再生します。
GameView上のプリメイドAIを模したロボットがpmaのサーボ値を元にポーズを取ります。

![dance_preview](https://user-images.githubusercontent.com/3115650/60764234-fcfb3f80-a0c0-11e9-9ae9-88d45da23fc4.gif)

**再生開始数秒は待機ポーズが入っている事が多いのでロボットが動かないですが、数秒後から動き始めます**



## BT(SPP)経由でUnityEditor上からのプリメイドAIへの既存ポーズ送信
MotionDataWriterSample.cs にあります。適切にSerialPortを設定して、決め打ちの命令をbyte[]で送る事でAndroidアプリ上と同じように指定ポーズを取らせられます。

https://twitter.com/izm/status/1146100976455053312


## BT(SPP)経由でUnityEditor上からのプリメイドAIへの任意ポーズ送信
MotionDataWriterSample.cs にあります。適切にSerialPortを設定して、「1フレームだけのダンスモーション」を生成してダンスを転送→再生　を行う事で任意ポーズをとらせることが出来ます。この処理を連続で行う事で2-3FPS程度で任意モーションを流し込めると思います。

https://twitter.com/izm/status/1146586612773470208

## 各サーボのリモコン操作サンプル
Assets/_PremaidAI/RemoteController/Scenes/RemoteControlSample.unity でスライダからサーボ角を変更してリモコン的に遊べます。

![premaid_remote](https://user-images.githubusercontent.com/3115650/60763727-7db43e80-a0b5-11e9-859a-88568630d1bb.gif)


# 現在やっていること

## 一部フレームを飛ばしたモーションデータの生成
ブラックボックス化しているpmaファイルの先頭と末尾はそのままに、中間モーションを削っても再生できることは確認済みです。
その処理をUnity内で出来るようにしたい


# 動作環境
- Windows 10
- Unity 2018.3.14f 

# 関連URL
信号解析については以下のgoogle spreadsheet 上で編集中です。  

https://docs.google.com/spreadsheets/d/1c6jqMwkBroCuF74viU_q7dgSQGzacbUW4mJg-957_Rs/edit#gid=2102495394

# 同梱モデルデータについて
黒イワシ(twitetr:@Schwarz_Sardine)さんよりApache2.0ライセンス下で使用許可を得ています。ここに記して感謝します。


