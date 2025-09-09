using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Twodoku;
using UnityEngine;

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

    public Material[] SymbolMaterials;
    public Material[] NumberMaterials;
    public Material EmptyMaterial;
    public Material HighlightMaterial;

    public MeshRenderer[] Squares;
    public KMSelectable[] Buttons;
    public TextMesh[] ButtonLabels;
    public TextMesh SolutionDisplay;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool _moduleSolved;

    private string _solutionWord;
    private int _solutionIx = 0;
    private int _inputLevel = 0;
    private int _inputStart = 0;

    void Start()
    {
        _moduleId = _moduleIdCounter++;

        for (var i = 0; i < 3; i++)
            ButtonLabels[i].text = "";
        for (var i = 0; i < 36; i++)
            Squares[i].sharedMaterial = EmptyMaterial;
        for (int i = 0; i < Buttons.Length; i++)
            Buttons[i].OnInteract += ButtonPress(i);
        SolutionDisplay.text = "";

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

            var reqLetter = _solutionWord[_solutionIx] - 'A';
            var isCorrect = _inputLevel switch
            {
                0 => reqLetter / 9 == btn,
                1 => (reqLetter / 3) % 3 == btn,
                _ => reqLetter % 3 == btn,
            };
            if (isCorrect)
            {
                _inputLevel++;
                if (_inputLevel == 3)
                {
                    _solutionIx++;
                    _inputLevel = 0;
                    SolutionDisplay.text = $"<color=#E053FF>{_solutionWord.Substring(0, _solutionIx)}</color><color=#FFFFFF>│</color>";
                }
                if (_solutionIx == _solutionWord.Length)
                {
                    _moduleSolved = true;
                    Module.HandlePass();
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                    Debug.Log($"[Twodoku #{_moduleId}] Module solved.");
                    SolutionDisplay.text = $"<color=#00FF00>{_solutionWord}</color>";
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

    private static string[] _buttonLevel0 = "A-I|J-R|S-Z".Split('|');
    private static bool[][] _arrangements = "1108378656,1108379664,1108410912,1108412424,1108476432,1108476936,1109410848,1109411856,1109459232,1109460996,1109524752,1109525508,1111507488,1111509000,1111523616,1111525380,1111621896,1111622148,1115701776,1115702280,1115717904,1115718660,1115750664,1115750916,1141408800,1141409808,1141441056,1141442568,1141506576,1141507080,1142957088,1142958096,1143013536,1143015426,1143079056,1143079938,1145053728,1145055240,1145077920,1145079810,1145176200,1145176578,1149248016,1149248520,1149272208,1149273090,1149304968,1149305346,1208501280,1208502288,1208549664,1208551428,1208615184,1208615940,1209017376,1209018384,1209073824,1209075714,1209139344,1209140226,1212162336,1212164100,1212170400,1212172290,1212285060,1212285186,1216356624,1216357380,1216364688,1216365570,1216413828,1216413954,1342718496,1342720008,1342734624,1342736388,1342832904,1342833156,1343234592,1343236104,1343258784,1343260674,1343357064,1343357442,1344282912,1344284676,1344290976,1344292866,1344405636,1344405762,1350574344,1350574596,1350582408,1350582786,1350598788,1350598914,1611153936,1611154440,1611170064,1611170820,1611202824,1611203076,1611670032,1611670536,1611694224,1611695106,1611726984,1611727362,1612718352,1612719108,1612726416,1612727298,1612775556,1612775682,1614815496,1614815748,1614823560,1614823938,1614839940,1614840066,2165343264,2165344272,2165375520,2165377032,2165441040,2165441544,2166375456,2166376464,2166423840,2166425604,2166489360,2166490116,2168472096,2168473608,2168488224,2168489988,2168586504,2168586756,2172666384,2172666888,2172682512,2172683268,2172715272,2172715524,2214888480,2214889488,2214920736,2214922248,2214986256,2214986760,2216694816,2216695824,2216755296,2216757249,2216820816,2216821761,2218791456,2218792968,2218819680,2218821633,2218917960,2218918401,2222985744,2222986248,2223013968,2223014913,2223046728,2223047169,2281980960,2281981968,2282029344,2282031108,2282094864,2282095620,2282755104,2282756112,2282815584,2282817537,2282881104,2282882049,2285900064,2285901828,2285912160,2285914113,2286026820,2286027009,2290094352,2290095108,2290106448,2290107393,2290155588,2290155777,2416198176,2416199688,2416214304,2416216068,2416312584,2416312836,2416972320,2416973832,2417000544,2417002497,2417098824,2417099265,2418020640,2418022404,2418032736,2418034689,2418147396,2418147585,2424312072,2424312324,2424324168,2424324609,2424340548,2424340737,2684633616,2684634120,2684649744,2684650500,2684682504,2684682756,2685407760,2685408264,2685435984,2685436929,2685468744,2685469185,2686456080,2686456836,2686468176,2686469121,2686517316,2686517505,2688553224,2688553476,2688565320,2688565761,2688581700,2688581889,4312302624,4312303632,4312334880,4312336392,4312400400,4312400904,4313850912,4313851920,4313907360,4313909250,4313972880,4313973762,4315947552,4315949064,4315971744,4315973634,4316070024,4316070402,4320141840,4320142344,4320166032,4320166914,4320198792,4320199170,4328817696,4328818704,4328849952,4328851464,4328915472,4328915976,4330624032,4330625040,4330684512,4330686465,4330750032,4330750977,4332720672,4332722184,4332748896,4332750849,4332847176,4332847617,4336914960,4336915464,4336943184,4336944129,4336975944,4336976385,4429456416,4429457424,4429512864,4429514754,4429578384,4429579266,4429714464,4429715472,4429774944,4429776897,4429840464,4429841409,4433383584,4433385474,4433387616,4433389569,4433510466,4433510529,4437577872,4437578754,4437581904,4437582849,4437639234,4437639297,4563673632,4563675144,4563697824,4563699714,4563796104,4563796482,4563931680,4563933192,4563959904,4563961857,4564058184,4564058625,4565504160,4565506050,4565508192,4565510145,4565631042,4565631105,4571795592,4571795970,4571799624,4571800065,4571824194,4571824257,4832109072,4832109576,4832133264,4832134146,4832166024,4832166402,4832367120,4832367624,4832395344,4832396289,4832428104,4832428545,4833939600,4833940482,4833943632,4833944577,4834000962,4834001025,4836036744,4836037122,4836040776,4836041217,4836065346,4836065409,8607253536,8607254544,8607301920,8607303684,8607367440,8607368196,8607769632,8607770640,8607826080,8607827970,8607891600,8607892482,8610914592,8610916356,8610922656,8610924546,8611037316,8611037442,8615108880,8615109636,8615116944,8615117826,8615166084,8615166210,8623768608,8623769616,8623816992,8623818756,8623882512,8623883268,8624542752,8624543760,8624603232,8624605185,8624668752,8624669697,8627687712,8627689476,8627699808,8627701761,8627814468,8627814657,8631882000,8631882756,8631894096,8631895041,8631943236,8631943425,8657314848,8657315856,8657371296,8657373186,8657436816,8657437698,8657572896,8657573904,8657633376,8657635329,8657698896,8657699841,8661242016,8661243906,8661246048,8661248001,8661368898,8661368961,8665436304,8665437186,8665440336,8665441281,8665497666,8665497729,8858640672,8858642436,8858648736,8858650626,8858763396,8858763522,8858898720,8858900484,8858910816,8858912769,8859025476,8859025665,8859422880,8859424770,8859426912,8859428865,8859549762,8859549825,8866762884,8866763010,8866766916,8866767105,8866775106,8866775169,9127076112,9127076868,9127084176,9127085058,9127133316,9127133442,9127334160,9127334916,9127346256,9127347201,9127395396,9127395585,9127858320,9127859202,9127862352,9127863297,9127919682,9127919745,9131004036,9131004162,9131008068,9131008257,9131016258,9131016321,17197187616,17197189128,17197203744,17197205508,17197302024,17197302276,17197703712,17197705224,17197727904,17197729794,17197826184,17197826562,17198752032,17198753796,17198760096,17198761986,17198874756,17198874882,17205043464,17205043716,17205051528,17205051906,17205067908,17205068034,17213702688,17213704200,17213718816,17213720580,17213817096,17213817348,17214476832,17214478344,17214505056,17214507009,17214603336,17214603777,17215525152,17215526916,17215537248,17215539201,17215651908,17215652097,17221816584,17221816836,17221828680,17221829121,17221845060,17221845249,17247248928,17247250440,17247273120,17247275010,17247371400,17247371778,17247506976,17247508488,17247535200,17247537153,17247633480,17247633921,17249079456,17249081346,17249083488,17249085441,17249206338,17249206401,17255370888,17255371266,17255374920,17255375361,17255399490,17255399553,17314357536,17314359300,17314365600,17314367490,17314480260,17314480386,17314615584,17314617348,17314627680,17314629633,17314742340,17314742529,17315139744,17315141634,17315143776,17315145729,17315266626,17315266689,17322479748,17322479874,17322483780,17322483969,17322491970,17322492033,17717010696,17717010948,17717018760,17717019138,17717035140,17717035266,17717268744,17717268996,17717280840,17717281281,17717297220,17717297409,17717792904,17717793282,17717796936,17717797377,17717821506,17717821569,17718841476,17718841602,17718845508,17718845697,17718853698,17718853761,34377056784,34377057288,34377072912,34377073668,34377105672,34377105924,34377572880,34377573384,34377597072,34377597954,34377629832,34377630210,34378621200,34378621956,34378629264,34378630146,34378678404,34378678530,34380718344,34380718596,34380726408,34380726786,34380742788,34380742914,34393571856,34393572360,34393587984,34393588740,34393620744,34393620996,34394346000,34394346504,34394374224,34394375169,34394406984,34394407425,34395394320,34395395076,34395406416,34395407361,34395455556,34395455745,34397491464,34397491716,34397503560,34397504001,34397519940,34397520129,34427118096,34427118600,34427142288,34427143170,34427175048,34427175426,34427376144,34427376648,34427404368,34427405313,34427437128,34427437569,34428948624,34428949506,34428952656,34428953601,34429009986,34429010049,34431045768,34431046146,34431049800,34431050241,34431074370,34431074433,34494226704,34494227460,34494234768,34494235650,34494283908,34494284034,34494484752,34494485508,34494496848,34494497793,34494545988,34494546177,34495008912,34495009794,34495012944,34495013889,34495070274,34495070337,34498154628,34498154754,34498158660,34498158849,34498166850,34498166913,34628444424,34628444676,34628452488,34628452866,34628468868,34628468994,34628702472,34628702724,34628714568,34628715009,34628730948,34628731137,34629226632,34629227010,34629230664,34629231105,34629255234,34629255297,34630275204,34630275330,34630279236,34630279425,34630287426,34630287489"
        .Split(',')
        .Select(ulong.Parse)
        .Select(u => Enumerable.Range(0, 36).Select(bit => (u & (1ul << bit)) != 0).ToArray())
        .ToArray();

    private static string[] _symbolNames = "ARROW,BOWTIE,CIRCLE,DIAMOND,FIVESTAR,HEXAGON,HONEYCOMB,INFINITY,JULIA,KITE,OVAL,SEMICIRCLE,SQUARE,TRAPEZOID,TRIANGLE".Split(',');
    private static (int sym, int num)[] _allPoss = Enumerable.Range(0, 6 * _symbolNames.Length).Select(i => (sym: i / 6, num: i % 6)).ToArray();

    private static int getTallBox(int cell) => cell % 6 / 2 + 3 * (cell / 18);
    private static int getWideBox(int cell) => cell % 6 / 3 + 2 * (cell / 12);

    public IEnumerator GeneratePuzzle()
    {
        var seed = Rnd.Range(int.MinValue, int.MaxValue);
        var rnd = new System.Random(seed);
        Debug.Log($"[Twodoku #{_moduleId}] Random seed: {seed}");

        var solutionWord = Data.AllWords.PickRandom(rnd);
        var isEditor = Application.isEditor;

        // These are assigned within the thread
        (int cell, bool isNumClue, int clue)[] req = null;
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
                        solution = recurse(new int?[36], new int?[36], startGrid, [], symIsWide, rnd).FirstOrDefault();
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
                        req = Ut.ReduceRequiredSet(Enumerable.Range(0, clues.Length), skipConsistencyTest: true, test: state =>
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
        Debug.Log($"<Twodoku #{_moduleId}> Thread started.");
        var lastSq = 0;
        void log(string msg)
        {
            if (msg.StartsWith("Exception"))
                Debug.LogError($"<Twodoku #{_moduleId}> {msg}");
            else if (msg.StartsWith("♦"))
                Debug.LogWarning($"<Twodoku #{_moduleId}> {msg}");
            else
                Debug.Log($"<Twodoku #{_moduleId}> {msg}");
        }
        while (!threadDone)
        {
            lastSq = (lastSq + Rnd.Range(0, 35)) % 36;
            Squares[lastSq].sharedMaterial = HighlightMaterial;
            yield return new WaitForSeconds(Rnd.Range(.4f, .9f));
            Squares[lastSq].sharedMaterial = EmptyMaterial;
            if (isEditor)
                lock (threadMessages)
                {
                    foreach (var msg in threadMessages)
                        log(msg);
                    threadMessages.Clear();
                }
        }
        if (isEditor)
            foreach (var msg in threadMessages)
                log(msg);
        _solutionWord = solutionWord;
        for (var btn = 0; btn < 3; btn++)
            ButtonLabels[btn].text = _buttonLevel0[btn];
        SolutionDisplay.text = "<color=#FFFFFF>│</color>";

        Debug.Log($"[Twodoku #{_moduleId}] Clues: {req.Select(tup => $"{(char) ('A' + tup.cell % 6)}{tup.cell / 6 + 1}={(tup.isNumClue ? $"{tup.clue + 1}" : _symbolNames[tup.clue])}").JoinString(",")}");
        Debug.Log($"[Twodoku #{_moduleId}] Cells highlighted: {Enumerable.Range(0, 36).Where(cell => arrangement[cell]).Select(cell => $"{(char) ('A' + cell % 6)}{cell / 6 + 1}").JoinString(", ")}");
        Debug.Log($"[Twodoku #{_moduleId}] Solution numbers: {solution.Select(c => c.num + 1).JoinString()}");
        Debug.Log($"[Twodoku #{_moduleId}] Solution symbols: {solution.Select(c => _symbolNames[c.sym]).JoinString(" ")}");
        Debug.Log($"[Twodoku #{_moduleId}] Solution word: {solutionWord}");

        req.Shuffle();
        foreach (var (cell, isNumClue, clue) in req)
            if (isNumClue)
            {
                Squares[cell].sharedMaterial = NumberMaterials[clue];
                yield return new WaitForSeconds(Rnd.Range(.1f, .2f));
            }
        yield return new WaitForSeconds(.6f);
        req.Shuffle();
        foreach (var (cell, isNumClue, clue) in req)
            if (!isNumClue)
            {
                Squares[cell].sharedMaterial = SymbolMaterials[clue];
                yield return new WaitForSeconds(Rnd.Range(.1f, .2f));
            }
        yield return new WaitForSeconds(.6f);
        foreach (var row in Enumerable.Range(0, 6).ToList().Shuffle())
        {
            var col = arrangement.Skip(6 * row).Take(6).IndexOf(true);
            Squares[col + 6 * row].sharedMaterial = HighlightMaterial;
            yield return new WaitForSeconds(Rnd.Range(.1f, .2f));
        }
    }

    private static IEnumerable<(int sym, int num)[]> recurse(int?[] sofarSym, int?[] sofarNum, (int sym, int num)[][] possibilities, int[] symsUsed, bool symIsWide, System.Random rnd)
    {
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
        var tb = getTallBox(bestCell);
        var wb = getWideBox(bestCell);
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
                        ? (ix / 6 == row || ix % 6 == col || (symIsWide ? getTallBox(ix) == tb : getWideBox(ix) == wb) ? np.Where(tup => tup.num != value).ToArray() : np)
                        : (ix / 6 == row || ix % 6 == col || (symIsWide ? getWideBox(ix) == wb : getTallBox(ix) == tb) ? np.Where(tup => tup.sym != value).ToArray() : np))
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

            bool elemNotAvailable(Func<(int sym, int num), int> getter)
            {
                var rowsHave = Ut.NewArray(6, _ => new bool[6]);
                var colsHave = Ut.NewArray(6, _ => new bool[6]);
                for (var i = 0; i < 36; i++)
                    if (newPoss[i] is { } arr)
                        foreach (var tup in arr)
                        {
                            var v = getter(tup);
                            rowsHave[i / 6][v] = true;
                            colsHave[i % 6][v] = true;
                        }
                    else
                    {
                        var v = getter((newSofarSym[i].Value, newSofarNum[i].Value));
                        rowsHave[i / 6][v] = true;
                        colsHave[i % 6][v] = true;
                    }
                return rowsHave.Any(r => r.Contains(false)) || colsHave.Any(c => c.Contains(false));
            }

            // If any row, column or box can no longer contain a particular number or symbol, bail out
            if (elemNotAvailable(tup => tup.num) || (newSymsUsed.Length == 6 && elemNotAvailable(tup => newSymsUsed.IndexOf(tup.sym))))
                continue;

            foreach (var sln in recurse(newSofarSym, newSofarNum, newPoss, newSymsUsed, symIsWide, rnd))
                yield return sln;
        }
    }
}
