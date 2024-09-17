using Roadmans_Fortnite.Scripts.Classes.Stats.Enums;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class District : MonoBehaviour
{
    public int level;
    public DistrictData district_data;

    public Race predominant_race;//>50%
    public int predominant_race_num;
    public Dictionary<Race, float> race_lst = new Dictionary<Race, float>();//race, ratio

    // TODO: Need to change the type from GameObject to Civilians 
    public List<GameObject> black_people_lst = new List<GameObject>();
    public List<GameObject> white_people_lst = new List<GameObject>();
    public List<GameObject> asian_people_lst = new List<GameObject>();
    public List<GameObject> halfcast_people_lst = new List<GameObject>();
    public List<List<GameObject>> all_people_lst = new List<List<GameObject>>();
    public void init_district(List<DistrictData> _districtData, int district_num)
    {
        district_data = _districtData[level];
        predominant_race = (Race)Random.Range(0, (int)Race.RaceNum);
        name = "District - No:" + district_num;
        generate_race_ratio();
        if_change_predominant_race();//set predominant_race_num

        all_people_lst.Add(black_people_lst);
        all_people_lst.Add(white_people_lst);
        all_people_lst.Add(asian_people_lst);
        all_people_lst.Add(halfcast_people_lst);
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

    public bool if_change_predominant_race()
    {
        switch (predominant_race)
        {
            case Race.Black:
                predominant_race_num = black_people_lst.Count;
                return black_people_lst.Count < district_data.volume / 2 ? true : false;
            case Race.White:
                predominant_race_num = white_people_lst.Count;
                return white_people_lst.Count < district_data.volume / 2 ? true : false;
            case Race.Asian:
                predominant_race_num = asian_people_lst.Count;
                return asian_people_lst.Count < district_data.volume / 2 ? true : false;
            case Race.HalfCast:
                predominant_race_num = halfcast_people_lst.Count;
                return halfcast_people_lst.Count < district_data.volume / 2 ? true : false;
            default:
                return false;
        }
    }

    //When respawn new npc, predominant race might change
    public void update_predominant_race()
    {
        if (if_change_predominant_race())
        {
            int tmp = 0;
            foreach (var i in all_people_lst)
            {
                if (i.Count > tmp)
                {
                    tmp = i.Count;
                    //TODO: change the race
                    //predominant_race = i[0].race;
                }
            }
        }
    }
}
