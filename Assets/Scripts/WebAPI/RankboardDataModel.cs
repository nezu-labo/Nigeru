using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MiniJSON;

public class RankboardDataModel {


    /// <summary>
    /// Jsonデータの複合化
    /// </summary>
    /// <returns>RankboardDataリスト</returns>
    /// <param name="sJson">解析するJson文字列</param>
    static public List<RankboardData> DesirializeFromJson(string sJson)
    {
        Debug.Log(">>DesirializeFromJson Start");
        List<RankboardData> result = new List<RankboardData>();

        // RankboardDataは配列のリスト形式で返すので最初はIListで取り出す
        IList lstRanboardData = (IList)Json.Deserialize(sJson);

        // 取得したデータを一つずつ取り出し、戻り値へ装填していく
        foreach(IDictionary row in lstRanboardData)
        {
            RankboardData tmp = new RankboardData();

            if (row.Contains("Name"))
            {
                tmp.name = (string)row["Name"];
            }
            if (row.Contains("Point"))
            {
                tmp.point = row["Point"].ToString();
            }

            // 戻り値に装填
            result.Add(tmp);
        }
        Debug.Log(">>DesirializeFromJson End");
        return result;
    }

}
