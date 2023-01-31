using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;
using KModkit;

public class RibbitProgrammingScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;

    public KMSelectable[] InputSels;
    public KMSelectable StartSel;
    public KMSelectable ResetSel;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int i = 0; i < InputSels.Length; i++)
            InputSels[i].OnInteract += InputPress(i);
        StartSel.OnInteract += StartPress;
        ResetSel.OnInteract += ResetPress;
    }

    private KMSelectable.OnInteractHandler InputPress(int i)
    {
        return delegate ()
        {

            return false;
        };
    }

    private bool StartPress()
    {

        return false;
    }

    private bool ResetPress()
    {

        return false;
    }
}
