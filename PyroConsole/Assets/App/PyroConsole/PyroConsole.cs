// ReSharper disable DelegateSubtraction

namespace App.PyroConsole
{
    using System;
    using System.IO;
    using System.Text;
    using System.Linq;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using UnityEditor;
    using TMPro;
    using UniRx;
    using Flow;
    using Pyro.Exec;
    using Pyro.ExecutionContext;
    using Pyro.Language;
    using Pyro.Network;
    using Pyro.RhoLang.Lexer;
    using Newtonsoft.Json;

    /// <summary>
    /// Interactive console supporting all Pyro languages.
    /// </summary>
    public partial class PyroConsole
        : MonoBehaviour
    {
        public int ListenPort = 9999;

        public Button SaveButton;
        public Button LoadButton;
        public Toggle PiToggle;
        public Toggle RhoToggle;
        public Canvas Canvas;
        public ConsoleInput PiInput;
        public ConsoleInput RhoInput;
        public TextMeshProUGUI Output;
        public TextMeshProUGUI ColoredPi;
        public TextMeshProUGUI ColoredRho;
        public Slider FontSize;
        public Color[] Colors;
        public TextAsset RhoTheme;
        public TextAsset[] StartupScripts;

        private bool _booted;
        private Stack<object> _data => _pyro.Executor.DataStack;
        private List<Continuation> _context => _pyro.Executor.ContextStack;
        private readonly Context _pyro = new Context { Language = ELanguage.Rho };
        private readonly IReactiveProperty<bool> _active = new ReactiveProperty<bool>(false);
        private string _scriptPath => Path.Combine(Application.dataPath, "Pyro/Scripts");
        private string _rc => Path.Combine(Application.persistentDataPath, "Pyro.rc").Replace('\\', '/');
        private string _lastPi => Path.Combine(Application.persistentDataPath, "Last.pi").Replace('\\', '/');
        private string _lastRho => Path.Combine(Application.persistentDataPath, "Last.rho").Replace('\\', '/');
        private ColoriseRho _colorise => _coloriseRho; // TODO: Pi coloring
        private ColoriseRho _coloriseRho;
        private readonly ColoriseRho _colorisePi;
        private readonly IReactiveProperty<float> _fontSize = new ReactiveProperty<float>(36);

        private IPeer _peer;
        private string HostName => _peer?.Remote?.HostName ?? "local";
        private int HostPort => _peer?.Remote?.HostPort ?? 0;

        private static readonly string[] _namespaces =
        {
            "App.Views.Impl", "App.Agents.Impl", "App.Models.Impl",
            "App", "App.Simple", "UnityEngine", "UnityEngine.UI"
        };

        private ConsoleInput _input => _pyro.Language == ELanguage.Pi ? PiInput : RhoInput;
        private TextMeshProUGUI _coloredInput => _pyro.Language == ELanguage.Pi ? ColoredPi : ColoredRho;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);

            LoadTheme();
            FontSize.value = _fontSize.Value;
            FontSize.onValueChanged.AddListener(UpdateFontSize);
            PiInput.onValueChanged.AddListener(s => PiInput.NeedUpdate = true);
            RhoInput.onValueChanged.AddListener(s => RhoInput.NeedUpdate = true);
            PiToggle.onValueChanged.AddListener(on => SetLang(on ? ELanguage.Pi : ELanguage.Rho));
        }

        private void UpdateFontSize(float size)
        {
            PiInput.pointSize = size;
            RhoInput.pointSize = size;
            ColoredPi.fontSize = size;
            ColoredRho.fontSize = size;
            LogText.fontSize = size;
            Output.fontSize = size;
        }

        [ContextMenu("Reload Theme")]
        private void LoadTheme()
        {
            _coloriseRho = new ColoriseRho(JsonConvert.DeserializeObject<Dictionary<ERhoToken, string>>(RhoTheme.text));
        }

        private void SetLang(ELanguage lang)
        {
            _pyro.Language = lang;
            PiInput.gameObject.SetActive(lang == ELanguage.Pi);
            RhoInput.gameObject.SetActive(lang == ELanguage.Rho);
        }

        private void Start()
        {
            PiInput.customCaretColor = true;
            PiInput.caretColor = new Color(1f, 0.58f, 0f);
            PiInput.selectionColor = new Color(1f, 0.79f, 0.5f);

            RhoInput.customCaretColor = true;
            RhoInput.caretColor = new Color(0.87f, 0f, 1f);
            RhoInput.selectionColor = new Color(0.94f, 0.52f, 1f);

            StartPeer();
            //_peer.Execute("1 2 3");
        }

        protected void Shutdown()
        {
            WriteConsole(ELogLevel.Info, "Shutting down...");
            _peer?.Stop();
            //Error("Done");
        }

        private bool StartPeer()
        {
            _peer = Pyro.Network.Create.NewPeer(ListenPort);
            //_peer.OnReceivedRequest += (server, client, text) => WriteConsole(ELogLevel.Verbose, text);

            _peer.OnWrite += (t, c) => WriteConsole(ELogLevel.Info, t);
            _peer.OnReceivedRequest += _peer_OnReceivedRequest;
            _peer.OnReceivedResponse += _peer_OnReceivedResponse;
            _peer.OnConnected += PeerOnOnConnected;

            return _peer.SelfHost() || Error("Failed to start local server");
        }

        private void PeerOnOnConnected(IPeer peer, IClient client)
        {
            //WriteConsole(ELogLevel.Info, $"Connected to {client}");
        }

        private void OnEnable()
        {
            PiInput.OnSubmitLine += Exec;
            RhoInput.OnSubmitLine += Exec;

        }

        private void _peer_OnReceivedResponse(IClient client, string text)
        {
            //WriteConsole(ELogLevel.Warn, $"Response: {server} {client}: {text}");
            _needRefresh = true;
        }

        bool _needRefresh;

        private void _peer_OnReceivedRequest(IClient client, string text)
        {
            Debug.Log($"Received {text}");
            _needRefresh = true;
        }

        public void Refresh()
        {
            _needRefresh = true;
        }

        private void OnDisable()
        {
            PiInput.OnSubmitLine -= Exec;
            RhoInput.OnSubmitLine -= Exec;

            Shutdown();
        }

        void Exec(string text)
        {
            Execute(text);
        }

        private void Update()
        {
            if (_needRefresh)
                WriteStack();

            if (Input.GetKeyDown(KeyCode.F1))
                _active.Value = !_active.Value;

            if (_input.NeedUpdate)
            {
                _input.NeedUpdate = false;
                _coloredInput.text = _colorise.Colorise(_input.text);
            }
        }

        private void ToggleActive(bool active)
        {
            Canvas.enabled = active;
            if (active)
                _input.Select();
        }

        // TODO: make these work outside of editor space
#if UNITY_EDITOR
        private void Save()
        {
            var fileName = EditorUtility.SaveFilePanel("Save Pyro Script", _scriptPath, "NewPyroScript", "txt");
            if (!string.IsNullOrEmpty(fileName))
                File.WriteAllText(Path.Combine(_scriptPath, fileName), _input.text);
        }

        private void Load()
        {
            var fileName = EditorUtility.OpenFilePanel("Load Pyro Script", _scriptPath, "txt");
            if (!string.IsNullOrEmpty(fileName))
                _input.text = File.ReadAllText(fileName);
        }
#endif

        private void LoadPrevious()
        {
            if (File.Exists(_rc))
                _pyro.ExecFile(_rc);

            foreach (var script in StartupScripts)
            {
                if (script == null)
                    continue;

                WriteConsole(ELogLevel.Info, $"Executing: {script.name}.");
                Execute(script.text);
            }

            if (File.Exists(_lastPi))
                PiInput.text = File.ReadAllText(_lastPi);
            if (File.Exists(_lastRho))
                RhoInput.text = File.ReadAllText(_lastRho);
        }

        private void OnApplicationQuit()
        {
            File.WriteAllText(_lastPi, PiInput.text);
            File.WriteAllText(_lastRho, RhoInput.text);
        }

        private static object GetInstance(string typeName)
            => FindType(typeName, out var type) ? FindObjectOfType(type) : null;

        private static object GetInstances(string typeName)
            => FindType(typeName, out var type) ? FindObjectsOfType(type) : null;

        private static bool FindType(string typeName, out Type type)
        {
            type = Type.GetType(typeName);
            if (type == null)
            {
                foreach (var ns in _namespaces)
                {
                    type = Type.GetType($"{ns}.{typeName}");
                    if (type != null)
                        return true;
                }
            }

            Debug.LogError($"Failed to find type {typeName}");
            return false;
        }

        private bool Execute(string input)
        {
            if (string.IsNullOrEmpty(input))
                return true;

            if (PreProcess(input))
                return true;

            try
            {
                input = input.Replace('\'', '`');
                if (!_pyro.Translate(input, out var cont))
                    return Error(_pyro.Error);

                if (!_peer.Execute(cont))
                    return Error(_peer.Error);
            }
            catch (Exception e)
            {
                return Error($"{e.Message}: {e.InnerException?.Message}");
            }

            WriteStack();

            return true;
        }

        private bool Error(string text)
        {
            // TODO: popup over error location.
            Debug.LogError($"{text}");
            WriteConsole(ELogLevel.Error, text);
            return false;
        }

        private bool PreProcess(string input)
        {
            switch (input.Trim())
            {
                case "help":
                    Application.OpenURL("https://github.com/cschladetsch/Pyro/blob/develop/Readme.md");
                    return true;
                case "pi":
                    _pyro.Language = ELanguage.Pi;
                    return true;
                case "rho":
                    _pyro.Language = ELanguage.Rho;
                    return true;
            }

            return false;
        }

        private bool WriteStack()
        {
            Output.text = LocalDataStackToString();
            _needRefresh = false;
            return true;
        }

        public string LocalDataStackToString(int max = 50)
        {
            var sb = new StringBuilder();
            var n = 0;
            var results = _peer.Remote.Results();
            foreach (var result in results.ToList())
            {
                sb.AppendLine($"<color=#a0a0a0>{n++:D2}</color> {_pyro.Registry.ToPiScript(result)}");
                if (n > max)
                    break;
            }

            return sb.ToString();
        }
    }
}

