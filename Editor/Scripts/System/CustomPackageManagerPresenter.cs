using System.Text;
using UnityEngine;
using UnityEditor;
using charcolle.CustomPackageManager.LitJson;

using FileUtility = charcolle.CustomPackageManager.FileUtility;

namespace charcolle.CustomPackageManager
{

    internal class CustomPackageManagerPresenter
    {

        private CustomPackageManagerModel model = default;
        private CustomPackageManagerGUI view = default;

        public CustomPackageManagerPresenter( CustomPackageManagerModel model, CustomPackageManagerGUI view )
        {
            this.model = model;
            this.view = view;

            this.view.GetManifestJsonData += GetManifestData;
            this.view.OnAddDependencies += onAddDependencies;
            this.view.OnAddRegistry += onAddRegistry;
            this.view.OnPackageContextClicked += onPackageContextClicked;
            this.view.OnChangePackageVersion += onPackageVersionChanged;

            this.view.OnAddScopedRegistry += onAddScopedRegistries;
            this.view.OnScopedRegistryContextClicked += onScopedRegistryContextClicked;
            this.view.OnScopedRegistryScopesChange += onScopedRegistryScopesChange;
        }

        public JsonData GetManifestData()
        {
            return model.ManifestJsonData;
        }

        #region package callback

        private void onAddDependencies( string packageName, string version )
        {
            if ( string.IsNullOrEmpty( packageName ) || string.IsNullOrEmpty( version ) )
            {
                Debug.LogWarning( "PackageName and Version cannot be null." );
                return;
            }
            model.ManifestJsonData[ "dependencies" ][ packageName ] = version;

            FileUtility.SaveManifestJson( convertPackagJsonString( model.ManifestJsonData ) );
            CustomPackageManagerWindow.PreferenceView();
            view.Reload();
            AssetDatabase.Refresh();

            Debug.Log( $"CustomPackageManager: Add package [ {packageName} : {version} ], wait for resolving package by PackageManager..." );
        }

        private void onRemoveDependencies( string packageName )
        {
            model.ManifestJsonData[ "dependencies" ].Remove( packageName );

            FileUtility.SaveManifestJson( convertPackagJsonString( model.ManifestJsonData ) );
            reloadProcess();

            Debug.Log( $"CustomPackageManager: Remove package [ {packageName} ], wait for resolving package by PackageManager..." );
        }

        private void onAddRegistry( string registryURL )
        {
            if ( string.IsNullOrEmpty( registryURL ) )
                model.ManifestJsonData.Remove( "registry" );
            else
                model.ManifestJsonData[ "registry" ] = registryURL;
            FileUtility.SaveManifestJson( convertPackagJsonString( model.ManifestJsonData ) );
            reloadProcess();

            if ( string.IsNullOrEmpty( registryURL ) )
                Debug.Log( $"CustomPackageManager: Remove Registry URL, wait for resolving package by PackageManager..." );
            else
                Debug.Log( $"CustomPackageManager: Add Registry URL [ {registryURL} ], wait for resolving package by PackageManager..." );
        }

        private void onUpdateGitPackage( string packageName )
        {
            model.ManifestJsonData[ "lock" ][ packageName ] = null;
            FileUtility.SaveManifestJson( convertPackagJsonString( model.ManifestJsonData ) );
            reloadProcess();

            Debug.Log( $"CustomPackageManager: Update git package [ {packageName} ], wait for resolving package by PackageManager..." );
        }

        private void onPackageContextClicked( CustomPackageManagerPackage contextClickedItem )
        {
            var menu = new GenericMenu();
            menu.AddItem( new GUIContent( $"Delete [ {contextClickedItem.PackageName} ]" ), false, () => {
                onRemoveDependencies( contextClickedItem.PackageName );
            } );
            if( contextClickedItem.Version.Contains( ".git" ) && model.ManifestJsonData.ContainsKey( "lock" ) )
            {
                menu.AddSeparator( "" );
                menu.AddItem( new GUIContent( "Update Git Package" ), false, () =>
                {
                    onUpdateGitPackage( contextClickedItem.PackageName );
                } );
            }
            menu.ShowAsContext();
        }

        private void onPackageVersionChanged( CustomPackageManagerPackage versionChangedItem )
        {
            onAddDependencies( versionChangedItem.PackageName, versionChangedItem.Version );
        }

        #endregion

        #region scopedRegistry process

        private void onAddScopedRegistries( CustomPackageManagerScopedRegistry scopedRegistry )
        {
            var addSR = new JsonData();
            addSR[ "name" ] = scopedRegistry.RegistryName;
            addSR[ "url" ] = scopedRegistry.URL;

            var scopesList = scopedRegistry.GetScopesByList();
            var addScopes = new JsonData();
            for ( int i = 0; i < scopesList.Count; i++ )
            {
                addScopes.Add( 1 );
                addScopes[ i ] = scopesList[ i ];
            }
            addSR[ "scopes" ] = addScopes;

            if ( !model.ManifestJsonData.ContainsKey( "scopedRegistries" ) )
            {
                var firstScope = new JsonData();
                firstScope.Add( 1 );
                firstScope[ 0 ] = addSR;
                model.ManifestJsonData[ "scopedRegistries" ] = firstScope;
            } else
            {
                var registryCount = model.ManifestJsonData[ "scopedRegistries" ].Count;
                model.ManifestJsonData[ "scopedRegistries" ].Add( 1 );
                model.ManifestJsonData[ "scopedRegistries" ][ registryCount ] = addSR;
            }

            FileUtility.SaveManifestJson( convertPackagJsonString( model.ManifestJsonData ) );
            reloadProcess();
            Debug.Log( $"CustomPackageManager: Add a scopedRegistry [ {scopedRegistry.RegistryName} ], wait for resolving package by PackageManager..." );
        }

        private void onScopedRegistryContextClicked( CustomPackageManagerScopedRegistry contextClickedItem )
        {
            var menu = new GenericMenu();
            menu.AddItem( new GUIContent( $"Delete [ {contextClickedItem.RegistryName} ]" ), false, () => {
                onRemoveScopedRegistry( contextClickedItem.id );
            } );
            menu.ShowAsContext();
        }

        private void onScopedRegistryScopesChange( CustomPackageManagerScopedRegistry scopedRegistry )
        {
            var changeSR = new JsonData();
            changeSR[ "name" ] = scopedRegistry.RegistryName;
            changeSR[ "url" ] = scopedRegistry.URL;

            var scopesList = scopedRegistry.GetScopesByList();
            var addScopes = new JsonData();
            for ( int i = 0; i < scopesList.Count; i++ )
            {
                addScopes.Add( 1 );
                addScopes[ i ] = scopesList[ i ];
            }
            changeSR[ "scopes" ] = addScopes;

            model.ManifestJsonData[ "scopedRegistries" ][ scopedRegistry.id ] = changeSR;

            FileUtility.SaveManifestJson( convertPackagJsonString( model.ManifestJsonData ) );
            reloadProcess();
            Debug.Log( $"CustomPackageManager: Change [ {scopedRegistry.RegistryName}'s scopes ], wait for resolving package by PackageManager..." );
        }

        private void onRemoveScopedRegistry( int index )
        {
            if( model.ManifestJsonData[ "scopedRegistries" ].Count > 1 )
            {
                var newScopedRegistries = new JsonData();
                var oldScopedRegistries = model.ManifestJsonData[ "scopedRegistries" ];
                var counter = 0;
                for ( int i = 0; i < oldScopedRegistries.Count; i++ )
                {
                    if ( i == index )
                        continue;
                    newScopedRegistries.Add( 1 );
                    newScopedRegistries[ counter++ ] = oldScopedRegistries[ i ];
                }
                model.ManifestJsonData[ "scopedRegistries" ] = newScopedRegistries;
            } else
            {
                model.ManifestJsonData.Remove( "scopedRegistries" );
            }

            FileUtility.SaveManifestJson( convertPackagJsonString( model.ManifestJsonData ) );
            reloadProcess();
            Debug.Log( $"CustomPackageManager: Remove a scopedRegistry, wait for resolving package by PackageManager..." );
        }

        #endregion

        #region common process

        private string convertPackagJsonString( JsonData jsonData )
        {
            var sb = new StringBuilder();
            var jw = new JsonWriter( sb ) { PrettyPrint = true, IndentValue = 2, };
            JsonMapper.ToJson( jsonData, jw );

            return sb.ToString();
        }

        private void reloadProcess()
        {
            CustomPackageManagerWindow.PreferenceView();
            view.Reload();
            AssetDatabase.Refresh();
        }

        #endregion

    }

}