﻿// ------------------------------------------------------------------------
// Set ListView column sort icons method
// ------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ListViewSort
{
    public class LVSort
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct HDITEM
        {
            public int mask;
            public int cxy;
            [MarshalAs(UnmanagedType.LPTStr)] public string pszText;
            public IntPtr hbm;
            public int cchTextMax;
            public int fmt;
            public int lParam;
            public int iImage;
            public int iOrder;
        }

        // Parameters for ListView-Headers
        private const int HDI_FORMAT = 0x0004;

        private const int HDF_LEFT = 0x0000;
        private const int HDF_STRING = 0x4000;
        private const int HDF_SORTUP = 0x0400;
        private const int HDF_SORTDOWN = 0x0200;
        private const int LVM_GETHEADER = 0x1000 + 31; // LVM_FIRST + 31
        private const int HDM_GETITEM = 0x1200 + 11; // HDM_FIRST + 11
        private const int HDM_SETITEM = 0x1200 + 12; // HDM_FIRST + 12

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        private static extern IntPtr SendMessageITEM(IntPtr Handle, int msg, IntPtr wParam, ref HDITEM lParam);


        public static void SetSortIcons(ListView listview1, ref int previouslySortedColumn, int newSortColumn)
        {
            var hHeader = SendMessage(listview1.Handle, LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
            var newColumn = new IntPtr(newSortColumn);
            var prevColumn = new IntPtr(previouslySortedColumn);
            HDITEM hdItem;
            IntPtr rtn;

            // Only update the previous item if it existed and if it was a different one.
            if (previouslySortedColumn != -1 && previouslySortedColumn != newSortColumn)
            {
                // Clear icon from the previous column.
                hdItem = new HDITEM();
                hdItem.mask = HDI_FORMAT;
                rtn = SendMessageITEM(hHeader, HDM_GETITEM, prevColumn, ref hdItem);
                hdItem.fmt &= ~HDF_SORTDOWN & ~HDF_SORTUP;
                rtn = SendMessageITEM(hHeader, HDM_SETITEM, prevColumn, ref hdItem);
            }

            // Set icon on the new column.
            hdItem = new HDITEM();
            hdItem.mask = HDI_FORMAT;
            rtn = SendMessageITEM(hHeader, HDM_GETITEM, newColumn, ref hdItem);
            if (listview1.Sorting == SortOrder.Ascending)
            {
                hdItem.fmt &= ~HDF_SORTDOWN;
                hdItem.fmt |= HDF_SORTUP;
            }
            else
            {
                hdItem.fmt &= ~HDF_SORTUP;
                hdItem.fmt |= HDF_SORTDOWN;
            }
            rtn = SendMessageITEM(hHeader, HDM_SETITEM, newColumn, ref hdItem);
            previouslySortedColumn = newSortColumn;
        }
    }
}