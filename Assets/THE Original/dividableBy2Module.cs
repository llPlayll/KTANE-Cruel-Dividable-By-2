using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class dividableBy2Module : MonoBehaviour
{

    public KMBombInfo Bomb;
    public KMAudio Audio;

    public KMSelectable[] Buttons;
    public TextMesh DisplayText;

    int ButtonToPress; 

    static int ModuleIdCounter = 1;
    int ModuleId;
    private bool ModuleSolved;

    void Awake ()
    {
        ModuleId = ModuleIdCounter++;

        foreach (KMSelectable Button in Buttons)
        {
            Button.OnInteract += delegate(){ButtonPress(Button); return false;};
        }
    }

    void ButtonPress (KMSelectable Button)
    {
        Button.AddInteractionPunch();
        if (ModuleSolved)
        {
            return;
        }
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Button.transform);
        for (int i = 0; i < 2; i++)
        {
            if (Button == Buttons[i])
            {
                if (ButtonToPress == i)
                {
                    GetComponent<KMBombModule>().HandlePass();
                    ModuleSolved = true;
                }
                else
                {
                    GetComponent<KMBombModule>().HandleStrike();
                }
            }
        }
   }

    void Start ()
    {
        int RandomNumber = Rnd.Range(0, 10);
        DisplayText.text = RandomNumber.ToString();

        Debug.LogFormat("[Dividable By Two #{0}] The number displayed is " + RandomNumber, ModuleId);

        if (RandomNumber % 2 == 0)
        {
            ButtonToPress = 0;
            Debug.LogFormat("[Dividable By Two #{0}] " + RandomNumber + " is dividable by 2, so the Y button should be pressed", ModuleId);
        }
        else
        {
            ButtonToPress = 1;
            Debug.LogFormat("[Dividable By Two #{0}] " + RandomNumber + " is not dividable by 2, so the N button should be pressed", ModuleId);
        }
    }

   void Update ()
   {

   }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} Y to press the Yes button, and !{0} N to press the No button.";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string Command)
    {
        Command = Command.Trim().ToUpper();
        yield return null;
        if (!"YN".Contains(Command) || Command.Length != 1)
        {
            yield return "sendtochaterror I don't understand!";
            yield break;
        }
        if (Command == "Y")
        {
            Buttons[0].OnInteract();
        }
        else
        {
            Buttons[1].OnInteract();
        }
    }
    IEnumerator TwitchHandleForcedSolve ()
    {
        Buttons[ButtonToPress].OnInteract();
        yield return null;
    }
}
