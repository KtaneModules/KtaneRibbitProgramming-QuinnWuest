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

    public Sprite[] CarSprites;
    public Sprite LorrySprite;
    public Sprite LogLSprite;
    public Sprite LogMSprite;
    public Sprite LogRSprite;
    public Sprite TurtleSprite;
    public Sprite FrogSprite;
    public Sprite FrogJumpSprite;
    public Sprite SkullSprite;

    public SpriteRenderer[] Cars;
    public SpriteRenderer[] LogsTop;
    public SpriteRenderer[] LogsBottom;
    public SpriteRenderer[] TurtlesTop;
    public SpriteRenderer[] TurtlesBottom;
    public SpriteRenderer Frog;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private bool _moduleSolved;

    private int _ribbitId;
    private static int _ribbitIdCounter = 1;

    private Coroutine _dpadAnimation;
    private const float _dpadTiltAngle = 4f;
    private static readonly Vector3[] _dpadTiltDirs = new Vector3[] { new Vector3(_dpadTiltAngle, 0, 0), new Vector3(0, 0, -_dpadTiltAngle), new Vector3(-_dpadTiltAngle, 0, 0), new Vector3(0, 0, _dpadTiltAngle) };

    private readonly int[] _startingPositions = new int[8];
    private int _lorryLane;
    private readonly int[] _speeds = new int[8];
    private readonly int[] _riverSizes = new int[4];
    private readonly Sprite[] _overflowSprites = new Sprite[8];
    private readonly List<Move> _program = new List<Move>();
    private bool _programRunning;
    private float _programStartTime;
    private bool _frogIsDead;
    private bool _avoidStrikeBecauseTpAutosolver;
    private readonly SpriteRenderer[] _duplicates = new SpriteRenderer[9];   // for wraparound; index 8 is the frog

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
        _ribbitId = _ribbitIdCounter++;
        Module.OnActivate += Activate;
        for (int i = 0; i < InputSels.Length; i++)
        {
            InputSels[i].OnInteract += InputPress((Move)i);
            InputSels[i].OnInteractEnded += InputRelease((Move)i);
        }
        StartSel.OnInteract += StartPress;
        ResetSel.OnInteract += ResetPress;
        ProgramDisplay.text = "";

        for (var i = 0; i < 8; i++)
            _startingPositions[i] = Rnd.Range(0, 7) * 10;
        _lorryLane = Rnd.Range(0, 4);
        Cars[_lorryLane].sprite = LorrySprite;
        for (var i = 0; i < 4; i++)
            _riverSizes[i] = Rnd.Range(2, 6);

        var skippedCarLane = (BombInfo.GetBatteryCount() + 3) % 4;
        var skippedRiverLane = (BombInfo.GetIndicators().Count() + 3) % 4;
        _speeds[skippedCarLane] = 1;
        _speeds[skippedRiverLane + 4] = 1;
        var sn = BombInfo.GetSerialNumber().Select(ch => ch >= 'A' && ch <= 'Z' ? ch - 'A' + 1 : ch == '0' ? 10 : ch - '0').ToArray();
        for (var i = 0; i < 6; i++)
            _speeds[i + (i >= skippedCarLane ? 1 : 0) + (i - 3 >= skippedRiverLane ? 1 : 0)] = sn[i];
        for (var lane = 0; lane < 8; lane++)
        {
            var direction = -2 * (lane < 4 ? Rnd.Range(0, 2) : lane % 2) + 1;
            _speeds[lane] *= direction;
            if (lane < 4 && direction > 0)
                setScaleX(Cars[lane].transform, -.095f);
        }

        setupSprites(0);

        for (var lane = 0; lane < 8; lane++)
        {
            Debug.LogFormat(@"[Ribbit Programming #{4}] Lane {0}: {3}; start position {1}, speed {2}",
                lane + 1,
                _startingPositions[lane],
                _speeds[lane],
                lane == _lorryLane ? "lorry" : lane < 4 ? "car" : string.Format(lane == 4 || lane == 6 ? "log of length {0}" : "{0} turtles", _riverSizes[lane - 4]),
                _moduleId);

            if (lane >= 4)
            {
                var arr =
                    lane == 4 ? LogsBottom :
                    lane == 5 ? TurtlesBottom :
                    lane == 6 ? LogsTop : TurtlesTop;
                for (var i = _riverSizes[lane - 4]; i < arr.Length; i++)
                {
                    Destroy(arr[i].gameObject);
                    arr[i] = null;
                }
            }
        }
    }

    private void Activate()
    {
        if (_ribbitId == 1)
            Audio.PlaySoundAtTransform("Startup", transform);
    }

    private void OnDestroy()
    {
        _ribbitId = 0;
    }

    private void setX(SpriteRenderer sr, float x, int lane)
    {
        var pos = sr.transform.localPosition;
        pos.x = -.045f + .015f * (x + (lane == _lorryLane ? .5f : 0));
        sr.transform.localPosition = pos;

        if (x > (lane == _lorryLane ? 5 : 6))
        {
            if (_duplicates[lane] == null)
                _duplicates[lane] = Instantiate(sr, sr.transform.parent);
            pos = _duplicates[lane].transform.localPosition;
            pos.x = -.045f + .015f * (x + (lane == _lorryLane ? .5f : 0) - 7);
            _duplicates[lane].transform.localPosition = pos;
            _duplicates[lane].sprite = sr.sprite;
            _duplicates[lane].gameObject.SetActive(true);
        }
    }

    private void setY(SpriteRenderer sr, float y)
    {
        var pos = sr.transform.localPosition;
        pos.z = .075f - .015f * y;
        sr.transform.localPosition = pos;
    }

    private void setScaleX(Transform transform, float x)
    {
        var scale = transform.localScale;
        scale.x = x;
        transform.localScale = scale;
    }

    private void setupSprites(float time, float frogX = 30, int frogY = 100, bool frogDead = false)
    {
        for (var lane = 0; lane < 8; lane++)
        {
            if (_duplicates[lane] != null)
                _duplicates[lane].gameObject.SetActive(false);
            var xPos = (((_startingPositions[lane] + time * _speeds[lane]) * .1f) % 7 + 7) % 7;
            if (lane < 4)   // car/lorry
                setX(Cars[lane], xPos, lane);
            else    // log or turtles
            {
                var arr =
                    lane == 4 ? LogsBottom :
                    lane == 5 ? TurtlesBottom :
                    lane == 6 ? LogsTop : TurtlesTop;
                for (var i = 0; i < _riverSizes[lane - 4]; i++)
                {
                    arr[i].sprite = lane % 2 != 0 ? TurtleSprite : i == 0 ? LogLSprite : i == _riverSizes[lane - 4] - 1 ? LogRSprite : LogMSprite;
                    setX(arr[i], (xPos + i) % 7, lane);
                }
            }
        }
        setX(Frog, frogX * .1f, 8);
        setY(Frog, frogY * .1f);
        Frog.sprite = frogDead ? SkullSprite : FrogSprite;
    }

    private KMSelectable.OnInteractHandler InputPress(Move move)
    {
        return delegate ()
        {
            if (_dpadAnimation != null)
                StopCoroutine(_dpadAnimation);
            if (move != Move.Idle)
                _dpadAnimation = StartCoroutine(DPadPressAnimation((int)move, true));

            if (!_moduleSolved && !_programRunning && !_frogIsDead)
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
                _dpadAnimation = StartCoroutine(DPadPressAnimation((int)move, false));
        };
    }

    private bool StartPress()
    {
        if (_programRunning || _moduleSolved || _frogIsDead)
            return false;
        _programStartTime = Time.time;
        Debug.LogFormat("[Ribbit Programming #{0}] Entered program: {1}", _moduleId, _program.Select(i => i == Move.Idle ? '-' : i.ToString()[0]).ToArray().Join(""));
        StartCoroutine(Run());
        return false;
    }

    private IEnumerator Run()
    {
        _programRunning = true;
        Audio.PlaySoundAtTransform("PrepProgram", transform);
        int mIx = -1;
        while (true)
        {
            var time = (Time.time - _programStartTime) * 1.4f;
            var frogX = 30f;
            var frogY = 100;
            for (var ip = 0; ip < (int)time && ip < _program.Count; ip++)
            {
                var frgLane = frogY == 0 || frogY == 50 || frogY == 100 ? (int?)null : frogY < 50 ? (8 - frogY / 10) : (9 - frogY / 10);
                if (frgLane != null && frgLane >= 4)
                    frogX = Math.Max(0, Math.Min(60, frogX + _speeds[frgLane.Value]));
                switch (_program[ip])
                {
                    case Move.Up:
                        if (frogY == 0)
                        {
                            Debug.LogFormat("[Ribbit Programming #{0}] You died because you moved up from the top row.", _moduleId);
                            goto dead;
                        }
                        frogY -= 10;
                        if (mIx < ip) { mIx = ip; PlayFrogMoveSound(); }
                        break;

                    case Move.Right:
                        if (frogX == 60)
                        {
                            Debug.LogFormat("[Ribbit Programming #{0}] You died because you moved right from the rightmost column.", _moduleId);
                            goto dead;
                        }
                        frogX += 10;
                        if (mIx < ip) { mIx = ip; PlayFrogMoveSound(); }
                        break;

                    case Move.Down:
                        if (frogY == 100)
                        {
                            Debug.LogFormat("[Ribbit Programming #{0}] You died because you moved down from the bottom row.", _moduleId);
                            goto dead;
                        }
                        frogY += 10;
                        if (mIx < ip) { mIx = ip; PlayFrogMoveSound(); }
                        break;

                    case Move.Left:
                        if (frogX == 0)
                        {
                            Debug.LogFormat("[Ribbit Programming #{0}] You died because you moved left from the leftmost column.", _moduleId);
                            goto dead;
                        }
                        frogX -= 10;
                        if (mIx < ip) { mIx = ip; PlayFrogMoveSound(); }
                        break;

                    case Move.Idle:
                        if (ip >= 5 && _program.Skip(ip - 5).Take(5).All(instr => instr == Move.Idle))
                        {
                            Debug.LogFormat("[Ribbit Programming #{0}] You died of boredom.", _moduleId);
                            goto dead;
                        }
                        break;
                }
            }

            if (frogY == 0 && !(
                (frogX > 5 && frogX < 15) ||
                (frogX > 25 && frogX < 35) ||
                (frogX > 45 && frogX < 55)))
            {
                Debug.LogFormat("[Ribbit Programming #{0}] You died because you hit one of the barriers on the top row.", _moduleId);
                goto dead;
            }

            var frogLane = frogY == 0 || frogY == 50 || frogY == 100 ? (int?)null : frogY < 50 ? (8 - frogY / 10) : (9 - frogY / 10);

            var timeWithinUnit = time % 1;

            if (frogLane != null)
            {
                if (frogLane.Value >= 4)
                    frogX = Math.Max(0, Math.Min(60, frogX + (timeWithinUnit * _speeds[frogLane.Value])));

                var result = doesFrogDie(time, frogX, frogLane.Value);
                if (result == DeathResult.Roadkill)
                    Debug.LogFormat("[Ribbit Programming #{0}] You died because you got hit by the {1} in lane {2}.", _moduleId, frogLane == _lorryLane ? "lorry" : "car", frogLane + 1);
                else if (result == DeathResult.Drown)
                    Debug.LogFormat("[Ribbit Programming #{0}] You died because you jumped in the water in lane {1}.", _moduleId, frogLane + 1);
                if (result == DeathResult.Roadkill || result == DeathResult.Drown)
                    goto dead;
            }

            if (time >= _program.Count)
            {
                if (frogY != 0)
                {
                    Debug.LogFormat("[Ribbit Programming #{0}] You died because the program ended before the frog reached a goal.", _moduleId);
                    goto dead;
                }
                if (!_moduleSolved)
                {
                    Debug.LogFormat("[Ribbit Programming #{0}] Module solved!", _moduleId);
                    frogX = (int)(frogX + 5) / 10 * 10;
                    Module.HandlePass();
                    _moduleSolved = true;
                    Audio.PlaySoundAtTransform("Solve", transform);
                }
            }

            setupSprites(time, frogX, frogY);
            yield return null;
            continue;

            dead:
            _frogIsDead = true;
            setupSprites(time, frogX, frogY, frogDead: true);
            Audio.PlaySoundAtTransform("FrogDead", transform);
            if (!_avoidStrikeBecauseTpAutosolver)
                Module.HandleStrike();
            break;
        }
        _programRunning = false;
    }

    enum DeathResult
    {
        Survive,
        Roadkill,
        Drown
    }

    private DeathResult doesFrogDie(float time, float frogX, int frogLane)
    {
        var itemX = ((_startingPositions[frogLane] + time * _speeds[frogLane]) % 70 + 70) % 70;
        var itemWidth = frogLane == _lorryLane ? 20 : frogLane < 4 ? 10 : _riverSizes[frogLane - 4] * 10;
        if (frogLane < 4)
        {
            if ((frogX + 10 > itemX && frogX < itemX + itemWidth) || (frogX + 10 > itemX - 70 && frogX < itemX + itemWidth - 70))
                return DeathResult.Roadkill;
        }
        else
        {
            if (!((frogX + 10 > itemX && frogX < itemX + itemWidth) || (frogX + 10 > itemX - 70 && frogX < itemX + itemWidth - 70)))
                return DeathResult.Drown;
        }
        return DeathResult.Survive;
    }

    private bool ResetPress()
    {
        if (_programRunning || _moduleSolved)
            return false;
        _program.Clear();
        _frogIsDead = false;
        updateProgramDisplay();
        setupSprites(0);
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

    private void PlayFrogMoveSound()
    {
        Audio.PlaySoundAtTransform("FrogMove", transform);
    }

#pragma warning disable 0414
    private static readonly string TwitchHelpMessage = @"!{0} UDL-R [enter program commands] | !{0} reset | !{0} start";
#pragma warning restore 0414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        Match m;
        if ((m = Regex.Match(command, @"^\s*([-udlr]+)\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success)
        {
            yield return null;
            foreach (var ch in m.Groups[1].Value.ToUpperInvariant())
            {
                InputSels["URDL-".IndexOf(ch)].OnInteract();
                yield return new WaitForSeconds(.05f);
            }
        }
        else if (Regex.IsMatch(command, @"^\s*reset\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
        {
            yield return null;
            ResetSel.OnInteract();
            yield return new WaitForSeconds(.1f);
        }
        else if (Regex.IsMatch(command, @"^\s*(start|go)\s*$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
        {
            yield return null;
            StartSel.OnInteract();
            yield return new WaitForSeconds(.1f);
        }
    }

    struct QueueItem
    {
        public int Time;
        public int FrogX;
        public int FrogY;
        public Move Move;
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        _avoidStrikeBecauseTpAutosolver = true;
        while (_programRunning)
            yield return true;
        if (_moduleSolved)
            yield break;

        yield return new WaitForSeconds(.2f);
        ResetSel.OnInteract();
        yield return new WaitForSeconds(.1f);

        var currentState = new QueueItem { FrogX = 30, FrogY = 100, Time = 0 };
    }
}
