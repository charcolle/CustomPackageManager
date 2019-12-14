using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace charcolle.CustomPackageManager
{

    internal class CustomPackageManagerTreeView : TreeViewWithTreeModel<CustomPackageManagerPackage>
    {

        public string CustomSearchText { get; set; }
        public bool HiddenUnityOfficialPackage { get; set; }
        public event Action<CustomPackageManagerPackage> OnRenameChanged;
        public event Action<CustomPackageManagerPackage> OnContextClicked;

        enum PackageColumn
        {
            PackageName,
            Version,
        }

        public CustomPackageManagerTreeView( TreeViewState state, MultiColumnHeader multicolumnHeader, TreeModel<CustomPackageManagerPackage> model ) : base( state, multicolumnHeader, model )
        {
            // Custom setup
            columnIndexForTreeFoldouts = 1;
            showAlternatingRowBackgrounds = true;
            showBorder = true;
            multicolumnHeader.sortingChanged += OnSortingChanged;
            Reload();
        }

        protected override IList<TreeViewItem> BuildRows( TreeViewItem root )
        {
            var rows = base.BuildRows( root );
            SortIfNeeded( root, rows );
            var result = new List<TreeViewItem>();
            if ( !string.IsNullOrEmpty( CustomSearchText ) )
            {
                foreach( var item in rows )
                {
                    var target = ( TreeViewItem<CustomPackageManagerPackage> )item;
                    if ( target.data.PackageName.Contains( CustomSearchText ) )
                        result.Add( item );
                }
                return result;
            } else
            {
                result = rows.ToList();
            }
            if ( HiddenUnityOfficialPackage )
                result = result.Select( item => ( TreeViewItem<CustomPackageManagerPackage> )item ).Where( item => !item.data.PackageName.Contains( "com.unity" ) ).Select( i => (TreeViewItem)i ).ToList();
            return result;
        }

        protected override void RowGUI( RowGUIArgs args )
        {
            var item = ( TreeViewItem<CustomPackageManagerPackage> )args.item;

            for ( int i = 0; i < args.GetNumVisibleColumns(); ++i )
                CellGUI( args.GetCellRect( i ), item, ( PackageColumn )args.GetColumn( i ), ref args );
        }

        void CellGUI( Rect cellRect, TreeViewItem<CustomPackageManagerPackage> item, PackageColumn column, ref RowGUIArgs args )
        {
            CenterRectUsingSingleLineHeight( ref cellRect );

            switch ( column )
            {
                case PackageColumn.PackageName:
                    {
                        GUI.Label( cellRect, item.data.PackageName );
                    }
                    break;
                case PackageColumn.Version:
                    {
                        GUI.Label( cellRect, item.data.Version );
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
                element.Version = args.newName;
                OnRenameChanged?.Invoke( new CustomPackageManagerPackage( element.PackageName, args.newName ) );
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
            var target = ( TreeViewItem<CustomPackageManagerPackage> )item;
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
                    headerContent = new GUIContent( " PackageName", EditorGUIUtility.FindTexture("FilterByLabel") ),
                    contextMenuText = "PackageName",
                    headerTextAlignment = TextAlignment.Center,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 150,
                    minWidth = 100,
                    maxWidth = 500,
                    autoResize = true,
                    allowToggleVisibility = true,
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent( " Version", EditorGUIUtility.FindTexture("FilterByType") ),
                    contextMenuText = "Version",
                    headerTextAlignment = TextAlignment.Center,
                    sortingArrowAlignment = TextAlignment.Right,
                    width = 150,
                    minWidth = 100,
                    maxWidth = 500,
                    autoResize = false,
                    allowToggleVisibility = true,
                    canSort = false
                },
            };

            var state = new MultiColumnHeaderState( columns );
            return state;
        }

        #endregion

        #region sort

        public enum SortOption
        {
            PackageName,
            Version,
        }

        // Sort options per column
        SortOption[] m_SortOptions =
        {
            SortOption.PackageName,
            SortOption.Version,
        };

        void OnSortingChanged( MultiColumnHeader multiColumnHeader )
        {
            SortIfNeeded( rootItem, GetRows() );
        }

        void SortIfNeeded( TreeViewItem root, IList<TreeViewItem> rows )
        {
            if ( rows.Count <= 1 )
                return;

            if ( multiColumnHeader.sortedColumnIndex == -1 )
            {
                return; // No column to sort for (just use the order the data are in)
            }

            // Sort the roots of the existing tree items
            SortByMultipleColumns();
            TreeToList( root, rows );
            Repaint();
        }

        void SortByMultipleColumns()
        {
            var sortedColumns = multiColumnHeader.state.sortedColumns;

            if ( sortedColumns.Length == 0 )
                return;

            var myTypes = rootItem.children.Cast<TreeViewItem<CustomPackageManagerPackage>>();
            var orderedQuery = InitialOrder( myTypes, sortedColumns );
            for ( int i = 1; i < sortedColumns.Length; i++ )
            {
                SortOption sortOption = m_SortOptions[ sortedColumns[ i ] ];
                bool ascending = multiColumnHeader.IsSortedAscending( sortedColumns[ i ] );

                switch ( sortOption )
                {
                    case SortOption.PackageName:
                        orderedQuery = orderedQuery.ThenBy( l => l.data.PackageName, ascending );
                        break;
                }
            }

            rootItem.children = orderedQuery.Cast<TreeViewItem>().ToList();
        }

        IOrderedEnumerable<TreeViewItem<CustomPackageManagerPackage>> InitialOrder( IEnumerable<TreeViewItem<CustomPackageManagerPackage>> myTypes, int[] history )
        {
            SortOption sortOption = m_SortOptions[ history[ 0 ] ];
            bool ascending = multiColumnHeader.IsSortedAscending( history[ 0 ] );
            switch ( sortOption )
            {
                case SortOption.PackageName:
                    return myTypes.Order( item => item.data.PackageName, ascending );
            }
            return myTypes.Order( l => l.data.name, ascending );
        }

        #endregion


        #region utility

        static void TreeToList( TreeViewItem root, IList<TreeViewItem> result )
        {
            if ( root == null )
                throw new NullReferenceException( "root" );
            if ( result == null )
                throw new NullReferenceException( "result" );

            result.Clear();

            if ( root.children == null )
                return;

            Stack<TreeViewItem> stack = new Stack<TreeViewItem>();
            for ( int i = root.children.Count - 1; i >= 0; i-- )
                stack.Push( root.children[ i ] );

            while ( stack.Count > 0 )
            {
                TreeViewItem current = stack.Pop();
                result.Add( current );

                if ( current.hasChildren && current.children[ 0 ] != null )
                {
                    for ( int i = current.children.Count - 1; i >= 0; i-- )
                    {
                        stack.Push( current.children[ i ] );
                    }
                }
            }
        }

        #endregion


    }

}