using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace charcolle.CustomPackageManager
{

    internal class CustomPackageManagerSRTreeView : TreeViewWithTreeModel<CustomPackageManagerScopedRegistry>
    {

        public event Action<CustomPackageManagerScopedRegistry> OnRenameChanged;
        public event Action<CustomPackageManagerScopedRegistry> OnContextClicked;

        enum PackageColumn
        {
            RegistryName,
            URL,
            Scopes,
        }

        public CustomPackageManagerSRTreeView( TreeViewState state, MultiColumnHeader multicolumnHeader, TreeModel<CustomPackageManagerScopedRegistry> model ) : base( state, multicolumnHeader, model )
        {
            // Custom setup
            columnIndexForTreeFoldouts = 2;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            Reload();
        }

        protected override IList<TreeViewItem> BuildRows( TreeViewItem root )
        {
            var rows = base.BuildRows( root );
            //SortIfNeeded (root, rows);
            return rows;
        }

        protected override void RowGUI( RowGUIArgs args )
        {
            var item = ( TreeViewItem<CustomPackageManagerScopedRegistry> )args.item;

            for ( int i = 0; i < args.GetNumVisibleColumns(); ++i )
                CellGUI( args.GetCellRect( i ), item, ( PackageColumn )args.GetColumn( i ), ref args );
        }

        void CellGUI( Rect cellRect, TreeViewItem<CustomPackageManagerScopedRegistry> item, PackageColumn column, ref RowGUIArgs args )
        {
            CenterRectUsingSingleLineHeight( ref cellRect );

            switch ( column )
            {
                case PackageColumn.RegistryName:
                    {
                        GUI.Label( cellRect, item.data.RegistryName );
                    }
                    break;
                case PackageColumn.URL:
                    {
                        GUI.Label( cellRect, item.data.URL );
                    }
                    break;
                case PackageColumn.Scopes:
                    {
                        GUI.Label( cellRect, item.data.Scopes );
                    }
                    break;
            }
        }

        #region rename process

        protected override bool CanRename( TreeViewItem item )
        {
            var renameRect = GetRenameRect( treeViewRect, 0, item );
            return renameRect.width > 30;
        }

        protected override void RenameEnded( RenameEndedArgs args )
        {
            if ( args.acceptedRename )
            {
                var element = treeModel.Find( args.itemID );
                if ( element.name == args.newName )
                    return;
                element.name = args.newName;
                element.Scopes = args.newName;
                OnRenameChanged?.Invoke( new CustomPackageManagerScopedRegistry( element.RegistryName, element.URL, args.newName ) );
                Reload();
            }
        }

        protected override Rect GetRenameRect( Rect rowRect, int row, TreeViewItem item )
        {
            var cellRect = GetCellRectForTreeFoldouts( rowRect );
            CenterRectUsingSingleLineHeight( ref cellRect );
            cellRect.x -= 14f;
            cellRect.xMax += 14f;
            cellRect.y -= 1f;
            return base.GetRenameRect( cellRect, row, item );
        }

        #endregion

        #region other settings

        protected override void ContextClickedItem( int id )
        {
            var item = FindItem( id, rootItem );
            var target = ( TreeViewItem<CustomPackageManagerScopedRegistry> )item;
            OnContextClicked( target.data );
        }

        protected override bool CanMultiSelect( TreeViewItem item )
        {
            return false;
        }

        protected override bool CanStartDrag( CanStartDragArgs args )
        {
            return false;
        }

        #endregion

        #region multicolumnHeader

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState( float treeViewWidth )
        {
            var columns = new[]
            {
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent( "RegistryName" ),
                    contextMenuText = "RegistryName",
                    headerTextAlignment = TextAlignment.Center,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 100,
                    minWidth = 50,
                    maxWidth = 500,
                    autoResize = true,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent( "URL" ),
                    contextMenuText = "URL",
                    headerTextAlignment = TextAlignment.Center,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 150,
                    minWidth = 100,
                    maxWidth = 500,
                    autoResize = true,
                    allowToggleVisibility = true
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent( " Scopes", EditorGUIUtility.FindTexture("FilterByType") ),
                    contextMenuText = "Scopes",
                    headerTextAlignment = TextAlignment.Center,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 150,
                    minWidth = 200,
                    maxWidth = 500,
                    autoResize = true,
                    allowToggleVisibility = true
                },
            };

            var state = new MultiColumnHeaderState( columns );
            return state;
        }

        #endregion

        #region sort

        //public enum SortOption
        //{
        //    Name,
        //    Value1,
        //    Value2,
        //    Value3,
        //}

        //// Sort options per column
        //SortOption[] m_SortOptions =
        //{
        //    SortOption.Value1,
        //    SortOption.Value3,
        //    SortOption.Name,
        //    SortOption.Value1,
        //    SortOption.Value2,
        //    SortOption.Value3
        //};
        //void OnSortingChanged( MultiColumnHeader multiColumnHeader )
        //{
        //    SortIfNeeded( rootItem, GetRows() );
        //}

        //void SortIfNeeded( TreeViewItem root, IList<TreeViewItem> rows )
        //{
        //    if ( rows.Count <= 1 )
        //        return;

        //    if ( multiColumnHeader.sortedColumnIndex == -1 )
        //    {
        //        return; // No column to sort for (just use the order the data are in)
        //    }

        //    // Sort the roots of the existing tree items
        //    SortByMultipleColumns();
        //    TreeToList( root, rows );
        //    Repaint();
        //}

        //void SortByMultipleColumns()
        //{
        //    var sortedColumns = multiColumnHeader.state.sortedColumns;

        //    if ( sortedColumns.Length == 0 )
        //        return;

        //    var myTypes = rootItem.children.Cast<TreeViewItem<CustomPackageManagerScopedRegistryClass>>();
        //    var orderedQuery = InitialOrder( myTypes, sortedColumns );
        //    for ( int i = 1; i < sortedColumns.Length; i++ )
        //    {
        //        SortOption sortOption = m_SortOptions[ sortedColumns[ i ] ];
        //        bool ascending = multiColumnHeader.IsSortedAscending( sortedColumns[ i ] );

        //        switch ( sortOption )
        //        {
        //            case SortOption.Name:
        //                orderedQuery = orderedQuery.ThenBy( l => l.data.name, ascending );
        //                break;
        //            case SortOption.Value1:
        //                orderedQuery = orderedQuery.ThenBy( l => l.data.floatValue1, ascending );
        //                break;
        //            case SortOption.Value2:
        //                orderedQuery = orderedQuery.ThenBy( l => l.data.floatValue2, ascending );
        //                break;
        //            case SortOption.Value3:
        //                orderedQuery = orderedQuery.ThenBy( l => l.data.floatValue3, ascending );
        //                break;
        //        }
        //    }

        //    rootItem.children = orderedQuery.Cast<TreeViewItem>().ToList();
        //}

        #endregion

    }

}