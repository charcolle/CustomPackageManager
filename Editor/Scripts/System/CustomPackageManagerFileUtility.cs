using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

namespace charcolle.CustomPackageManager
{
    internal class FileUtility
    {

        internal static string LoadManifestJson()
        {
            var manifestText = loadRawFile( manifestJsonPath );
            return manifestText;
        }

        internal static void SaveManifestJson( string newManifestJson )
        {
            saveRawFile( manifestJsonPath, newManifestJson );
        }

        internal static StyleSheet LoadStyleSheet( string fileName )
        {
            return null;
        }

        internal static VisualTreeAsset LoadUXML( string fileName )
        {
            return null;
        }

        #region io process

        private static string rootPath {
            get {
                return null;
            }
        }

        private static string manifestJsonPath {
            get {
                var projectPath = pathSlashFix( Application.dataPath ).Replace( "/Assets", "" );
                var manifestJsonPath = pathSlashFix( Path.Combine( projectPath, "Packages/manifest.json" ) );

                return manifestJsonPath;
            }
        }

        private static string loadRawFile( string path )
        {
            var text = "";
            using ( var sr = new StreamReader( path ) )
            {
                text = sr.ReadToEnd();
            }
            return text;
        }

        private static void saveRawFile( string path, string data )
        {
            var utf8WithoutBOM = new System.Text.UTF8Encoding( false );
            using ( var sw = new StreamWriter( path, false, utf8WithoutBOM ) )
            {
                sw.WriteLine( data );
            }
        }

        private static List<T> findAssetsByType<T>( string type ) where T : Object
        {
            var searchFilter = "t:" + type;
            var guids = AssetDatabase.FindAssets( searchFilter );
            if ( guids == null || guids.Length == 0 )
                return null;

            var list = new List<T>();
            for ( int i = 0; i < guids.Length; i++ )
            {
                var assetPath = AssetDatabase.GUIDToAssetPath( guids[ i ] );
                list.Add( AssetDatabase.LoadAssetAtPath<T>( assetPath ) );
            }
            return list;
        }

        private static string getAssetGUID( string searchFilter )
        {
            var guids = AssetDatabase.FindAssets( searchFilter );
            if ( guids == null || guids.Length == 0 )
                return null;

            if ( guids.Length > 1 )
            {
            }
            return guids[ 0 ];
        }

        private const string forwardSlash = "/";
        private const string backSlash = "\\";
        public static string pathSlashFix( string path )
        {
            return path.Replace( backSlash, forwardSlash );
        }

        #endregion

    }

}