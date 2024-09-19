/////////////////////////////////////////////////////////////////////////////////
//
//  DontDestroyOnLoad.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	Keeps this GameObject alive during scene loading.
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.Shared.Game
{
    using UnityEngine;

    public class DontDestroyObject : MonoBehaviour
    {
        void Start()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}