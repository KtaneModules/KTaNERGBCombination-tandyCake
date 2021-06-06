using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using KeepCoding;

public class RGBCombinationScript : MonoBehaviour {

    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMBombModule Module;
    public KMColorblindMode Colorblind;

    public MeshRenderer[] LeftGrid;
    public MeshRenderer[] RightGrid;
    public KMSelectable[] Buttons;
    public Material[] Colors;
    public TextMesh[] leftCB;
    public TextMesh[] rightCB;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    public bool cbON;

    private bool[] pressed = new bool[4];
    private int[] leftValues = new int[4];
    private int[] rightValues = new int[4];
    private int[] finalValues = new int[4]; 
    private int[] solution = new int[4];
    List<int> allNums = Enumerable.Range(0, 8).ToList();
    int stage = 0;


    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in Buttons) 
            button.OnInteract += delegate () { ButtonPress(Buttons.IndexOf(button)); return false; };
        Module.OnActivate += delegate () { StartCoroutine(GridFadeIn(LeftGrid, leftValues, leftCB)); StartCoroutine(GridFadeIn(RightGrid, rightValues, rightCB)); };
    }

    void ButtonPress(int pos)
    {
        if (pressed[pos])
            return;
        Buttons[pos].AddInteractionPunch(0.3f);
        if (moduleSolved)
        {
            Audio.PlaySoundAtTransform("Chime" + pos, Buttons[pos].transform);
            return;
        }
        if (pos == solution[stage])
        {
            Audio.PlaySoundAtTransform("Chime" + stage, Buttons[pos].transform);
            StartCoroutine(Fade(Buttons[pos].GetComponent<MeshRenderer>(), 0.5f, null));
                        stage++;
            Debug.LogFormat("[RGB Combination #{0}] You pressed button {1}, that is correct.", moduleId, pos + 1);
            if (stage == 4)
            {
                moduleSolved = true;
                for (int i = 0; i < 4; i++)
                    pressed[i] = false;
                Module.HandlePass();
                Debug.LogFormat("[RGB Combination #{0}] Module solved!", moduleId);
            }
            if (!moduleSolved)
                pressed[pos] = true;
        }
        else
        {
            Debug.LogFormat("[RGB Combination #{0}] You pressed button {1}, strike.", moduleId, pos + 1);
            Module.HandleStrike();
        }
    }

    void Start()
    {
        cbON = Colorblind.ColorblindModeActive;
        SetCB();
        GenerateAnswer();
        LogThimgy();
    }

    void GenerateAnswer()
    {
        for (int i = 0; i < 4; i++)
        {
            leftValues[i] = allNums.PickRandom();
            allNums.Remove(leftValues[i]);
            rightValues[i] = allNums.PickRandom();
            allNums.Remove(rightValues[i]);
            finalValues[i] = leftValues[i] ^ rightValues[i];
        }
        solution = Enumerable.Range(0, 4).OrderBy(x => finalValues[x]).ToArray();
    }
    void LogThimgy()
    {
        Debug.LogFormat("[RGB Combination #{0}] The left grid's colors are {1}.", moduleId, leftValues.Select(x => Colors[x].name).Join(", "));
        Debug.LogFormat("[RGB Combination #{0}] The right grid's colors are {1}.", moduleId, rightValues.Select(x => Colors[x].name).Join(", "));
        Debug.LogFormat("[RGB Combination #{0}] The result grid's colors are {1}, which corresponds to the order {2}.", moduleId, finalValues.Select(x => Colors[x].name).Join(", "), solution.Select(x => x+1).Join());
    }

    void SetCB()
    {
        for (int i = 0; i < 4; i++)
        {
            leftCB[i].gameObject.SetActive(cbON);
            rightCB[i].gameObject.SetActive(cbON);
        }
    }

    IEnumerator GridFadeIn(MeshRenderer[] renderers, int[] values, TextMesh[] texts)
    {
        yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 0.5f));
        int[] order = Enumerable.Range(0, 4).ToArray().Shuffle();
        for (int i = 0; i < 4; i++)
        {
            int pos = order[i];
            renderers[pos].material = Colors[values[pos]];
            if (values[pos] != 0 && values[pos] != 7) //We don't need to enable cb support for black & white
                texts[pos].text = Colors[values[pos]].name.Substring(0,1);
            StartCoroutine(Fade(renderers[pos], 3, texts[pos]));
            yield return new WaitForSeconds(0.3f);
        }
    }

    IEnumerator Fade(MeshRenderer renderer, float time, TextMesh text)
    {
        Debug.Log("started");
        for (float i = 0.01f; i < time; i += Time.deltaTime)
        {
            renderer.material.SetFloat("_Lerp", i / time);
            renderer.UpdateGIMaterials();
            if (text != null)
                text.color = Color.Lerp(new Color(1, 1, 1, 0), new Color(1, 1, 1, 1), i / time);
            yield return null;
        }
        renderer.material.SetFloat("_Lerp", 1);
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} press 1234> to press those buttons in reading order. Use <!{0} colorblind> to toggle colorblind mode.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string command)
    {
        command = command.Trim().ToUpperInvariant();
        List<string> parameters = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

        if (new string[] { "COLORBLIND", "COLOURBLIND", "COLOR-BLIND", "COLOUR-BLIND", "CB", "I HATE COLORS" }.Contains(command))
        {
            yield return null;
            cbON = !cbON;
            SetCB();
            yield break;
        }

        if (parameters.First() != "PRESS")
            yield break;
        parameters.RemoveAt(0);
        if (parameters.All(param => param.All(ch => "1234".Contains(ch))))
        {
            yield return null;
            foreach (string param in parameters)
            {
                foreach (char ch in param)
                {
                    Buttons[ch - '1'].OnInteract();
                    yield return new WaitForSeconds(0.3f);
                }
            }
        }
    }

    IEnumerator TwitchHandleForcedSolve ()
    {
        while (!moduleSolved)
        {
            Buttons[solution[stage]].OnInteract();
            yield return new WaitForSeconds(0.3f);
        }
    }
}
