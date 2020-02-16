using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PhotonView))]
public class SceneControl : MonoBehaviour
{

    private PhotonView photonView;
    private WebAPIControl webApiControl;

    [SerializeField] private bool m_MouseCursorGameMode = false;
    [SerializeField] private Transform[] m_arrayRespawnPoints;
    public Material colorOni;
    public Material colorDef;

    //
    [SerializeField] private GameObject playerPrefab;

    public Text txtDebugStatus;                           // Debug用の状態表示テキストエリア
    public Text txtTime;                                  // 残プレイ時間テキストエリア
    public Text txtMessage;                               // メッセージ表示用テキストエリア
    public Text txtScore;                                 // スコア表示用テキストエリア
    public Text txtOniStatus;                             // Playerの鬼状態表示用テキストエリア
    public GameObject reloadTime;                         // Bombのリロード時間表示用Slider
    public float reloadTime_inProgress;                   // Reloadの進行状態を表示                         

    [SerializeField] private float playTime;
    [SerializeField] private float bombTime;                // Bombの爆発時間
    [SerializeField] private float bombTimeRange_Min = 15.0f;
    [SerializeField] private float bombTImeRange_Max = 25.0f;
    [SerializeField] private float waitTime;

    public List<PlayerControl> playerList = new List<PlayerControl>();


    public bool inputStop = true;                           // 入力を止める時、trueへ
    [SerializeField] private float roundCounter = 1;


    private void Awake()
    {

        // 参照先の初期化
        this.photonView = GetComponent<PhotonView>();
        this.webApiControl = GetComponent<WebAPIControl>();
        this.playerPrefab = (GameObject)Resources.Load(GlobalGameManager._instance.PATH_PREFAB + "PlayerUni");

        // Materialの読み込みを行なっておく
        this.colorOni = (Material)Resources.Load(GlobalGameManager._instance.PATH_MATERIAL + "PlayerOniMaterial");
        this.colorDef = (Material)Resources.Load(GlobalGameManager._instance.PATH_MATERIAL + "PlayerDefMaterial");

        // プレイ時間の初期化
        this.playTime = GlobalGameManager._instance.GAME_TIME;

    }



    // Use this for initialization
    void Start()
    {

        // プレイヤー作成
        CreatePlayerCharacter_Photon();

        // 
        InitMouseControl();

        // Bomb制限時間、最初の値を設定する
        Mst_ResetBombTime();

        //TODO:ループする方法を
        StartCoroutine(GameLoop());

    }

    /// <summary>
    /// Playerオブジェクトを作成(Photon)
    /// </summary>
    private void CreatePlayerCharacter_Photon()
    {
        // Playerの番号を取得
        int playerNo = GlobalGameManager._instance.GetPlayerNo();

        // Playerのインスタンスを作成
        GameObject playerInstance =
            GlobalGameManager._instance.photonManager.CreateGameObject(
                GlobalGameManager._instance.PATH_PREFAB + this.playerPrefab.name,
                m_arrayRespawnPoints[playerNo].position,
                m_arrayRespawnPoints[playerNo].rotation
                );

        if (playerInstance == null)
        {
            Debug.Log("Create PlayerInstance Error!");
            return;
        }

        // Playerへのパラメータ設定
        PlayerControl playerControl = playerInstance.GetComponent<PlayerControl>();
        if(playerControl != null)
        {
            playerControl.playerName = GlobalGameManager._instance.PlayerName;

            // 最初の鬼状態を設定
            if (GlobalGameManager._instance.startingOniID == playerNo)
                playerControl.oniState = true;
        }

    }


    private IEnumerator PlayerWait()
    {
        //TODO:途中抜けするとここで進行が止まる、対応の必要がある
        while (this.playerList.Count != GlobalGameManager._instance.playerIds.Length)
        {
            Debug.Log(string.Format("PlayerList:{0}/{1}", this.playerList.Count, GlobalGameManager._instance.playerIds.Length));
            yield return null;
        }
    }


    private IEnumerator GameLoop()
    {

        yield return StartCoroutine(PlayerWait());  // Player情報が集まるまで待機する

        yield return StartCoroutine(RoundStarting());
        yield return StartCoroutine(RoundPlaying());
        yield return StartCoroutine(RoundEnding());


        // 制限時間が残っている場合、Bomb時間を再設定しゲームを再開
        if(this.playTime > 0.0f)
        {
            // ラウンド数をカウントアップ
            this.roundCounter++;

            // Bomb時間のリセット
            Mst_ResetBombTime();

            // MasterClientによるBombリセットが行われるまで待機
            while (this.bombTime <= 0.0f)
                yield return null;

            StartCoroutine(GameLoop());
        }
        else
        {
            // 勝敗を確認する
            string gameResult = "";
            int result = ScoreCheck();
            if(result > 0)
            {
                gameResult = "Win!";
            }
            else if(result < 0)
            {
                gameResult = "Lose..";
            }
            else
            {
                gameResult = "Drow";
            }

            this.txtMessage.text = string.Format("- Result -\n{0}", gameResult);
            yield return new WaitForSeconds(3.0f);

            // 結果が勝ちの場合のみ、WebAPIを用いてプレイヤーの勝ち数をDB反映
            if(result == 1)
            {
                // TODO:この機能は残す？
                //this.webApiControl.SetRankingValue(GlobalGameManager._instance.PlayerName, "1");
            }


            this.txtMessage.text = "GAME OVER";
        }

    }

    private IEnumerator RoundStarting()
    {
        string msgHeader = "";


        // 自分が鬼かどうかを調べる
        if (OniCheck() == true)
        {
            msgHeader = "あなたが鬼です！";
        }
        else
        {
            msgHeader = "　あなたは逃走者です。";
        }

        // 待ち時間を設定 
        Mst_SetWaitTime(5.0f);

        // 入力受付まで待機
        while(this.inputStop == true)
        {
            this.txtMessage.text = string.Format("- Round:{0} -\n{1}\n{2}", this.roundCounter, msgHeader, (int)waitTime);

            //
            Mst_WaitTimeDown(Time.deltaTime);

            // 待ち時間が無くなったら入力禁止フラグをオフへ
            if(this.waitTime <= 1.0f)
            {
                // 同じタイミングで待ち時間を再設定
                Mst_SetWaitTime(2.0f);

                //
                Mst_InputStop_OFF();
            }
            yield return null;
        }

        // TODO:上にまとめるんでコメントアウト
/*        //
        Mst_SetWaitTime(5.0f);
        while (this.waitTime > 0.0f && this.inputStop == true)
        {
            Debug.Log("CheckPoint A");
            DebugStatus();

            m_txtMessage.text = string.Format("- Round:{0} -\n{1}\n{2}", this.roundCounter, msgHeader, (int)waitTime);

            Mst_WaitTimeDown(Time.deltaTime);

            yield return null;
        }

        // 入力の制限を解除
        Mst_InputStop_OFF();
        while(this.inputStop == true)
        {
            Debug.Log("CheckPoint B");
            // MasterClientによる入力制限の解除が実行されるまで待機
            yield return null;
        }
*/

        //
        //Mst_SetWaitTime(2.0f);
        while(this.waitTime > 0.0f)
        {
            //
            this.txtMessage.text = string.Format("- Round:{0} -\n{1}\n{2}", this.roundCounter, msgHeader, "Start!!");

            //
            Mst_WaitTimeDown(Time.deltaTime);
            yield return null;
        }

        // 最後にメッセージを消す
        this.txtMessage.text = "";

        yield return null;
    }


    private IEnumerator RoundPlaying()
    {
        // ゲームの停止はBomb時間が無くなったタイミングで行う
        while(this.bombTime > 0.0f)
        {
            // 時間経過
            MstTimerCountdown();

            // MouseCursorの制御
            if (Input.GetKeyUp(KeyCode.Escape))
            {
                InitMouseControl();
            }

            yield return null;
        }

        // 入力の制限を停止
        Mst_InputStop_ON();
        while (this.inputStop == false)
        {
            // MasterClientによる入力制限の停止が実行されるまで待機
            yield return null;
        }

        //
        this.txtMessage.text = string.Format("- Round:{0} -\n{1}", this.roundCounter, "Finish!!");
        yield return new WaitForSeconds(3.0f);

    }


    private IEnumerator RoundEnding()
    {

        // ポイントの反映
        AddPoint(); 

        //
        string resultMsg = "";
        if(OniCheck() == true)
        {
            resultMsg = "Bad Point -1";

        }
        else
        {
            resultMsg = "Good Point +1";
        }
        this.txtMessage.text = string.Format("- Round:{0} -\n{1}", this.roundCounter, resultMsg);
        yield return new WaitForSeconds(3.0f);

    }



    // Update is called once per frame
    void Update()
    {
        DebugStatus();
        Display_PlayTimer();
        Display_Score();
        Display_OniStatus();
        Display_ReloadTime();

    }


    /// <summary>
    /// Photon間でのパラメータ同期を行う
    /// </summary>
    /// <param name="stream">Stream.</param>
    /// <param name="info">Info.</param>
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.isWriting)
        {
            // 送信処理
            stream.SendNext(this.playTime);                 // 制限時間
            stream.SendNext(this.bombTime);                 // Bomb制限時間
            stream.SendNext(this.inputStop);                // 入力停止フラグ
            stream.SendNext(this.waitTime);                 // 待ち時間
        }
        else
        {
            // 受信処理
            this.playTime = (float)stream.ReceiveNext();    // 制限時間
            this.bombTime = (float)stream.ReceiveNext();    // Bomb制限時間
            this.inputStop = (bool)stream.ReceiveNext();    // 入力停止フラグ
            this.waitTime = (float)stream.ReceiveNext();    // 待ち時間
        }
    }



    void InitMouseControl()
    {
        if (m_MouseCursorGameMode == false)
        {
            // GamePlay用にマウスカーソル設定を反映させる
            // (1)cursorの非表示
            Cursor.visible = false;
            // (2)cursorのセンターロック
            // TODO: macだとWindow内で固定できない？
            Cursor.lockState = CursorLockMode.Confined;
            // (3)cursorのGameModeを変更する
            m_MouseCursorGameMode = true;
        }
        else
        {
            // OS側のコントロールが可能なようにマウスカーソル設定を解放
            // (1)cursorの表示
            Cursor.visible = true;
            // (2)cursorのロック解除
            Cursor.lockState = CursorLockMode.None;
            // (3)cursorのGameModeを変更する
            m_MouseCursorGameMode = false;
        }
    }


    void DebugStatus()
    {
        this.txtDebugStatus.text = "";

        //
        DebugStatusDisplay(string.Format("MasterClient:{0}", PhotonNetwork.isMasterClient.ToString()));
        DebugStatusDisplay(string.Format("Player_ID:{0}", GlobalGameManager._instance.photonManager.isPlayer_ID));
        DebugStatusDisplay(string.Format("Starting_Oni_ID:{0}", GlobalGameManager._instance.startingOniID.ToString()));

        DebugStatusDisplay(string.Format("BombTime:{0}", this.bombTime));


        DebugStatusDisplay("\n\n-- Players Info --");
        foreach (PlayerControl pl in this.playerList)
        {
            DebugStatusDisplay(string.Format("PhotonID:{0}  ONI:{1}  Name:{2}", pl.photonId, pl.oniState.ToString(), pl.playerName));
        }        


    }
    public void DebugStatusDisplay(string msg)
    {
        if (GlobalGameManager._instance.DebugMode)
        {
            this.txtDebugStatus.text += string.Format("\n{0}", msg);
        }
    }

    /// <summary>
    /// 制限時間を表示する
    /// </summary>
    void Display_PlayTimer()
    {
        this.txtTime.text = string.Format("TIME:{0:00}", (int)this.playTime);
    }


    /// <summary>
    ///  現在のPlayer状態を画面に表示する
    /// </summary>
    void Display_OniStatus()
    {

        // 現在の状態を確認する
        if(OniCheck() == true)
        {
            this.txtOniStatus.text = "鬼";
            this.txtOniStatus.color = this.colorOni.color;
            this.txtMessage.color = this.colorOni.color;
        }
        else
        {
            this.txtOniStatus.text = "逃";
            this.txtOniStatus.color = this.colorDef.color;
            this.txtMessage.color = this.colorDef.color;
        }

    }

    void Display_ReloadTime()
    {
        // ReloadTimeオブジェクトが有効の場合のみ実行する
        if(this.reloadTime.GetActive() == true)
        {
            this.reloadTime.GetComponent<Slider>().value = this.reloadTime_inProgress;
        }
    }
    /// <summary>
    /// ReloadTimeオブジェクトを有効にする
    /// </summary>
    public void ReloadTime_ON()
    {
        // 状態が無効の場合のみ、初期化と状態変更を実行する
        if(this.reloadTime.GetActive() == false)
        {
            this.reloadTime_inProgress = 0.0f;
            this.reloadTime.SetActive(true);
            this.reloadTime.GetComponent<Slider>().value = 0.0f;
        }
    }

    /// <summary>
    /// ReloadTimeオブジェクトを無効にする
    /// </summary>
    public void ReloadTime_OFF()
    {
        this.reloadTime.SetActive(false);
    }

    public void SetReloadTime(float progressValue)
    {
        this.reloadTime_inProgress = progressValue;
    }


    private void MstTimerCountdown()
    {
        // MasterClientが残時間のコントロールを行う
        if (PhotonNetwork.isMasterClient)
        {
            this.playTime -= Time.deltaTime;
            if(this.playTime < 0.0f)
            {
                this.playTime = 0.0f;
            }

            this.bombTime -= Time.deltaTime;
            if(this.bombTime < 0.0f)
            {
                this.bombTime = 0.0f;
            }
        }

    }



    public void OniChange(int NewOni_PhotonId) 
    {
        foreach (PlayerControl pl in this.playerList)
        {
            // 管理者のみ処理を行う事ができる
            if(pl.IsMine() == true)
            {
                // 新しい鬼以外は全て、鬼状態を解除する
                if(pl.photonId != NewOni_PhotonId)
                {
                    pl.oniState = false;
                }
                else
                {
                    pl.oniState = true;
                }
            }
        }
    }


    public bool OniCheck() 
    {
        bool result = false;

        foreach (PlayerControl pl in this.playerList)
        {
            // 管理可能なPlayerの鬼かどうかを調べる
            if (pl.IsMine() == true)
            {
                //Debug.Log(string.Format("OniCheck:{0}",pl.photonId));
                result = pl.oniState;
                //break;
            }

            Debug.Log(string.Format("OniCheck:{0}  State:{1}", pl.photonId, pl.oniState.ToString()));
        }

        return result;
    }





    [PunRPC]
    public void AddPoint()
    {
        foreach (PlayerControl pl in this.playerList)
        {
            // 管理者のみ処理を行う事ができる
            if (pl.IsMine() == true)
            {
                if(pl.oniState == true)
                {
                    // 鬼なら-1ポイント
                    pl.scorePoint -= 1;
                }
                else
                {
                    // 鬼以外は+1ポイント
                    pl.scorePoint += 1;
                }
            }
        }
    }


    private void Display_Score()
    {
        this.txtScore.text = string.Format("POINT:{0:00}", GetPoint());
    }

    private int GetPoint()
    {
        int result = 0;

        foreach (PlayerControl pl in this.playerList)
        {
            // 管理者のみ処理を行う事ができる
            if (pl.IsMine() == true)
            {
                result = pl.scorePoint;
                break;
            }
        }

        return result;
    }


    private int ScoreCheck()
    {
        int result = 0;

        float myScore = 0;
        float bestScore = 0;

        foreach (PlayerControl pl in this.playerList)
        {
            // 管理者のみ処理を行う事ができる
            if (pl.IsMine() == true)
            {
                myScore = pl.scorePoint;
            }
            else
            {
                if(bestScore < pl.scorePoint)
                {
                    bestScore = pl.scorePoint;
                }
            }
        }

        if(myScore > bestScore)
        {
            result = 1;
        }
        else if(myScore < bestScore)
        {
            result = -1;
        }
        else
        {
            result = 0;
        }

        return result;
    }

    /// <summary>
    /// Msts the input stop off.
    /// </summary>
    private void Mst_InputStop_OFF()
    {
        // MasterClientのみ入力の制限を解除できる
        if (GlobalGameManager._instance.photonManager.IsMasterClient() == true)
        {
            this.inputStop = false;
        }
    }

    /// <summary>
    /// Msts the input stop on.
    /// </summary>
    private void Mst_InputStop_ON()
    {

        // MasterClientのみ入力の制限を停止できる
        if (GlobalGameManager._instance.photonManager.IsMasterClient() == true)
        {
            this.inputStop = true;
        }

    }

    /// <summary>
    /// WaitTimeの初期値を設定
    /// </summary>
    /// <param name="time">Time.</param>
    private void Mst_SetWaitTime(float time)
    {
        // MasterClientのみが待ち時間の変更が可能
        if(GlobalGameManager._instance.photonManager.IsMasterClient() == true)
        {
            this.waitTime = time;
        }
    }

    /// <summary>
    /// WaitTimeへ指定値減らす
    /// </summary>
    /// <param name="time">Time.</param>
    private void Mst_WaitTimeDown(float time)
    {
        // MasterClientのみが待ち時間を減らす事が可能
        if (GlobalGameManager._instance.photonManager.IsMasterClient() == true)
        {
            this.waitTime -= time;
            if(this.waitTime < 0.0f)
                this.waitTime = 0.0f;
        }
    }

    /// <summary>
    /// Bombの制限時間を設定する
    /// </summary>
    public void Mst_ResetBombTime()
    {
        // MasterClientのみBombの制限時間を設定できる
        if (GlobalGameManager._instance.photonManager.IsMasterClient() == true)
        {
            this.bombTime = Random.Range(this.bombTimeRange_Min, this.bombTImeRange_Max);
        }
    }



}
