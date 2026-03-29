using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class cruelDividableBy2 : MonoBehaviour
{
    [SerializeField] private KMBombInfo Bomb;
    [SerializeField] private KMAudio Audio;

    [SerializeField] KMSelectable[] Buttons; // 0 - "N"; 1 - "Y"
    [SerializeField] KMSelectable DisplaySelectable;
    [SerializeField] TextMesh DisplayText;

    string[] Maze = {
        "ULD", "UR", "ULD", "UD", "UR",
        "URL", "DL", "UD", "UD", "R",
        "L", "UR", "UL", "UR", "LR",
        "LR", "LR", "LR", "DL", "DR",
        "RDL", "DL", "D", "UD", "UDR"
    };
    string[] Directions = { "Up", "Right", "Down", "Left" };

    int genAttempts = 0;
    int N;
    List<int> digitValues = new List<int>();
    List<int> digitPositions = new List<int>();
    List<int> moves = new List<int>();
    string digitSequence;
    int[] correctPresses = new int[5];
    int[] correctTimes = new int[5];
    bool success;

    bool displayReset;
    int curRow = 0;

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    void Awake()
    {
        ModuleId = ModuleIdCounter++;
        foreach (KMSelectable button in Buttons) button.OnInteract += delegate () { ButtonPress(button); return false; };
        DisplaySelectable.OnInteract += delegate () { displayReset = true; return false; };
    }

    void ButtonPress(KMSelectable button)
    {
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        button.AddInteractionPunch();
        if (ModuleSolved) return;

        int b = Buttons.IndexOf(x => x == button);
        int t = (int)Bomb.GetTime() % 60;
        string bn = b == 0 ? "N" : "Y";
        string s = t < 10 ? $":0{t}" : $":{t}";

        if (correctPresses[curRow] != b)
        {
            Log($"Pressed the \"{bn}\" button for Row {curRow}, but the \"{(b == 0 ? "Y" : "N")}\" button was expected. Strike!");
            GetComponent<KMBombModule>().HandleStrike();
        }
        else if (t % 15 != correctTimes[curRow])
        {
            Log($"Pressed the \"{bn}\" button for Row {curRow} at an incorrect time {s}. Strike!");
            GetComponent<KMBombModule>().HandleStrike();
        }
        else
        {
            Log($"Pressed the \"{bn}\" button for Row {curRow} at a correct time {s}.");
            curRow++;
            if (curRow == 5)
            {
                Log($"Pressed the correct buttons at the correct times for all five rows. Module solved!");
                StopAllCoroutines();
                ModuleSolved = true;
                DisplayText.text = "!";
                DisplayText.color = Color.green;
                GetComponent<KMBombModule>().HandlePass();
            }
            else StartCoroutine("FlashCorrect");
        }
    }

    void Start()
    {
        while (!success) Generate();
        StartCoroutine("FlashSequence");
    }

    void Generate()
    {
        genAttempts++;
        digitValues.Clear();
        digitPositions.Clear();
        moves.Clear();

        N = Rnd.Range(10, 15);
        for (int i = 0; i < N; i++)
        {
            digitValues.Add(Rnd.Range(0, 10));
            int addPosition = Rnd.Range(0, 25);
            while (digitPositions.Contains(addPosition)) addPosition = Rnd.Range(0, 25);
            digitPositions.Add(addPosition);
        }
        for (int i = 0; i < Rnd.Range(4, 7); i++)
        {
            int addMove = Rnd.Range(0, 4);
            if (i != 0) while (addMove == moves[i - 1]) addMove = Rnd.Range(0, 4);
            moves.Add(addMove);
        }

        List<int> startingDigitPositions = digitPositions.Select(x => x).ToList();
        List<List<int>> postMovePositions = new List<List<int>>();
        List<List<int>> directionTurns = new List<List<int>>();

        for (int i = 0; i < moves.Count; i++)
        {
            List<int> postMove = new List<int>();
            List<int> turns = new List<int>();
            for (int p = 0; p < N; p++)
            {
                for (int r = 0; r < 4; r++)
                {
                    bool moved = false;
                    int m = (moves[i] + r) % 4;
                    switch (m)
                    {
                        case 0: // Up
                            if (Maze[digitPositions[p]].Contains("U")) continue;
                            digitPositions[p] -= 5;
                            moved = true;
                            break;
                        case 1: // Right
                            if (Maze[digitPositions[p]].Contains("R")) continue;
                            digitPositions[p] += 1;
                            moved = true;
                            break;
                        case 2: // Down
                            if (Maze[digitPositions[p]].Contains("D")) continue;
                            digitPositions[p] += 5;
                            moved = true;
                            break;
                        case 3: // Left
                            if (Maze[digitPositions[p]].Contains("L")) continue;
                            digitPositions[p] -= 1;
                            moved = true;
                            break;
                        default:
                            break;
                    }
                    if (moved)
                    {
                        postMove.Add(digitPositions[p]);
                        turns.Add(r);
                        break;
                    }
                }
            }
            postMovePositions.Add(postMove);
            directionTurns.Add(turns);
        }
        bool valid = true;
        for (int r = 0; r < 5; r++)
        {
            valid = digitPositions.Select(x => x / 5).Contains(r);
            if (!valid) break;
        }
        if (!valid) return;

        success = true;
        Log($"(Note: all logged coordinates follow the format of \"(Row, Column)\")");
        Log($"Generated the module in {genAttempts} attempt(s).");
        digitSequence = N.ToString();
        for (int d = 0; d < N; d++) digitSequence += $"{digitValues[d]}{startingDigitPositions[d] / 5}{startingDigitPositions[d] % 5}";
        digitSequence += moves.Join("");

        Log($"Module's flashing sequence of digits is: {digitSequence}. This gives:");
        Log($"N = {N}:");
        for (int d = 0; d < N; d++) Log($"Digit #{d + 1}: {digitValues[d]}, starting at {PosToCoordinate(startingDigitPositions[d])}.");
        Log($"The Moves are: {moves.Join(", ")}:");

        for (int m = 0; m < moves.Count; m++)
        {
            Log($"Move #{m + 1} - {moves[m]} ({Directions[moves[m]]}):");
            for (int d = 0; d < N; d++)
            {
                List<string> movePossibilities = new List<string>();
                int move = moves[m];
                int t = directionTurns[m][d];
                while (t > 0)
                {
                    movePossibilities.Add($"Cannot move {Directions[move]}");
                    move = (move + 1) % 4;
                    t--;
                }
                movePossibilities.Add($"Moves {Directions[move]}");
                Log($"{digitValues[d]} at {PosToCoordinate(m == 0 ? startingDigitPositions[d] : postMovePositions[m - 1][d])}: " +
                    $"{movePossibilities.Join(" → ")} → Ends up at {PosToCoordinate(postMovePositions[m][d])}.");
            }
        }

        Log($"Determining button presses and button press times:");
        for (int r = 0; r < 5; r++)
        {
            int rowSum = 0;
            int rowPowerSum = 0;
            for (int d = 0; d < N; d++)
            {
                if (digitPositions[d] / 5 == r)
                {
                    rowSum += digitValues[d];
                    rowPowerSum += (int)Math.Pow(digitValues[d], digitPositions[d] % 5);
                }
            }
            rowSum *= 2;
            correctPresses[r] = (23 < rowSum && rowSum < 87) ? 1 : 0;
            correctTimes[r] = rowPowerSum % 15;
            Log($"Row {r}:");
            if (correctPresses[r] == 1) Log($"Doubled sum of this row's digits is {rowSum}, which falls in range 23 to 87 (exclusive), so the \"Y\" button should be pressed.");
            else Log($"Doubled sum of this row's digits is {rowSum}, which falls outside of the range 23 to 87 (exclusive), so the \"N\" button should be pressed.");
            Log($"Sum of this row's digits, each taken to the power of its column number, is {rowPowerSum}. " +
                $"Modulo 15 is {correctTimes[r]}, so the button can be pressed at {LogCorrectTimes(correctTimes[r])}.");
        }
    }

    IEnumerator FlashSequence()
    {
        while (!ModuleSolved)
        {
            foreach (char d in digitSequence)
            {
                DisplayText.text = d.ToString();
                yield return new WaitForSeconds(30 / 53f);
                DisplayText.text = "";
                yield return new WaitForSeconds(1 / 8f);
                if (displayReset)
                {
                    displayReset = false;
                    break;
                }
            }
            yield return new WaitForSeconds(3f);
        }
    }

    IEnumerator FlashCorrect()
    {
        Color[] CorrectColors =
        {
            Color.white,
            new Color(208/255f, 1, 208/255f),
            new Color(156/255f, 1, 156/255f),
            new Color(105/255f, 1, 105/255f),
            new Color(54/255f, 1, 54/255f)
        };
        DisplayText.color = CorrectColors[curRow];
        yield return new WaitForSeconds(3 / 4f);
        DisplayText.color = Color.white;
    }

    void Log(object arg)
    {
        Debug.Log($"[Cruel Dividable By 2 #{ModuleId}] {arg}");
    }

    string PosToCoordinate(int p)
    {
        return $"({p / 5}, {p % 5})";
    }

    string LogCorrectTimes(int n)
    {
        string[] l = new string[4];
        l[0] = $":{(n < 10 ? "0" : "")}{n}";
        l[1] = $":{n + 15}";
        l[2] = $":{n + 30}";
        l[3] = $":{n + 45}";
        return l.Join(", ");
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} y/n ##> to press the ""Y""/""N"" button when the bomb timer's seconds are ##. Specify multiple press times via spaces (e.g. <!{0} y 00 01 02 03>). Use <!{0} reset> to reset the flashing sequence of digits.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
		var commandArgs = Command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
        if (commandArgs.Length < 1) yield return "sendtochaterror!h Invalid command!";
        switch (commandArgs[0])
        {
            case "RESET":
                if (commandArgs.Length != 1) yield return "sendtochaterror!h Invalid command!";
                yield return null;
                DisplaySelectable.OnInteract();
                yield return new WaitForSeconds(0.5f);
                break;
            case "Y":
            case "N":
                if (commandArgs.Length < 2) yield return "sendtochaterror!h No press time(s) specified!";
                int press = commandArgs[0] == "N" ? 0 : 1;
                List<int> pressTimes = new List<int>();
                foreach (string arg in commandArgs.Skip(1))
                {
                    if (arg.Length != 2) yield return $"sendtochaterror!h Non-two-digit press time ({arg}) specified!";

                    int time;
                    if (int.TryParse(arg, out time))
                    {
                        if (0 <= time && time <= 59) pressTimes.Add(time);
                        else yield return $"sendtochaterror!h Invalid press time ({arg}) specified!";

                    }
                    else yield return $"sendtochaterror!h Invalid press time ({arg}) specified!";
                }
                while (!pressTimes.Contains((int)Bomb.GetTime() % 60)) yield return null;
                Buttons[press].OnInteract();
                yield return new WaitForSeconds(0.5f);
                break;
            default:
                yield return "sendtochaterror!h Invalid command!";
                break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        StartCoroutine("ForcedSolve");
    }

    IEnumerator ForcedSolve()
    {
        while (curRow < 5)
        {
            while ((int)Bomb.GetTime() % 15 != correctTimes[curRow]) yield return null;
            Buttons[correctPresses[curRow]].OnInteract();
        }
    }
}
