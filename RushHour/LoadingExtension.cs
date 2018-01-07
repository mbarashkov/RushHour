using System.Collections.Generic;
using System.Reflection;
using ICities;
using RushHour.Redirection;
using UnityEngine;
using RushHour.UI;
using RushHour.Events;
using RushHour.Experiments;
using RushHour.Logging;
using System;
using System.Collections.Generic;
using RushHour.StatisticsFix;
using RushHour.Utils;

namespace RushHour
{
    public class LoadingExtension : LoadingExtensionBase
    {
        private static Dictionary<MethodInfo, RedirectCallsState> redirects;
        private static bool _redirected = false; //Temporary to solve crashing for now. I think it needs to stop threads from calling it while it's reverting the redirect.
        private static bool _simulationRegistered = false;

        public static GameObject _mainUIGameObject = null;

        private GameObject _dateTimeGameObject = null;
        private DateTimeBar _dateTimeBar = null;
        private SimulationExtension _simulationManager = new SimulationExtension();

        public override void OnCreated(ILoading loading)
        {
            base.OnCreated(loading);

            Debug.Log("Loading up Rush Hour main");
        }

        public class Detour
        {
            public MethodInfo OriginalMethod;
            public MethodInfo CustomMethod;
            public RedirectCallsState Redirect;

            public Detour(MethodInfo originalMethod, MethodInfo customMethod)
            {
                this.OriginalMethod = originalMethod;
                this.CustomMethod = customMethod;
                this.Redirect = RedirectionHelper.RedirectCalls(originalMethod, customMethod);
            }
        }

        public static List<Detour> Detours { get; set; }

        public override void OnLevelLoaded(LoadMode mode)
        {
            base.OnLevelLoaded(mode);

            if (mode == LoadMode.LoadGame || mode == LoadMode.NewGame || (ExperimentsToggle.EnableInScenarios && mode == (LoadMode)11)) //11 is some new mode not implemented in ICities... 
            {
                LoggingWrapper.Log("Loading mod");
                CimTools.CimToolsHandler.CimToolBase.Changelog.DownloadChangelog();
                CimTools.CimToolsHandler.CimToolBase.XMLFileOptions.Load();

                if (!ExperimentsToggle.GhostMode)
                {
                    if (_dateTimeGameObject == null)
                    {
                        _dateTimeGameObject = new GameObject("DateTimeBar");
                    }

                    if (_mainUIGameObject == null)
                    {
                        _mainUIGameObject = new GameObject("RushHourUI");
                        EventPopupManager popupManager = EventPopupManager.Instance;
                    }

                    if (_dateTimeBar == null)
                    {
                        _dateTimeBar = _dateTimeGameObject.AddComponent<DateTimeBar>();
                        _dateTimeBar.Initialise();
                    }

                    if (!_simulationRegistered)
                    {
                        SimulationManager.RegisterSimulationManager(_simulationManager);
                        _simulationRegistered = true;
                        LoggingWrapper.Log("Simulation hooked");
                    }

                    Redirect();

                    Detours = new List<Detour>();
                    bool detourFailed = false;
                    try
                    {
                        var methodToReplace = typeof(TransportLineAI).GetMethod("SimulationStep",
                            new[]
                            {
                        typeof(ushort),
                        typeof(NetNode).MakeByRefType()
                            });
                        var methodToReplaceWith = typeof(CustomTransportLineAI).GetMethod("CustomNodeSimulationStep");

                        if (methodToReplace == null)
                        {
                            Log.Info("methodToReplace == null");
                        }

                        if (methodToReplaceWith == null)
                        {
                            Log.Info("methodToReplaceWith == null");
                        }

                        var detour = new Detour(methodToReplace,
                            methodToReplaceWith);

                        Detours.Add(detour);
                    }
                    catch (Exception ex)
                    {
                        detourFailed = true;
                    }

                    Log.Info("Redirection TransportTool::RenderOverlay calls");
                    detourFailed = false;
                    try
                    {
                        var methodToReplace = typeof(TransportManager).GetMethod("RenderLines",
                            BindingFlags.NonPublic | BindingFlags.Instance);

                        var methodToReplaceWith = typeof(TransportLineRenderer).GetMethod("CustomRenderLines",
                            BindingFlags.NonPublic | BindingFlags.Instance);

                        if (methodToReplace == null)
                        {
                            Log.Info("methodToReplace == null");
                        }

                        if (methodToReplaceWith == null)
                        {
                            Log.Info("methodToReplaceWith == null");
                        }

                        var detour = new Detour(methodToReplace,
                            methodToReplaceWith);
                        Log.Info("Redirection TransportLineAI::RenderOverlay calls step 2");

                        Detours.Add(detour);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Could not redirect TransportTool::RenderOverlay " + ex.Message);
                        detourFailed = true;
                    }
                }
            }
            else
            {
                Debug.Log("Rush Hour is not set to start up in this mode. " + mode.ToString());
            }
        }

        public override void OnLevelUnloading()
        {
            if (!ExperimentsToggle.GhostMode)
            {
                if (ExperimentsToggle.RevertRedirects)
                {
                    RevertRedirect();
                }

                if (_dateTimeBar != null)
                {
                    _dateTimeBar.CloseEverything();
                    _dateTimeBar = null;
                }

                _dateTimeGameObject = null;
                _simulationManager = null;
                _mainUIGameObject = null;
            }

            base.OnLevelUnloading();
        }

        public static void Redirect()
        {
            if (!_redirected || ExperimentsToggle.RevertRedirects)
            {
                _redirected = true;

                redirects = new Dictionary<MethodInfo, RedirectCallsState>();
                foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
                {
                    redirects.AddRange(RedirectionUtil.RedirectType(type));
                }
            }
        }

        private static void RevertRedirect()
        {
            if (redirects == null)
            {
                return;
            }
            foreach (var kvp in redirects)
            {
                RedirectionHelper.RevertRedirect(kvp.Key, kvp.Value);
            }
            redirects.Clear();
        }
    }
}