using Roadmans_Fortnite.Scripts.Classes.Stats.Enums;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersonData : MonoBehaviour
{
    public enum PersonRace
    {
        WHITE_RACE = 0,
        //European
        PR_BRITAIN, //58.65% -- predominant 1
        PR_IRELAND, //1.96%
        PR_FRANCE, //0.98%
        PR_GERMANY, //1.17%
        PR_ITALY, //1.17%
        PR_POLAND, //2.44%
        PR_ROMANIA, //1.66%
        //American
        PR_USA, //1.47%
        PR_BRAZIL, //0.59%
        PR_CANADA, //0.88%
        PR_COLOMBIA, //0.49%
        PR_MEXICO, //0.29%
        PR_CHILE, //0.20%

        YELLOW_RACE,
        //Eastern Asian
        PR_CHINA, //1.22% -- predominant 2
        PR_JAPAN, //0.49%
        PR_KOREA, //0.39%

        BROWN_RACE,
        //Southern Asian
        PR_INDIA, //4.89%  -- predominant 3
        PR_PAKISTAN, //2.20%
        PR_BANGLADESH, //1.47
        //MiddleEast
        PR_Iran, //0.68%
        PR_Iraq, //0.63%
        PR_Turkey, //0.88%
        PR_Syria, //0.49%
        PR_SaudiArabia, //0.34%

        BALCK_RACE,
        //African
        PR_NIGERIA, //1.96%  -- predominant 4
        PR_JAMAICA, //1.37%

        PR_NUM
    }

    private int[] white_class_ratio = { 20, 50, 30 };
    private int[] yellow_class_ratio = { 25, 55, 20 };
    private int[] brown_class_ratio = { 35, 50, 15 };
    private int[] black_class_ratio = { 55, 35, 10 };
    public enum PersonClass
    {
        PC_UPPER,
        PC_MIDDLE,
        PC_UNDER,

        PC_CLASSNUM
    }

    public enum PersonReligion
    {
        PR_CHRISTIAN, //30%
        PR_ISLAM, //13%
        PR_HINDU, //6%
        PR_SIKH, //2%
        PR_BUDDHIST, //1%
        PR_NONE, //	35%

        PR_NUM
    }

    PersonRace race;
    public PersonRace Race { get { return race; } }
    PersonRace skin_color;
    public PersonRace Skin_Color { get { return skin_color; } }

    PersonReligion religion;
    public PersonReligion Religion { get { return religion; } }

    PersonClass pclass;
    public PersonClass PClass { get { return pclass; } }

    public PersonData(PersonRace p_race, PersonReligion p_religion)
    {
        race = p_race;
        skin_color = get_skin_color_from_race(p_race);
        religion = p_religion;
        pclass = get_class_from_race(race);
    }

    public PersonRace get_skin_color_from_race(PersonRace race)
    {
        if (race > PersonRace.WHITE_RACE && race < PersonRace.YELLOW_RACE)
        {
            return PersonRace.WHITE_RACE;
        }
        else if (race > PersonRace.YELLOW_RACE && race < PersonRace.BROWN_RACE)
        {
            return PersonRace.YELLOW_RACE;
        }
        else if (race > PersonRace.BROWN_RACE && race < PersonRace.BALCK_RACE)
        {
            return PersonRace.BROWN_RACE;
        }
        else
        {
            return PersonRace.BALCK_RACE;
        }
    }

    public PersonClass determine_class_by_ratio(int[] lst_ratio)
    {
        int wealth_num = Random.Range(0, 100);//generate the wealth

        int temp = 0;
        List<int> lst_bar = new List<int>();
        foreach (int i in lst_ratio)
        {
            temp += i;
            lst_bar.Add(temp);
        }

        if (wealth_num < lst_bar[0])
        {
            return PersonClass.PC_UNDER;
        }
        else if (wealth_num < lst_bar[1])
        {
            return PersonClass.PC_MIDDLE;
        }
        else
        {
            return PersonClass.PC_UPPER;
        }
    }

    public PersonClass get_class_from_race(PersonRace race)
    {
        var skin_color = get_skin_color_from_race(race);
        switch (skin_color)
        {
            case PersonRace.WHITE_RACE:
                return determine_class_by_ratio(white_class_ratio);
            case PersonRace.YELLOW_RACE:
                return determine_class_by_ratio(yellow_class_ratio);
            case PersonRace.BROWN_RACE:
                return determine_class_by_ratio(brown_class_ratio);
            case PersonRace.BALCK_RACE:
                return determine_class_by_ratio(black_class_ratio);
            default:
                return PersonClass.PC_UNDER;
        }
    }
}
