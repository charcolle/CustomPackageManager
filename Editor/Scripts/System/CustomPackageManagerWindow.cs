using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using FileUtility = charcolle.CustomPackageManager.FileUtility;

namespace charcolle.CustomPackageManager
{

    internal class CustomPackageManagerWindow
    {

        [SettingsProvider]
        public static SettingsProvider PreferenceView()
        {
            var manifestJson = FileUtility.LoadManifestJson();
            var model = new CustomPackageManagerModel( manifestJson );
            var view = new CustomPackageManagerGUI();
            var presenter = new CustomPackageManagerPresenter( model, view );

            var provider = new SettingsProvider( "Preferences/CustomPackageManager", SettingsScope.User )
            {
                label = "CustomPackageManager",
                activateHandler = ( searchContext, rootElement ) => {
                    var basicContainer = new VisualElement()
                    {
                        style = {
                            paddingTop = 5,
                            paddingLeft = 10,
                            paddingRight = 10,
                            flexDirection = FlexDirection.Column,
                        }
                    };

                    var titleElement = new VisualElement();
                    titleElement.Add( new Label()
                    {
                        text = "CustomPackageManager",
                        style =
                        {
                            fontSize = 15,
                            unityFontStyleAndWeight = FontStyle.Bold,
                            flexBasis = 25,
                            minHeight = 25,
                        }
                    } );

                    var imguiContainer = new IMGUIContainer( () =>
                    {
                        view.OnIMGUI();
                    } );
                    imguiContainer.style.flexBasis = 1000;

                    basicContainer.Add( titleElement );
                    basicContainer.Add( imguiContainer );
                    rootElement.Add( basicContainer );
                },
                guiHandler = ( searchText ) => {

                },
                keywords = new[] { "CustomPackageManager" }
            };
            return provider;
        }

    }

}