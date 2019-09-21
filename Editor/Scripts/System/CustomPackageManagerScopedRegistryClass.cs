using System.Collections.Generic;

namespace charcolle.CustomPackageManager
{

    internal class CustomPackageManagerScopedRegistry : TreeElement
    {
        public string RegistryName { get; set; }
        public string URL { get; set; }
        public string Scopes { get; set; }

        public CustomPackageManagerScopedRegistry( string registryName, string url, string scopes )
        {
            this.RegistryName = registryName;
            this.URL = url;
            this.Scopes = scopes;
        }

        public List<string> GetScopesByList()
        {
            var list = new List<string>();
            var splits = Scopes.Split( ',' );
            for ( int i = 0; i < splits.Length; i++ )
                if ( !string.IsNullOrEmpty( splits[ i ] ) )
                    list.Add( splits[ i ] );
            return list;
        }

        public static CustomPackageManagerScopedRegistry Root {
            get {
                return new CustomPackageManagerScopedRegistry( "", "", null )
                {
                    id = -1,
                    depth = -1,
                    name = "root",
                };
            }
        }
    }
}
