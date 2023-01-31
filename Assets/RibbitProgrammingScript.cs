using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

using Rnd = UnityEngine.Random;

public class RibbitProgrammingScript : MonoBehaviour
{
    public KMBombModule Module;
    public KMBombInfo BombInfo;
    public KMAudio Audio;

    public KMSelectable[] InputSels; // 0 = Up, 1 = Right, 2 = Down, 3 = Left, 4 = Wait
    public KMSelectable StartSel;
    public KMSelectable ResetSel;
    public GameObject DPadObject;
    public TextMesh ProgramDisplay;

    public Sprite[] _carSprites;
    public Sprite _lorrySprite;
    public Sprite _logLSprite;
    public Sprite _logMSprite;
    public Sprite _logRSprite;
    public Sprite _turtleSprite;
    public Sprite _skullSprite;

    public SpriteRenderer[] _cars;
    public SpriteRenderer[] _logsTop;
    public SpriteRenderer[] _logsBottom;
    public SpriteRenderer[] _turtlesTop;
    public SpriteRenderer[] _turtlesBottom;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private Coroutine _dpadAnimation;
    private const float _dpadTiltAngle = 4f;
    private static readonly Vector3[] _dpadTiltDirs = new Vector3[] { new Vector3(_dpadTiltAngle, 0, 0), new Vector3(0, 0, -_dpadTiltAngle), new Vector3(-_dpadTiltAngle, 0, 0), new Vector3(0, 0, _dpadTiltAngle) };

    private int[] _startingPositions = new int[8];
    private int _lorryLane;
    private int[] _speeds = new int[8];
    private readonly List<Move> _program = new List<Move>();
    private bool _programRunning;

    enum Move
    {
        Up,
        Right,
        Down,
        Left,
        Idle
    }

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        for (int i = 0; i < InputSels.Length; i++)
        {
            InputSels[i].OnInteract += InputPress((Move) i);
            InputSels[i].OnInteractEnded += InputRelease((Move) i);
        }
        StartSel.OnInteract += StartPress;
        ResetSel.OnInteract += ResetPress;
        ProgramDisplay.text = "";

        for (var i = 0; i < 8; i++)
            _startingPositions[i] = Rnd.Range(0, 70);
        _lorryLane = Rnd.Range(0, 4);

        var skippedCarLane = (BombInfo.GetBatteryCount() + 3) % 4;
        var skippedRiverLane = (BombInfo.GetIndicators().Count() + 3) % 4;
        _speeds[skippedCarLane] = 1;
        _speeds[skippedRiverLane] = 1;
        var sn = BombInfo.GetSerialNumber().Select(ch => ch >= 'A' && ch <= 'Z' ? ch - 'A' + 1 : ch == '0' ? 10 : ch - '0').ToArray();
        for (var i = 0; i < 6; i++)
            _speeds[i + (i >= skippedCarLane ? 1 : 0) + (i - 3 >= skippedRiverLane ? 1 : 0)] = sn[i];

        setupSprites(0);
    }

    private void setupSprites(float time)
    {
        for (var carLane = 0; carLane < 4; carLane++)
        {
            var pos = _cars[carLane].transform.localPosition;
            pos.x = -.045f + .0015f * (_startingPositions[carLane] + time * _speeds[carLane] + (carLane == _lorryLane ? 5 : 0));
            _cars[carLane].transform.localPosition = pos;
        }
    }

    private KMSelectable.OnInteractHandler InputPress(Move move)
    {
        return delegate ()
        {
            if (_dpadAnimation != null)
                StopCoroutine(_dpadAnimation);
            if (move != Move.Idle)
                _dpadAnimation = StartCoroutine(DPadPressAnimation((int) move, true));

            if (!_moduleSolved && !_programRunning)
            {
                _program.Add(move);
                updateProgramDisplay();
            }

            return false;
        };
    }

    private void updateProgramDisplay()
    {
        if (_program.Count <= 5)
            ProgramDisplay.text = _program.Select(p => p == Move.Idle ? '-' : p.ToString()[0]).Join("");
        else
            ProgramDisplay.text = "…" + _program.Skip(_program.Count - 4).Select(p => p == Move.Idle ? '-' : p.ToString()[0]).Join("");
    }

    private Action InputRelease(Move move)
    {
        return delegate ()
        {
            if (_dpadAnimation != null)
                StopCoroutine(_dpadAnimation);
            if (move != Move.Idle)
                _dpadAnimation = StartCoroutine(DPadPressAnimation((int) move, false));
        };
    }

    private bool StartPress()
    {

        return false;
    }

    private bool ResetPress()
    {
        _program.Clear();
        updateProgramDisplay();
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
