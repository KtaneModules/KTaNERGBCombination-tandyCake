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

    public MeshRenderer[] leds;
    public KMSelectable[] buttons;
    public TextMesh[] cbTexts;
    public MeshRenderer[] stageLEDs;
    public Material unlit;
    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;
    public bool cbON;
    private Color[] colors = { new Color32(0x31, 0x31, 0x31, 0xFF), Color.blue, Color.green, Color.cyan, Color.red, Color.magenta, Color.yellow, Color.white };
    private string[] colorNames = { "Black", "Blue", "Green", "Cyan", "Red", "Magenta", "Yellow", "White" };

    private int[] dispColors = new int[8];
    private int[] solution = new int[4];
    int stage = 0;


    void Awake()
    {
        moduleId = moduleIdCounter++;
        for (int i = 0; i < 8; i++)
        {
            int ix = i;
            buttons[ix].OnInteract += delegate () { ButtonPress(ix); return false; };
        }
        Module.OnActivate += DoFade;
    }

    void ButtonPress(int pos)
    {
        buttons[pos].AddInteractionPunch(0.5f);
        if (moduleSolved)
            Audio.PlaySoundAtTransform("Chime" + ((pos % 4) + 1), transform);
        else if (dispColors[pos] == solution[stage])
        {
            Debug.LogFormat("[RGB Combination #{0}] Pressed {1}.", moduleId, colorNames[dispColors[pos]]);
            StartCoroutine(Fade(stageLEDs[stage++], Color.white, 0.5f));
            Audio.PlaySoundAtTransform("Chime" + stage, transform);
            if (stage == 4)
            {
                moduleSolved = true;
                Debug.LogFormat("[RGB Combination #{0}] Module solved.", moduleId);
                Module.HandlePass();
            }
        }
        else
        {
            Debug.LogFormat("[RGB Combination #{0}] Pressed {1} while expected {2}. Strike!", moduleId, colorNames[dispColors[pos]], colorNames[solution[stage]]);
            Module.HandleStrike();
        }
    }
    void Start()
    {
        GenerateAnswer();
        DoLogging();
    }
    void GenerateAnswer()
    {
        dispColors = Enumerable.Range(0, 8).ToArray().Shuffle();
        for (int i = 0; i < 4; i++)
            solution[i] = dispColors[i] ^ dispColors[i + 4];
    }
    void DoLogging()
    {
        Debug.LogFormat("[RGB Combination #{0}] The left grid has colors {1}.", moduleId, dispColors.Take(4).Select(x => colorNames[x]).Join(", "));
        Debug.LogFormat("[RGB Combination #{0}] The right grid has colors {1}.", moduleId, dispColors.Skip(4).Select(x => colorNames[x]).Join(", "));
        Debug.LogFormat("[RGB Combination #{0}] The solution colors are {1}.", moduleId, solution.Select(x => colorNames[x]).Join(", "));
    }
    void DoFade()
    {
        StartCoroutine(GridFadeIn(leds.Take(4).ToArray(), dispColors.Take(4).ToArray(), cbTexts.Take(4).ToArray()));
        StartCoroutine(GridFadeIn(leds.Skip(4).ToArray(), dispColors.Skip(4).ToArray(), cbTexts.Skip(4).ToArray()));
    }

    void ToggleCB()
    {
        cbON = !cbON;
        for (int i = 0; i < 8; i++)
            if (cbON && dispColors[i] != 7 && dispColors[i] != 0)
                cbTexts[i].text = colorNames[dispColors[i]][0].ToString();
    }

    IEnumerator GridFadeIn(MeshRenderer[] renderers, int[] values, TextMesh[] texts = null)
    {
        for (int i = 0; i < 4; i++)
        {
            StartCoroutine(Fade(renderers[i], colors[values[i]], 1.0f, texts[i]));
            yield return new WaitForSeconds(0.75f);
        }
        if (Colorblind.ColorblindModeActive && !cbON)
            ToggleCB();
    }

    IEnumerator Fade(MeshRenderer renderer, Color target, float time, TextMesh text = null)
    {
        renderer.material = unlit;
        float delta = 0;
        while (delta < 1)
        {
            delta += Time.deltaTime / time;
            renderer.material.color = Color.Lerp(unlit.color, target, delta);
            if (text != null)
                text.color = Color.Lerp(Color.clear, Color.white, delta);
            yield return null;
        }
    }

    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use <!{0} press RGBCMYWK> to press those colors. Every color is abbreviated by their first letter except black. Black is identified by 'K'. Use <!{0} colorblind> to toggle colorblind mode.";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand (string command)
    {
        command = command.Trim().ToUpperInvariant();
        Match m = Regex.Match(command, @"^(?:PRESS\s+)?((?:[KBGCRMYW]\s*)+)$");
        if (command.EqualsAny("COLORBLIND", "COLOURBLIND", "COLOR-BLIND", "COLOUR-BLIND", "CB", "I HATE COLORS"))
        {
            yield return null;
            ToggleCB();
        }
        else if (m.Success)
        {
            yield return null;
            foreach (int color in m.Groups[1].Value.Where(x => x != ' ').Select(x => "KBGCRMYW".IndexOf(x)))
            {
                buttons[dispColors.IndexOf(color)].OnInteract();
                yield return new WaitForSeconds(0.3f);
            }
        }

        
    }

    IEnumerator TwitchHandleForcedSolve ()
    {
        for (int i = stage; i < 4; i++)
        {
            buttons[dispColors.IndexOf(solution[i])].OnInteract();
            yield return new WaitForSeconds(0.3f);
        }
    }
}
