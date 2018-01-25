using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using KSP.UI.Screens;
using UnityEngine;
using Utilities;

namespace NodeHelper
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]

    public class NodeHelperAddon : MonoBehaviour
    {
        const int CleanupInterval = 30;
        const string ZeroVector = "0.0, 0.0, 0.0";
        const string TransShader = "Transparent/Diffuse";
        const bool PrintAdvancedConfig = false;

        readonly Color _nodeColor = UI.GetColorFromRgb (0, 226, 94, 150);
        readonly Color _selectedNodeColor = UI.GetColorFromRgb (249, 255, 94, 200);
        readonly Color _planeColor = UI.GetColorFromRgb (100, 191, 219, 150);
        readonly Color _orientColor = UI.GetColorFromRgb (255, 0, 42, 200);

        HashSet<Part> _affectedParts;

        Dictionary<AttachNode, GameObject> _nodeMapping;
        Dictionary<AttachNode, string> _nodeNameMapping;
        Dictionary<AttachNode, Vector3> _nodePosBackup;

        IButton _nodeHelperButton;

        Material _nodeMaterial;

        AttachNode _selectedNode;

        Part _selectedPart;

        ConfigNode _settings;

        GameObject _orientationPointer;
        GameObject[] _planes;

        bool[] _selectedPartRules;
        bool[] _showPlanes;

        bool _initialized;
        bool inputLockActive;
        bool _printingActive;
        bool _show;
        bool _showCreateMenu;
        bool _showOrientationPointer = true;

        string ToolbarButtonTexture = "NodeHelper" + Path.AltDirectorySeparatorChar + "Textures" + Path.AltDirectorySeparatorChar + "icon_toolbar";

        static ApplicationLauncherButton btnLauncher;

        float _stepWidth = 0.1f;
        float _planeRadius = 0.625f;

        string _stepWidthString = "0.1";
        string _targetPos = ZeroVector;
        string _nodeOrientationCust = ZeroVector;
        string _planeRadiusString = "0.625";
        const string inputLock = "NH_Lock";
        string _newNodeName = "newNode";
        string _newNodePos = ZeroVector;

        int _cleanupCounter;

        //  Need to add code to make sure window isn't off - screen when loading options.
        //  Also need to make sure WindowPos isn't off - screen at start.

        Rect nodeListPos = new Rect (315, 100, 160, 40);
        Rect windowPos = new Rect (1375, 80, 160, 40);
        Rect nodeEditPos = new Rect (315, 470, 160, 40);

        static Vector3 GetGoScaleForNode (AttachNode attachNode)
        {
            return Vector3.one * attachNode.radius * (attachNode.size > 0 ? attachNode.size : 0.2f);
        }

        void HandleActionMenuClosed (Part data)
        {
            if (_selectedPart != null)
            {
                _selectedPart.SetHighlightDefault ();
            }

            _clearMapping ();
        }

        void HandleActionMenuOpened (Part data)
        {
            if (data == null)
            {
                return;
            }

            if (_selectedPart != null && data != _selectedPart)
            {
                HandleActionMenuClosed (null);
            }

            _selectedPart = data;
        }

        public void OnDestroy ()
        {
            GameEvents.onPartActionUIDismiss.Remove (HandleActionMenuClosed);

            GameEvents.onPartActionUICreate.Remove (HandleActionMenuOpened);

            if (btnLauncher != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication (btnLauncher);
            }
        }

        void HandleWindow (ref Rect position, int id, string windowname, GUI.WindowFunction func)
        {
            position = GUILayout.Window (GetType ().FullName.GetHashCode () + id, position, func, windowname, GUILayout.Width (200), GUILayout.Height (20));
        }

        public void OnGUI ()
        {
            if (HighLogic.LoadedScene != GameScenes.EDITOR)
            {
                if (inputLockActive)
                {
                    InputLockManager.RemoveControlLock (inputLock);

                    inputLockActive = false;
                }

                return;
            }

            if (!_show)
            {
                return;
            }

            if (windowPos.x + windowPos.width > Screen.width)
            {
                windowPos.x = Screen.width - windowPos.width;
            }

            if (windowPos.y + windowPos.height > Screen.height)
            {
                windowPos.y = Screen.height - windowPos.height;
            }

            if (nodeListPos.x + nodeListPos.width > Screen.width)
            {
                nodeListPos.x = Screen.width - nodeListPos.width;
            }

            if (nodeListPos.y + nodeListPos.height > Screen.height)
            {
                nodeListPos.y = Screen.height - nodeListPos.height;
            }

            if (nodeEditPos.x + nodeEditPos.width > Screen.width)
            {
                nodeEditPos.x = Screen.width - nodeEditPos.width;
            }

            if (nodeEditPos.y + nodeEditPos.height > Screen.height)
            {
                nodeEditPos.y = Screen.height - nodeEditPos.height;
            }

            HandleWindow (ref nodeListPos, 0, "Node Helper - Node List", NodeListGui);

            if (_selectedPart != null)
            {
                HandleWindow (ref windowPos, 1, "Node Helper - Part Data", WindowGui);
            }

            if (_selectedNode != null)
            {
                HandleWindow (ref nodeEditPos, 2, "Node Helper - Edit Node", NodeEditGui);
            }
        }

        public void Start ()
        {
            _settings = GameDatabase.Instance.GetConfigNodes ("NodeHelperSettings").FirstOrDefault ();

            if (_settings != null)
            {
                int coord;

                if (int.TryParse (_settings.GetNode ("ListWindow").GetValue ("x"), out coord))
                {
                    nodeListPos.x = coord;
                }

                if (int.TryParse (_settings.GetNode ("ListWindow").GetValue ("y"), out coord))
                {
                    nodeListPos.y = coord;
                }

                if (int.TryParse (_settings.GetNode ("PartWindow").GetValue ("x"), out coord))
                {
                    windowPos.x = coord;
                }

                if (int.TryParse (_settings.GetNode ("PartWindow").GetValue ("y"), out coord))
                {
                    windowPos.y = coord;
                }

                if (int.TryParse (_settings.GetNode ("NodeWindow").GetValue ("x"), out coord))
                {
                    nodeEditPos.x = coord;
                }

                if (int.TryParse (_settings.GetNode ("NodeWindow").GetValue ("y"), out coord))
                {
                    nodeEditPos.y = coord;
                }
            }
            else
            {
                string ConfigFilePath = KSPUtil.ApplicationRootPath + "GameData" + Path.AltDirectorySeparatorChar + "NodeHelper" + Path.AltDirectorySeparatorChar + "Configs" + Path.AltDirectorySeparatorChar + "NodeHelper_Settings.cfg";

                var ConfigNodeSettings = new ConfigNode ();

                var NodeHelperSettings = ConfigNodeSettings.AddNode ("NodeHelperSettings");

                var ListWindowNode = NodeHelperSettings.AddNode ("ListWindow");

                ListWindowNode.AddValue ("x", 100);
                ListWindowNode.AddValue ("y", 100);

                var NodeWindowNode = NodeHelperSettings.AddNode ("NodeWindow");

                NodeWindowNode.AddValue ("x", 300);
                NodeWindowNode.AddValue ("y", 300);

                var PartWindowNode = NodeHelperSettings.AddNode ("PartWindow");

                PartWindowNode.AddValue ("x", 200);
                PartWindowNode.AddValue ("y", 200);

                ConfigNodeSettings.Save (ConfigFilePath);
            }

            bool UseBlizzyToolbar = HighLogic.CurrentGame.Parameters.CustomParams<NodeHelper>()._blizzyToolbar;

            if (UseBlizzyToolbar.Equals (false))
            {
                if (btnLauncher == null)
                {
                    btnLauncher = ApplicationLauncher.Instance.AddModApplication (() => _show = !_show, () => _show = !_show, null, null, null, null, ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, GameDatabase.Instance.GetTexture (ToolbarButtonTexture, false));
                }
            }
            else
            {
                if (ToolbarManager.Instance == null)
                {
                    _nodeHelperButton = ToolbarManager.Instance.add ("NodeHelper", "NodeHelperButton");

                    _nodeHelperButton.TexturePath = ToolbarButtonTexture;
                    _nodeHelperButton.ToolTip = "NodeHelper";

                    _nodeHelperButton.Visibility = new GameScenesVisibility (GameScenes.EDITOR);

                    _nodeHelperButton.OnClick += (e => _show = !_show);
                }
                else
                {
                    return;
                }
            }

            GameEvents.onPartActionUICreate.Add (HandleActionMenuOpened);

            GameEvents.onPartActionUIDismiss.Add (HandleActionMenuClosed);

            _selectedPart = null;
            _selectedNode = null;
            _show = false;
            _nodeMapping = new Dictionary<AttachNode, GameObject>();
            _nodeNameMapping = new Dictionary<AttachNode, string>();
            _nodePosBackup = new Dictionary<AttachNode, Vector3>();
            _affectedParts = new HashSet<Part>();
            _selectedPartRules = new bool[5];
            _showPlanes = new bool[3];
            _planes = new GameObject[3];

            _createPlanes ();

            _orientationPointer = UI.CreatePrimitive (PrimitiveType.Cylinder, _orientColor, new Vector3 (0.01f, 0.15f, 0.01f), false, "Orientation Pointer", TransShader);

            var vesselOverlays = (EditorVesselOverlays) FindObjectOfType (typeof (EditorVesselOverlays));

            _nodeMaterial = vesselOverlays.CoMmarker.gameObject.GetComponent<Renderer>().material;

            _nodeMaterial.shader = Shader.Find (TransShader);

            _initialized = true;
        }

        bool ShouldBeLocked ()
        {
            if (nodeListPos.Contains (new Vector2 (Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
            {
                return true;
            }

            if (_selectedPart != null && windowPos.Contains (new Vector2 (Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
            {
                return true;
            }

            if (_selectedNode != null && nodeEditPos.Contains (new Vector2 (Input.mousePosition.x, Screen.height - Input.mousePosition.y)))
            {
                return true;
            }

            return false;
        }

        void UpdateLock ()
        {
            if (ShouldBeLocked ())
            {
                if (!inputLockActive)
                {
                    InputLockManager.SetControlLock (ControlTypes.ALLBUTCAMERAS, inputLock);

                    inputLockActive = true;
                }
            }
            else
            {
                if (inputLockActive)
                {
                    InputLockManager.RemoveControlLock (inputLock);

                    inputLockActive = false;
                }
            }
        }

        public void Update ()
        {
            if (EditorLogic.fetch == null)
            {
                return;
            }

            UpdateLock ();

            if (_cleanupCounter > 0)
            {
                _cleanupCounter--;
            }
            else
            {
                _cleanupCounter = CleanupInterval;

                foreach (var affectedPart in _affectedParts.Where (affectedPart => _selectedPart == null || affectedPart != _selectedPart))
                {
                    affectedPart.SetHighlightDefault ();
                }

                foreach (var attachNode in _nodePosBackup.Keys.Where (posBkup => posBkup == null || posBkup.owner == null).ToList())
                {
                    _nodePosBackup.Remove (attachNode);
                }

                if (EditorLogic.SelectedPart != null)
                {
                    _clearMapping ();

                    _cleanSelectedPartSetup ();
                }
            }

            if (!_initialized || !_show || _selectedPart == null)
            {
                if (_selectedPart != null && !_show)
                {
                    _cleanSelectedPartSetup ();

                    _clearMapping ();
                }

                return;
            }

            _updateMapping ();

            _updateAttachRules ();

            _setupSelectedPart ();

            _processPlanes ();

            foreach (var mapping in _nodeMapping.Select (kv => new {node = kv.Key, go = kv.Value}))
            {
                var localPos = _selectedPart.transform.TransformPoint (mapping.node.position);

                var goTrans = mapping.go.transform;

                goTrans.position = localPos;

                goTrans.localScale = GetGoScaleForNode (mapping.node);

                goTrans.up = mapping.node.orientation;

                if (_selectedNode != null && mapping.node == _selectedNode)
                {
                    _updateGoColor (mapping.go, _selectedNodeColor);
                }
                else
                {
                    _updateGoColor (mapping.go, _nodeColor);
                }
            }
        }

        protected void NodeEditGui (int windowID)
        {
            var expandWidth = GUILayout.ExpandWidth (true);

            if (_selectedNode != null)
            {
                if (GUILayout.Button ("Clear Selection", expandWidth))
                {
                    _selectedNode = null;
                }

                GUILayout.BeginVertical ("box");

                GUILayout.BeginHorizontal ();

                GUILayout.Label ("Step:", GUILayout.Width (90));

                if (GUILayout.Button ("1.0"))
                {
                    _stepWidth = 1;
                    _stepWidthString = "1.0";
                }

                if (GUILayout.Button ("0.1"))
                {
                    _stepWidth = 0.1f;
                    _stepWidthString = "0.1";
                }

                if (GUILayout.Button ("0.01"))
                {
                    _stepWidth = 0.01f;
                    _stepWidthString = "0.01";
                }

                if (GUILayout.Button ("0.001"))
                {
                    _stepWidth = 0.001f;
                    _stepWidthString = "0.001";
                }

                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();

                _stepWidthString = GUILayout.TextField (_stepWidthString, GUILayout.Width (90));

                if (GUILayout.Button ("Set"))
                {
                    _parseStepWidth ();
                }

                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();

                if (GUILayout.Button ("X+"))
                {
                    _moveNode (MoveDirs.X, true);
                }

                if (GUILayout.Button ("Y+"))
                {
                    _moveNode (MoveDirs.Y, true);
                }

                if (GUILayout.Button ("Z+"))
                {
                    _moveNode (MoveDirs.Z, true);
                }

                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();

                if (GUILayout.Button ("X-"))
                {
                    _moveNode (MoveDirs.X, false);
                }

                if (GUILayout.Button ("Y-"))
                {
                    _moveNode (MoveDirs.Y, false);
                }

                if (GUILayout.Button ("Z-"))
                {
                    _moveNode (MoveDirs.Z, false);
                }

                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();

                var cPos = _selectedNode.position;
                var posText = string.Format ("{0}, {1}, {2}", _formatNumberForOutput (cPos.x), _formatNumberForOutput (cPos.y), _formatNumberForOutput (cPos.z));

                GUILayout.Label ("Position (" + posText + ")", expandWidth);

                GUILayout.EndHorizontal ();

                _targetPos = GUILayout.TextField (_targetPos, GUILayout.Width (180));

                GUILayout.BeginHorizontal ();

                if (GUILayout.Button ("Copy Pos", expandWidth))
                {
                    _targetPos = posText;
                }

                if (GUILayout.Button ("Set Pos", expandWidth))
                {
                    _setToPos();
                }

                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();

                if (GUILayout.Button ("Use Default", expandWidth))
                {
                    _resetCurrNode ();
                }

                if (GUILayout.Button ("Set Default", expandWidth))
                {
                    _updateResetPosCurrNode ();
                }

                GUILayout.EndHorizontal ();

                GUILayout.EndVertical ();

                GUILayout.BeginVertical ("box");

                GUILayout.BeginHorizontal ();

                GUILayout.Label ("Size: " + _selectedNode.size, expandWidth);

                if (GUILayout.Button ("-1", expandWidth) && (_selectedNode.size > 0))
                {
                    _selectedNode.size -= 1;
                }

                if (GUILayout.Button ("+1", expandWidth) && (_selectedNode.size < int.MaxValue - 1))
                {
                    _selectedNode.size += 1;
                }

                GUILayout.EndHorizontal ();

                var or = _selectedNode.orientation;
                var orientationString = _formatNumberForOutput (or.x) + ", " + _formatNumberForOutput (or.y) + ", " + _formatNumberForOutput (or.z);

                GUILayout.Label ("Orient: " + orientationString, expandWidth);

                GUILayout.BeginHorizontal ();

                if (GUILayout.Button ("+X", expandWidth))
                {
                    _selectedNode.orientation = new Vector3 (1f, 0f, 0f);
                }

                if (GUILayout.Button ("+Y", expandWidth))
                {
                    _selectedNode.orientation = new Vector3 (0f, 1f, 0f);
                }

                if (GUILayout.Button ("+Z", expandWidth))
                {
                    _selectedNode.orientation = new Vector3 (0f, 0f, 1f);
                }

                _nodeOrientationCust = GUILayout.TextField (_nodeOrientationCust, expandWidth);

                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();

                if (GUILayout.Button ("-X", expandWidth))
                {
                    _selectedNode.orientation = new Vector3 (-1f, 0f, 0f);
                }

                if (GUILayout.Button ("-Y", expandWidth))
                {
                    _selectedNode.orientation = new Vector3 (0f, -1f, 0f);
                }

                if (GUILayout.Button ("-Z", expandWidth))
                {
                    _selectedNode.orientation = new Vector3 (0f, 0f, -1f);
                }

                if (GUILayout.Button ("Custom", expandWidth))
                {
                    _orientNodeToCust ();
                }

                GUILayout.EndHorizontal ();

                _showOrientationPointer = GUILayout.Toggle (_showOrientationPointer, "Show Orientation Pointer", "Button", expandWidth);

                GUILayout.EndVertical ();

                if (GUILayout.Button ("Delete node", expandWidth))
                {
                    _deleteCurrNode ();
                }

                GUI.DragWindow ();
            }
        }

        Vector2 scrollPos = new Vector2 (0f, 0f);

        protected void NodeListGui (int windowID)
        {
            var expandWidth = GUILayout.ExpandWidth (true);

            if (_selectedPart == null)
            {
                GUILayout.Label ("Right-click a part to select it.", expandWidth);

                GUI.DragWindow ();

                return;
            }

            GUILayout.BeginVertical ("box");

            if (_nodeMapping.Keys.Count > 20)
            {
                scrollPos = GUILayout.BeginScrollView (scrollPos, GUILayout.Width (200), GUILayout.Height (500));
            }

            foreach (var node in _nodeMapping.Keys)
            {
                var isSel = _selectedNode != null && _selectedNode == node;

                GUILayout.BeginHorizontal ();

                if (GUILayout.Toggle (isSel, _getNodeName (node), "Button", expandWidth))
                {
                    _selectedNode = node;
                }

                GUILayout.EndHorizontal ();
            }

            if (_nodeMapping.Keys.Count > 20)
            {
                GUILayout.EndScrollView ();
            }

            GUILayout.EndVertical ();

            GUI.DragWindow ();
        }

        protected void WindowGui (int windowID)
        {
            var expandWidth = GUILayout.ExpandWidth (true);

            GUILayout.BeginVertical ("box");

            GUILayout.Label ("Attachment Rules: " + _getSelPartAttRulesString ());

            var tempArr = new bool[5];

            Array.Copy (_selectedPartRules, tempArr, 5);

            GUILayout.BeginHorizontal ();

            tempArr[0] = GUILayout.Toggle (tempArr[0], "stack", "Button", expandWidth);
            tempArr[1] = GUILayout.Toggle (tempArr[1], "srfAttach", "Button", expandWidth);
            tempArr[2] = GUILayout.Toggle (tempArr[2], "allowStack", "Button", expandWidth);

            GUILayout.EndHorizontal ();

            GUILayout.BeginHorizontal ();

            tempArr[3] = GUILayout.Toggle (tempArr[3], "allowSrfAttach", "Button", expandWidth);
            tempArr[4] = GUILayout.Toggle (tempArr[4], "allowCollision", "Button", expandWidth);

            _processAttachRules (tempArr);

            GUILayout.EndHorizontal ();

            GUILayout.EndVertical ();

            GUILayout.BeginVertical ("box");

            if (!_showCreateMenu)
            {
                _showCreateMenu |= GUILayout.Button ("Show Node Creation");
            }
            else
            {
                _showCreateMenu &= !GUILayout.Button ("Hide Node Creation");

                GUILayout.Label ("Note: New attachment nodes are not automatically saved with the craft file.", GUILayout.ExpandWidth (false));

                GUILayout.BeginHorizontal ();

                GUILayout.Label ("Add a node", expandWidth);

                if (GUILayout.Button ("Create"))
                {
                    _createNewNode ();
                }

                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();

                GUILayout.Label ("Name:", expandWidth);

                _newNodeName = GUILayout.TextField (_newNodeName, GUILayout.Width (120));

                GUILayout.EndHorizontal ();

                GUILayout.BeginHorizontal ();

                GUILayout.Label ("Pos:", expandWidth);

                _newNodePos = GUILayout.TextField (_newNodePos, GUILayout.Width (120));

                GUILayout.EndHorizontal ();
            }

            GUILayout.EndVertical ();

            GUILayout.BeginVertical ("box");

            GUILayout.BeginHorizontal ();

            GUILayout.Label ("Normal planes", expandWidth);

            _showPlanes[0] = GUILayout.Toggle (_showPlanes[0], "X", "Button");
            _showPlanes[1] = GUILayout.Toggle (_showPlanes[1], "Y", "Button");
            _showPlanes[2] = GUILayout.Toggle (_showPlanes[2], "Z", "Button");

            GUILayout.EndHorizontal ();

            GUILayout.BeginHorizontal ();

            GUILayout.Label ("Radius");

            _planeRadiusString = GUILayout.TextField (_planeRadiusString, GUILayout.Width (90));

            if (GUILayout.Button ("Set"))
            {
                _parsePlaneRadius ();
            }

            GUILayout.EndHorizontal ();

            GUILayout.EndVertical ();

            if (GUILayout.Button ("Write node data") && _selectedPart != null && !_printingActive)
            {
                _printingActive = true;

                _printNodeConfigForPart (true);
            }

            GUI.DragWindow ();
        }

        void _cleanSelectedPartSetup ()
        {
            if (_selectedPart == null)
            {
                return;
            }

            _selectedPart.SetHighlightDefault ();
        }

        void _clearMapping (bool deselect = true)
        {
            if (!_initialized)
            {
                return;
            }

            _orientationPointer.SetActive (false);

            foreach (var kv in _nodeMapping)
            {
                Destroy (kv.Value);
            }

            _nodeMapping.Clear ();

            _nodeNameMapping.Clear ();

            for (var i = 0; i < 3; i++)
            {
                _showPlanes[i] = false;

                _planes[i].SetActive (false);
            }

            if (deselect)
            {
                _selectedPart = null;
                _selectedNode = null;
                _selectedPartRules = new bool[5];
            }
        }

        List<Tuple<string, string>> _constructNodeValues ()
        {
            var nameList = new List<AttachNode>(_selectedPart.attachNodes.Count + 1);

            nameList.AddRange (_selectedPart.attachNodes);

            var retList = _uniquifyNames (nameList).Select (attachNode => _nodeToString (attachNode.Key, attachNode.Value)).ToList ();

            if (_selectedPart.srfAttachNode != null)
            {
                retList.Add (_nodeToString (_selectedPart.srfAttachNode, string.Empty, false));
            }

            return retList;
        }

        void _createNewNode ()
        {
            try
            {
                if (string.IsNullOrEmpty (_newNodeName))
                {
                    OSD.PostMessageUpperCenter ("[NH]: Name for new attachment node empty!");

                    return;
                }

                var pos = KSPUtil.ParseVector3 (_newNodePos);

                var an = new AttachNode
                {
                    owner = _selectedPart,
                    position = pos,
                    nodeType = AttachNode.NodeType.Stack,
                    size = 1,
                    id = _findUniqueId (_newNodeName),
                    attachMethod = AttachNodeMethod.FIXED_JOINT,
                    nodeTransform = _selectedPart.transform,
                    orientation = _selectedPart.transform.up
                };

                _selectedPart.attachNodes.Add (an);

                _clearMapping (false);

                OSD.PostMessageUpperCenter ("[NH]: New attachment node created!");
            }
            catch (Exception e)
            {
                Debug.Log ("[NH]: Creating new node threw exception: " + e.Message);

                OSD.PostMessageUpperCenter ("[NH]: Unable to create node, please check vector format!");
            }
        }

        void _createPlanes ()
        {
            for (var i = 0; i < 3; i++)
            {
                _planes[i] = UI.CreatePrimitive (PrimitiveType.Cube, _planeColor, new Vector3 (1f, 1f, 1f), false, "Helper Plane", TransShader);
            }
        }

        void _deleteCurrNode ()
        {
            if (_selectedNode == null)
            {
                return;
            }

            _selectedPart.attachNodes.Remove (_selectedNode);

            _clearMapping (false);

            _selectedNode = null;

            OSD.PostMessageUpperCenter ("NodeHelper: Attachment node deleted...");
        }

        string _findUniqueId (string newNodeName)
        {
            if (_nodeNameMapping.Keys.All (k => k.id != newNodeName))
            {
                return newNodeName;
            }

            var sameNameCount = _nodeNameMapping.Keys.Count (k => k.id.Contains (newNodeName));

            sameNameCount++;

            return newNodeName + sameNameCount;
        }

        /// <summary>
        /// Finds the precision of a float up to approx. 5 digits which is fine for this context.
        /// </summary>
        /// <param name="inNr"></param>
        /// <returns>The position of last significant position after 0.
        /// </returns>

        static int _floatPrecision (float inNr)
        {
            inNr = Mathf.Abs (inNr);

            var cnt = 0;

            while (inNr % 1 > 0)
            {
                cnt++;

                inNr *= 10;
            }

            return cnt;
        }

        static string _formatNumberForOutput (float inputNumber)
        {
            var precision = Mathf.Clamp (_floatPrecision (inputNumber), 1, 5);
            var formatString = "{0:F" + precision + "}";
            var trimmedString = string.Format (formatString, inputNumber).TrimEnd ('0');

            if (trimmedString[trimmedString.Length - 1] == '.' || trimmedString[trimmedString.Length - 1] == ',')
            {
                trimmedString = trimmedString + "0";
            }

            return trimmedString;
        }

        string _getNodeName (AttachNode node)
        {
            return (_nodeNameMapping.ContainsKey (node)) ? _nodeNameMapping[node] : string.Empty;
        }

        string _getSelPartAttRulesString ()
        {
            var sb = new StringBuilder (9);

            for (var i = 0; i < 5; i++)
            {
                var val = _selectedPartRules[i] ? 1 : 0;

                sb.Append (val);

                if (i < 4)
                {
                    sb.Append (",");
                }
            }

            return sb.ToString ();
        }

        void _moveNode(MoveDirs moveDir, bool positive)
        {
            if (_selectedNode == null)
            {
                Debug.Log ("[NH]: No attachment node selected!");

                return;
            }

            var debugtext = new StringBuilder (5);

            debugtext.Append (_getNodeName(_selectedNode));

            var newPos = _selectedNode.position;
            var sw = positive ? _stepWidth : _stepWidth * -1f;

            debugtext.Append (sw);
            debugtext.Append (" into ");

            switch (moveDir)
            {
                case MoveDirs.X:
                {
                    newPos = new Vector3 (newPos.x + sw, newPos.y, newPos.z);

                    debugtext.Append ("x position");
                }

                break;

                case MoveDirs.Y:
                {
                    newPos = new Vector3 (newPos.x, newPos.y + sw, newPos.z);

                    debugtext.Append ("y position");
                }

                break;

                default:
                {
                    newPos = new Vector3 (newPos.x, newPos.y, newPos.z + sw);

                    debugtext.Append ("z position");
                }

                break;
            }

            Debug.Log (debugtext.ToString ());

            _setToPos (newPos);
        }

        static Tuple<string, string> _nodeToString (AttachNode node, string id, bool stack = true)
        {
            const string delim = ", ";

            string retKey;

            if (stack)
            {
                retKey = "node_stack_" + id;
            }
            else
            {
                retKey = "node_attach";
            }

            var sb = new StringBuilder ();
            var pos = node.position;
            var or = node.orientation;

            sb.Append (_formatNumberForOutput (pos.x));
            sb.Append (delim);
            sb.Append (_formatNumberForOutput (pos.y));
            sb.Append (delim);
            sb.Append (_formatNumberForOutput (pos.z));
            sb.Append (delim);
            sb.Append (_formatNumberForOutput (or.x));
            sb.Append (delim);
            sb.Append (_formatNumberForOutput (or.y));
            sb.Append (delim);
            sb.Append (_formatNumberForOutput (or.z));

            if (node.size >= 0)
            {
                sb.Append (delim);
                sb.Append (node.size);
            }

            return new Tuple<string, string>(retKey, sb.ToString ());
        }

        static string _normalizePartName (string messedupName)
        {
            var delim = new[] {"(Clone"};
            var parts = messedupName.Split (delim, StringSplitOptions.None);

            return parts[0];
        }

        void _orientNodeToCust ()
        {
            if (_selectedNode == null)
            {
                return;
            }

            try
            {
                var custOr = KSPUtil.ParseVector3 (_nodeOrientationCust);

                _selectedNode.orientation = custOr;
            }
            catch (Exception)
            {
                OSD.PostMessageUpperCenter ("NodeHelper: Unable to set attachment node orientation, please check vector format!");
            }
        }

        void _parsePlaneRadius ()
        {
            float pr;

            if (float.TryParse (_planeRadiusString, out pr))
            {
                pr = Mathf.Abs (pr);
            }

            _planeRadius = pr;
            _planeRadiusString = _formatNumberForOutput (pr);
        }

        void _parseStepWidth ()
        {
            var psw = 0f;

            float sw;

            if (float.TryParse (_stepWidthString, out sw))
            {
                psw = Mathf.Abs (sw);
            }

            _stepWidth = psw;
            _stepWidthString = _formatNumberForOutput (psw);
        }

        void _printNodeConfigForPart (bool simple = false)
        {
            try
            {
                var normName = _normalizePartName (_selectedPart.name);

                var cfg = GameDatabase.Instance.root.AllConfigs.Where (c => c.name == normName).Select (c => c).FirstOrDefault ();

                if (cfg != null && !string.IsNullOrEmpty (cfg.url))
                {
                    var oldConf = cfg.config;

                    oldConf.RemoveValuesStartWith ("node_");

                    var newConf = oldConf;
                    var nodeAttributes = _constructNodeValues ();

                    foreach (var nodeValue in nodeAttributes)
                    {
                        newConf.AddValue (nodeValue.Item1, nodeValue.Item2);
                    }

                    var cfgurl = _stripUrl (cfg.url, cfg.name);
                    var path = KSPUtil.ApplicationRootPath + "/GameData/" + cfgurl + "_NH.cfg";

                    if (!simple)
                    {
                        File.WriteAllText (path, newConf.ToString ());
                    }
                    else
                    {
                        var text = new StringBuilder ();

                        foreach (var nodeAttribute in nodeAttributes)
                        {
                            text.Append (nodeAttribute.Item1);
                            text.Append (" = ");
                            text.Append (nodeAttribute.Item2);
                            text.Append (Environment.NewLine);
                        }

                        if (_selectedPart != null)
                        {
                            text.Append (Environment.NewLine);
                            text.Append ("attachRules = ");
                            text.Append (_getSelPartAttRulesString ());
                            text.Append (Environment.NewLine);
                        }

                        File.WriteAllText (path, text.ToString ());
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log ("[NH]: Writing attachemnt node file threw exception: " + e.Message);
            }
            finally
            {
                _printingActive = false;
            }
        }

        void _processAttachRules (bool[] tempArr)
        {
            if (_selectedPart == null || _selectedPart.attachRules == null)
            {
                return;
            }

            var arr = _selectedPartRules;
            var pr = _selectedPart.attachRules;

            if (arr[0] != tempArr[0])
            {
                pr.stack = !pr.stack;
            }

            if (arr[1] != tempArr[1])
            {
                pr.srfAttach = !pr.srfAttach;
            }

            if (arr[2] != tempArr[2])
            {
                pr.allowStack = !pr.allowStack;
            }

            if (arr[3] != tempArr[3])
            {
                pr.allowSrfAttach = !pr.allowSrfAttach;
            }

            if (arr[4] != tempArr[4])
            {
                pr.allowCollision = !pr.allowCollision;
            }
        }

        void _processPlanes ()
        {
            var center = _selectedPart.transform.position;
            var up = _selectedPart.transform.up;

            if (_selectedNode != null)
            {
                center = _selectedPart.transform.TransformPoint (_selectedNode.position);
                up = _selectedNode.orientation;

                _positionOrientationPointer (center, up);
            }
            else
            {
                _orientationPointer.SetActive (false);
            }

            for (var i = 0; i < 3; i++)
            {
                var plane = _planes[i];
                var pT = plane.transform;

                if (!_showPlanes[i])
                {
                    plane.SetActive (false);

                    pT.localScale = new Vector3 (1f, 1f, 1f);

                    continue;
                }

                pT.position = center;
                pT.up = up;

                var diameter = _planeRadius * 2f;

                pT.localScale = new Vector3 (diameter, 0.01f, diameter);

                var rotVec = Vector3.zero;

                switch (i)
                {
                    case 2:

                        rotVec = new Vector3 (90f, 0f, 0f);

                    break;

                    case 0:

                        rotVec = new Vector3 (0f, 0f, 90f);

                    break;
                }

                pT.Rotate (rotVec);

                plane.SetActive (true);
            }
        }

        void _positionOrientationPointer (Vector3 center, Vector3 up)
        {
            _orientationPointer.transform.up = up;
            _orientationPointer.transform.position = center;
            _orientationPointer.transform.Translate (0f, 0.25f, 0f);

            if (_showOrientationPointer)
            {
                _orientationPointer.SetActive (true);
            }
            else
            {
                _orientationPointer.SetActive (false);
            }
        }

        void _resetCurrNode ()
        {
            if (_nodePosBackup.ContainsKey (_selectedNode))
            {
                _setToPos (_nodePosBackup[_selectedNode]);

                OSD.PostMessageUpperCenter ("NodeHelper: Attachment node position reset.");
            }

            Debug.Log ("[NH]: Failed to reset attachment node to backup position!");
        }

        void _setToPos ()
        {
            try
            {
                var nPos = KSPUtil.ParseVector3 (_targetPos);

                _setToPos (nPos);
            }
            catch (Exception e)
            {
                Debug.Log ("[NH]: setToPos threw exception: " + e.Message);

                OSD.PostMessageUpperCenter ("NodeHelper: Unable to set attachment node position, please check vector format!");
            }
        }

        void _setToPos (Vector3 newPos)
        {
            var currPos = _selectedNode.position;
            var delta = newPos - currPos;

            _selectedNode.position = newPos;

            if (_selectedNode.attachedPart != null)
            {
                var pPos = _selectedNode.attachedPart.transform.position;
                _selectedNode.attachedPart.transform.position = pPos + delta;
            }
        }

        void _setupSelectedPart()
        {
            if (_selectedPart == null)
            {
                return;
            }

            _selectedPart.SetHighlightColor (Color.blue);

            _selectedPart.SetHighlight (true, false);
        }

        static string _stripUrl (string url, string stripName)
        {
            var nameLength = stripName.Length;
            var urlLength = url.Length - nameLength - 1;

            return url.Substring (0, urlLength);
        }

        static Dictionary<AttachNode, string> _uniquifyNames (ICollection<AttachNode> nodes)
        {
            var nameDic = new Dictionary<AttachNode, string>(nodes.Count);

            foreach (var attachNode in nodes)
            {
                var n = attachNode.id;
                var cnt = nameDic.Values.Count (v => v == attachNode.id);

                if (cnt > 0)
                {
                    n = n + "_" + (cnt + 1);
                }

                nameDic.Add (attachNode, n);
            }

            return nameDic;
        }

        void _updateAttachRules ()
        {
            if (_selectedPart == null || _selectedPart.attachRules == null)
            {
                return;
            }

            var arr = _selectedPartRules;
            var pr = _selectedPart.attachRules;

            arr[0] = pr.stack;
            arr[1] = pr.srfAttach;
            arr[2] = pr.allowStack;
            arr[3] = pr.allowSrfAttach;
            arr[4] = pr.allowCollision;
        }

        static void _updateGoColor (GameObject go, Color color)
        {
            var mr = go.GetComponent<MeshRenderer>();

            if (mr != null && mr.material != null)
            {
                mr.material.color = color;
            }
        }

        void _updateMapping ()
        {
            foreach (var attachNode in _selectedPart.attachNodes.Where (an => an != null))
            {
                if (_nodeMapping.ContainsKey (attachNode))
                {
                    continue;
                }

                if (_selectedPart.srfAttachNode != null && attachNode == _selectedPart.srfAttachNode)
                {
                    continue;
                }

                var scale = GetGoScaleForNode (attachNode);

                var go = UI.CreatePrimitive (PrimitiveType.Sphere, _nodeColor, scale, true, "Helper Node", TransShader);

                go.GetComponent<MeshRenderer>().material = _nodeMaterial;

                go.transform.SetParent (_selectedPart.transform);

                _nodeMapping.Add (attachNode, go);

                _nodeNameMapping.Add (attachNode, attachNode.id);

                if (!_nodePosBackup.ContainsKey (attachNode))
                {
                    _nodePosBackup.Add (attachNode, attachNode.position);
                }
            }
        }

        void _updateResetPosCurrNode ()
        {
            if (_nodePosBackup.ContainsKey (_selectedNode))
            {
                _nodePosBackup[_selectedNode] = _selectedNode.position;

                OSD.PostMessageUpperCenter ("NodeHelper: Node attachment position updated.");
            }

            Debug.Log ("[NH]: Failed to update attachment node backup position!");
        }

        enum MoveDirs
        {
            X,
            Y,
            Z
        }
    }
}
