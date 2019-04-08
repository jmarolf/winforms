﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Windows.Forms
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;

    /// <include file='doc\DataGridViewEditingControl.uex' path='docs/doc[@for="IDataGridViewEditingControl"]/*' />
    public interface IDataGridViewEditingControl
    {
        /// <include file='doc\DataGridViewEditingControl.uex' path='docs/doc[@for="IDataGridViewEditingControl.EditingControlDataGridView"]/*' />
        DataGridView EditingControlDataGridView
        {
            get;
            set;
        }

        /// <include file='doc\DataGridViewEditingControl.uex' path='docs/doc[@for="IDataGridViewEditingControl.EditingControlFormattedValue"]/*' />
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        object EditingControlFormattedValue
        {
            get;
            set;
        }

        /// <include file='doc\DataGridViewEditingControl.uex' path='docs/doc[@for="IDataGridViewEditingControl.EditingControlRowIndex"]/*' />
        int EditingControlRowIndex
        {
            get;
            set;
        }

        /// <include file='doc\DataGridViewEditingControl.uex' path='docs/doc[@for="IDataGridViewEditingControl.EditingControlValueChanged"]/*' />
        bool EditingControlValueChanged
        {
            get;
            set;
        }

        /// <include file='doc\DataGridViewEditingControl.uex' path='docs/doc[@for="IDataGridViewEditingControl.EditingPanelCursor"]/*' />
        Cursor EditingPanelCursor
        {
            get;
        }

        /// <include file='doc\DataGridViewEditingControl.uex' path='docs/doc[@for="IDataGridViewEditingControl.RepositionEditingControlOnValueChange"]/*' />
        bool RepositionEditingControlOnValueChange
        {
            get;
        }

        /// <include file='doc\DataGridViewEditingControl.uex' path='docs/doc[@for="IDataGridViewEditingControl.ApplyCellStyleToEditingControl"]/*' />
        void ApplyCellStyleToEditingControl(DataGridViewCellStyle dataGridViewCellStyle);

        /// <include file='doc\DataGridViewEditingControl.uex' path='docs/doc[@for="IDataGridViewEditingControl.EditingControlWantsInputKey"]/*' />
        bool EditingControlWantsInputKey(Keys keyData, bool dataGridViewWantsInputKey);

        /// <include file='doc\DataGridViewEditingControl.uex' path='docs/doc[@for="IDataGridViewEditingControl.GetEditingControlFormattedValue"]/*' />
        [SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        object GetEditingControlFormattedValue(DataGridViewDataErrorContexts context);

        /// <include file='doc\DataGridViewEditingControl.uex' path='docs/doc[@for="IDataGridViewEditingControl.PrepareEditingControlForEdit"]/*' />
        void PrepareEditingControlForEdit(bool selectAll);
    }

    /// <include file='doc\DataGridViewEditingControl.uex' path='docs/doc[@for="DataGridViewEditingControlAccessibleObject"]/*' />
    /// <devdoc>
    ///    Implements a custom AccessibleObject that fixes editing control's accessibility ancestor chain.
    /// </devdoc>
    [System.Runtime.InteropServices.ComVisible(true)]
    internal class DataGridViewEditingControlAccessibleObject : Control.ControlAccessibleObject
    {
        public DataGridViewEditingControlAccessibleObject(Control ownerControl) : base(ownerControl)
        {
            Debug.Assert(ownerControl is IDataGridViewEditingControl, "ownerControl must implement IDataGridViewEditingControl");
        }

        internal override bool IsIAccessibleExSupported()
        {
            return true;
        }

        public override AccessibleObject Parent
        {
            get
            {
                return (Owner as IDataGridViewEditingControl)?.EditingControlDataGridView?.CurrentCell?.AccessibilityObject;
            }
        }

        internal override bool IsPatternSupported(int patternId)
        {
            if (patternId == NativeMethods.UIA_ExpandCollapsePatternId)
            {
                ComboBox ownerComboBoxControl = Owner as ComboBox;
                if (ownerComboBoxControl != null)
                {
                    return ownerComboBoxControl.DropDownStyle != ComboBoxStyle.Simple;
                }
            }

            return base.IsPatternSupported(patternId);
        }

        internal override object GetPropertyValue(int propertyID)
        {
            if (propertyID == NativeMethods.UIA_IsExpandCollapsePatternAvailablePropertyId)
            {
                return IsPatternSupported(NativeMethods.UIA_ExpandCollapsePatternId);
            }

            return base.GetPropertyValue(propertyID);
        }

        internal override UnsafeNativeMethods.ExpandCollapseState ExpandCollapseState
        {
            get
            {
                ComboBox ownerComboBoxControl = Owner as ComboBox;
                if (ownerComboBoxControl != null)
                {
                    return ownerComboBoxControl.DroppedDown == true ? UnsafeNativeMethods.ExpandCollapseState.Expanded : UnsafeNativeMethods.ExpandCollapseState.Collapsed;
                }

                return base.ExpandCollapseState;
            }
        }
    }
}
