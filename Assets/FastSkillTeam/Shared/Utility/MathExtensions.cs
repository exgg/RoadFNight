/////////////////////////////////////////////////////////////////////////////////
//
//  MathExtensions.cs
//  @ FastSkillTeam Productions. All Rights Reserved.
//  http://www.fastskillteam.com/
//  https://twitter.com/FastSkillTeam
//  https://www.facebook.com/FastSkillTeam
//
//	Description:	A helper script with math extensions.
//
/////////////////////////////////////////////////////////////////////////////////

namespace FastSkillTeam.Shared.Utility
{
    public static class MathExtensions
    {
        public static bool IsOdd(this int value)
        {
            return value % 2 != 0;
        }
    }
}