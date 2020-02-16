using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiniJSON;

using System;
//using System.IO;

public class WebAPIControl : MonoBehaviour {

    private List<RankboardData> lstRankboard = null;
    public string RANKING_URL = "http://192.168.11.4/rankboardsystem/rankboard/";

    /// <summary>
    /// ランキング情報を表示用に加工して戻す
    /// </summary>
    /// <returns>The output.</returns>
    public string RankingOutput()
    {
        Debug.Log(">>RankingOutput Start");

        string result = "";

        // Wwwを利用してJsonデータ取得が実行できてた場合に表示用の加工に入る
        // このタイミングだとWWWからの読み込み設定が間に合ってない
        if (lstRankboard != null)
        {
            int rankNo = 1;

            //
            result = "-RANKING-\n";

            foreach (RankboardData row in lstRankboard)
            {
                result += string.Format("#{0}  {1}  :{2}points\n",
                                    rankNo,
                                    row.name,
                                    row.point);

                // 順位番号を加算
                rankNo++;
            }
        }

        Debug.Log(">>RankingOutput End:" + result);
        return result;
    }




    /// <summary>
    /// ランキング情報文字列情報を取得する
    /// </summary>
    /// <returns>The ranking result.</returns>
    public IEnumerator GetRankingValue()
    {
        Debug.Log(">>GetRankingValue Start");

        // Wwwを利用してJsonデータ取得をリクエストする
        yield return StartCoroutine(
            DownloadJson(
                string.Format("{0}/{1}", RANKING_URL, "getRanking"),
                CallbackWwwRankboardSuccess,
                CallbackWwwRankboardFailed
            )
        );

        Debug.Log(">>GetRankingValue End");
    }

    private IEnumerator DownloadJson(string targetUrl, Action<string> cbkSuccess = null, Action cbkFailed = null)
    {
        Debug.Log(">>DownloadJson Start");

        // WWWを利用してリクエストを送信する
        WWW www = new WWW(targetUrl);

        // WWWのレスポンスを待機
        yield return StartCoroutine(ResponceCheckForTimeOutWWW(www, 5.0f));

        if (www.error != null) 
        {
            // WWWリクエストの失敗

            Debug.LogError(www.error);
            if(cbkFailed != null)
            {
                // ダウンロード失敗時、失敗用のメソッドをコールする
                cbkFailed();
            }
        }
        else if(www.isDone)
        {
            // WWWリクエストのダウンロード完了

            if(cbkSuccess != null)
            {
                // ダウンロード成功時、取得したJson情報を用いてメソッドをコールする
                cbkSuccess(www.text);
            }
        }

        Debug.Log(">>DownloadJson End");
    }

    /// <summary>
    /// Rankboard用のJson取得が成功した場合にCallするメソッド
    /// </summary>
    /// <param name="responseJson">Wwwを通して取得できたJson文字列</param>
    private void CallbackWwwRankboardSuccess(string responseJson)
    {
        Debug.Log(">>CallbackWwwRankboardSuccess Start");

        // パラメータとして入ってくるJson文字列をデコードする
        lstRankboard = RankboardDataModel.DesirializeFromJson(responseJson);

        // ログを出力して処理終了
        //        Debug.Log("CallbackWwwRankboardSuccess:");
        //        Debug.Log(lstRankboard.ToString());
        Debug.Log(">>CallbackWwwRankboardSuccess End");

    }

    /// <summary>
    /// Rankboard用のJson取得が失敗した場合にCallするメソッド
    /// </summary>
    private void CallbackWwwRankboardFailed()
    {
        Debug.Log("CallbackWwwRankboardFailed: faliled");
    }

    /// <summary>
    /// ダウンロードが完了する間、待機するためのメソッド
    /// </summary>
    /// <returns>The check for time out www.</returns>
    /// <param name="www">監視するWWWオブジェクト</param>
    /// <param name="timeout">タイムアウトまでの時間</param>
    private IEnumerator ResponceCheckForTimeOutWWW(WWW www, float timeout)
    {
        // 待機開始時刻を退避する
        float requestTime = Time.time;

        // ダウンロードの完了まで繰り返す
        while(!www.isDone)
        {
            // 待機時間がパラメータとして与えられた時間を超えていないかをチェック
            if(Time.time - requestTime < timeout)
            {
                // 待機許容時間ないの為、さらに待機する
                yield return null;
            }
            else
            {
                // 待機許容時間を超えた為、処理終了する
                Debug.LogWarning("Download:TimeOut!");
                break;
            }
        }
        yield return null;
    }





    public int SetRankingValue(string name, string point)
    {
        int result = 0;

        // Wwwへ送るパラメータを設定
        WWWForm form = new WWWForm();
        form.AddField("name", name);
        form.AddField("point", point);

        // Wwwを利用してJsonデータ取得をリクエストする
        StartCoroutine(
            UploadJson(
                string.Format("{0}/{1}", RANKING_URL, "setRanking"),
                form,
                CallbackWwwRankboardApiSuccess,
                CallbackWwwRankboardApiFailed
            )
        );



        return result;
    }

    private IEnumerator UploadJson(string targetUrl, WWWForm form, Action<string> cbkSuccess = null, Action cbkFailed = null)
    {

        // WWWを利用してリクエストを送信する
        WWW www = new WWW(targetUrl, form);

        // WWWのレスポンスを待機
        yield return StartCoroutine(ResponceCheckForTimeOutWWW(www, 5.0f));


        if (www.error != null)
        {
            // WWWリクエストの失敗

            Debug.LogError(www.error);
            if (cbkFailed != null)
            {
                // ダウンロード失敗時、失敗用のメソッドをコールする
                cbkFailed();
            }
        }
        else if (www.isDone)
        {
            // WWWリクエストのダウンロード完了

            if (cbkSuccess != null)
            {
                // ダウンロード成功時、取得したJson情報を用いてメソッドをコールする
                cbkSuccess(www.text);
            }
        }
    }

    /// <summary>
    /// Rankboard用のJson取得が成功した場合にCallするメソッド
    /// </summary>
    /// <param name="responseJson">Wwwを通して取得できたJson文字列</param>
    private void CallbackWwwRankboardApiSuccess(string responseJson)
    {
        // ログを出力して処理終了
        Debug.Log("CallbackWwwRankboardApiSuccess:");
        Debug.Log(responseJson.ToString());
    }

    /// <summary>
    /// Rankboard用のJson取得が失敗した場合にCallするメソッド
    /// </summary>
    private void CallbackWwwRankboardApiFailed()
    {
        Debug.Log("CallbackWwwRankboardApiFailed: Failed");
    }


}
