using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BombControl : MonoBehaviour {

    [SerializeField] private float destroyTimeLimit = 3.0f;       // Bombが削除されるまでの時間
    [SerializeField] private float destroyTime;
    [SerializeField] private bool destoryActiveFlg = false;

    public PhotonView photonView;


    private void Awake()
    {
        this.photonView = GetComponent<PhotonView>();
    }


    // Use this for initialization
    void Start () {
        this.destroyTime = 0.0f;
        this.destoryActiveFlg = false;
    }
	
	// Update is called once per frame
	void Update () {

        // 
        if (IsMine() == true)
        {
            if (this.destoryActiveFlg == true)
            {
                this.destroyTime += Time.deltaTime;

                // 有効時間後、オブジェクトを消滅させる
                if (this.destroyTime >= this.destroyTimeLimit)
                {
                    Destroy();
                }
            }
        }

    }


    private void OnCollisionEnter(Collision collision)
    {
        if (IsMine() ==  true)
        {
            // Bombは何かに当たった事をきっかけに削除までのカウントダウンが開始する
            if (this.destoryActiveFlg == false)
            {
                this.destoryActiveFlg = true;
                this.destroyTime = 0.0f;
            }
        }
    }

    private void Destroy()
    {
        // TODO:オブジェクトを即削除、余韻などが必要ならここかも
        GlobalGameManager._instance.photonManager.DestroyGameObject(gameObject);

    }


    public void SetDestroy()
    {
        this.destroyTime = this.destroyTimeLimit;
    }



    /// <summary>
    /// 作成したGameObjectかを判定する
    /// </summary>
    /// <returns><c>true</c>, if mine was ised, <c>false</c> otherwise.</returns>
    public bool IsMine()
    {
        bool result = false;

        if (this.photonView != null)
            result = this.photonView.isMine;

        return result;
    }



}
