﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Twodoku;
using UnityEngine;
using UnityEngine.UI;
using Rnd = UnityEngine.Random;

/// <summary>
/// On the Subject of Twodoku
/// Created by Timwi
/// </summary>
public class TwodokuModule : MonoBehaviour
{
    public KMBombInfo Bomb;
    public KMBombModule Module;
    public KMAudio Audio;
    public KMRuleSeedable RuleSeedable;

    public Sprite HighlightSprite;
    public Sprite[] Numbers;
    public Sprite[] Symbols;
    public Sprite[] Letters;
    public Image SymbolTemplate;

    public KMSelectable[] Buttons;
    public TextMesh[] ButtonLabels;
    public TextMesh SolutionDisplay;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;

    private string _solutionWord;
    private int _solutionIx = 0;
    private int _inputLevel = 0;
    private bool _activated;

    private List<Image> _instantiatedSymbols = [];
    private List<Image> _instantiatedMarkers = [];

    private Func<int, int> _getTypeARegion;
    private Func<int, int> _getTypeBRegion;

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        for (var i = 0; i < 3; i++)
            ButtonLabels[i].text = "";
        for (int i = 0; i < Buttons.Length; i++)
            Buttons[i].OnInteract += ButtonPress(i);
        SolutionDisplay.text = "";
        SymbolTemplate.gameObject.SetActive(false);
        Module.OnActivate += delegate { _activated = true; };

        var rnd = RuleSeedable.GetRNG();
        Debug.Log($"[Twodoku #{_moduleId}] Using rule seed: {rnd.Seed}.");
        if (rnd.Seed == 1)
        {
            _getTypeARegion = cell => cell % 6 / 3 + 2 * (cell / 12);   // “wide”
            _getTypeBRegion = cell => cell % 6 / 2 + 3 * (cell / 18);   // “tall”
        }
        else
        {
            var regionsA = GenerateRegions(rnd);
            var regionsB = GenerateRegions(rnd);
            var regionAMap = Ut.NewArray(36, cell => regionsA.IndexOf(reg => reg.Contains(cell)));
            var regionBMap = Ut.NewArray(36, cell => regionsB.IndexOf(reg => reg.Contains(cell)));
            _getTypeARegion = cell => regionAMap[cell];
            _getTypeBRegion = cell => regionBMap[cell];
            Debug.Log($"<Twodoku #{_moduleId}> Type-A regions: {regionsA.Select(r => r.Select(cell => $"{(char) ('A' + cell % 6)}{cell / 6 + 1}").JoinString(", ")).JoinString(" // ")}");
            Debug.Log($"<Twodoku #{_moduleId}> Type-B regions: {regionsB.Select(r => r.Select(cell => $"{(char) ('A' + cell % 6)}{cell / 6 + 1}").JoinString(", ")).JoinString(" // ")}");
        }

        StartCoroutine(GeneratePuzzle());
    }

    private KMSelectable.OnInteractHandler ButtonPress(int btn)
    {
        return delegate
        {
            Buttons[btn].AddInteractionPunch();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, Buttons[btn].transform);
            if (_moduleSolved || _solutionWord == null)
                return false;

            if (btn == GetCorrectButtonToPress())
            {
                _inputLevel++;
                Audio.PlaySoundAtTransform("input" + _inputLevel, transform);
                if (_inputLevel == 3)
                {
                    _solutionIx++;
                    _inputLevel = 0;
                    SolutionDisplay.text = $"<color=#FF4B4B>{_solutionWord.Substring(0, _solutionIx)}</color><color=#888>│</color>";
                }
                if (_solutionIx == _solutionWord.Length)
                {
                    _moduleSolved = true;
                    Module.HandlePass();
                    StartCoroutine(SolveAnim());
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                    Debug.Log($"[Twodoku #{_moduleId}] Module solved.");
                    SolutionDisplay.text = $"<color=#3F3>{_solutionWord}</color>";
                    for (var i = 0; i < 3; i++)
                    {
                        ButtonLabels[i].text = "✓";
                        ButtonLabels[i].color = Color.green;
                    }
                }
                else
                {
                    for (var i = 0; i < 3; i++)
                    {
                        ButtonLabels[i].text = _inputLevel switch
                        {
                            0 => _buttonLevel0[i],
                            1 => ((_solutionWord[_solutionIx] - 'A') / 9).Apply(s => Enumerable.Range(0, s == 2 && i == 2 ? 2 : 3).Select(ix => $"{(char) ('A' + s * 9 + 3 * i + ix)}").JoinString()),
                            _ => ((_solutionWord[_solutionIx] - 'A') / 3).Apply(s => s == 8 && i == 2 ? "" : $"{(char) ('A' + s * 3 + i)}")
                        };
                        ButtonLabels[i].fontSize = _inputLevel == 1 ? 92 : 128;
                    }
                }
            }
            else
            {
                Debug.Log($@"[Twodoku #{_moduleId}] After input “{_solutionWord.Substring(0, _solutionIx)}”, you pressed {ButtonLabels[btn].text} which is incorrect. Strike.");
                Module.HandleStrike();
            }

            return false;
        };
    }

    private int GetCorrectButtonToPress()
    {
        var reqLetter = _solutionWord[_solutionIx] - 'A';
        var expectedButton = _inputLevel switch
        {
            0 => reqLetter / 9,
            1 => (reqLetter / 3) % 3,
            _ => reqLetter % 3
        };
        return expectedButton;
    }

    private static string[] _buttonLevel0 = "A-I|J-R|S-Z".Split('|');
    private static bool[][] _arrangements = "1108378656,1108379664,1108410912,1108412424,1108476432,1108476936,1109410848,1109411856,1109459232,1109460996,1109524752,1109525508,1111507488,1111509000,1111523616,1111525380,1111621896,1111622148,1115701776,1115702280,1115717904,1115718660,1115750664,1115750916,1141408800,1141409808,1141441056,1141442568,1141506576,1141507080,1142957088,1142958096,1143013536,1143015426,1143079056,1143079938,1145053728,1145055240,1145077920,1145079810,1145176200,1145176578,1149248016,1149248520,1149272208,1149273090,1149304968,1149305346,1208501280,1208502288,1208549664,1208551428,1208615184,1208615940,1209017376,1209018384,1209073824,1209075714,1209139344,1209140226,1212162336,1212164100,1212170400,1212172290,1212285060,1212285186,1216356624,1216357380,1216364688,1216365570,1216413828,1216413954,1342718496,1342720008,1342734624,1342736388,1342832904,1342833156,1343234592,1343236104,1343258784,1343260674,1343357064,1343357442,1344282912,1344284676,1344290976,1344292866,1344405636,1344405762,1350574344,1350574596,1350582408,1350582786,1350598788,1350598914,1611153936,1611154440,1611170064,1611170820,1611202824,1611203076,1611670032,1611670536,1611694224,1611695106,1611726984,1611727362,1612718352,1612719108,1612726416,1612727298,1612775556,1612775682,1614815496,1614815748,1614823560,1614823938,1614839940,1614840066,2165343264,2165344272,2165375520,2165377032,2165441040,2165441544,2166375456,2166376464,2166423840,2166425604,2166489360,2166490116,2168472096,2168473608,2168488224,2168489988,2168586504,2168586756,2172666384,2172666888,2172682512,2172683268,2172715272,2172715524,2214888480,2214889488,2214920736,2214922248,2214986256,2214986760,2216694816,2216695824,2216755296,2216757249,2216820816,2216821761,2218791456,2218792968,2218819680,2218821633,2218917960,2218918401,2222985744,2222986248,2223013968,2223014913,2223046728,2223047169,2281980960,2281981968,2282029344,2282031108,2282094864,2282095620,2282755104,2282756112,2282815584,2282817537,2282881104,2282882049,2285900064,2285901828,2285912160,2285914113,2286026820,2286027009,2290094352,2290095108,2290106448,2290107393,2290155588,2290155777,2416198176,2416199688,2416214304,2416216068,2416312584,2416312836,2416972320,2416973832,2417000544,2417002497,2417098824,2417099265,2418020640,2418022404,2418032736,2418034689,2418147396,2418147585,2424312072,2424312324,2424324168,2424324609,2424340548,2424340737,2684633616,2684634120,2684649744,2684650500,2684682504,2684682756,2685407760,2685408264,2685435984,2685436929,2685468744,2685469185,2686456080,2686456836,2686468176,2686469121,2686517316,2686517505,2688553224,2688553476,2688565320,2688565761,2688581700,2688581889,4312302624,4312303632,4312334880,4312336392,4312400400,4312400904,4313850912,4313851920,4313907360,4313909250,4313972880,4313973762,4315947552,4315949064,4315971744,4315973634,4316070024,4316070402,4320141840,4320142344,4320166032,4320166914,4320198792,4320199170,4328817696,4328818704,4328849952,4328851464,4328915472,4328915976,4330624032,4330625040,4330684512,4330686465,4330750032,4330750977,4332720672,4332722184,4332748896,4332750849,4332847176,4332847617,4336914960,4336915464,4336943184,4336944129,4336975944,4336976385,4429456416,4429457424,4429512864,4429514754,4429578384,4429579266,4429714464,4429715472,4429774944,4429776897,4429840464,4429841409,4433383584,4433385474,4433387616,4433389569,4433510466,4433510529,4437577872,4437578754,4437581904,4437582849,4437639234,4437639297,4563673632,4563675144,4563697824,4563699714,4563796104,4563796482,4563931680,4563933192,4563959904,4563961857,4564058184,4564058625,4565504160,4565506050,4565508192,4565510145,4565631042,4565631105,4571795592,4571795970,4571799624,4571800065,4571824194,4571824257,4832109072,4832109576,4832133264,4832134146,4832166024,4832166402,4832367120,4832367624,4832395344,4832396289,4832428104,4832428545,4833939600,4833940482,4833943632,4833944577,4834000962,4834001025,4836036744,4836037122,4836040776,4836041217,4836065346,4836065409,8607253536,8607254544,8607301920,8607303684,8607367440,8607368196,8607769632,8607770640,8607826080,8607827970,8607891600,8607892482,8610914592,8610916356,8610922656,8610924546,8611037316,8611037442,8615108880,8615109636,8615116944,8615117826,8615166084,8615166210,8623768608,8623769616,8623816992,8623818756,8623882512,8623883268,8624542752,8624543760,8624603232,8624605185,8624668752,8624669697,8627687712,8627689476,8627699808,8627701761,8627814468,8627814657,8631882000,8631882756,8631894096,8631895041,8631943236,8631943425,8657314848,8657315856,8657371296,8657373186,8657436816,8657437698,8657572896,8657573904,8657633376,8657635329,8657698896,8657699841,8661242016,8661243906,8661246048,8661248001,8661368898,8661368961,8665436304,8665437186,8665440336,8665441281,8665497666,8665497729,8858640672,8858642436,8858648736,8858650626,8858763396,8858763522,8858898720,8858900484,8858910816,8858912769,8859025476,8859025665,8859422880,8859424770,8859426912,8859428865,8859549762,8859549825,8866762884,8866763010,8866766916,8866767105,8866775106,8866775169,9127076112,9127076868,9127084176,9127085058,9127133316,9127133442,9127334160,9127334916,9127346256,9127347201,9127395396,9127395585,9127858320,9127859202,9127862352,9127863297,9127919682,9127919745,9131004036,9131004162,9131008068,9131008257,9131016258,9131016321,17197187616,17197189128,17197203744,17197205508,17197302024,17197302276,17197703712,17197705224,17197727904,17197729794,17197826184,17197826562,17198752032,17198753796,17198760096,17198761986,17198874756,17198874882,17205043464,17205043716,17205051528,17205051906,17205067908,17205068034,17213702688,17213704200,17213718816,17213720580,17213817096,17213817348,17214476832,17214478344,17214505056,17214507009,17214603336,17214603777,17215525152,17215526916,17215537248,17215539201,17215651908,17215652097,17221816584,17221816836,17221828680,17221829121,17221845060,17221845249,17247248928,17247250440,17247273120,17247275010,17247371400,17247371778,17247506976,17247508488,17247535200,17247537153,17247633480,17247633921,17249079456,17249081346,17249083488,17249085441,17249206338,17249206401,17255370888,17255371266,17255374920,17255375361,17255399490,17255399553,17314357536,17314359300,17314365600,17314367490,17314480260,17314480386,17314615584,17314617348,17314627680,17314629633,17314742340,17314742529,17315139744,17315141634,17315143776,17315145729,17315266626,17315266689,17322479748,17322479874,17322483780,17322483969,17322491970,17322492033,17717010696,17717010948,17717018760,17717019138,17717035140,17717035266,17717268744,17717268996,17717280840,17717281281,17717297220,17717297409,17717792904,17717793282,17717796936,17717797377,17717821506,17717821569,17718841476,17718841602,17718845508,17718845697,17718853698,17718853761,34377056784,34377057288,34377072912,34377073668,34377105672,34377105924,34377572880,34377573384,34377597072,34377597954,34377629832,34377630210,34378621200,34378621956,34378629264,34378630146,34378678404,34378678530,34380718344,34380718596,34380726408,34380726786,34380742788,34380742914,34393571856,34393572360,34393587984,34393588740,34393620744,34393620996,34394346000,34394346504,34394374224,34394375169,34394406984,34394407425,34395394320,34395395076,34395406416,34395407361,34395455556,34395455745,34397491464,34397491716,34397503560,34397504001,34397519940,34397520129,34427118096,34427118600,34427142288,34427143170,34427175048,34427175426,34427376144,34427376648,34427404368,34427405313,34427437128,34427437569,34428948624,34428949506,34428952656,34428953601,34429009986,34429010049,34431045768,34431046146,34431049800,34431050241,34431074370,34431074433,34494226704,34494227460,34494234768,34494235650,34494283908,34494284034,34494484752,34494485508,34494496848,34494497793,34494545988,34494546177,34495008912,34495009794,34495012944,34495013889,34495070274,34495070337,34498154628,34498154754,34498158660,34498158849,34498166850,34498166913,34628444424,34628444676,34628452488,34628452866,34628468868,34628468994,34628702472,34628702724,34628714568,34628715009,34628730948,34628731137,34629226632,34629227010,34629230664,34629231105,34629255234,34629255297,34630275204,34630275330,34630279236,34630279425,34630287426,34630287489"
        .Split(',')
        .Select(ulong.Parse)
        .Select(u => Enumerable.Range(0, 36).Select(bit => (u & (1ul << bit)) != 0).ToArray())
        .ToArray();

    private static string[] _symbolNames = "ARROW,BOWTIE,CIRCLE,DIAMOND,FIVESTAR,HEXAGON,HONEYCOMB,INFINITY,JULIA,KITE,OVAL,SEMICIRCLE,SQUARE,TRAPEZOID,TRIANGLE".Split(',');
    private static (int sym, int num)[] _allPoss = Enumerable.Range(0, 6 * _symbolNames.Length).Select(i => (sym: i / 6, num: i % 6)).ToArray();

    public IEnumerator GeneratePuzzle()
    {
        yield return null;
        var seed = Rnd.Range(int.MinValue, int.MaxValue);
        var rnd = new System.Random(seed);
        Debug.Log($"[Twodoku #{_moduleId}] Random seed: {seed}");

        var solutionWord = Data.AllWords.PickRandom(rnd);
        var isEditor = Application.isEditor;

        // These are assigned within the thread
        (int cell, bool isNumClue, int clue)[] displayedClues = null;
        (int sym, int num)[] solution = null;
        bool symIsWide = false;
        bool[] arrangement = null;

        var threadMessages = new List<string>();
        var threadDone = false;
        var thread = new Thread(() =>
        {
            try
            {
                var encs = Enumerable.Range(0, 6)
                    .Select(letterIx => Enumerable.Range(0, 6 * _symbolNames.Length)
                        .Select(i => (sym: i / 6, num: i % 6))
                        .Where(tup => _symbolNames[tup.sym][tup.num % _symbolNames[tup.sym].Length] == solutionWord[letterIx])
                        .ToArray()
                        .Shuffle(rnd))
                    .ToArray();

                var symIsWideStart = rnd.Next(0, 2) != 0;
                var allArrangements = _arrangements.ToList().Shuffle(rnd);

                for (var arrIx = 0; arrIx < allArrangements.Count; arrIx++)
                    foreach (var symIsWideF in new[] { symIsWideStart, !symIsWideStart })
                    {
                        symIsWide = symIsWideF;
                        arrangement = allArrangements[arrIx];
                        var startGrid = Enumerable.Range(0, 36).Select(i => arrangement[i] ? encs[i / 6] : _allPoss).ToArray();
                        solution = recurse(new int?[36], new int?[36], startGrid, [], symIsWide, rnd, DateTime.UtcNow).FirstOrDefault();
                        if (solution == null)
                        {
                            if (isEditor)
                                lock (threadMessages)
                                    threadMessages.Add($"♦ Bad combo: {solutionWord}, symIsWide={symIsWide}, arrangement={arrangement.Select(b => b ? "1" : "0").JoinString()}");
                            continue;
                        }

                        var candidateClueIndexes = Enumerable.Range(0, 36).Where(i => !arrangement[i]).ToList();

                        var iter = 0;
                        tryAgain:
                        iter++;
                        if (iter >= 20)
                        {
                            if (isEditor)
                                lock (threadMessages)
                                    threadMessages.Add($"♦ Bad iter: {solutionWord}, symIsWide={symIsWide}, arrangement={arrangement.Select(b => b ? "1" : "0").JoinString()}");
                            continue;
                        }

                        candidateClueIndexes.Shuffle(rnd);
                        var clues = candidateClueIndexes.Select((cell, n) => n >= 15 ? (cell, isNumClue: true, clue: solution[cell].num) : (cell, isNumClue: false, clue: solution[cell].sym)).ToArray();
                        bool testUniqueness(IEnumerable<int> setToTest)
                        {
                            var poss = Enumerable.Repeat(_allPoss, 36).ToArray();
                            foreach (var clueIx in setToTest)
                            {
                                var (cell, isNumClue, clue) = clues[clueIx];
                                poss[cell] = poss[cell].Where(tup => (isNumClue ? tup.num : tup.sym) == clue).ToArray();
                            }
                            return !recurse(new int?[36], new int?[36], poss, [], false, null).Concat(recurse(new int?[36], new int?[36], poss, [], true, null)).Skip(1).Any();
                        }
                        if (!testUniqueness(Enumerable.Range(0, clues.Length)))
                            goto tryAgain;
                        displayedClues = Ut.ReduceRequiredSet(Enumerable.Range(0, clues.Length), skipConsistencyTest: true, test: state =>
                        {
                            // Must have at least 6 unique symbols
                            if (state.SetToTest.Where(clueIx => !clues[clueIx].isNumClue).Select(clueIx => clues[clueIx].clue).Distinct().Count() < 6)
                                return false;
                            if (isEditor)
                                lock (threadMessages)
                                    threadMessages.Add(Enumerable.Range(0, clues.Length).Select(clueIx => state.SetToTest.Contains(clueIx) ? "█" : "░").JoinString());
                            return testUniqueness(state.SetToTest);
                        }).Select(ix => clues[ix])
                            .OrderBy(tup => tup.cell)
                            .ToArray();
                        threadDone = true;
                        if (isEditor)
                            lock (threadMessages)
                                threadMessages.Add($"iter = {iter}");
                        return;
                    }
            }
            catch (Exception e)
            {
                if (isEditor)
                    lock (threadMessages)
                        threadMessages.Add($"Exception {e.GetType()}: {e.Message}\n{e.StackTrace}");
            }
        });
        thread.Start();

        void log(string msg)
        {
            if (msg.StartsWith("Exception"))
                Debug.LogError($"<Twodoku #{_moduleId}> {msg}");
            else if (msg.StartsWith("♦"))
                Debug.LogWarning($"<Twodoku #{_moduleId}> {msg}");
            else
                Debug.Log($"<Twodoku #{_moduleId}> {msg}");
        }

        var lastSq = Rnd.Range(0, 36);
        SymbolTemplate.transform.localPosition = GetLocationFromSq(lastSq);
        SymbolTemplate.sprite = HighlightSprite;
        SymbolTemplate.gameObject.SetActive(true);
        while (!threadDone || !_activated)
        {
            var newSq = (lastSq + Rnd.Range(1, 36)) % 36;

            float duration = .3f, elapsed = 0;
            while (elapsed < duration)
            {
                SymbolTemplate.transform.localPosition = Vector3.Lerp(GetLocationFromSq(lastSq), GetLocationFromSq(newSq), Easing.InOutQuad(elapsed, 0, 1, duration));
                yield return null;
                elapsed += Time.deltaTime;
            }
            SymbolTemplate.transform.localPosition = GetLocationFromSq(newSq);

            lastSq = newSq;
            yield return new WaitForSeconds(Rnd.Range(0.2f, 0.4f));
            if (isEditor)
                lock (threadMessages)
                {
                    foreach (var msg in threadMessages)
                        log(msg);
                    threadMessages.Clear();
                }
        }
        SymbolTemplate.gameObject.SetActive(false);

        if (isEditor)
            foreach (var msg in threadMessages)
                log(msg);
        _solutionWord = solutionWord;
        for (var btn = 0; btn < 3; btn++)
            ButtonLabels[btn].text = _buttonLevel0[btn];
        SolutionDisplay.text = "<color=#888>│</color>";

        Debug.Log($"[Twodoku #{_moduleId}] Clues: {displayedClues.Select(tup => $"{(char) ('A' + tup.cell % 6)}{tup.cell / 6 + 1}={(tup.isNumClue ? $"{tup.clue + 1}" : _symbolNames[tup.clue])}").JoinString(",")}");
        Debug.Log($"[Twodoku #{_moduleId}] Cells highlighted: {Enumerable.Range(0, 36).Where(cell => arrangement[cell]).Select(cell => $"{(char) ('A' + cell % 6)}{cell / 6 + 1}").JoinString(", ")}");
        Debug.Log($"[Twodoku #{_moduleId}] Solution numbers: {solution.Select(c => c.num + 1).JoinString()}");
        Debug.Log($"[Twodoku #{_moduleId}] Solution symbols: {solution.Select(c => _symbolNames[c.sym]).JoinString(" ")}");
        Debug.Log($"[Twodoku #{_moduleId}] Solution word: {solutionWord}");

        foreach (var (cell, isNumClue, clue) in displayedClues.GroupBy(c => c.cell / 6).OrderByDescending(g => g.Key).SelectMany(g => g.ToArray().Shuffle()))
        {
            yield return new WaitForSeconds(.1f);
            StartCoroutine(HandleSymbolIn(cell, (isNumClue ? Numbers : Symbols)[clue], $"{(isNumClue ? "Number" : "Symbol")}-{cell}-{clue}"));
        }

        yield return new WaitForSeconds(1.25f);

        foreach (var cell in arrangement.SelectIndexWhere(b => b).ToArray().Shuffle())
        {
            yield return new WaitForSeconds(.1f);
            SpawnMarker(cell);
        }
    }

    private IEnumerable<(int sym, int num)[]> recurse(int?[] sofarSym, int?[] sofarNum, (int sym, int num)[][] possibilities, int[] symsUsed, bool symIsWide, System.Random rnd, DateTime? startTime = null)
    {
        if (startTime is { } dt && (DateTime.UtcNow - dt).TotalSeconds > 1)
            yield break;
        var bestCell = -1;
        var bestIsNum = false;
        HashSet<int> bestPosses = null;
        for (var cell = 0; cell < 36; cell++)
        {
            if (sofarSym[cell] == null)
            {
                var possSyms = possibilities[cell].Select(t => t.sym).ToHashSet();
                if (possSyms.Count == 0)
                    yield break;
                if (bestPosses == null || possSyms.Count < bestPosses.Count)
                {
                    bestCell = cell;
                    bestIsNum = false;
                    bestPosses = possSyms;
                    if (possSyms.Count == 1)
                        goto shortcut;
                }
            }
            if (sofarNum[cell] == null)
            {
                var possNums = possibilities[cell].Select(t => t.num).ToHashSet();
                if (possNums.Count == 0)
                    yield break;
                if (bestPosses == null || possNums.Count < bestPosses.Count)
                {
                    bestCell = cell;
                    bestIsNum = true;
                    bestPosses = possNums;
                    if (possNums.Count == 1)
                        goto shortcut;
                }
            }
        }
        if (bestPosses == null)
        {
            yield return Enumerable.Range(0, 36).Select(i => (sym: sofarSym[i].Value, num: sofarNum[i].Value)).ToArray();
            yield break;
        }

        shortcut:
        var col = bestCell % 6;
        var row = bestCell / 6;
        var regA = _getTypeARegion(bestCell);
        var regB = _getTypeBRegion(bestCell);
        var bestArr = bestPosses.ToArray();
        var ofs = rnd == null ? 0 : rnd.Next(0, bestArr.Length);
        for (var arrIx = 0; arrIx < bestArr.Length; arrIx++)
        {
            var value = bestArr[(arrIx + ofs) % bestArr.Length];

            // Determine what symbol/number combinations are still possible in the other cells
            var newPoss = possibilities
                .Select((np, ix) =>
                    np == null ? null :
                    ix == bestCell ? np :
                    // Remove symbols/numbers from the same row, column, or box
                    bestIsNum
                        ? (ix / 6 == row || ix % 6 == col || (symIsWide ? _getTypeBRegion(ix) == regB : _getTypeARegion(ix) == regA) ? np.Where(tup => tup.num != value).ToArray() : np)
                        : (ix / 6 == row || ix % 6 == col || (symIsWide ? _getTypeARegion(ix) == regA : _getTypeBRegion(ix) == regB) ? np.Where(tup => tup.sym != value).ToArray() : np))
                .ToArray();

            // Place the new symbol/number
            var newSofar = (bestIsNum ? sofarNum : sofarSym).ToArray();
            newSofar[bestCell] = value;
            var newSofarNum = bestIsNum ? newSofar : sofarNum;
            var newSofarSym = bestIsNum ? sofarSym : newSofar;

            var isNewSymbol = !bestIsNum && !symsUsed.Contains(value);
            var newSymsUsed = isNewSymbol ? symsUsed.Append(value) : symsUsed;

            // Remove symbol possibilities if we’ve reached 6 symbols
            if (isNewSymbol && newSymsUsed.Length == 6)
                newPoss = newPoss.Select(np => np?.Where(tup => newSymsUsed.Contains(tup.sym)).ToArray()).ToArray();

            newPoss[bestCell] = (bestIsNum ? sofarSym : sofarNum)[bestCell] == null
                ? newPoss[bestCell].Where(tup => bestIsNum ? tup.num == value : tup.sym == value).ToArray()
                : null;

            bool elemNotAvailable(Func<(int sym, int num), int> getter, Func<int, int> getBox)
            {
                var rowsHave = Ut.NewArray(6, _ => new bool[6]);
                var colsHave = Ut.NewArray(6, _ => new bool[6]);
                var boxesHave = Ut.NewArray(6, _ => new bool[6]);
                for (var i = 0; i < 36; i++)
                    if (newPoss[i] is { } arr)
                        foreach (var tup in arr)
                        {
                            var v = getter(tup);
                            rowsHave[i / 6][v] = true;
                            colsHave[i % 6][v] = true;
                            boxesHave[getBox(i)][v] = true;
                        }
                    else
                    {
                        var v = getter((newSofarSym[i].Value, newSofarNum[i].Value));
                        rowsHave[i / 6][v] = true;
                        colsHave[i % 6][v] = true;
                        boxesHave[getBox(i)][v] = true;
                    }
                return rowsHave.Any(r => r.Contains(false)) || colsHave.Any(c => c.Contains(false)) || boxesHave.Any(c => c.Contains(false));
            }

            // If any row, column or box can no longer contain a particular number or symbol, bail out
            if (elemNotAvailable(tup => tup.num, symIsWide ? _getTypeBRegion : _getTypeARegion) ||
                        (newSymsUsed.Length == 6 && elemNotAvailable(tup => newSymsUsed.IndexOf(tup.sym), symIsWide ? _getTypeARegion : _getTypeBRegion)))
                continue;

            foreach (var sln in recurse(newSofarSym, newSofarNum, newPoss, newSymsUsed, symIsWide, rnd, startTime))
                yield return sln;
        }
    }

    private static int[] _solveLetters = [3, 4, 5, 1, 0, 1, 2, 2];
    private static int[] _solveLocations = [19, 20, 21, 22, 13, 14, 15, 16];

    private IEnumerator SolveAnim()
    {
        yield return null;

        foreach (var image in _instantiatedSymbols)
            StartCoroutine(HandleSymbolOut(image));

        foreach (var marker in _instantiatedMarkers)
            Destroy(marker.gameObject);

        _instantiatedSymbols.Clear();
        _instantiatedMarkers.Clear();

        yield return new WaitForSeconds(1.25f);

        for (int i = 0; i < _solveLetters.Length; i++)
        {
            yield return new WaitForSeconds(.1f);
            StartCoroutine(HandleSymbolIn(_solveLocations[i], Letters[_solveLetters[i]], $"Letter-{i}"));
        }
    }

    private Vector3 GetLocationFromSq(int location) => new(Mathf.Lerp(-0.05555f, 0.05555f, location % 6 / 5f), Mathf.Lerp(0.05555f, -0.05555f, location / 6 / 5f));

    private void PlaceSymbol(Image target, int location)
    {
        target.transform.localPosition = GetLocationFromSq(location);
    }

    private void SpawnMarker(int location)
    {
        _instantiatedMarkers.Add(Instantiate(SymbolTemplate, SymbolTemplate.transform.parent));
        var image = _instantiatedMarkers.Last();
        image.name = $"Highlight-{location}";
        image.gameObject.SetActive(true);
        image.rectTransform.sizeDelta = Vector2.one * 0.022f;
        image.sprite = HighlightSprite;
        PlaceSymbol(image, location);
        Audio.PlaySoundAtTransform("beep3", transform);
    }

    private IEnumerator HandleSymbolIn(int location, Sprite sprite, string symbolName)
    {
        _instantiatedSymbols.Add(Instantiate(SymbolTemplate, SymbolTemplate.transform.parent));
        var image = _instantiatedSymbols.Last();
        image.name = symbolName;
        image.gameObject.SetActive(true);
        image.rectTransform.sizeDelta = Vector2.one * 0.0175f;

        Vector3 endLocation = image.transform.localPosition = GetLocationFromSq(location);
        Vector3 initLocation = image.transform.localPosition = endLocation + (Vector3.up * (location / 6 + 1) / 45f);
        image.sprite = sprite;

        float duration = Rnd.Range(0.85f, 1.1f) * (location / 6 + 1) / 6f, elapsed = 0;
        while (elapsed < duration)
        {
            image.transform.localPosition = Vector3.Lerp(initLocation, endLocation, Easing.InSine(elapsed, 0, 1, duration));
            yield return null;
            elapsed += Time.deltaTime;
        }
        image.transform.localPosition = endLocation;
        Audio.PlaySoundAtTransform("beep" + Rnd.Range(1, 4), transform);
    }

    private IEnumerator HandleSymbolOut(Image target)
    {
        Vector3 initLocation = target.transform.localPosition;
        Vector3 endLocation = initLocation - (Vector3.up * 0.13333f);

        float duration = Rnd.Range(0.85f, 1.1f), elapsed = 0;
        while (elapsed < duration)
        {
            target.transform.localPosition = new Vector3(Easing.InSine(elapsed, initLocation.x, endLocation.x, duration),
                Easing.InSine(elapsed, initLocation.y, endLocation.y, duration), 0);
            yield return null;
            elapsed += Time.deltaTime;
        }
        Destroy(target.gameObject);
    }

    private static readonly int[][] _allRegions = @"0,1,2,3,4,6|0,1,2,3,4,7|0,1,2,3,4,8|0,1,2,3,4,9|0,1,2,3,4,10|0,1,2,3,6,7|0,1,2,3,6,8|0,1,2,3,6,9|0,1,2,3,6,12|0,1,2,3,7,8|0,1,2,3,7,9|0,1,2,3,7,13|0,1,2,3,8,9|0,1,2,3,8,14|0,1,2,3,9,10|0,1,2,3,9,15|0,1,2,6,7,8|0,1,2,6,7,12|0,1,2,6,7,13|0,1,2,6,8,9|0,1,2,6,8,12|0,1,2,6,8,14|0,1,2,6,12,13|0,1,2,6,12,18|0,1,2,7,8,9|0,1,2,7,8,13|0,1,2,7,8,14|0,1,2,7,12,13|0,1,2,7,13,14|0,1,2,7,13,19|0,1,2,8,9,10|0,1,2,8,9,14|0,1,2,8,9,15|0,1,2,8,13,14|0,1,2,8,14,15|0,1,2,8,14,20|0,1,6,7,8,9|0,1,6,7,8,12|0,1,6,7,8,13|0,1,6,7,8,14|0,1,6,7,12,13|0,1,6,7,12,18|0,1,6,7,13,14|0,1,6,7,13,19|0,1,6,12,13,14|0,1,6,12,13,18|0,1,6,12,13,19|0,1,6,12,18,19|0,1,6,12,18,24|0,1,3,7,8,9|0,1,7,8,9,10|0,1,7,8,9,13|0,1,7,8,9,14|0,1,7,8,9,15|0,1,7,8,12,13|0,1,7,8,13,14|0,1,7,8,13,19|0,1,7,8,14,15|0,1,7,8,14,20|0,1,7,12,13,14|0,1,7,12,13,18|0,1,7,12,13,19|0,1,7,13,14,15|0,1,7,13,14,19|0,1,7,13,14,20|0,1,7,13,18,19|0,1,7,13,19,20|0,1,7,13,19,25|0,2,3,6,7,8|0,2,6,7,8,9|0,2,6,7,8,12|0,2,6,7,8,13|0,2,6,7,8,14|0,3,6,7,8,9|0,6,7,8,9,10|0,6,7,8,9,12|0,6,7,8,9,13|0,6,7,8,9,14|0,6,7,8,9,15|0,6,7,8,12,13|0,6,7,8,12,14|0,6,7,8,12,18|0,6,7,8,13,14|0,6,7,8,13,19|0,6,7,8,14,15|0,6,7,8,14,20|0,6,7,12,13,14|0,6,7,12,13,18|0,6,7,12,13,19|0,6,7,12,18,19|0,6,7,12,18,24|0,6,7,13,14,15|0,6,7,13,14,19|0,6,7,13,14,20|0,6,7,13,18,19|0,6,7,13,19,20|0,6,7,13,19,25|0,6,8,12,13,14|0,6,12,13,14,15|0,6,12,13,14,18|0,6,12,13,14,19|0,6,12,13,14,20|0,6,12,13,18,19|0,6,12,13,18,24|0,6,12,13,19,20|0,6,12,13,19,25|0,6,12,18,19,20|0,6,12,18,19,24|0,6,12,18,19,25|0,6,12,18,24,25|1,2,3,4,5,7|1,2,3,4,5,8|1,2,3,4,5,9|1,2,3,4,5,10|1,2,3,4,5,11|1,2,3,4,6,7|1,2,3,4,7,8|1,2,3,4,7,9|1,2,3,4,7,10|1,2,3,4,7,13|1,2,3,4,8,9|1,2,3,4,8,10|1,2,3,4,8,14|1,2,3,4,9,10|1,2,3,4,9,15|1,2,3,4,10,11|1,2,3,4,10,16|1,2,3,6,7,8|1,2,3,6,7,9|1,2,3,6,7,12|1,2,3,6,7,13|1,2,3,7,8,9|1,2,3,7,8,13|1,2,3,7,8,14|1,2,3,7,9,10|1,2,3,7,9,13|1,2,3,7,9,15|1,2,3,7,12,13|1,2,3,7,13,14|1,2,3,7,13,19|1,2,3,8,9,10|1,2,3,8,9,14|1,2,3,8,9,15|1,2,3,8,13,14|1,2,3,8,14,15|1,2,3,8,14,20|1,2,3,9,10,11|1,2,3,9,10,15|1,2,3,9,10,16|1,2,3,9,14,15|1,2,3,9,15,16|1,2,3,9,15,21|1,2,6,7,8,9|1,2,6,7,8,12|1,2,6,7,8,13|1,2,6,7,8,14|1,2,6,7,12,13|1,2,6,7,12,18|1,2,6,7,13,14|1,2,6,7,13,19|1,2,7,8,9,10|1,2,7,8,9,13|1,2,7,8,9,14|1,2,7,8,9,15|1,2,7,8,12,13|1,2,7,8,13,14|1,2,7,8,13,19|1,2,7,8,14,15|1,2,7,8,14,20|1,2,7,12,13,14|1,2,7,12,13,18|1,2,7,12,13,19|1,2,7,13,14,15|1,2,7,13,14,19|1,2,7,13,14,20|1,2,7,13,18,19|1,2,7,13,19,20|1,2,7,13,19,25|1,2,4,8,9,10|1,2,8,9,10,11|1,2,8,9,10,14|1,2,8,9,10,15|1,2,8,9,10,16|1,2,8,9,13,14|1,2,8,9,14,15|1,2,8,9,14,20|1,2,8,9,15,16|1,2,8,9,15,21|1,2,8,12,13,14|1,2,8,13,14,15|1,2,8,13,14,19|1,2,8,13,14,20|1,2,8,14,15,16|1,2,8,14,15,20|1,2,8,14,15,21|1,2,8,14,19,20|1,2,8,14,20,21|1,2,8,14,20,26|1,3,6,7,8,9|1,6,7,8,9,10|1,6,7,8,9,12|1,6,7,8,9,13|1,6,7,8,9,14|1,6,7,8,9,15|1,6,7,8,12,13|1,6,7,8,12,14|1,6,7,8,12,18|1,6,7,8,13,14|1,6,7,8,13,19|1,6,7,8,14,15|1,6,7,8,14,20|1,6,7,12,13,14|1,6,7,12,13,18|1,6,7,12,13,19|1,6,7,12,18,19|1,6,7,12,18,24|1,6,7,13,14,15|1,6,7,13,14,19|1,6,7,13,14,20|1,6,7,13,18,19|1,6,7,13,19,20|1,6,7,13,19,25|1,3,4,7,8,9|1,3,7,8,9,10|1,3,7,8,9,13|1,3,7,8,9,14|1,3,7,8,9,15|1,4,7,8,9,10|1,7,8,9,10,11|1,7,8,9,10,13|1,7,8,9,10,14|1,7,8,9,10,15|1,7,8,9,10,16|1,7,8,9,12,13|1,7,8,9,13,14|1,7,8,9,13,15|1,7,8,9,13,19|1,7,8,9,14,15|1,7,8,9,14,20|1,7,8,9,15,16|1,7,8,9,15,21|1,7,8,12,13,14|1,7,8,12,13,18|1,7,8,12,13,19|1,7,8,13,14,15|1,7,8,13,14,19|1,7,8,13,14,20|1,7,8,13,18,19|1,7,8,13,19,20|1,7,8,13,19,25|1,7,8,14,15,16|1,7,8,14,15,20|1,7,8,14,15,21|1,7,8,14,19,20|1,7,8,14,20,21|1,7,8,14,20,26|1,7,12,13,14,15|1,7,12,13,14,18|1,7,12,13,14,19|1,7,12,13,14,20|1,7,12,13,18,19|1,7,12,13,18,24|1,7,12,13,19,20|1,7,12,13,19,25|1,7,9,13,14,15|1,7,13,14,15,16|1,7,13,14,15,19|1,7,13,14,15,20|1,7,13,14,15,21|1,7,13,14,18,19|1,7,13,14,19,20|1,7,13,14,19,25|1,7,13,14,20,21|1,7,13,14,20,26|1,7,13,18,19,20|1,7,13,18,19,24|1,7,13,18,19,25|1,7,13,19,20,21|1,7,13,19,20,25|1,7,13,19,20,26|1,7,13,19,24,25|1,7,13,19,25,26|2,3,4,5,7,8|2,3,4,5,8,9|2,3,4,5,8,10|2,3,4,5,8,11|2,3,4,5,8,14|2,3,4,5,9,10|2,3,4,5,9,11|2,3,4,5,9,15|2,3,4,5,10,11|2,3,4,5,10,16|2,3,4,5,11,17|2,3,4,6,7,8|2,3,4,7,8,9|2,3,4,7,8,10|2,3,4,7,8,13|2,3,4,7,8,14|2,3,4,8,9,10|2,3,4,8,9,14|2,3,4,8,9,15|2,3,4,8,10,11|2,3,4,8,10,14|2,3,4,8,10,16|2,3,4,8,13,14|2,3,4,8,14,15|2,3,4,8,14,20|2,3,4,9,10,11|2,3,4,9,10,15|2,3,4,9,10,16|2,3,4,9,14,15|2,3,4,9,15,16|2,3,4,9,15,21|2,3,4,10,11,16|2,3,4,10,11,17|2,3,4,10,15,16|2,3,4,10,16,17|2,3,4,10,16,22|2,3,6,7,8,9|2,3,6,7,8,12|2,3,6,7,8,13|2,3,6,7,8,14|2,3,7,8,9,10|2,3,7,8,9,13|2,3,7,8,9,14|2,3,7,8,9,15|2,3,7,8,12,13|2,3,7,8,13,14|2,3,7,8,13,19|2,3,7,8,14,15|2,3,7,8,14,20|2,3,8,9,10,11|2,3,8,9,10,14|2,3,8,9,10,15|2,3,8,9,10,16|2,3,8,9,13,14|2,3,8,9,14,15|2,3,8,9,14,20|2,3,8,9,15,16|2,3,8,9,15,21|2,3,8,12,13,14|2,3,8,13,14,15|2,3,8,13,14,19|2,3,8,13,14,20|2,3,8,14,15,16|2,3,8,14,15,20|2,3,8,14,15,21|2,3,8,14,19,20|2,3,8,14,20,21|2,3,8,14,20,26|2,3,5,9,10,11|2,3,9,10,11,15|2,3,9,10,11,16|2,3,9,10,11,17|2,3,9,10,14,15|2,3,9,10,15,16|2,3,9,10,15,21|2,3,9,10,16,17|2,3,9,10,16,22|2,3,9,13,14,15|2,3,9,14,15,16|2,3,9,14,15,20|2,3,9,14,15,21|2,3,9,15,16,17|2,3,9,15,16,21|2,3,9,15,16,22|2,3,9,15,20,21|2,3,9,15,21,22|2,3,9,15,21,27|2,6,7,8,9,10|2,6,7,8,9,12|2,6,7,8,9,13|2,6,7,8,9,14|2,6,7,8,9,15|2,6,7,8,12,13|2,6,7,8,12,14|2,6,7,8,12,18|2,6,7,8,13,14|2,6,7,8,13,19|2,6,7,8,14,15|2,6,7,8,14,20|2,4,7,8,9,10|2,7,8,9,10,11|2,7,8,9,10,13|2,7,8,9,10,14|2,7,8,9,10,15|2,7,8,9,10,16|2,7,8,9,12,13|2,7,8,9,13,14|2,7,8,9,13,15|2,7,8,9,13,19|2,7,8,9,14,15|2,7,8,9,14,20|2,7,8,9,15,16|2,7,8,9,15,21|2,7,8,12,13,14|2,7,8,12,13,18|2,7,8,12,13,19|2,7,8,13,14,15|2,7,8,13,14,19|2,7,8,13,14,20|2,7,8,13,18,19|2,7,8,13,19,20|2,7,8,13,19,25|2,7,8,14,15,16|2,7,8,14,15,20|2,7,8,14,15,21|2,7,8,14,19,20|2,7,8,14,20,21|2,7,8,14,20,26|2,4,5,8,9,10|2,4,8,9,10,11|2,4,8,9,10,14|2,4,8,9,10,15|2,4,8,9,10,16|2,5,8,9,10,11|2,8,9,10,11,14|2,8,9,10,11,15|2,8,9,10,11,16|2,8,9,10,11,17|2,8,9,10,13,14|2,8,9,10,14,15|2,8,9,10,14,16|2,8,9,10,14,20|2,8,9,10,15,16|2,8,9,10,15,21|2,8,9,10,16,17|2,8,9,10,16,22|2,8,9,12,13,14|2,8,9,13,14,15|2,8,9,13,14,19|2,8,9,13,14,20|2,8,9,14,15,16|2,8,9,14,15,20|2,8,9,14,15,21|2,8,9,14,19,20|2,8,9,14,20,21|2,8,9,14,20,26|2,8,9,15,16,17|2,8,9,15,16,21|2,8,9,15,16,22|2,8,9,15,20,21|2,8,9,15,21,22|2,8,9,15,21,27|2,6,8,12,13,14|2,8,12,13,14,15|2,8,12,13,14,18|2,8,12,13,14,19|2,8,12,13,14,20|2,8,13,14,15,16|2,8,13,14,15,19|2,8,13,14,15,20|2,8,13,14,15,21|2,8,13,14,18,19|2,8,13,14,19,20|2,8,13,14,19,25|2,8,13,14,20,21|2,8,13,14,20,26|2,8,10,14,15,16|2,8,14,15,16,17|2,8,14,15,16,20|2,8,14,15,16,21|2,8,14,15,16,22|2,8,14,15,19,20|2,8,14,15,20,21|2,8,14,15,20,26|2,8,14,15,21,22|2,8,14,15,21,27|2,8,14,18,19,20|2,8,14,19,20,21|2,8,14,19,20,25|2,8,14,19,20,26|2,8,14,20,21,22|2,8,14,20,21,26|2,8,14,20,21,27|2,8,14,20,25,26|2,8,14,20,26,27|3,4,5,7,8,9|3,4,5,8,9,10|3,4,5,8,9,11|3,4,5,8,9,14|3,4,5,8,9,15|3,4,5,9,10,11|3,4,5,9,10,15|3,4,5,9,10,16|3,4,5,9,11,15|3,4,5,9,11,17|3,4,5,9,14,15|3,4,5,9,15,16|3,4,5,9,15,21|3,4,5,10,11,16|3,4,5,10,11,17|3,4,5,10,15,16|3,4,5,10,16,17|3,4,5,10,16,22|3,4,5,11,16,17|3,4,5,11,17,23|3,4,6,7,8,9|3,4,7,8,9,10|3,4,7,8,9,13|3,4,7,8,9,14|3,4,7,8,9,15|3,4,8,9,10,11|3,4,8,9,10,14|3,4,8,9,10,15|3,4,8,9,10,16|3,4,8,9,13,14|3,4,8,9,14,15|3,4,8,9,14,20|3,4,8,9,15,16|3,4,8,9,15,21|3,4,9,10,11,15|3,4,9,10,11,16|3,4,9,10,11,17|3,4,9,10,14,15|3,4,9,10,15,16|3,4,9,10,15,21|3,4,9,10,16,17|3,4,9,10,16,22|3,4,9,13,14,15|3,4,9,14,15,16|3,4,9,14,15,20|3,4,9,14,15,21|3,4,9,15,16,17|3,4,9,15,16,21|3,4,9,15,16,22|3,4,9,15,20,21|3,4,9,15,21,22|3,4,9,15,21,27|3,4,10,11,15,16|3,4,10,11,16,17|3,4,10,11,16,22|3,4,10,11,17,23|3,4,10,14,15,16|3,4,10,15,16,17|3,4,10,15,16,21|3,4,10,15,16,22|3,4,10,16,17,22|3,4,10,16,17,23|3,4,10,16,21,22|3,4,10,16,22,23|3,4,10,16,22,28|3,6,7,8,9,10|3,6,7,8,9,12|3,6,7,8,9,13|3,6,7,8,9,14|3,6,7,8,9,15|3,7,8,9,10,11|3,7,8,9,10,13|3,7,8,9,10,14|3,7,8,9,10,15|3,7,8,9,10,16|3,7,8,9,12,13|3,7,8,9,13,14|3,7,8,9,13,15|3,7,8,9,13,19|3,7,8,9,14,15|3,7,8,9,14,20|3,7,8,9,15,16|3,7,8,9,15,21|3,5,8,9,10,11|3,8,9,10,11,14|3,8,9,10,11,15|3,8,9,10,11,16|3,8,9,10,11,17|3,8,9,10,13,14|3,8,9,10,14,15|3,8,9,10,14,16|3,8,9,10,14,20|3,8,9,10,15,16|3,8,9,10,15,21|3,8,9,10,16,17|3,8,9,10,16,22|3,8,9,12,13,14|3,8,9,13,14,15|3,8,9,13,14,19|3,8,9,13,14,20|3,8,9,14,15,16|3,8,9,14,15,20|3,8,9,14,15,21|3,8,9,14,19,20|3,8,9,14,20,21|3,8,9,14,20,26|3,8,9,15,16,17|3,8,9,15,16,21|3,8,9,15,16,22|3,8,9,15,20,21|3,8,9,15,21,22|3,8,9,15,21,27|3,5,9,10,11,15|3,5,9,10,11,16|3,5,9,10,11,17|3,9,10,11,14,15|3,9,10,11,15,16|3,9,10,11,15,17|3,9,10,11,15,21|3,9,10,11,16,17|3,9,10,11,16,22|3,9,10,11,17,23|3,9,10,13,14,15|3,9,10,14,15,16|3,9,10,14,15,20|3,9,10,14,15,21|3,9,10,15,16,17|3,9,10,15,16,21|3,9,10,15,16,22|3,9,10,15,20,21|3,9,10,15,21,22|3,9,10,15,21,27|3,9,10,16,17,22|3,9,10,16,17,23|3,9,10,16,21,22|3,9,10,16,22,23|3,9,10,16,22,28|3,7,9,13,14,15|3,9,12,13,14,15|3,9,13,14,15,16|3,9,13,14,15,19|3,9,13,14,15,20|3,9,13,14,15,21|3,9,14,15,16,17|3,9,14,15,16,20|3,9,14,15,16,21|3,9,14,15,16,22|3,9,14,15,19,20|3,9,14,15,20,21|3,9,14,15,20,26|3,9,14,15,21,22|3,9,14,15,21,27|3,9,11,15,16,17|3,9,15,16,17,21|3,9,15,16,17,22|3,9,15,16,17,23|3,9,15,16,20,21|3,9,15,16,21,22|3,9,15,16,21,27|3,9,15,16,22,23|3,9,15,16,22,28|3,9,15,19,20,21|3,9,15,20,21,22|3,9,15,20,21,26|3,9,15,20,21,27|3,9,15,21,22,23|3,9,15,21,22,27|3,9,15,21,22,28|3,9,15,21,26,27|3,9,15,21,27,28|4,5,7,8,9,10|4,5,8,9,10,11|4,5,8,9,10,14|4,5,8,9,10,15|4,5,8,9,10,16|4,5,9,10,11,15|4,5,9,10,11,16|4,5,9,10,11,17|4,5,9,10,14,15|4,5,9,10,15,16|4,5,9,10,15,21|4,5,9,10,16,17|4,5,9,10,16,22|4,5,10,11,15,16|4,5,10,11,16,17|4,5,10,11,16,22|4,5,10,11,17,23|4,5,10,14,15,16|4,5,10,15,16,17|4,5,10,15,16,21|4,5,10,15,16,22|4,5,10,16,17,22|4,5,10,16,17,23|4,5,10,16,21,22|4,5,10,16,22,23|4,5,10,16,22,28|4,5,11,15,16,17|4,5,11,16,17,22|4,5,11,16,17,23|4,5,11,17,22,23|4,5,11,17,23,29|4,6,7,8,9,10|4,7,8,9,10,11|4,7,8,9,10,13|4,7,8,9,10,14|4,7,8,9,10,15|4,7,8,9,10,16|4,8,9,10,11,14|4,8,9,10,11,15|4,8,9,10,11,16|4,8,9,10,11,17|4,8,9,10,13,14|4,8,9,10,14,15|4,8,9,10,14,16|4,8,9,10,14,20|4,8,9,10,15,16|4,8,9,10,15,21|4,8,9,10,16,17|4,8,9,10,16,22|4,9,10,11,14,15|4,9,10,11,15,16|4,9,10,11,15,17|4,9,10,11,15,21|4,9,10,11,16,17|4,9,10,11,16,22|4,9,10,11,17,23|4,9,10,13,14,15|4,9,10,14,15,16|4,9,10,14,15,20|4,9,10,14,15,21|4,9,10,15,16,17|4,9,10,15,16,21|4,9,10,15,16,22|4,9,10,15,20,21|4,9,10,15,21,22|4,9,10,15,21,27|4,9,10,16,17,22|4,9,10,16,17,23|4,9,10,16,21,22|4,9,10,16,22,23|4,9,10,16,22,28|4,10,11,14,15,16|4,10,11,15,16,17|4,10,11,15,16,21|4,10,11,15,16,22|4,10,11,16,17,22|4,10,11,16,17,23|4,10,11,16,21,22|4,10,11,16,22,23|4,10,11,16,22,28|4,10,11,17,22,23|4,10,11,17,23,29|4,8,10,14,15,16|4,10,13,14,15,16|4,10,14,15,16,17|4,10,14,15,16,20|4,10,14,15,16,21|4,10,14,15,16,22|4,10,15,16,17,21|4,10,15,16,17,22|4,10,15,16,17,23|4,10,15,16,20,21|4,10,15,16,21,22|4,10,15,16,21,27|4,10,15,16,22,23|4,10,15,16,22,28|4,10,16,17,21,22|4,10,16,17,22,23|4,10,16,17,22,28|4,10,16,17,23,29|4,10,16,20,21,22|4,10,16,21,22,23|4,10,16,21,22,27|4,10,16,21,22,28|4,10,16,22,23,28|4,10,16,22,23,29|4,10,16,22,27,28|4,10,16,22,28,29|5,7,8,9,10,11|5,8,9,10,11,14|5,8,9,10,11,15|5,8,9,10,11,16|5,8,9,10,11,17|5,9,10,11,14,15|5,9,10,11,15,16|5,9,10,11,15,17|5,9,10,11,15,21|5,9,10,11,16,17|5,9,10,11,16,22|5,9,10,11,17,23|5,10,11,14,15,16|5,10,11,15,16,17|5,10,11,15,16,21|5,10,11,15,16,22|5,10,11,16,17,22|5,10,11,16,17,23|5,10,11,16,21,22|5,10,11,16,22,23|5,10,11,16,22,28|5,10,11,17,22,23|5,10,11,17,23,29|5,9,11,15,16,17|5,11,14,15,16,17|5,11,15,16,17,21|5,11,15,16,17,22|5,11,15,16,17,23|5,11,16,17,21,22|5,11,16,17,22,23|5,11,16,17,22,28|5,11,16,17,23,29|5,11,17,21,22,23|5,11,17,22,23,28|5,11,17,22,23,29|5,11,17,23,28,29|6,7,8,9,10,12|6,7,8,9,10,13|6,7,8,9,10,14|6,7,8,9,10,15|6,7,8,9,10,16|6,7,8,9,12,13|6,7,8,9,12,14|6,7,8,9,12,15|6,7,8,9,12,18|6,7,8,9,13,14|6,7,8,9,13,15|6,7,8,9,13,19|6,7,8,9,14,15|6,7,8,9,14,20|6,7,8,9,15,16|6,7,8,9,15,21|6,7,8,12,13,14|6,7,8,12,13,18|6,7,8,12,13,19|6,7,8,12,14,15|6,7,8,12,14,18|6,7,8,12,14,20|6,7,8,12,18,19|6,7,8,12,18,24|6,7,8,13,14,15|6,7,8,13,14,19|6,7,8,13,14,20|6,7,8,13,18,19|6,7,8,13,19,20|6,7,8,13,19,25|6,7,8,14,15,16|6,7,8,14,15,20|6,7,8,14,15,21|6,7,8,14,19,20|6,7,8,14,20,21|6,7,8,14,20,26|6,7,12,13,14,15|6,7,12,13,14,18|6,7,12,13,14,19|6,7,12,13,14,20|6,7,12,13,18,19|6,7,12,13,18,24|6,7,12,13,19,20|6,7,12,13,19,25|6,7,12,18,19,20|6,7,12,18,19,24|6,7,12,18,19,25|6,7,12,18,24,25|6,7,12,18,24,30|6,7,9,13,14,15|6,7,13,14,15,16|6,7,13,14,15,19|6,7,13,14,15,20|6,7,13,14,15,21|6,7,13,14,18,19|6,7,13,14,19,20|6,7,13,14,19,25|6,7,13,14,20,21|6,7,13,14,20,26|6,7,13,18,19,20|6,7,13,18,19,24|6,7,13,18,19,25|6,7,13,19,20,21|6,7,13,19,20,25|6,7,13,19,20,26|6,7,13,19,24,25|6,7,13,19,25,26|6,7,13,19,25,31|6,8,9,12,13,14|6,8,12,13,14,15|6,8,12,13,14,18|6,8,12,13,14,19|6,8,12,13,14,20|6,9,12,13,14,15|6,12,13,14,15,16|6,12,13,14,15,18|6,12,13,14,15,19|6,12,13,14,15,20|6,12,13,14,15,21|6,12,13,14,18,19|6,12,13,14,18,20|6,12,13,14,18,24|6,12,13,14,19,20|6,12,13,14,19,25|6,12,13,14,20,21|6,12,13,14,20,26|6,12,13,18,19,20|6,12,13,18,19,24|6,12,13,18,19,25|6,12,13,18,24,25|6,12,13,18,24,30|6,12,13,19,20,21|6,12,13,19,20,25|6,12,13,19,20,26|6,12,13,19,24,25|6,12,13,19,25,26|6,12,13,19,25,31|6,12,14,18,19,20|6,12,18,19,20,21|6,12,18,19,20,24|6,12,18,19,20,25|6,12,18,19,20,26|6,12,18,19,24,25|6,12,18,19,24,30|6,12,18,19,25,26|6,12,18,19,25,31|6,12,18,24,25,26|6,12,18,24,25,30|6,12,18,24,25,31|6,12,18,24,30,31|7,8,9,10,11,13|7,8,9,10,11,14|7,8,9,10,11,15|7,8,9,10,11,16|7,8,9,10,11,17|7,8,9,10,12,13|7,8,9,10,13,14|7,8,9,10,13,15|7,8,9,10,13,16|7,8,9,10,13,19|7,8,9,10,14,15|7,8,9,10,14,16|7,8,9,10,14,20|7,8,9,10,15,16|7,8,9,10,15,21|7,8,9,10,16,17|7,8,9,10,16,22|7,8,9,12,13,14|7,8,9,12,13,15|7,8,9,12,13,18|7,8,9,12,13,19|7,8,9,13,14,15|7,8,9,13,14,19|7,8,9,13,14,20|7,8,9,13,15,16|7,8,9,13,15,19|7,8,9,13,15,21|7,8,9,13,18,19|7,8,9,13,19,20|7,8,9,13,19,25|7,8,9,14,15,16|7,8,9,14,15,20|7,8,9,14,15,21|7,8,9,14,19,20|7,8,9,14,20,21|7,8,9,14,20,26|7,8,9,15,16,17|7,8,9,15,16,21|7,8,9,15,16,22|7,8,9,15,20,21|7,8,9,15,21,22|7,8,9,15,21,27|7,8,12,13,14,15|7,8,12,13,14,18|7,8,12,13,14,19|7,8,12,13,14,20|7,8,12,13,18,19|7,8,12,13,18,24|7,8,12,13,19,20|7,8,12,13,19,25|7,8,13,14,15,16|7,8,13,14,15,19|7,8,13,14,15,20|7,8,13,14,15,21|7,8,13,14,18,19|7,8,13,14,19,20|7,8,13,14,19,25|7,8,13,14,20,21|7,8,13,14,20,26|7,8,13,18,19,20|7,8,13,18,19,24|7,8,13,18,19,25|7,8,13,19,20,21|7,8,13,19,20,25|7,8,13,19,20,26|7,8,13,19,24,25|7,8,13,19,25,26|7,8,13,19,25,31|7,8,10,14,15,16|7,8,14,15,16,17|7,8,14,15,16,20|7,8,14,15,16,21|7,8,14,15,16,22|7,8,14,15,19,20|7,8,14,15,20,21|7,8,14,15,20,26|7,8,14,15,21,22|7,8,14,15,21,27|7,8,14,18,19,20|7,8,14,19,20,21|7,8,14,19,20,25|7,8,14,19,20,26|7,8,14,20,21,22|7,8,14,20,21,26|7,8,14,20,21,27|7,8,14,20,25,26|7,8,14,20,26,27|7,8,14,20,26,32|7,9,12,13,14,15|7,12,13,14,15,16|7,12,13,14,15,18|7,12,13,14,15,19|7,12,13,14,15,20|7,12,13,14,15,21|7,12,13,14,18,19|7,12,13,14,18,20|7,12,13,14,18,24|7,12,13,14,19,20|7,12,13,14,19,25|7,12,13,14,20,21|7,12,13,14,20,26|7,12,13,18,19,20|7,12,13,18,19,24|7,12,13,18,19,25|7,12,13,18,24,25|7,12,13,18,24,30|7,12,13,19,20,21|7,12,13,19,20,25|7,12,13,19,20,26|7,12,13,19,24,25|7,12,13,19,25,26|7,12,13,19,25,31|7,9,10,13,14,15|7,9,13,14,15,16|7,9,13,14,15,19|7,9,13,14,15,20|7,9,13,14,15,21|7,10,13,14,15,16|7,13,14,15,16,17|7,13,14,15,16,19|7,13,14,15,16,20|7,13,14,15,16,21|7,13,14,15,16,22|7,13,14,15,18,19|7,13,14,15,19,20|7,13,14,15,19,21|7,13,14,15,19,25|7,13,14,15,20,21|7,13,14,15,20,26|7,13,14,15,21,22|7,13,14,15,21,27|7,13,14,18,19,20|7,13,14,18,19,24|7,13,14,18,19,25|7,13,14,19,20,21|7,13,14,19,20,25|7,13,14,19,20,26|7,13,14,19,24,25|7,13,14,19,25,26|7,13,14,19,25,31|7,13,14,20,21,22|7,13,14,20,21,26|7,13,14,20,21,27|7,13,14,20,25,26|7,13,14,20,26,27|7,13,14,20,26,32|7,13,18,19,20,21|7,13,18,19,20,24|7,13,18,19,20,25|7,13,18,19,20,26|7,13,18,19,24,25|7,13,18,19,24,30|7,13,18,19,25,26|7,13,18,19,25,31|7,13,15,19,20,21|7,13,19,20,21,22|7,13,19,20,21,25|7,13,19,20,21,26|7,13,19,20,21,27|7,13,19,20,24,25|7,13,19,20,25,26|7,13,19,20,25,31|7,13,19,20,26,27|7,13,19,20,26,32|7,13,19,24,25,26|7,13,19,24,25,30|7,13,19,24,25,31|7,13,19,25,26,27|7,13,19,25,26,31|7,13,19,25,26,32|7,13,19,25,30,31|7,13,19,25,31,32|8,9,10,11,13,14|8,9,10,11,14,15|8,9,10,11,14,16|8,9,10,11,14,17|8,9,10,11,14,20|8,9,10,11,15,16|8,9,10,11,15,17|8,9,10,11,15,21|8,9,10,11,16,17|8,9,10,11,16,22|8,9,10,11,17,23|8,9,10,12,13,14|8,9,10,13,14,15|8,9,10,13,14,16|8,9,10,13,14,19|8,9,10,13,14,20|8,9,10,14,15,16|8,9,10,14,15,20|8,9,10,14,15,21|8,9,10,14,16,17|8,9,10,14,16,20|8,9,10,14,16,22|8,9,10,14,19,20|8,9,10,14,20,21|8,9,10,14,20,26|8,9,10,15,16,17|8,9,10,15,16,21|8,9,10,15,16,22|8,9,10,15,20,21|8,9,10,15,21,22|8,9,10,15,21,27|8,9,10,16,17,22|8,9,10,16,17,23|8,9,10,16,21,22|8,9,10,16,22,23|8,9,10,16,22,28|8,9,12,13,14,15|8,9,12,13,14,18|8,9,12,13,14,19|8,9,12,13,14,20|8,9,13,14,15,16|8,9,13,14,15,19|8,9,13,14,15,20|8,9,13,14,15,21|8,9,13,14,18,19|8,9,13,14,19,20|8,9,13,14,19,25|8,9,13,14,20,21|8,9,13,14,20,26|8,9,14,15,16,17|8,9,14,15,16,20|8,9,14,15,16,21|8,9,14,15,16,22|8,9,14,15,19,20|8,9,14,15,20,21|8,9,14,15,20,26|8,9,14,15,21,22|8,9,14,15,21,27|8,9,14,18,19,20|8,9,14,19,20,21|8,9,14,19,20,25|8,9,14,19,20,26|8,9,14,20,21,22|8,9,14,20,21,26|8,9,14,20,21,27|8,9,14,20,25,26|8,9,14,20,26,27|8,9,14,20,26,32|8,9,11,15,16,17|8,9,15,16,17,21|8,9,15,16,17,22|8,9,15,16,17,23|8,9,15,16,20,21|8,9,15,16,21,22|8,9,15,16,21,27|8,9,15,16,22,23|8,9,15,16,22,28|8,9,15,19,20,21|8,9,15,20,21,22|8,9,15,20,21,26|8,9,15,20,21,27|8,9,15,21,22,23|8,9,15,21,22,27|8,9,15,21,22,28|8,9,15,21,26,27|8,9,15,21,27,28|8,9,15,21,27,33|8,12,13,14,15,16|8,12,13,14,15,18|8,12,13,14,15,19|8,12,13,14,15,20|8,12,13,14,15,21|8,12,13,14,18,19|8,12,13,14,18,20|8,12,13,14,18,24|8,12,13,14,19,20|8,12,13,14,19,25|8,12,13,14,20,21|8,12,13,14,20,26|8,10,13,14,15,16|8,13,14,15,16,17|8,13,14,15,16,19|8,13,14,15,16,20|8,13,14,15,16,21|8,13,14,15,16,22|8,13,14,15,18,19|8,13,14,15,19,20|8,13,14,15,19,21|8,13,14,15,19,25|8,13,14,15,20,21|8,13,14,15,20,26|8,13,14,15,21,22|8,13,14,15,21,27|8,13,14,18,19,20|8,13,14,18,19,24|8,13,14,18,19,25|8,13,14,19,20,21|8,13,14,19,20,25|8,13,14,19,20,26|8,13,14,19,24,25|8,13,14,19,25,26|8,13,14,19,25,31|8,13,14,20,21,22|8,13,14,20,21,26|8,13,14,20,21,27|8,13,14,20,25,26|8,13,14,20,26,27|8,13,14,20,26,32|8,10,11,14,15,16|8,10,14,15,16,17|8,10,14,15,16,20|8,10,14,15,16,21|8,10,14,15,16,22|8,11,14,15,16,17|8,14,15,16,17,20|8,14,15,16,17,21|8,14,15,16,17,22|8,14,15,16,17,23|8,14,15,16,19,20|8,14,15,16,20,21|8,14,15,16,20,22|8,14,15,16,20,26|8,14,15,16,21,22|8,14,15,16,21,27|8,14,15,16,22,23|8,14,15,16,22,28|8,14,15,18,19,20|8,14,15,19,20,21|8,14,15,19,20,25|8,14,15,19,20,26|8,14,15,20,21,22|8,14,15,20,21,26|8,14,15,20,21,27|8,14,15,20,25,26|8,14,15,20,26,27|8,14,15,20,26,32|8,14,15,21,22,23|8,14,15,21,22,27|8,14,15,21,22,28|8,14,15,21,26,27|8,14,15,21,27,28|8,14,15,21,27,33|8,12,14,18,19,20|8,14,18,19,20,21|8,14,18,19,20,24|8,14,18,19,20,25|8,14,18,19,20,26|8,14,19,20,21,22|8,14,19,20,21,25|8,14,19,20,21,26|8,14,19,20,21,27|8,14,19,20,24,25|8,14,19,20,25,26|8,14,19,20,25,31|8,14,19,20,26,27|8,14,19,20,26,32|8,14,16,20,21,22|8,14,20,21,22,23|8,14,20,21,22,26|8,14,20,21,22,27|8,14,20,21,22,28|8,14,20,21,25,26|8,14,20,21,26,27|8,14,20,21,26,32|8,14,20,21,27,28|8,14,20,21,27,33|8,14,20,24,25,26|8,14,20,25,26,27|8,14,20,25,26,31|8,14,20,25,26,32|8,14,20,26,27,28|8,14,20,26,27,32|8,14,20,26,27,33|8,14,20,26,31,32|8,14,20,26,32,33|9,10,11,13,14,15|9,10,11,14,15,16|9,10,11,14,15,17|9,10,11,14,15,20|9,10,11,14,15,21|9,10,11,15,16,17|9,10,11,15,16,21|9,10,11,15,16,22|9,10,11,15,17,21|9,10,11,15,17,23|9,10,11,15,20,21|9,10,11,15,21,22|9,10,11,15,21,27|9,10,11,16,17,22|9,10,11,16,17,23|9,10,11,16,21,22|9,10,11,16,22,23|9,10,11,16,22,28|9,10,11,17,22,23|9,10,11,17,23,29|9,10,12,13,14,15|9,10,13,14,15,16|9,10,13,14,15,19|9,10,13,14,15,20|9,10,13,14,15,21|9,10,14,15,16,17|9,10,14,15,16,20|9,10,14,15,16,21|9,10,14,15,16,22|9,10,14,15,19,20|9,10,14,15,20,21|9,10,14,15,20,26|9,10,14,15,21,22|9,10,14,15,21,27|9,10,15,16,17,21|9,10,15,16,17,22|9,10,15,16,17,23|9,10,15,16,20,21|9,10,15,16,21,22|9,10,15,16,21,27|9,10,15,16,22,23|9,10,15,16,22,28|9,10,15,19,20,21|9,10,15,20,21,22|9,10,15,20,21,26|9,10,15,20,21,27|9,10,15,21,22,23|9,10,15,21,22,27|9,10,15,21,22,28|9,10,15,21,26,27|9,10,15,21,27,28|9,10,15,21,27,33|9,10,16,17,21,22|9,10,16,17,22,23|9,10,16,17,22,28|9,10,16,17,23,29|9,10,16,20,21,22|9,10,16,21,22,23|9,10,16,21,22,27|9,10,16,21,22,28|9,10,16,22,23,28|9,10,16,22,23,29|9,10,16,22,27,28|9,10,16,22,28,29|9,10,16,22,28,34|9,12,13,14,15,16|9,12,13,14,15,18|9,12,13,14,15,19|9,12,13,14,15,20|9,12,13,14,15,21|9,13,14,15,16,17|9,13,14,15,16,19|9,13,14,15,16,20|9,13,14,15,16,21|9,13,14,15,16,22|9,13,14,15,18,19|9,13,14,15,19,20|9,13,14,15,19,21|9,13,14,15,19,25|9,13,14,15,20,21|9,13,14,15,20,26|9,13,14,15,21,22|9,13,14,15,21,27|9,11,14,15,16,17|9,14,15,16,17,20|9,14,15,16,17,21|9,14,15,16,17,22|9,14,15,16,17,23|9,14,15,16,19,20|9,14,15,16,20,21|9,14,15,16,20,22|9,14,15,16,20,26|9,14,15,16,21,22|9,14,15,16,21,27|9,14,15,16,22,23|9,14,15,16,22,28|9,14,15,18,19,20|9,14,15,19,20,21|9,14,15,19,20,25|9,14,15,19,20,26|9,14,15,20,21,22|9,14,15,20,21,26|9,14,15,20,21,27|9,14,15,20,25,26|9,14,15,20,26,27|9,14,15,20,26,32|9,14,15,21,22,23|9,14,15,21,22,27|9,14,15,21,22,28|9,14,15,21,26,27|9,14,15,21,27,28|9,14,15,21,27,33|9,11,15,16,17,21|9,11,15,16,17,22|9,11,15,16,17,23|9,15,16,17,20,21|9,15,16,17,21,22|9,15,16,17,21,23|9,15,16,17,21,27|9,15,16,17,22,23|9,15,16,17,22,28|9,15,16,17,23,29|9,15,16,19,20,21|9,15,16,20,21,22|9,15,16,20,21,26|9,15,16,20,21,27|9,15,16,21,22,23|9,15,16,21,22,27|9,15,16,21,22,28|9,15,16,21,26,27|9,15,16,21,27,28|9,15,16,21,27,33|9,15,16,22,23,28|9,15,16,22,23,29|9,15,16,22,27,28|9,15,16,22,28,29|9,15,16,22,28,34|9,13,15,19,20,21|9,15,18,19,20,21|9,15,19,20,21,22|9,15,19,20,21,25|9,15,19,20,21,26|9,15,19,20,21,27|9,15,20,21,22,23|9,15,20,21,22,26|9,15,20,21,22,27|9,15,20,21,22,28|9,15,20,21,25,26|9,15,20,21,26,27|9,15,20,21,26,32|9,15,20,21,27,28|9,15,20,21,27,33|9,15,17,21,22,23|9,15,21,22,23,27|9,15,21,22,23,28|9,15,21,22,23,29|9,15,21,22,26,27|9,15,21,22,27,28|9,15,21,22,27,33|9,15,21,22,28,29|9,15,21,22,28,34|9,15,21,25,26,27|9,15,21,26,27,28|9,15,21,26,27,32|9,15,21,26,27,33|9,15,21,27,28,29|9,15,21,27,28,33|9,15,21,27,28,34|9,15,21,27,32,33|9,15,21,27,33,34|10,11,13,14,15,16|10,11,14,15,16,17|10,11,14,15,16,20|10,11,14,15,16,21|10,11,14,15,16,22|10,11,15,16,17,21|10,11,15,16,17,22|10,11,15,16,17,23|10,11,15,16,20,21|10,11,15,16,21,22|10,11,15,16,21,27|10,11,15,16,22,23|10,11,15,16,22,28|10,11,16,17,21,22|10,11,16,17,22,23|10,11,16,17,22,28|10,11,16,17,23,29|10,11,16,20,21,22|10,11,16,21,22,23|10,11,16,21,22,27|10,11,16,21,22,28|10,11,16,22,23,28|10,11,16,22,23,29|10,11,16,22,27,28|10,11,16,22,28,29|10,11,16,22,28,34|10,11,17,21,22,23|10,11,17,22,23,28|10,11,17,22,23,29|10,11,17,23,28,29|10,11,17,23,29,35|10,12,13,14,15,16|10,13,14,15,16,17|10,13,14,15,16,19|10,13,14,15,16,20|10,13,14,15,16,21|10,13,14,15,16,22|10,14,15,16,17,20|10,14,15,16,17,21|10,14,15,16,17,22|10,14,15,16,17,23|10,14,15,16,19,20|10,14,15,16,20,21|10,14,15,16,20,22|10,14,15,16,20,26|10,14,15,16,21,22|10,14,15,16,21,27|10,14,15,16,22,23|10,14,15,16,22,28|10,15,16,17,20,21|10,15,16,17,21,22|10,15,16,17,21,23|10,15,16,17,21,27|10,15,16,17,22,23|10,15,16,17,22,28|10,15,16,17,23,29|10,15,16,19,20,21|10,15,16,20,21,22|10,15,16,20,21,26|10,15,16,20,21,27|10,15,16,21,22,23|10,15,16,21,22,27|10,15,16,21,22,28|10,15,16,21,26,27|10,15,16,21,27,28|10,15,16,21,27,33|10,15,16,22,23,28|10,15,16,22,23,29|10,15,16,22,27,28|10,15,16,22,28,29|10,15,16,22,28,34|10,16,17,20,21,22|10,16,17,21,22,23|10,16,17,21,22,27|10,16,17,21,22,28|10,16,17,22,23,28|10,16,17,22,23,29|10,16,17,22,27,28|10,16,17,22,28,29|10,16,17,22,28,34|10,16,17,23,28,29|10,16,17,23,29,35|10,14,16,20,21,22|10,16,19,20,21,22|10,16,20,21,22,23|10,16,20,21,22,26|10,16,20,21,22,27|10,16,20,21,22,28|10,16,21,22,23,27|10,16,21,22,23,28|10,16,21,22,23,29|10,16,21,22,26,27|10,16,21,22,27,28|10,16,21,22,27,33|10,16,21,22,28,29|10,16,21,22,28,34|10,16,22,23,27,28|10,16,22,23,28,29|10,16,22,23,28,34|10,16,22,23,29,35|10,16,22,26,27,28|10,16,22,27,28,29|10,16,22,27,28,33|10,16,22,27,28,34|10,16,22,28,29,34|10,16,22,28,29,35|10,16,22,28,33,34|10,16,22,28,34,35|11,13,14,15,16,17|11,14,15,16,17,20|11,14,15,16,17,21|11,14,15,16,17,22|11,14,15,16,17,23|11,15,16,17,20,21|11,15,16,17,21,22|11,15,16,17,21,23|11,15,16,17,21,27|11,15,16,17,22,23|11,15,16,17,22,28|11,15,16,17,23,29|11,16,17,20,21,22|11,16,17,21,22,23|11,16,17,21,22,27|11,16,17,21,22,28|11,16,17,22,23,28|11,16,17,22,23,29|11,16,17,22,27,28|11,16,17,22,28,29|11,16,17,22,28,34|11,16,17,23,28,29|11,16,17,23,29,35|11,15,17,21,22,23|11,17,20,21,22,23|11,17,21,22,23,27|11,17,21,22,23,28|11,17,21,22,23,29|11,17,22,23,27,28|11,17,22,23,28,29|11,17,22,23,28,34|11,17,22,23,29,35|11,17,23,27,28,29|11,17,23,28,29,34|11,17,23,28,29,35|11,17,23,29,34,35|12,13,14,15,16,18|12,13,14,15,16,19|12,13,14,15,16,20|12,13,14,15,16,21|12,13,14,15,16,22|12,13,14,15,18,19|12,13,14,15,18,20|12,13,14,15,18,21|12,13,14,15,18,24|12,13,14,15,19,20|12,13,14,15,19,21|12,13,14,15,19,25|12,13,14,15,20,21|12,13,14,15,20,26|12,13,14,15,21,22|12,13,14,15,21,27|12,13,14,18,19,20|12,13,14,18,19,24|12,13,14,18,19,25|12,13,14,18,20,21|12,13,14,18,20,24|12,13,14,18,20,26|12,13,14,18,24,25|12,13,14,18,24,30|12,13,14,19,20,21|12,13,14,19,20,25|12,13,14,19,20,26|12,13,14,19,24,25|12,13,14,19,25,26|12,13,14,19,25,31|12,13,14,20,21,22|12,13,14,20,21,26|12,13,14,20,21,27|12,13,14,20,25,26|12,13,14,20,26,27|12,13,14,20,26,32|12,13,18,19,20,21|12,13,18,19,20,24|12,13,18,19,20,25|12,13,18,19,20,26|12,13,18,19,24,25|12,13,18,19,24,30|12,13,18,19,25,26|12,13,18,19,25,31|12,13,18,24,25,26|12,13,18,24,25,30|12,13,18,24,25,31|12,13,18,24,30,31|12,13,15,19,20,21|12,13,19,20,21,22|12,13,19,20,21,25|12,13,19,20,21,26|12,13,19,20,21,27|12,13,19,20,24,25|12,13,19,20,25,26|12,13,19,20,25,31|12,13,19,20,26,27|12,13,19,20,26,32|12,13,19,24,25,26|12,13,19,24,25,30|12,13,19,24,25,31|12,13,19,25,26,27|12,13,19,25,26,31|12,13,19,25,26,32|12,13,19,25,30,31|12,13,19,25,31,32|12,14,15,18,19,20|12,14,18,19,20,21|12,14,18,19,20,24|12,14,18,19,20,25|12,14,18,19,20,26|12,15,18,19,20,21|12,18,19,20,21,22|12,18,19,20,21,24|12,18,19,20,21,25|12,18,19,20,21,26|12,18,19,20,21,27|12,18,19,20,24,25|12,18,19,20,24,26|12,18,19,20,24,30|12,18,19,20,25,26|12,18,19,20,25,31|12,18,19,20,26,27|12,18,19,20,26,32|12,18,19,24,25,26|12,18,19,24,25,30|12,18,19,24,25,31|12,18,19,24,30,31|12,18,19,25,26,27|12,18,19,25,26,31|12,18,19,25,26,32|12,18,19,25,30,31|12,18,19,25,31,32|12,18,20,24,25,26|12,18,24,25,26,27|12,18,24,25,26,30|12,18,24,25,26,31|12,18,24,25,26,32|12,18,24,25,30,31|12,18,24,25,31,32|12,18,24,30,31,32|13,14,15,16,17,19|13,14,15,16,17,20|13,14,15,16,17,21|13,14,15,16,17,22|13,14,15,16,17,23|13,14,15,16,18,19|13,14,15,16,19,20|13,14,15,16,19,21|13,14,15,16,19,22|13,14,15,16,19,25|13,14,15,16,20,21|13,14,15,16,20,22|13,14,15,16,20,26|13,14,15,16,21,22|13,14,15,16,21,27|13,14,15,16,22,23|13,14,15,16,22,28|13,14,15,18,19,20|13,14,15,18,19,21|13,14,15,18,19,24|13,14,15,18,19,25|13,14,15,19,20,21|13,14,15,19,20,25|13,14,15,19,20,26|13,14,15,19,21,22|13,14,15,19,21,25|13,14,15,19,21,27|13,14,15,19,24,25|13,14,15,19,25,26|13,14,15,19,25,31|13,14,15,20,21,22|13,14,15,20,21,26|13,14,15,20,21,27|13,14,15,20,25,26|13,14,15,20,26,27|13,14,15,20,26,32|13,14,15,21,22,23|13,14,15,21,22,27|13,14,15,21,22,28|13,14,15,21,26,27|13,14,15,21,27,28|13,14,15,21,27,33|13,14,18,19,20,21|13,14,18,19,20,24|13,14,18,19,20,25|13,14,18,19,20,26|13,14,18,19,24,25|13,14,18,19,24,30|13,14,18,19,25,26|13,14,18,19,25,31|13,14,19,20,21,22|13,14,19,20,21,25|13,14,19,20,21,26|13,14,19,20,21,27|13,14,19,20,24,25|13,14,19,20,25,26|13,14,19,20,25,31|13,14,19,20,26,27|13,14,19,20,26,32|13,14,19,24,25,26|13,14,19,24,25,30|13,14,19,24,25,31|13,14,19,25,26,27|13,14,19,25,26,31|13,14,19,25,26,32|13,14,19,25,30,31|13,14,19,25,31,32|13,14,16,20,21,22|13,14,20,21,22,23|13,14,20,21,22,26|13,14,20,21,22,27|13,14,20,21,22,28|13,14,20,21,25,26|13,14,20,21,26,27|13,14,20,21,26,32|13,14,20,21,27,28|13,14,20,21,27,33|13,14,20,24,25,26|13,14,20,25,26,27|13,14,20,25,26,31|13,14,20,25,26,32|13,14,20,26,27,28|13,14,20,26,27,32|13,14,20,26,27,33|13,14,20,26,31,32|13,14,20,26,32,33|13,15,18,19,20,21|13,18,19,20,21,22|13,18,19,20,21,24|13,18,19,20,21,25|13,18,19,20,21,26|13,18,19,20,21,27|13,18,19,20,24,25|13,18,19,20,24,26|13,18,19,20,24,30|13,18,19,20,25,26|13,18,19,20,25,31|13,18,19,20,26,27|13,18,19,20,26,32|13,18,19,24,25,26|13,18,19,24,25,30|13,18,19,24,25,31|13,18,19,24,30,31|13,18,19,25,26,27|13,18,19,25,26,31|13,18,19,25,26,32|13,18,19,25,30,31|13,18,19,25,31,32|13,15,16,19,20,21|13,15,19,20,21,22|13,15,19,20,21,25|13,15,19,20,21,26|13,15,19,20,21,27|13,16,19,20,21,22|13,19,20,21,22,23|13,19,20,21,22,25|13,19,20,21,22,26|13,19,20,21,22,27|13,19,20,21,22,28|13,19,20,21,24,25|13,19,20,21,25,26|13,19,20,21,25,27|13,19,20,21,25,31|13,19,20,21,26,27|13,19,20,21,26,32|13,19,20,21,27,28|13,19,20,21,27,33|13,19,20,24,25,26|13,19,20,24,25,30|13,19,20,24,25,31|13,19,20,25,26,27|13,19,20,25,26,31|13,19,20,25,26,32|13,19,20,25,30,31|13,19,20,25,31,32|13,19,20,26,27,28|13,19,20,26,27,32|13,19,20,26,27,33|13,19,20,26,31,32|13,19,20,26,32,33|13,19,24,25,26,27|13,19,24,25,26,30|13,19,24,25,26,31|13,19,24,25,26,32|13,19,24,25,30,31|13,19,24,25,31,32|13,19,21,25,26,27|13,19,25,26,27,28|13,19,25,26,27,31|13,19,25,26,27,32|13,19,25,26,27,33|13,19,25,26,30,31|13,19,25,26,31,32|13,19,25,26,32,33|13,19,25,30,31,32|13,19,25,31,32,33|14,15,16,17,19,20|14,15,16,17,20,21|14,15,16,17,20,22|14,15,16,17,20,23|14,15,16,17,20,26|14,15,16,17,21,22|14,15,16,17,21,23|14,15,16,17,21,27|14,15,16,17,22,23|14,15,16,17,22,28|14,15,16,17,23,29|14,15,16,18,19,20|14,15,16,19,20,21|14,15,16,19,20,22|14,15,16,19,20,25|14,15,16,19,20,26|14,15,16,20,21,22|14,15,16,20,21,26|14,15,16,20,21,27|14,15,16,20,22,23|14,15,16,20,22,26|14,15,16,20,22,28|14,15,16,20,25,26|14,15,16,20,26,27|14,15,16,20,26,32|14,15,16,21,22,23|14,15,16,21,22,27|14,15,16,21,22,28|14,15,16,21,26,27|14,15,16,21,27,28|14,15,16,21,27,33|14,15,16,22,23,28|14,15,16,22,23,29|14,15,16,22,27,28|14,15,16,22,28,29|14,15,16,22,28,34|14,15,18,19,20,21|14,15,18,19,20,24|14,15,18,19,20,25|14,15,18,19,20,26|14,15,19,20,21,22|14,15,19,20,21,25|14,15,19,20,21,26|14,15,19,20,21,27|14,15,19,20,24,25|14,15,19,20,25,26|14,15,19,20,25,31|14,15,19,20,26,27|14,15,19,20,26,32|14,15,20,21,22,23|14,15,20,21,22,26|14,15,20,21,22,27|14,15,20,21,22,28|14,15,20,21,25,26|14,15,20,21,26,27|14,15,20,21,26,32|14,15,20,21,27,28|14,15,20,21,27,33|14,15,20,24,25,26|14,15,20,25,26,27|14,15,20,25,26,31|14,15,20,25,26,32|14,15,20,26,27,28|14,15,20,26,27,32|14,15,20,26,27,33|14,15,20,26,31,32|14,15,20,26,32,33|14,15,17,21,22,23|14,15,21,22,23,27|14,15,21,22,23,28|14,15,21,22,23,29|14,15,21,22,26,27|14,15,21,22,27,28|14,15,21,22,27,33|14,15,21,22,28,29|14,15,21,22,28,34|14,15,21,25,26,27|14,15,21,26,27,28|14,15,21,26,27,32|14,15,21,26,27,33|14,15,21,27,28,29|14,15,21,27,28,33|14,15,21,27,28,34|14,15,21,27,32,33|14,15,21,27,33,34|14,18,19,20,21,22|14,18,19,20,21,24|14,18,19,20,21,25|14,18,19,20,21,26|14,18,19,20,21,27|14,18,19,20,24,25|14,18,19,20,24,26|14,18,19,20,24,30|14,18,19,20,25,26|14,18,19,20,25,31|14,18,19,20,26,27|14,18,19,20,26,32|14,16,19,20,21,22|14,19,20,21,22,23|14,19,20,21,22,25|14,19,20,21,22,26|14,19,20,21,22,27|14,19,20,21,22,28|14,19,20,21,24,25|14,19,20,21,25,26|14,19,20,21,25,27|14,19,20,21,25,31|14,19,20,21,26,27|14,19,20,21,26,32|14,19,20,21,27,28|14,19,20,21,27,33|14,19,20,24,25,26|14,19,20,24,25,30|14,19,20,24,25,31|14,19,20,25,26,27|14,19,20,25,26,31|14,19,20,25,26,32|14,19,20,25,30,31|14,19,20,25,31,32|14,19,20,26,27,28|14,19,20,26,27,32|14,19,20,26,27,33|14,19,20,26,31,32|14,19,20,26,32,33|14,16,17,20,21,22|14,16,20,21,22,23|14,16,20,21,22,26|14,16,20,21,22,27|14,16,20,21,22,28|14,17,20,21,22,23|14,20,21,22,23,26|14,20,21,22,23,27|14,20,21,22,23,28|14,20,21,22,23,29|14,20,21,22,25,26|14,20,21,22,26,27|14,20,21,22,26,28|14,20,21,22,26,32|14,20,21,22,27,28|14,20,21,22,27,33|14,20,21,22,28,29|14,20,21,22,28,34|14,20,21,24,25,26|14,20,21,25,26,27|14,20,21,25,26,31|14,20,21,25,26,32|14,20,21,26,27,28|14,20,21,26,27,32|14,20,21,26,27,33|14,20,21,26,31,32|14,20,21,26,32,33|14,20,21,27,28,29|14,20,21,27,28,33|14,20,21,27,28,34|14,20,21,27,32,33|14,20,21,27,33,34|14,18,20,24,25,26|14,20,24,25,26,27|14,20,24,25,26,30|14,20,24,25,26,31|14,20,24,25,26,32|14,20,25,26,27,28|14,20,25,26,27,31|14,20,25,26,27,32|14,20,25,26,27,33|14,20,25,26,30,31|14,20,25,26,31,32|14,20,25,26,32,33|14,20,22,26,27,28|14,20,26,27,28,29|14,20,26,27,28,32|14,20,26,27,28,33|14,20,26,27,28,34|14,20,26,27,31,32|14,20,26,27,32,33|14,20,26,27,33,34|14,20,26,30,31,32|14,20,26,31,32,33|14,20,26,32,33,34|15,16,17,19,20,21|15,16,17,20,21,22|15,16,17,20,21,23|15,16,17,20,21,26|15,16,17,20,21,27|15,16,17,21,22,23|15,16,17,21,22,27|15,16,17,21,22,28|15,16,17,21,23,27|15,16,17,21,23,29|15,16,17,21,26,27|15,16,17,21,27,28|15,16,17,21,27,33|15,16,17,22,23,28|15,16,17,22,23,29|15,16,17,22,27,28|15,16,17,22,28,29|15,16,17,22,28,34|15,16,17,23,28,29|15,16,17,23,29,35|15,16,18,19,20,21|15,16,19,20,21,22|15,16,19,20,21,25|15,16,19,20,21,26|15,16,19,20,21,27|15,16,20,21,22,23|15,16,20,21,22,26|15,16,20,21,22,27|15,16,20,21,22,28|15,16,20,21,25,26|15,16,20,21,26,27|15,16,20,21,26,32|15,16,20,21,27,28|15,16,20,21,27,33|15,16,21,22,23,27|15,16,21,22,23,28|15,16,21,22,23,29|15,16,21,22,26,27|15,16,21,22,27,28|15,16,21,22,27,33|15,16,21,22,28,29|15,16,21,22,28,34|15,16,21,25,26,27|15,16,21,26,27,28|15,16,21,26,27,32|15,16,21,26,27,33|15,16,21,27,28,29|15,16,21,27,28,33|15,16,21,27,28,34|15,16,21,27,32,33|15,16,21,27,33,34|15,16,22,23,27,28|15,16,22,23,28,29|15,16,22,23,28,34|15,16,22,23,29,35|15,16,22,26,27,28|15,16,22,27,28,29|15,16,22,27,28,33|15,16,22,27,28,34|15,16,22,28,29,34|15,16,22,28,29,35|15,16,22,28,33,34|15,16,22,28,34,35|15,18,19,20,21,22|15,18,19,20,21,24|15,18,19,20,21,25|15,18,19,20,21,26|15,18,19,20,21,27|15,19,20,21,22,23|15,19,20,21,22,25|15,19,20,21,22,26|15,19,20,21,22,27|15,19,20,21,22,28|15,19,20,21,24,25|15,19,20,21,25,26|15,19,20,21,25,27|15,19,20,21,25,31|15,19,20,21,26,27|15,19,20,21,26,32|15,19,20,21,27,28|15,19,20,21,27,33|15,17,20,21,22,23|15,20,21,22,23,26|15,20,21,22,23,27|15,20,21,22,23,28|15,20,21,22,23,29|15,20,21,22,25,26|15,20,21,22,26,27|15,20,21,22,26,28|15,20,21,22,26,32|15,20,21,22,27,28|15,20,21,22,27,33|15,20,21,22,28,29|15,20,21,22,28,34|15,20,21,24,25,26|15,20,21,25,26,27|15,20,21,25,26,31|15,20,21,25,26,32|15,20,21,26,27,28|15,20,21,26,27,32|15,20,21,26,27,33|15,20,21,26,31,32|15,20,21,26,32,33|15,20,21,27,28,29|15,20,21,27,28,33|15,20,21,27,28,34|15,20,21,27,32,33|15,20,21,27,33,34|15,17,21,22,23,27|15,17,21,22,23,28|15,17,21,22,23,29|15,21,22,23,26,27|15,21,22,23,27,28|15,21,22,23,27,29|15,21,22,23,27,33|15,21,22,23,28,29|15,21,22,23,28,34|15,21,22,23,29,35|15,21,22,25,26,27|15,21,22,26,27,28|15,21,22,26,27,32|15,21,22,26,27,33|15,21,22,27,28,29|15,21,22,27,28,33|15,21,22,27,28,34|15,21,22,27,32,33|15,21,22,27,33,34|15,21,22,28,29,34|15,21,22,28,29,35|15,21,22,28,33,34|15,21,22,28,34,35|15,19,21,25,26,27|15,21,24,25,26,27|15,21,25,26,27,28|15,21,25,26,27,31|15,21,25,26,27,32|15,21,25,26,27,33|15,21,26,27,28,29|15,21,26,27,28,32|15,21,26,27,28,33|15,21,26,27,28,34|15,21,26,27,31,32|15,21,26,27,32,33|15,21,26,27,33,34|15,21,23,27,28,29|15,21,27,28,29,33|15,21,27,28,29,34|15,21,27,28,29,35|15,21,27,28,32,33|15,21,27,28,33,34|15,21,27,28,34,35|15,21,27,31,32,33|15,21,27,32,33,34|15,21,27,33,34,35|16,17,19,20,21,22|16,17,20,21,22,23|16,17,20,21,22,26|16,17,20,21,22,27|16,17,20,21,22,28|16,17,21,22,23,27|16,17,21,22,23,28|16,17,21,22,23,29|16,17,21,22,26,27|16,17,21,22,27,28|16,17,21,22,27,33|16,17,21,22,28,29|16,17,21,22,28,34|16,17,22,23,27,28|16,17,22,23,28,29|16,17,22,23,28,34|16,17,22,23,29,35|16,17,22,26,27,28|16,17,22,27,28,29|16,17,22,27,28,33|16,17,22,27,28,34|16,17,22,28,29,34|16,17,22,28,29,35|16,17,22,28,33,34|16,17,22,28,34,35|16,17,23,27,28,29|16,17,23,28,29,34|16,17,23,28,29,35|16,17,23,29,34,35|16,18,19,20,21,22|16,19,20,21,22,23|16,19,20,21,22,25|16,19,20,21,22,26|16,19,20,21,22,27|16,19,20,21,22,28|16,20,21,22,23,26|16,20,21,22,23,27|16,20,21,22,23,28|16,20,21,22,23,29|16,20,21,22,25,26|16,20,21,22,26,27|16,20,21,22,26,28|16,20,21,22,26,32|16,20,21,22,27,28|16,20,21,22,27,33|16,20,21,22,28,29|16,20,21,22,28,34|16,21,22,23,26,27|16,21,22,23,27,28|16,21,22,23,27,29|16,21,22,23,27,33|16,21,22,23,28,29|16,21,22,23,28,34|16,21,22,23,29,35|16,21,22,25,26,27|16,21,22,26,27,28|16,21,22,26,27,32|16,21,22,26,27,33|16,21,22,27,28,29|16,21,22,27,28,33|16,21,22,27,28,34|16,21,22,27,32,33|16,21,22,27,33,34|16,21,22,28,29,34|16,21,22,28,29,35|16,21,22,28,33,34|16,21,22,28,34,35|16,22,23,26,27,28|16,22,23,27,28,29|16,22,23,27,28,33|16,22,23,27,28,34|16,22,23,28,29,34|16,22,23,28,29,35|16,22,23,28,33,34|16,22,23,28,34,35|16,22,23,29,34,35|16,20,22,26,27,28|16,22,25,26,27,28|16,22,26,27,28,29|16,22,26,27,28,32|16,22,26,27,28,33|16,22,26,27,28,34|16,22,27,28,29,33|16,22,27,28,29,34|16,22,27,28,29,35|16,22,27,28,32,33|16,22,27,28,33,34|16,22,27,28,34,35|16,22,28,29,33,34|16,22,28,29,34,35|16,22,28,32,33,34|16,22,28,33,34,35|17,19,20,21,22,23|17,20,21,22,23,26|17,20,21,22,23,27|17,20,21,22,23,28|17,20,21,22,23,29|17,21,22,23,26,27|17,21,22,23,27,28|17,21,22,23,27,29|17,21,22,23,27,33|17,21,22,23,28,29|17,21,22,23,28,34|17,21,22,23,29,35|17,22,23,26,27,28|17,22,23,27,28,29|17,22,23,27,28,33|17,22,23,27,28,34|17,22,23,28,29,34|17,22,23,28,29,35|17,22,23,28,33,34|17,22,23,28,34,35|17,22,23,29,34,35|17,21,23,27,28,29|17,23,26,27,28,29|17,23,27,28,29,33|17,23,27,28,29,34|17,23,27,28,29,35|17,23,28,29,33,34|17,23,28,29,34,35|17,23,29,33,34,35|18,19,20,21,22,24|18,19,20,21,22,25|18,19,20,21,22,26|18,19,20,21,22,27|18,19,20,21,22,28|18,19,20,21,24,25|18,19,20,21,24,26|18,19,20,21,24,27|18,19,20,21,24,30|18,19,20,21,25,26|18,19,20,21,25,27|18,19,20,21,25,31|18,19,20,21,26,27|18,19,20,21,26,32|18,19,20,21,27,28|18,19,20,21,27,33|18,19,20,24,25,26|18,19,20,24,25,30|18,19,20,24,25,31|18,19,20,24,26,27|18,19,20,24,26,30|18,19,20,24,26,32|18,19,20,24,30,31|18,19,20,25,26,27|18,19,20,25,26,31|18,19,20,25,26,32|18,19,20,25,30,31|18,19,20,25,31,32|18,19,20,26,27,28|18,19,20,26,27,32|18,19,20,26,27,33|18,19,20,26,31,32|18,19,20,26,32,33|18,19,24,25,26,27|18,19,24,25,26,30|18,19,24,25,26,31|18,19,24,25,26,32|18,19,24,25,30,31|18,19,24,25,31,32|18,19,24,30,31,32|18,19,21,25,26,27|18,19,25,26,27,28|18,19,25,26,27,31|18,19,25,26,27,32|18,19,25,26,27,33|18,19,25,26,30,31|18,19,25,26,31,32|18,19,25,26,32,33|18,19,25,30,31,32|18,19,25,31,32,33|18,20,21,24,25,26|18,20,24,25,26,27|18,20,24,25,26,30|18,20,24,25,26,31|18,20,24,25,26,32|18,21,24,25,26,27|18,24,25,26,27,28|18,24,25,26,27,30|18,24,25,26,27,31|18,24,25,26,27,32|18,24,25,26,27,33|18,24,25,26,30,31|18,24,25,26,30,32|18,24,25,26,31,32|18,24,25,26,32,33|18,24,25,30,31,32|18,24,25,31,32,33|18,24,26,30,31,32|18,24,30,31,32,33|19,20,21,22,23,25|19,20,21,22,23,26|19,20,21,22,23,27|19,20,21,22,23,28|19,20,21,22,23,29|19,20,21,22,24,25|19,20,21,22,25,26|19,20,21,22,25,27|19,20,21,22,25,28|19,20,21,22,25,31|19,20,21,22,26,27|19,20,21,22,26,28|19,20,21,22,26,32|19,20,21,22,27,28|19,20,21,22,27,33|19,20,21,22,28,29|19,20,21,22,28,34|19,20,21,24,25,26|19,20,21,24,25,27|19,20,21,24,25,30|19,20,21,24,25,31|19,20,21,25,26,27|19,20,21,25,26,31|19,20,21,25,26,32|19,20,21,25,27,28|19,20,21,25,27,31|19,20,21,25,27,33|19,20,21,25,30,31|19,20,21,25,31,32|19,20,21,26,27,28|19,20,21,26,27,32|19,20,21,26,27,33|19,20,21,26,31,32|19,20,21,26,32,33|19,20,21,27,28,29|19,20,21,27,28,33|19,20,21,27,28,34|19,20,21,27,32,33|19,20,21,27,33,34|19,20,24,25,26,27|19,20,24,25,26,30|19,20,24,25,26,31|19,20,24,25,26,32|19,20,24,25,30,31|19,20,24,25,31,32|19,20,25,26,27,28|19,20,25,26,27,31|19,20,25,26,27,32|19,20,25,26,27,33|19,20,25,26,30,31|19,20,25,26,31,32|19,20,25,26,32,33|19,20,25,30,31,32|19,20,25,31,32,33|19,20,22,26,27,28|19,20,26,27,28,29|19,20,26,27,28,32|19,20,26,27,28,33|19,20,26,27,28,34|19,20,26,27,31,32|19,20,26,27,32,33|19,20,26,27,33,34|19,20,26,30,31,32|19,20,26,31,32,33|19,20,26,32,33,34|19,21,24,25,26,27|19,24,25,26,27,28|19,24,25,26,27,30|19,24,25,26,27,31|19,24,25,26,27,32|19,24,25,26,27,33|19,24,25,26,30,31|19,24,25,26,30,32|19,24,25,26,31,32|19,24,25,26,32,33|19,24,25,30,31,32|19,24,25,31,32,33|19,21,22,25,26,27|19,21,25,26,27,28|19,21,25,26,27,31|19,21,25,26,27,32|19,21,25,26,27,33|19,22,25,26,27,28|19,25,26,27,28,29|19,25,26,27,28,31|19,25,26,27,28,32|19,25,26,27,28,33|19,25,26,27,28,34|19,25,26,27,30,31|19,25,26,27,31,32|19,25,26,27,31,33|19,25,26,27,32,33|19,25,26,27,33,34|19,25,26,30,31,32|19,25,26,31,32,33|19,25,26,32,33,34|19,25,30,31,32,33|19,25,27,31,32,33|19,25,31,32,33,34|20,21,22,23,25,26|20,21,22,23,26,27|20,21,22,23,26,28|20,21,22,23,26,29|20,21,22,23,26,32|20,21,22,23,27,28|20,21,22,23,27,29|20,21,22,23,27,33|20,21,22,23,28,29|20,21,22,23,28,34|20,21,22,23,29,35|20,21,22,24,25,26|20,21,22,25,26,27|20,21,22,25,26,28|20,21,22,25,26,31|20,21,22,25,26,32|20,21,22,26,27,28|20,21,22,26,27,32|20,21,22,26,27,33|20,21,22,26,28,29|20,21,22,26,28,32|20,21,22,26,28,34|20,21,22,26,31,32|20,21,22,26,32,33|20,21,22,27,28,29|20,21,22,27,28,33|20,21,22,27,28,34|20,21,22,27,32,33|20,21,22,27,33,34|20,21,22,28,29,34|20,21,22,28,29,35|20,21,22,28,33,34|20,21,22,28,34,35|20,21,24,25,26,27|20,21,24,25,26,30|20,21,24,25,26,31|20,21,24,25,26,32|20,21,25,26,27,28|20,21,25,26,27,31|20,21,25,26,27,32|20,21,25,26,27,33|20,21,25,26,30,31|20,21,25,26,31,32|20,21,25,26,32,33|20,21,26,27,28,29|20,21,26,27,28,32|20,21,26,27,28,33|20,21,26,27,28,34|20,21,26,27,31,32|20,21,26,27,32,33|20,21,26,27,33,34|20,21,26,30,31,32|20,21,26,31,32,33|20,21,26,32,33,34|20,21,23,27,28,29|20,21,27,28,29,33|20,21,27,28,29,34|20,21,27,28,29,35|20,21,27,28,32,33|20,21,27,28,33,34|20,21,27,28,34,35|20,21,27,31,32,33|20,21,27,32,33,34|20,21,27,33,34,35|20,24,25,26,27,28|20,24,25,26,27,30|20,24,25,26,27,31|20,24,25,26,27,32|20,24,25,26,27,33|20,24,25,26,30,31|20,24,25,26,30,32|20,24,25,26,31,32|20,24,25,26,32,33|20,22,25,26,27,28|20,25,26,27,28,29|20,25,26,27,28,31|20,25,26,27,28,32|20,25,26,27,28,33|20,25,26,27,28,34|20,25,26,27,30,31|20,25,26,27,31,32|20,25,26,27,31,33|20,25,26,27,32,33|20,25,26,27,33,34|20,25,26,30,31,32|20,25,26,31,32,33|20,25,26,32,33,34|20,22,23,26,27,28|20,22,26,27,28,29|20,22,26,27,28,32|20,22,26,27,28,33|20,22,26,27,28,34|20,23,26,27,28,29|20,26,27,28,29,32|20,26,27,28,29,33|20,26,27,28,29,34|20,26,27,28,29,35|20,26,27,28,31,32|20,26,27,28,32,33|20,26,27,28,32,34|20,26,27,28,33,34|20,26,27,28,34,35|20,26,27,30,31,32|20,26,27,31,32,33|20,26,27,32,33,34|20,26,27,33,34,35|20,24,26,30,31,32|20,26,30,31,32,33|20,26,31,32,33,34|20,26,28,32,33,34|20,26,32,33,34,35|21,22,23,25,26,27|21,22,23,26,27,28|21,22,23,26,27,29|21,22,23,26,27,32|21,22,23,26,27,33|21,22,23,27,28,29|21,22,23,27,28,33|21,22,23,27,28,34|21,22,23,27,29,33|21,22,23,27,29,35|21,22,23,27,32,33|21,22,23,27,33,34|21,22,23,28,29,34|21,22,23,28,29,35|21,22,23,28,33,34|21,22,23,28,34,35|21,22,23,29,34,35|21,22,24,25,26,27|21,22,25,26,27,28|21,22,25,26,27,31|21,22,25,26,27,32|21,22,25,26,27,33|21,22,26,27,28,29|21,22,26,27,28,32|21,22,26,27,28,33|21,22,26,27,28,34|21,22,26,27,31,32|21,22,26,27,32,33|21,22,26,27,33,34|21,22,27,28,29,33|21,22,27,28,29,34|21,22,27,28,29,35|21,22,27,28,32,33|21,22,27,28,33,34|21,22,27,28,34,35|21,22,27,31,32,33|21,22,27,32,33,34|21,22,27,33,34,35|21,22,28,29,33,34|21,22,28,29,34,35|21,22,28,32,33,34|21,22,28,33,34,35|21,24,25,26,27,28|21,24,25,26,27,30|21,24,25,26,27,31|21,24,25,26,27,32|21,24,25,26,27,33|21,25,26,27,28,29|21,25,26,27,28,31|21,25,26,27,28,32|21,25,26,27,28,33|21,25,26,27,28,34|21,25,26,27,30,31|21,25,26,27,31,32|21,25,26,27,31,33|21,25,26,27,32,33|21,25,26,27,33,34|21,23,26,27,28,29|21,26,27,28,29,32|21,26,27,28,29,33|21,26,27,28,29,34|21,26,27,28,29,35|21,26,27,28,31,32|21,26,27,28,32,33|21,26,27,28,32,34|21,26,27,28,33,34|21,26,27,28,34,35|21,26,27,30,31,32|21,26,27,31,32,33|21,26,27,32,33,34|21,26,27,33,34,35|21,23,27,28,29,33|21,23,27,28,29,34|21,23,27,28,29,35|21,27,28,29,32,33|21,27,28,29,33,34|21,27,28,29,33,35|21,27,28,29,34,35|21,27,28,31,32,33|21,27,28,32,33,34|21,27,28,33,34,35|21,25,27,31,32,33|21,27,30,31,32,33|21,27,31,32,33,34|21,27,32,33,34,35|21,27,29,33,34,35|22,23,25,26,27,28|22,23,26,27,28,29|22,23,26,27,28,32|22,23,26,27,28,33|22,23,26,27,28,34|22,23,27,28,29,33|22,23,27,28,29,34|22,23,27,28,29,35|22,23,27,28,32,33|22,23,27,28,33,34|22,23,27,28,34,35|22,23,28,29,33,34|22,23,28,29,34,35|22,23,28,32,33,34|22,23,28,33,34,35|22,23,29,33,34,35|22,24,25,26,27,28|22,25,26,27,28,29|22,25,26,27,28,31|22,25,26,27,28,32|22,25,26,27,28,33|22,25,26,27,28,34|22,26,27,28,29,32|22,26,27,28,29,33|22,26,27,28,29,34|22,26,27,28,29,35|22,26,27,28,31,32|22,26,27,28,32,33|22,26,27,28,32,34|22,26,27,28,33,34|22,26,27,28,34,35|22,27,28,29,32,33|22,27,28,29,33,34|22,27,28,29,33,35|22,27,28,29,34,35|22,27,28,31,32,33|22,27,28,32,33,34|22,27,28,33,34,35|22,28,29,32,33,34|22,28,29,33,34,35|22,26,28,32,33,34|22,28,31,32,33,34|22,28,32,33,34,35|23,25,26,27,28,29|23,26,27,28,29,32|23,26,27,28,29,33|23,26,27,28,29,34|23,26,27,28,29,35|23,27,28,29,32,33|23,27,28,29,33,34|23,27,28,29,33,35|23,27,28,29,34,35|23,28,29,32,33,34|23,28,29,33,34,35|23,27,29,33,34,35|23,29,32,33,34,35|24,25,26,27,28,30|24,25,26,27,28,31|24,25,26,27,28,32|24,25,26,27,28,33|24,25,26,27,28,34|24,25,26,27,30,31|24,25,26,27,30,32|24,25,26,27,30,33|24,25,26,27,31,32|24,25,26,27,31,33|24,25,26,27,32,33|24,25,26,27,33,34|24,25,26,30,31,32|24,25,26,30,32,33|24,25,26,31,32,33|24,25,26,32,33,34|24,25,30,31,32,33|24,25,27,31,32,33|24,25,31,32,33,34|24,26,27,30,31,32|24,26,30,31,32,33|24,27,30,31,32,33|24,30,31,32,33,34|25,26,27,28,29,31|25,26,27,28,29,32|25,26,27,28,29,33|25,26,27,28,29,34|25,26,27,28,29,35|25,26,27,28,30,31|25,26,27,28,31,32|25,26,27,28,31,33|25,26,27,28,31,34|25,26,27,28,32,33|25,26,27,28,32,34|25,26,27,28,33,34|25,26,27,28,34,35|25,26,27,30,31,32|25,26,27,30,31,33|25,26,27,31,32,33|25,26,27,31,33,34|25,26,27,32,33,34|25,26,27,33,34,35|25,26,30,31,32,33|25,26,31,32,33,34|25,26,28,32,33,34|25,26,32,33,34,35|25,27,30,31,32,33|25,30,31,32,33,34|25,27,28,31,32,33|25,27,31,32,33,34|25,28,31,32,33,34|25,31,32,33,34,35|26,27,28,29,31,32|26,27,28,29,32,33|26,27,28,29,32,34|26,27,28,29,32,35|26,27,28,29,33,34|26,27,28,29,33,35|26,27,28,29,34,35|26,27,28,30,31,32|26,27,28,31,32,33|26,27,28,31,32,34|26,27,28,32,33,34|26,27,28,32,34,35|26,27,28,33,34,35|26,27,30,31,32,33|26,27,31,32,33,34|26,27,32,33,34,35|26,27,29,33,34,35|26,30,31,32,33,34|26,28,31,32,33,34|26,31,32,33,34,35|26,28,29,32,33,34|26,28,32,33,34,35|26,29,32,33,34,35|27,28,29,31,32,33|27,28,29,32,33,34|27,28,29,32,33,35|27,28,29,33,34,35|27,28,30,31,32,33|27,28,31,32,33,34|27,28,32,33,34,35|27,30,31,32,33,34|27,31,32,33,34,35|27,29,32,33,34,35|28,29,31,32,33,34|28,29,32,33,34,35|28,30,31,32,33,34|28,31,32,33,34,35|29,31,32,33,34,35"
        .Split('|')
        .Select(str => str.Split(',').Select(int.Parse).ToArray())
        .ToArray();

    public static int[][] GenerateRegions(MonoRandom rnd)
    {
        int[][] recurse(int[][] sofar, int[][][] candidates, bool[] used)
        {
            if (sofar.Length == 6)
                return sofar;

            var bestCell = -1;
            int[][] bestCandidates = null;
            for (var cell = 0; cell < 36; cell++)
                if (!used[cell])
                {
                    if (bestCandidates == null || candidates[cell].Length < bestCandidates.Length)
                    {
                        bestCandidates = candidates[cell];
                        bestCell = cell;
                        if (bestCandidates.Length == 0)
                            return null;
                        if (bestCandidates.Length == 1)
                            break;
                    }
                }

            foreach (var candidate in bestCandidates)
            {
                var newSofar = sofar.Append(candidate);
                var newUsed = used.ToArray();
                var newCandidates = candidates.ToArray();
                foreach (var cell in candidate)
                {
                    newUsed[cell] = true;
                    newCandidates[cell] = null;
                }
                for (var cell = 0; cell < 36; cell++)
                    if (!newUsed[cell])
                        newCandidates[cell] = newCandidates[cell].Where(reg => !reg.Intersect(candidate).Any()).ToArray();
                if (recurse(newSofar, newCandidates, newUsed) is { } solution)
                    return solution;
            }
            return null;
        }
        return recurse([], Enumerable.Range(0, 36).Select(cell => rnd.ShuffleFisherYates(_allRegions.Where(reg => reg.Contains(cell)).ToArray())).ToArray(), new bool[36]);
    }

    private readonly string TwitchHelpMessage = @"!{0} 123 [press buttons 1 2 3, top to bottom]";

    List<KMSelectable> ProcessTwitchCommand(string command)
    {
        var btns = new List<KMSelectable>();
        foreach (var ch in command)
            if ("123".Contains(ch))
                btns.Add(Buttons[ch - '1']);
            else if (!char.IsWhiteSpace(ch))
                return null;
        return btns;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (!_activated)
            yield return true;

        while (!_moduleSolved)
        {
            Buttons[GetCorrectButtonToPress()].OnInteract();
            yield return new WaitForSeconds(.1f);
        }
    }
}
