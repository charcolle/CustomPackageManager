using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using charcolle.CustomPackageManager.LitJson;

namespace charcolle.CustomPackageManager
{
    internal class CustomPackageManagerModel
    {
        internal JsonData ManifestJsonData { get; }

        public CustomPackageManagerModel( string manifestJson )
        {
            ManifestJsonData = JsonMapper.ToObject( manifestJson );
        }

    }

}