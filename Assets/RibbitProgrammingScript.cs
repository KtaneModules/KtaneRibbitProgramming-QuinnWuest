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

    public KMSelectable[] InputSels; // 0 = Up, 1 = Right, 2 = Down, 3 = Left, 4 = Wait
    public KMSelectable StartSel;
    public KMSelectable ResetSel;
    public GameObject DPadObject;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private Coroutine _dpadAnimation;
    private const float _dpadTiltAngle = 4f;
    private static readonly Vector3[] _dpadTiltDirs = new Vector3[] { new Vector3(_dpadTiltAngle, 0, 0), new Vector3(0, 0, -_dpadTiltAngle), new Vector3(-_dpadTiltAngle, 0, 0), new Vector3(0, 0, _dpadTiltAngle) };

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int i = 0; i < InputSels.Length; i++)
        {
            InputSels[i].OnInteract += InputPress(i);
            InputSels[i].OnInteractEnded += InputRelease(i);
        }
        StartSel.OnInteract += StartPress;
        ResetSel.OnInteract += ResetPress;
    }

    private KMSelectable.OnInteractHandler InputPress(int i)
    {
        return delegate ()
        {
            if (_dpadAnimation != null)
                StopCoroutine(_dpadAnimation);
            if (i != 4)
                _dpadAnimation = StartCoroutine(DPadPressAnimation(i, true));
            return false;
        };
    }

    private Action InputRelease(int i)
    {
        return delegate ()
        {
            if (_dpadAnimation != null)
                StopCoroutine(_dpadAnimation);
            if (i != 4)
                _dpadAnimation = StartCoroutine(DPadPressAnimation(i, false));
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

    private IEnumerator DPadPressAnimation(int btn, bool pressIn)
    {
        if (pressIn)
            DPadObject.transform.localEulerAngles = new Vector3(0, 0, 0);
        var curAngles = DPadObject.transform.localEulerAngles;
        if (curAngles.x > _dpadTiltAngle) curAngles.x -= 360f;
        if (curAngles.z > _dpadTiltAngle) curAngles.z -= 360f;
        var duration = 0.1f;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            DPadObject.transform.localEulerAngles = new Vector3(
                Easing.InOutQuad(elapsed, curAngles.x, pressIn ? _dpadTiltDirs[btn].x : 0f, duration),
                0f,
                Easing.InOutQuad(elapsed, curAngles.z, pressIn ? _dpadTiltDirs[btn].z : 0f, duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        DPadObject.transform.localEulerAngles = new Vector3(pressIn ? _dpadTiltDirs[btn].x : 0f, 0f, pressIn ? _dpadTiltDirs[btn].z : 0f);
    }

#pragma warning disable 0414
    private static readonly string TwitchHelpMessage = @"cuarenta y siete";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        yield break;
    }
}
