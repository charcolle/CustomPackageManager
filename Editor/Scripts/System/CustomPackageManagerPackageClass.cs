namespace charcolle.CustomPackageManager
{

    internal class CustomPackageManagerPackage : TreeElement
    {
        public string PackageName { get; set; }
        public string Version { get; set; }

        public CustomPackageManagerPackage( string packageName, string version )
        {
            PackageName = packageName;
            Version = version;
        }

        public static CustomPackageManagerPackage Root {
            get {
                return new CustomPackageManagerPackage( "", "" )
                {
                    id = -1,
                    depth = -1,
                    name = "root",
                };
            }
        }

    }

}