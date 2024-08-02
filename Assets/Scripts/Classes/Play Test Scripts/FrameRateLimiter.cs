using System;
using UnityEngine;

namespace Play_Test_Scripts
{
    public class FrameRateLimiter : MonoBehaviour
    {
        public int frameRateLimit = 100;

        private void Start()
        {
            Application.targetFrameRate = frameRateLimit;
        }
    }
}
