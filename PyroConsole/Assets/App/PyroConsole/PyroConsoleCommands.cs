namespace App.PyroConsole
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Collections.Generic;
    using UnityEngine;
    using Flow;
    using TMPro;
    using UniRx;
    using Pyro.Impl;

    using static Pyro.Create;

    public partial class PyroConsole
    {
        public TextMeshProUGUI LogText;

        public void Boot()
        {
            if (_booted)
                return;

            _booted = true;

            var main = FindObjectOfType<Main>();

            DontDestroyOnLoad(gameObject);

            _active.Subscribe(ToggleActive).AddTo(this);

//#if UNITY_EDITOR
//            SaveButton.Bind(Save).AddTo(this);
//            LoadButton.Bind(Load).AddTo(this);
//#endif

            var scope = _pyro.Scope;
            LogText.text = string.Empty;

            // accessors
            scope["main"] = main;
            scope["kernel"] = main.Kernel;
            scope["root"] = main.Kernel.Root;
//            scope["server"] = main.Server;
//            scope["nav"] = main.Navigator;

            // traces
            scope["print"] = Function<object>(q => WriteConsole(ELogLevel.Info, q));
            scope["Info"] = Function<object>(q => WriteConsole(ELogLevel.Info, q));
            scope["Warn"] = Function<object>(q => WriteConsole(ELogLevel.Warn, q));
            scope["Error"] = Function<object>(q => WriteConsole(ELogLevel.Error, q));

            // object info
            scope["GetInstance"] = Function<string, object>(GetInstance);
            scope["GetInstances"] = Function<string, object>(GetInstances);
            scope["GetMethods"] = Function<object, List<string>>(q => q.GetType().GetMethods(BindingFlags.ExactBinding).Select(m => m.Name).ToList());
            scope["GetAllMethods"] = Function<object, List<string>>(q => q.GetType().GetMethods().Select(m => m.Name).ToList());
            scope["GetProperties"] = Function<object, List<string>>(q => q.GetType().GetProperties().Select(m => m.Name).ToList());
            scope["GetFields"] = Function<object, List<string>>(q => q.GetType().GetFields().Select(m => m.Name).ToList());
            scope["GetMembers"] = Function<object, List<string>>(q => q.GetType().GetMembers().Select(m => m.Name).ToList());

            // server
            scope["Get"] = NotDone();
            scope["GetJson"] = NotDone();
            scope["GetResource"] = NotDone();
            scope["GetResourceGuid"] = NotDone();
            scope["Health"] = NotDone();

            // basic bash-like commands
            scope["ls"] = Function(() =>
                (Directory.GetDirectories(Directory.GetCurrentDirectory())
                    .Concat(Directory.GetFiles(Directory.GetCurrentDirectory())).ToList()));
            scope["cd"] = Function<string>(Directory.SetCurrentDirectory);
            scope["cat"] = Function<string>(f => File.ReadAllText(f));
            scope["vi"] = NotDone();
            // TODO: make work scope["alias"] = VoidFunction<string, string>(MakeAlias);

            scope["ClearLog"] = Function(() => LogText.text = string.Empty);
            scope["ClearStack"] = Function(() => Execute("`clear`"));
            scope["MyIpAddress"] = NotDone();
            scope["MyLocalIpAddress"] = NotDone();
            scope["ServerIpAddress"] = NotDone();

            // TODO: git commands
            scope["gl"] = NotDone();
            scope["gs"] = NotDone();
            scope["gu"] = NotDone();
            scope["gp"] = NotDone();
            scope["gc"] = NotDone();
            scope["gacp"] = NotDone();
            scope["grhh"] = NotDone();
            scope["issue"] = NotDone();
            scope["issues"] = NotDone();
            scope["browse"] = NotDone();
            scope["mpr"] = NotDone();

            // TODO: Unity commands
            scope["Instantiate"] = NotDone();
            scope["SetPos"] = NotDone();
            scope["SetRot"] = NotDone();

            LoadPrevious();
        }

        private void WriteConsole(ELogLevel level, object output)
        {
            if (output == null)
            {
                WriteConsole(ELogLevel.Error, "null");
                return;
            }

            var text = _pyro.Registry.ToPiScript(output);
            if (output is string)
            {
                // strip surrounding quotes
                text = text.Substring(1, text.Length - 2);
            }

            string color;
            bool bold = false;
            switch (level)
            {
                case ELogLevel.Info:
                    color = "white";
                    break;
                case ELogLevel.Warn:
                    color = "#ebe067";
                    break;
                case ELogLevel.Error:
                    //color = "#d14167";
                    color = "#F1858D";
                    bold = true;
                    break;
                default:
                    color="#b3b5ba";
                    break;
            }

            if (bold)
                LogText.fontStyle |= FontStyles.Bold;
            LogText.text = $"<size=80%><color={color}>{DateTime.Now.ToShortTimeString()}:</size> {text}</color>" + "\n" + LogText.text;
            if (bold)
                LogText.fontStyle ^= FontStyles.Bold;

            //Debug.Log($"Write: {LogText.text}");
        }

        private static VoidFunction NotDone()
            => new VoidFunction(() => Debug.LogError("Not implemented"));
    }
}

