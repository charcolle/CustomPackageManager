using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using charcolle.CustomPackageManager.LitJson;

namespace charcolle.CustomPackageManager
{

    [Serializable]
    internal class CustomPackageManagerGUI
    {
        public delegate JsonData FetchData();

        public event Action<string, string> OnAddDependencies;
        public event Action<string> OnAddRegistry;
        public event Action<CustomPackageManagerPackage> OnPackageContextClicked;
        public event Action<CustomPackageManagerPackage> OnChangePackageVersion;

        public event Action<CustomPackageManagerScopedRegistry> OnAddScopedRegistry;
        public event Action<CustomPackageManagerScopedRegistry> OnScopedRegistryContextClicked;
        public event Action<CustomPackageManagerScopedRegistry> OnScopedRegistryScopesChange;

        public event FetchData GetManifestJsonData;

        public CustomPackageManagerGUI()
        {

        }

        public void OnIMGUI()
        {
            if ( packages == null )
            {
                var manifestJsonData = GetManifestJsonData.Invoke();
                if ( manifestJsonData == null )
                {
                    EditorGUILayout.HelpBox( "fatal error", MessageType.Error );
                    return;
                }
                onInitialize( manifestJsonData );
            }

            EditorGUILayout.BeginVertical();
            {
                GUILayout.Space( 10 );
                drawDependencies();
                drawRegistryURL();
                GUILayout.Space( 15 );
                drawScopedRegistry();
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndVertical();
        }

        public void Reload()
        {
            packages = null;
        }

        #region gui

        #region varies

        private List<CustomPackageManagerPackage> packages;
        private List<CustomPackageManagerScopedRegistry> scopedRegistries;

        private TreeViewState packageTreeViewState;
        private MultiColumnHeaderState packageMultiColumnHeaderState;
        private CustomPackageManagerTreeView packageTreeView;

        private TreeViewState scopedRegistriesTreeViesState;
        private MultiColumnHeaderState scopedRegistriesMultiColumnHeaderState;
        private CustomPackageManagerSRTreeView scopedRegistriesTreeView;

        private GUIStyle noSpaceBoxStyle;
        private GUIStyle searchTextField;
        private GUIStyle searchFieldCancelButton;

        #endregion

        #region initialize

        private void onInitialize( JsonData manifestData )
        {
            noSpaceBoxStyle = new GUIStyle( "Tooltip" )
            {
                margin = new RectOffset( 0, 0, 0, 0 ),
                padding = new RectOffset( 0, 0, 0, 0 ),
            };
            searchTextField = new GUIStyle( "SearchTextField" );
            searchFieldCancelButton = new GUIStyle( "SearchCancelButton" );

            {
                packages = new List<CustomPackageManagerPackage>();
                var dependencies = manifestData[ "dependencies" ];
                var counter = 0;
                foreach ( var pkg in dependencies.Keys )
                {
                    var p = new CustomPackageManagerPackage( pkg, dependencies[ pkg ].ToString() )
                    {
                        id = ++counter,
                        depth = 0,
                        name = dependencies[ pkg ].ToString(),
                    };
                    packages.Add( p );
                }
                packages.Insert( 0, CustomPackageManagerPackage.Root );

                // check registry exists
                if ( manifestData.ContainsKey( "registry" ) )
                    registryURL = manifestData[ "registry" ]?.ToString();
            }

            {
                // check scopedRegistry exists
                scopedRegistries = new List<CustomPackageManagerScopedRegistry>();
                if ( manifestData.ContainsKey( "scopedRegistries" ) )
                {
                    var scopes = manifestData[ "scopedRegistries" ];
                    if ( scopes != null )
                    {
                        var counter = 1000; // holy shit
                        for ( int i = 0; i < scopes.Count; i++ )
                        {
                            var srName = scopes[ i ][ "name" ].ToString();
                            var url = scopes[ i ][ "url" ].ToString();
                            var tmp = scopes[ i ][ "scopes" ];
                            var scopeList = "";
                            for ( int j = 0; j < tmp.Count; j++ )
                                scopeList += tmp[ j ].ToString() + ( j == tmp.Count - 1 ? "" : "," );
                            var sr = new CustomPackageManagerScopedRegistry( srName, url, scopeList )
                            {
                                id = ++counter,
                                depth = 0,
                                name = scopeList,
                            };
                            scopedRegistries.Add( sr );
                        }
                    }
                }
                scopedRegistries.Insert( 0, CustomPackageManagerScopedRegistry.Root );
            }

            packageTreeViewInitialize();
            scopedRegistryTreeViewInitialize();
        }

        private void packageTreeViewInitialize()
        {
            if ( packageTreeViewState == null )
                packageTreeViewState = new TreeViewState();
            var firstInit = packageMultiColumnHeaderState == null;
            var headerState = CustomPackageManagerTreeView.CreateDefaultMultiColumnHeaderState( GUILayoutUtility.GetLastRect().width );
            if ( MultiColumnHeaderState.CanOverwriteSerializedFields( packageMultiColumnHeaderState, headerState ) )
                MultiColumnHeaderState.OverwriteSerializedFields( packageMultiColumnHeaderState, headerState );
            packageMultiColumnHeaderState = headerState;

            var multiColumnHeader = new CustomPackageManagerMultiColumnHeader( headerState );
            if ( firstInit )
                multiColumnHeader.ResizeToFit();

            var treeModel = new TreeModel<CustomPackageManagerPackage>( packages );
            packageTreeView = new CustomPackageManagerTreeView( packageTreeViewState, multiColumnHeader, treeModel );
            packageTreeView.OnContextClicked += OnPackageContextClicked;
            packageTreeView.OnRenameChanged += OnChangePackageVersion;
        }

        private void scopedRegistryTreeViewInitialize()
        {
            if ( scopedRegistriesTreeViesState == null )
                scopedRegistriesTreeViesState = new TreeViewState();
            var firstInit = scopedRegistriesMultiColumnHeaderState == null;
            var headerState = CustomPackageManagerSRTreeView.CreateDefaultMultiColumnHeaderState( GUILayoutUtility.GetLastRect().width );
            if ( MultiColumnHeaderState.CanOverwriteSerializedFields( scopedRegistriesMultiColumnHeaderState, headerState ) )
                MultiColumnHeaderState.OverwriteSerializedFields( scopedRegistriesMultiColumnHeaderState, headerState );
            scopedRegistriesMultiColumnHeaderState = headerState;

            var multiColumnHeader = new CustomPackageManagerMultiColumnHeader( headerState );
            if ( firstInit )
                multiColumnHeader.ResizeToFit();

            var treeModel = new TreeModel<CustomPackageManagerScopedRegistry>( scopedRegistries );
            scopedRegistriesTreeView = new CustomPackageManagerSRTreeView( scopedRegistriesTreeViesState, multiColumnHeader, treeModel );
            scopedRegistriesTreeView.OnContextClicked += OnScopedRegistryContextClicked;
            scopedRegistriesTreeView.OnRenameChanged += OnScopedRegistryScopesChange;
        }

        #endregion

        #region draw packages

        private void drawDependencies()
        {
            if ( packages == null )
                return;

            EditorGUILayout.BeginVertical();
            {
                GUILayout.Label( "Control custom packages for PackageManager." );
                addDependencies();
                // search text
                var reloadRequest = false;
                EditorGUILayout.BeginHorizontal();
                {
                    var st = EditorGUILayout.TextField( packageTreeView.CustomSearchText, searchTextField );
                    if ( GUILayout.Button( "", searchFieldCancelButton ) )
                    {
                        EditorGUIUtility.keyboardControl = 0;
                        st = "";
                    }
                    if( st != packageTreeView.CustomSearchText )
                        reloadRequest = true;
                    var hiddenFlag = GUILayout.Toggle( packageTreeView.HiddenUnityOfficialPackage, "Hide official package", EditorStyles.toolbarButton, GUILayout.Width( 130 ) );
                    if ( hiddenFlag != packageTreeView.HiddenUnityOfficialPackage )
                        reloadRequest = true;
                    packageTreeView.CustomSearchText = st;
                    packageTreeView.HiddenUnityOfficialPackage = hiddenFlag;
                    if ( reloadRequest )
                    {
                        packageTreeView.Reload();
                    }
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();

                packageTreeView.OnGUI( GUILayoutUtility.GetLastRect() );
            }
            EditorGUILayout.EndVertical();
        }

        [SerializeField]
        private string addDependenciesName;
        [SerializeField]
        private string addDependenciesVersion;
        private void addDependencies()
        {
            EditorGUILayout.BeginVertical( noSpaceBoxStyle );
            {
                GUILayout.Space( 5 );
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label( "Package Name", GUILayout.Width( 100 ) );
                    addDependenciesName = EditorGUILayout.TextField( addDependenciesName );
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label( "Version", GUILayout.Width( 100 ) );
                    addDependenciesVersion = EditorGUILayout.TextField( addDependenciesVersion );
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Space( 7 );
                GUI.backgroundColor = Color.green;
                if ( GUILayout.Button( "Add Package", EditorStyles.toolbarButton ) )
                {
                    OnAddDependencies?.Invoke( addDependenciesName, addDependenciesVersion );
                    addDependenciesName = "";
                    addDependenciesVersion = "";
                    EditorGUIUtility.keyboardControl = 0;
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndVertical();
        }

        [SerializeField]
        private string registryURL;
        [NonSerialized]
        private string addRegistryURL;
        private void drawRegistryURL()
        {
            EditorGUILayout.BeginVertical( EditorStyles.helpBox );
            {
                GUILayout.Label( "Set Registry URL", EditorStyles.boldLabel );
                if ( !string.IsNullOrEmpty( registryURL ) )
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        GUILayout.Label( registryURL );
                        GUI.backgroundColor = Color.red;
                        if( GUILayout.Button( "x", GUILayout.Width( 25 ) ) )
                        {
                            OnAddRegistry?.Invoke( "" );
                            EditorGUIUtility.keyboardControl = 0;
                        }
                        GUI.backgroundColor = Color.white;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.BeginHorizontal();
                {
                    addRegistryURL = EditorGUILayout.TextField( addRegistryURL );
                    if ( GUILayout.Button( "Add", GUILayout.Width( 100 ) ) )
                    {
                        if( !string.IsNullOrEmpty( addRegistryURL ) )
                        {
                            OnAddRegistry?.Invoke( addRegistryURL );
                            addRegistryURL = "";
                        } else
                        {
                            Debug.LogWarning( "Registry URL cannot be null." );
                        }
                        EditorGUIUtility.keyboardControl = 0;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        #endregion

        #region scopedRegistry

        private void drawScopedRegistry()
        {
            if ( scopedRegistriesTreeView == null )
                return;

            EditorGUILayout.BeginVertical();
            {
                GUILayout.Label( "Set up scoped registries." );
                addScopedRegistry();
                EditorGUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndVertical();

                scopedRegistriesTreeView.OnGUI( GUILayoutUtility.GetLastRect() );
                //if ( scopedRegistriesTreeView.state.selectedIDs != null && scopedRegistriesTreeView.state.selectedIDs.Count > 0 )
                //    Debug.Log( scopedRegistriesTreeView.state.selectedIDs[ 0 ] );
            }
            EditorGUILayout.EndVertical();
        }

        [SerializeField]
        private string addScopedRegistryName;
        [SerializeField]
        private string addScopedRegistryURL;
        [SerializeField]
        private string addScopedRegistryScopes;
        private void addScopedRegistry()
        {
            EditorGUILayout.BeginVertical( noSpaceBoxStyle );
            {
                GUILayout.Space( 5 );
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label( "Registry Name", GUILayout.Width( 100 ) );
                    addScopedRegistryName = EditorGUILayout.TextField( addScopedRegistryName );
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label( "URL", GUILayout.Width( 100 ) );
                    addScopedRegistryURL = EditorGUILayout.TextField( addScopedRegistryURL );
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Label( "Scopes", GUILayout.Width( 100 ) );
                    addScopedRegistryScopes = EditorGUILayout.TextField( addScopedRegistryScopes );
                }
                EditorGUILayout.EndHorizontal();
                GUILayout.Space( 7 );
                GUI.backgroundColor = Color.magenta;
                if ( GUILayout.Button( "Add Scoped Registry", EditorStyles.toolbarButton ) )
                {
                    if( string.IsNullOrEmpty( addScopedRegistryName ) || string.IsNullOrEmpty( addScopedRegistryURL ) || string.IsNullOrEmpty( addScopedRegistryScopes ) )
                    {
                        Debug.LogWarning( "ScopedRegistry cannot be null." );
                    } else
                    {
                        var scopedRegistry = new CustomPackageManagerScopedRegistry( addScopedRegistryName, addScopedRegistryURL, addScopedRegistryScopes );
                        OnAddScopedRegistry?.Invoke( scopedRegistry );
                        addScopedRegistryName = "";
                        addScopedRegistryURL = "";
                        addScopedRegistryScopes = "";
                    }
                    EditorGUIUtility.keyboardControl = 0;
                }
                GUI.backgroundColor = Color.white;
            }
            EditorGUILayout.EndVertical();
        }

        #endregion

        #endregion

    }

}