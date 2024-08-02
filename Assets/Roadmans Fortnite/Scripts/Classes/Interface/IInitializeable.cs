using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public interface IInitializeable
{ 
    public void OnInitialize();
}

public interface ITick
{
    public void OnTick();
}

public interface ILate
{
    public void OnLate();
}

