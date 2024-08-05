using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestGen : MonoBehaviour
{
   [SerializeField]
   private CityGen cityGen;


   private void Start()
   {
      cityGen.Generate();
   }
}
