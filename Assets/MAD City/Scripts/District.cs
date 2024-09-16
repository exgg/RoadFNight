using Roadmans_Fortnite.Scripts.Classes.Stats.Enums;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class District : MonoBehaviour
{
    public int level;
    public DistrictData district_data;

    public Race predominant_race;//>50%
    public Dictionary<Race, float> race_lst = new Dictionary<Race, float>();//race, ratio

    public void init_district(List<DistrictData> _districtData, int district_num)
    {
        district_data = _districtData[level];
        predominant_race = (Race)Random.Range(0, (int)Race.RaceNum);
        name = "District - No:" + district_num;
        generate_race_ratio();
    }

    //The generated proportions all conform to 0.0 format
    //e.g. 0.9
    void generate_race_ratio()
    {
        //Debug.Log(name);
        int tmp = Random.Range(5, 10);
        race_lst[predominant_race] = tmp / 10.0f;
        int index = 10 - tmp;
        //Debug.LogFormat("{0} = {1}", predominant_race, race_lst[predominant_race]);

        List<Race> all_race = new List<Race>{ Race.Black, Race.White, Race.Asian, Race.HalfCast};
        all_race.Remove(predominant_race);
        int cnt = 0;
        foreach (var i in all_race)
        {
            if (cnt != all_race.Count - 1)
            {
                tmp = Random.Range(0, index + 1);
                race_lst[i] = tmp / 10.0f;
                index -= tmp;
            }
            else
            {
                race_lst[i] = index / 10.0f;
            }
            //Debug.LogFormat("race_lst[{0}] = {1}", i, race_lst[i]);
            cnt += 1;
        }
    }
}
