﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Windows.Forms {
    using System;
    using System.Drawing;
    using System.Windows.Forms;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Windows.Forms.Layout;
    
    /// <include file='doc\ToolStripDropDownItem.uex' path='docs/doc[@for="ToolStripDropDownItem"]/*' />
    /// <devdoc>
    /// Base class for ToolStripItems that display DropDown windows.
    /// </devdoc>
    [Designer("System.Windows.Forms.Design.ToolStripMenuItemDesigner, " + AssemblyRef.SystemDesign)]
    [DefaultProperty(nameof(DropDownItems))]
    public abstract class ToolStripDropDownItem : ToolStripItem {

        private ToolStripDropDown dropDown     = null;
        private ToolStripDropDownDirection toolStripDropDownDirection = ToolStripDropDownDirection.Default;
        private static readonly object EventDropDownShow              = new object();
        private static readonly object EventDropDownHide              = new object();
        private static readonly object EventDropDownOpened               = new object();
        private static readonly object EventDropDownClosed               = new object();
        private static readonly object EventDropDownItemClicked               = new object();
        
        /// <include file='doc\ToolStripDropDownItem.uex' path='docs/doc[@for="ToolStripDropDownItem.ToolStripDropDownItem"]/*' />
        /// <devdoc>
        /// Protected ctor so you can't create one of these without deriving from it.
        /// </devdoc>
        protected ToolStripDropDownItem() {
        }

        protected ToolStripDropDownItem(string text, Image image, EventHandler onClick) : base(text, image, onClick) {
        }

        protected ToolStripDropDownItem(string text, Image image, EventHandler onClick, string name) : base(text, image, onClick, name) {
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
        protected ToolStripDropDownItem(string text, Image image, params ToolStripItem[] dropDownItems) : this(text, image, (EventHandler)null) {
            if (dropDownItems != null) {
                this.DropDownItems.AddRange(dropDownItems);
            }
        }


        /// <include file='doc\ToolStripDropDownItem.uex' path='docs/doc[@for="ToolStripDropDownItem.DropDown"]/*' />
        /// <devdoc>
        /// The ToolStripDropDown that will be displayed when this item is clicked.
        /// </devdoc>
        [
        TypeConverter(typeof(ReferenceConverter)),
        SRCategory(nameof(SR.CatData)),
        SRDescription(nameof(SR.ToolStripDropDownDescr))
        ]
        public ToolStripDropDown DropDown {
            get {               
                if (dropDown == null) {
                    DropDown = CreateDefaultDropDown();
                    if (!(this is ToolStripOverflowButton))
                    {
                        dropDown.SetAutoGeneratedInternal(true);
                    }

                    if (ParentInternal != null) {
                        dropDown.ShowItemToolTips = ParentInternal.ShowItemToolTips;
                    }
                }   
                return dropDown; 
            } 
            set { 
                if (dropDown != value) {

                    if (dropDown != null) {
                        dropDown.Opened -= new EventHandler(DropDown_Opened);
                        dropDown.Closed -= new ToolStripDropDownClosedEventHandler(DropDown_Closed);
                        dropDown.ItemClicked -= new ToolStripItemClickedEventHandler(DropDown_ItemClicked);
                        dropDown.UnassignDropDownItem();
                    }

                    dropDown = value;
                    if (dropDown != null) {
                        dropDown.Opened += new EventHandler(DropDown_Opened);
                        dropDown.Closed += new ToolStripDropDownClosedEventHandler(DropDown_Closed);
                        dropDown.ItemClicked += new ToolStripItemClickedEventHandler(DropDown_ItemClicked);
                        dropDown.AssignToDropDownItem();
                    }
                    
                }
                
               
            }
        }

        // the area which activates the dropdown.
        internal virtual Rectangle DropDownButtonArea {
            get { return this.Bounds; }
        }

        [Browsable(false)]
        [SRDescription(nameof(SR.ToolStripDropDownItemDropDownDirectionDescr))]
        [SRCategory(nameof(SR.CatBehavior))]
        public ToolStripDropDownDirection DropDownDirection {
            get { 
                if (toolStripDropDownDirection == ToolStripDropDownDirection.Default) {
                   ToolStrip parent = ParentInternal;
                   if (parent != null) {
                       ToolStripDropDownDirection dropDownDirection = parent.DefaultDropDownDirection;
                       if (OppositeDropDownAlign || this.RightToLeft != parent.RightToLeft && (this.RightToLeft != RightToLeft.Inherit)) {
                            dropDownDirection = RTLTranslateDropDownDirection(dropDownDirection, RightToLeft);
                       }

                       if  (IsOnDropDown) {
                          // we gotta make sure that we dont collide with the existing menu.
                          Rectangle bounds = GetDropDownBounds(dropDownDirection);
                          Rectangle ownerItemBounds = new Rectangle(TranslatePoint(Point.Empty, ToolStripPointType.ToolStripItemCoords, ToolStripPointType.ScreenCoords), Size);
                          Rectangle intersectionBetweenChildAndParent = Rectangle.Intersect(bounds, ownerItemBounds);
                            
                          // grab the intersection 
                          if (intersectionBetweenChildAndParent.Width >= 2) {
                              RightToLeft toggledRightToLeft = (RightToLeft == RightToLeft.Yes) ? RightToLeft.No : RightToLeft.Yes;
                              ToolStripDropDownDirection newDropDownDirection = RTLTranslateDropDownDirection(dropDownDirection, toggledRightToLeft);

                              // verify that changing the dropdown direction actually causes less intersection.
                              int newIntersectionWidth = Rectangle.Intersect(GetDropDownBounds(newDropDownDirection), ownerItemBounds).Width;
                              if (newIntersectionWidth < intersectionBetweenChildAndParent.Width) {
                                 dropDownDirection = newDropDownDirection;
                              }
                          }
                          
                       }
                       return dropDownDirection;

                   }
                }

                // someone has set a custom override
                return toolStripDropDownDirection; 

            }
            set { 
                // cant use Enum.IsValid as its not sequential
                switch (value) {
                   case ToolStripDropDownDirection.AboveLeft:
                   case ToolStripDropDownDirection.AboveRight:
                   case ToolStripDropDownDirection.BelowLeft:
                   case ToolStripDropDownDirection.BelowRight:
                   case ToolStripDropDownDirection.Left:
                   case ToolStripDropDownDirection.Right:
                   case ToolStripDropDownDirection.Default:
                      break;
                   default:
                    throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ToolStripDropDownDirection));
                }

                if (toolStripDropDownDirection != value) {
                    toolStripDropDownDirection = value;
                    if (HasDropDownItems && DropDown.Visible) {
                        DropDown.Location = DropDownLocation;
                    }
                }
            }
        }

    
        /// <include file='doc\ToolStripDropDownItem.uex' path='docs/doc[@for="ToolStripDropDownItem.DropDownClosed"]/*' />
        /// <devdoc>
        /// Occurs when the dropdown is closed
        /// </devdoc>
        [
        SRCategory(nameof(SR.CatAction)),
        SRDescription(nameof(SR.ToolStripDropDownClosedDecr))
        ]
        public event EventHandler DropDownClosed {
            add {
                Events.AddHandler(EventDropDownClosed, value);
            }
            remove {
                Events.RemoveHandler(EventDropDownClosed, value);
            }
        }

        /// <include file='doc\ToolStripDropDownItem.uex' path='docs/doc[@for="ToolStripDropDownItem.DropDownLocation"]/*' />
        internal protected virtual Point DropDownLocation 
        {
            get { 
                
                if (ParentInternal == null || !HasDropDownItems){
                    return Point.Empty;
                }
                ToolStripDropDownDirection dropDownDirection = DropDownDirection;
                return GetDropDownBounds(dropDownDirection).Location;
            }
        }

        /// <include file='doc\WinBarPopupItem.uex' path='docs/doc[@for="ToolStripDropDownItem.DropDownOpening"]/*' />
        [
        SRCategory(nameof(SR.CatAction)),
        SRDescription(nameof(SR.ToolStripDropDownOpeningDescr))
        ]
        public event EventHandler DropDownOpening {
            add {
                Events.AddHandler(EventDropDownShow, value);
            }
            remove {
                Events.RemoveHandler(EventDropDownShow, value);
            }
        }   
        /// <include file='doc\ToolStripDropDownItem.uex' path='docs/doc[@for="ToolStripDropDownItem.DropDownOpened"]/*' />
        /// <devdoc>
        /// Occurs when the dropdown is opened
        /// </devdoc>
        [
        SRCategory(nameof(SR.CatAction)),
        SRDescription(nameof(SR.ToolStripDropDownOpenedDescr))
        ]
        public event EventHandler DropDownOpened {
            add {
                Events.AddHandler(EventDropDownOpened, value);
            }
            remove {
                Events.RemoveHandler(EventDropDownOpened, value);
            }
        }   

        /// <include file='doc\ToolStripDropDownItem.uex' path='docs/doc[@for="ToolStripDropDownItem.DropDownItems"]/*' />
        /// <devdoc>
        /// Returns the DropDown's items collection.
        /// </devdoc>
        [
        DesignerSerializationVisibility(DesignerSerializationVisibility.Content),
        SRCategory(nameof(SR.CatData)),
        SRDescription(nameof(SR.ToolStripDropDownItemsDescr))
        ]
        public ToolStripItemCollection DropDownItems {
            get {
                return DropDown.Items;
            }
        }

        /// <include file='doc\ToolStripDropDownItem.uex' path='docs/doc[@for="ToolStripDropDownItem.DropDownItemClicked"]/*' />
        /// <devdoc>
        /// Occurs when the dropdown is opened
        /// </devdoc>
        [SRCategory(nameof(SR.CatAction))]
        public event ToolStripItemClickedEventHandler DropDownItemClicked {
            add {
                Events.AddHandler(EventDropDownItemClicked, value);
            }
            remove {
                Events.RemoveHandler(EventDropDownItemClicked, value);
            }
        }

        /// <include file='doc\ToolStripPopupItem.uex' path='docs/doc[@for="ToolStripDropDownItem.HasDropDownItems"]/*' />
        [Browsable(false)]
        public virtual bool HasDropDownItems {
            get {
                //Use count of visible DisplayedItems instead so that we take into account things that arent visible
                return (dropDown != null) && dropDown.HasVisibleItems;
            }
        }

        /// <include file='doc\ToolStripPopupItem.uex' path='docs/doc[@for="ToolStripDropDownItem.HasDropDown"]/*' />
        [Browsable(false)]
        public bool HasDropDown {
            get { return dropDown != null; }
        }

        /// <include file='doc\ToolStripDropDownItem.uex' path='docs/doc[@for="ToolStripDropDownItem.Pressed"]/*' />
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public override bool Pressed {
            get {
                 // 
                 if (dropDown != null) {
                    if (DropDown.AutoClose || !IsInDesignMode || (IsInDesignMode && !IsOnDropDown)){
                        return DropDown.OwnerItem == this && DropDown.Visible; 
                    }
                 }
                 return base.Pressed;
            }
        }

        internal virtual bool OppositeDropDownAlign {
            get { return false; }
        }


        internal virtual void AutoHide(ToolStripItem otherItemBeingSelected) {
            HideDropDown();
        }

        /// <include file='doc\ToolStripDropDownItem.uex' path='docs/doc[@for="ToolStripDropDownItem.CreateAccessibilityInstance"]/*' />
        protected override AccessibleObject CreateAccessibilityInstance() {
            return new ToolStripDropDownItemAccessibleObject(this);
        }

        /// <include file='doc\ToolStripPopupItem.uex' path='docs/doc[@for="ToolStripDropDownItem.CreateDefaultDropDown"]/*' />
        protected virtual ToolStripDropDown CreateDefaultDropDown() {
            // AutoGenerate a Winbar DropDown - set the property so we hook events
             return new ToolStripDropDown(this, true);
        }

        private Rectangle DropDownDirectionToDropDownBounds(ToolStripDropDownDirection dropDownDirection, Rectangle dropDownBounds) {
              Point offset = Point.Empty;
 
              switch (dropDownDirection) {
                  case ToolStripDropDownDirection.AboveLeft:
                      offset.X = - dropDownBounds.Width + this.Width;
                      offset.Y = - dropDownBounds.Height+1;
                      break;
                  case ToolStripDropDownDirection.AboveRight:                        
                      offset.Y = - dropDownBounds.Height+1;
                      break;
                  case ToolStripDropDownDirection.BelowRight:
                      offset.Y = this.Height-1;
                      break;
                  case ToolStripDropDownDirection.BelowLeft:
                      offset.X = - dropDownBounds.Width + this.Width;
                      offset.Y = this.Height-1;
                      break;
                  case ToolStripDropDownDirection.Right:
                      offset.X = this.Width;
                      if (!IsOnDropDown) {
                          // overlap the toplevel toolstrip
                          offset.X--;
                      }
                      break;
   
                  case ToolStripDropDownDirection.Left:
                      offset.X = - dropDownBounds.Width;
                      break;
              }
              
              Point itemScreenLocation = this.TranslatePoint(Point.Empty, ToolStripPointType.ToolStripItemCoords, ToolStripPointType.ScreenCoords);
              dropDownBounds.Location = new Point(itemScreenLocation.X + offset.X, itemScreenLocation.Y + offset.Y);
              dropDownBounds =  WindowsFormsUtils.ConstrainToScreenWorkingAreaBounds(dropDownBounds);
              return dropDownBounds;
        }

        
        private void DropDown_Closed(object sender, ToolStripDropDownClosedEventArgs e) {
            OnDropDownClosed(EventArgs.Empty);
        }
   
        private void DropDown_Opened(object sender, EventArgs e) {
            OnDropDownOpened(EventArgs.Empty);
        }

        private void DropDown_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            OnDropDownItemClicked(e);
        }
        /// <include file='doc\ToolStripDropDownItem.uex' path='docs/doc[@for="ToolStripDropDownItem.Dispose"]/*' />
        /// <devdoc>
        /// Make sure we unhook dropdown events.
        /// </devdoc>
        protected override void Dispose(bool disposing)
        {
            if (this.dropDown != null) {
                dropDown.Opened -= new EventHandler(DropDown_Opened);
                dropDown.Closed -= new ToolStripDropDownClosedEventHandler(DropDown_Closed);
                dropDown.ItemClicked -= new ToolStripItemClickedEventHandler(DropDown_ItemClicked);

                if (disposing && dropDown.IsAutoGenerated) {
                    // if we created the dropdown, dispose it and its children.
                    dropDown.Dispose();
                    dropDown = null;
                }
            }
            base.Dispose(disposing);
        }


        private Rectangle GetDropDownBounds(ToolStripDropDownDirection dropDownDirection) {

            Rectangle dropDownBounds = new Rectangle(Point.Empty, DropDown.GetSuggestedSize());
            // calculate the offset from the upper left hand corner of the item.
            dropDownBounds = DropDownDirectionToDropDownBounds(dropDownDirection, dropDownBounds);

            // we should make sure we dont obscure the owner item.
            Rectangle itemScreenBounds = new Rectangle(this.TranslatePoint(Point.Empty, ToolStripPointType.ToolStripItemCoords, ToolStripPointType.ScreenCoords), this.Size);

            if (Rectangle.Intersect(dropDownBounds, itemScreenBounds).Height > 1) {

                bool rtl = (RightToLeft == RightToLeft.Yes);

                // try positioning to the left
                if (Rectangle.Intersect(dropDownBounds, itemScreenBounds).Width > 1) {
                    dropDownBounds = DropDownDirectionToDropDownBounds(!rtl ? ToolStripDropDownDirection.Right : ToolStripDropDownDirection.Left, dropDownBounds);
                }

                // try positioning to the right
                if (Rectangle.Intersect(dropDownBounds, itemScreenBounds).Width > 1) {
                    dropDownBounds = DropDownDirectionToDropDownBounds(!rtl ? ToolStripDropDownDirection.Left : ToolStripDropDownDirection.Right, dropDownBounds);
                }
            }

            return dropDownBounds;

        }
        

        
        /// <include file='doc\ToolStripDropDownItem.uex' path='docs/doc[@for="ToolStripDropDownItem.HideDropDown"]/*' />
        /// <devdoc>
        /// Hides the DropDown, if it is visible.  
        /// </devdoc>
        public void HideDropDown() {
            // consider - CloseEventArgs to prevent shutting down.
            OnDropDownHide(EventArgs.Empty);
        
           if (this.dropDown != null && this.dropDown.Visible) {
               DropDown.Visible = false;

               AccessibilityNotifyClients(AccessibleEvents.StateChange);
               AccessibilityNotifyClients(AccessibleEvents.NameChange);
           }
        }

        protected override void OnFontChanged(EventArgs e) {
            base.OnFontChanged(e);
            if (dropDown != null) {
                dropDown.OnOwnerItemFontChanged(EventArgs.Empty);
            }
        }


         /// <include file='doc\ToolStripItem.uex' path='docs/doc[@for="ToolStripItem.OnBoundsChanged"]/*' />
        protected override void OnBoundsChanged() {
            base.OnBoundsChanged();
            //Reset the Bounds...
            if (this.dropDown != null && this.dropDown.Visible)
            {
                this.dropDown.Bounds = GetDropDownBounds(DropDownDirection);
            }
        }

        protected override void OnRightToLeftChanged(EventArgs e) {
            base.OnRightToLeftChanged(e);
            if (HasDropDownItems) {
                // only perform a layout on a visible dropdown - otherwise clear the preferred size cache.
                if (DropDown.Visible) {
                    LayoutTransaction.DoLayout(DropDown, this, PropertyNames.RightToLeft);
                }
                else {
                    CommonProperties.xClearPreferredSizeCache(DropDown);
                    DropDown.LayoutRequired = true;
                }
            }
        }


        internal override void OnImageScalingSizeChanged(EventArgs e) {            
            base.OnImageScalingSizeChanged(e);
            if (HasDropDown && DropDown.IsAutoGenerated) {
                DropDown.DoLayoutIfHandleCreated(new ToolStripItemEventArgs(this));
            }
        }
        /// <include file='doc\ToolStripDropDownItem.uex' path='docs/doc[@for="ToolStripDropDownItem.OnDropDownHide"]/*' />
        /// <devdoc>
        /// Called as a response to HideDropDown
        /// </devdoc>
        protected virtual void OnDropDownHide(EventArgs e) {
            this.Invalidate();

            EventHandler handler = (EventHandler)Events[EventDropDownHide];
            if (handler != null) handler(this, e);
        }
        /// <include file='doc\ToolStripDropDownItem.uex' path='docs/doc[@for="ToolStripDropDownItem.OnDropDownShow"]/*' />
        /// <devdoc>
        /// Last chance to stick in the DropDown before it is shown.
        /// </devdoc>
        protected virtual void OnDropDownShow(EventArgs e) {
            EventHandler handler = (EventHandler)Events[EventDropDownShow];
            if (handler != null) { 
                handler(this, e);
            }
        }
        
        /// <include file='doc\ToolStripDropDownItem.uex' path='docs/doc[@for="ToolStripDropDownItem.OnDropDownOpened"]/*' />
        /// <devdoc>
        /// called when the default item is clicked
        /// </devdoc>
        protected internal virtual void OnDropDownOpened(System.EventArgs e) {
            // only send the event if we're the thing that currently owns the DropDown.
                         
            if (DropDown.OwnerItem == this) {
                EventHandler handler = (EventHandler)Events[EventDropDownOpened];
                if (handler != null) handler(this, e);
            }
        }

        /// <include file='doc\ToolStripDropDownItem.uex' path='docs/doc[@for="ToolStripDropDownItem.OnDropDownClosed"]/*' />
        /// <devdoc>
        /// called when the default item is clicked
        /// </devdoc>
        protected internal virtual void OnDropDownClosed(System.EventArgs e) {
            // only send the event if we're the thing that currently owns the DropDown.
            this.Invalidate();  
            
            if (DropDown.OwnerItem == this)  {
                EventHandler handler = (EventHandler)Events[EventDropDownClosed];
                if (handler != null) handler(this, e);
                
                if (!DropDown.IsAutoGenerated) {
                    DropDown.OwnerItem = null;
                }
            }
            
        }

       

        /// <include file='doc\ToolStripDropDownItem.uex' path='docs/doc[@for="ToolStripDropDownItem.OnDropDownItemClicked"]/*' />
        /// <devdoc>
        /// called when the default item is clicked
        /// </devdoc>
        protected internal virtual void OnDropDownItemClicked(ToolStripItemClickedEventArgs e) {
            // only send the event if we're the thing that currently owns the DropDown.
            
            if (DropDown.OwnerItem == this) {
                ToolStripItemClickedEventHandler handler = (ToolStripItemClickedEventHandler)Events[EventDropDownItemClicked];
                if (handler != null) handler(this, e);
            }
        }
        
        protected internal override bool ProcessCmdKey(ref Message m, Keys keyData)  {
            if (HasDropDownItems) {
                return DropDown.ProcessCmdKeyInternal(ref m, keyData);
            }
            return base.ProcessCmdKey(ref m, keyData);
        }


        /// <include file='doc\ToolStripPopupItem.uex' path='docs/doc[@for="ToolStripDropDownItem.ProcessDialogKey"]/*' />
        protected internal override bool ProcessDialogKey(Keys keyData) {
            Keys keyCode = (Keys)keyData & Keys.KeyCode;

            if (HasDropDownItems) {

                // Items on the overflow should have the same kind of keyboard handling as a toplevel 
                bool isToplevel = (!IsOnDropDown || IsOnOverflow);

                
                if (isToplevel && (keyCode == Keys.Down || keyCode == Keys.Up || keyCode == Keys.Enter || (SupportsSpaceKey && keyCode == Keys.Space))) {                
                    Debug.WriteLineIf(ToolStrip.SelectionDebug.TraceVerbose, "[SelectDBG ProcessDialogKey] open submenu from toplevel item");

                    if (Enabled || DesignMode) {
                         // |__[ * File ]_____|  * is where you are.  Up or down arrow hit should expand menu
                         this.ShowDropDown();
                         KeyboardToolTipStateMachine.Instance.NotifyAboutLostFocus(this);
                         this.DropDown.SelectNextToolStripItem(null, true);
                    }// else eat the key
                    return true;
                
                } 
                else if (!isToplevel) {
                          

                    // if we're on a DropDown - then cascade out.
                    bool menusCascadeRight = (((int)DropDownDirection & 0x0001) == 0);
                    bool forward = ((keyCode == Keys.Enter) || (SupportsSpaceKey && keyCode == Keys.Space));
                    forward = (forward || (menusCascadeRight && keyCode == Keys.Left) ||  (!menusCascadeRight && keyCode == Keys.Right));

                   
                    if (forward) {
                        Debug.WriteLineIf(ToolStrip.SelectionDebug.TraceVerbose, "[SelectDBG ProcessDialogKey] open submenu from NON-toplevel item");
                                            
                        if (Enabled || DesignMode) {
                            this.ShowDropDown();
                            KeyboardToolTipStateMachine.Instance.NotifyAboutLostFocus(this);
                            this.DropDown.SelectNextToolStripItem(null, true);
                        } // else eat the key
                        return true;
                    }

                }
            }


            if (IsOnDropDown) {

                bool menusCascadeRight = (((int)DropDownDirection & 0x0001) == 0);
                bool backward = ((menusCascadeRight && keyCode == Keys.Right) ||  (!menusCascadeRight && keyCode == Keys.Left));
                
                if (backward) {
                    
                
                   Debug.WriteLineIf(ToolStrip.SelectionDebug.TraceVerbose, "[SelectDBG ProcessDialogKey] close submenu from NON-toplevel item");
                                        
                   // we're on a drop down but we're heading back up the chain. 
                   // remember to select the item that displayed this dropdown.
                   ToolStripDropDown parent = GetCurrentParentDropDown();
                   if (parent != null && !parent.IsFirstDropDown) {
                      // we're walking back up the dropdown chain.
                      parent.SetCloseReason(ToolStripDropDownCloseReason.Keyboard);
                      KeyboardToolTipStateMachine.Instance.NotifyAboutLostFocus(this);
                      parent.SelectPreviousToolStrip();
                      return true;
                   }
                   // else if (parent.IsFirstDropDown)
                   //    the base handling (ToolStripDropDown.ProcessArrowKey) will perform auto-expansion of 
                   //    the previous item in the menu.
                   
                }
            }

            Debug.WriteLineIf(ToolStrip.SelectionDebug.TraceVerbose, "[SelectDBG ProcessDialogKey] ddi calling base");
            return base.ProcessDialogKey(keyData);
     
        }
        
        private ToolStripDropDownDirection RTLTranslateDropDownDirection(ToolStripDropDownDirection dropDownDirection, RightToLeft rightToLeft) {
            switch (dropDownDirection) {
                case ToolStripDropDownDirection.AboveLeft:
                     return ToolStripDropDownDirection.AboveRight;
                 case ToolStripDropDownDirection.AboveRight:
                     return ToolStripDropDownDirection.AboveLeft;
                 case ToolStripDropDownDirection.BelowRight:
                     return ToolStripDropDownDirection.BelowLeft;
                 case ToolStripDropDownDirection.BelowLeft:
                     return ToolStripDropDownDirection.BelowRight;
                 case ToolStripDropDownDirection.Right:
                     return ToolStripDropDownDirection.Left;
                 case ToolStripDropDownDirection.Left:
                     return ToolStripDropDownDirection.Right;
              }
              Debug.Fail("Why are we here");
              
              // dont expect it to come to this but just in case here are the real defaults.
              if (IsOnDropDown) {
                   return (rightToLeft == RightToLeft.Yes) ? ToolStripDropDownDirection.Left : ToolStripDropDownDirection.Right;
              }
              else {
                  return (rightToLeft == RightToLeft.Yes) ? ToolStripDropDownDirection.BelowLeft : ToolStripDropDownDirection.BelowRight;
              }
    
    
        }
        
        /// <include file='doc\ToolStripDropDownItem.uex' path='docs/doc[@for="ToolStripDropDownItem.ShowDropDown"]/*' />
        /// <devdoc>
        /// Shows the DropDown, if one is set.
        /// </devdoc>
        public void ShowDropDown() {
            this.ShowDropDown(false);
        }

        internal void ShowDropDown(bool mousePush) {
            this.ShowDropDownInternal();
            ToolStripDropDownMenu menu = this.dropDown as ToolStripDropDownMenu;
            if (menu != null) {
                if (!mousePush) {
                    menu.ResetScrollPosition();
                }
                menu.RestoreScrollPosition();
            }
        }

        private void ShowDropDownInternal() {

            if (this.dropDown == null || (!this.dropDown.Visible)) {
                // We want to show if there's no dropdown
                // or if the dropdown is not visible.
                OnDropDownShow(EventArgs.Empty);
            }

            // the act of setting the drop down visible the first time sets the parent
            // it seems that GetVisibleCore returns true if your parent is null.

            if (this.dropDown != null && !this.dropDown.Visible) {

                if (this.dropDown.IsAutoGenerated && this.DropDownItems.Count <= 0) {
                    return;  // this is a no-op for autogenerated drop downs.
                }

                if (this.DropDown == this.ParentInternal) {
                    throw new InvalidOperationException(SR.ToolStripShowDropDownInvalidOperation);
                }

                this.dropDown.OwnerItem = this;
                this.dropDown.Location = DropDownLocation;
                this.dropDown.Show();
                this.Invalidate();

                AccessibilityNotifyClients(AccessibleEvents.StateChange);
                AccessibilityNotifyClients(AccessibleEvents.NameChange);
            }
        }

        private bool ShouldSerializeDropDown() {
            return dropDown != null && !dropDown.IsAutoGenerated;
        }

        private bool ShouldSerializeDropDownDirection() {
            return (toolStripDropDownDirection != ToolStripDropDownDirection.Default);
        }

        private bool ShouldSerializeDropDownItems() {
            return (dropDown != null && dropDown.IsAutoGenerated);
        }

        internal override void OnKeyboardToolTipHook(ToolTip toolTip) {
            base.OnKeyboardToolTipHook(toolTip);
            KeyboardToolTipStateMachine.Instance.Hook(this.DropDown, toolTip);
        }

        internal override void OnKeyboardToolTipUnhook(ToolTip toolTip) {
            base.OnKeyboardToolTipUnhook(toolTip);
            KeyboardToolTipStateMachine.Instance.Unhook(this.DropDown, toolTip);
        }
    }
        

    /// <include file='doc\ToolStripDropDownItem.uex' path='docs/doc[@for="ToolStripDropDownItemAccessibleObject"]/*' />
    [System.Runtime.InteropServices.ComVisible(true)]
    public class ToolStripDropDownItemAccessibleObject : ToolStripItem.ToolStripItemAccessibleObject {
        private ToolStripDropDownItem owner;
        /// <include file='doc\ToolStripDropDownItem.uex' path='docs/doc[@for="ToolStripDropDownItemAccessibleObject.ToolStripDropDownItemAccessibleObject"]/*' />
        public ToolStripDropDownItemAccessibleObject(ToolStripDropDownItem item) : base(item) {
            owner = item;
        }
        /// <include file='doc\ToolStripDropDownItem.uex' path='docs/doc[@for="ToolStripDropDownItemAccessibleObject.Role"]/*' />
        public override AccessibleRole Role {
            get {
                AccessibleRole role = Owner.AccessibleRole;
                if (role != AccessibleRole.Default) {
                    return role;
                }
                return AccessibleRole.MenuItem;
            }
        }

        /// <include file='doc\ToolStripItem.uex' path='docs/doc[@for="ToolStripItemAccessibleObject.DoDefaultAction"]/*' />
        public override void DoDefaultAction() {
            ToolStripDropDownItem item = Owner as ToolStripDropDownItem;
            if (item != null && item.HasDropDownItems) {
                item.ShowDropDown();
            }
            else {
                base.DoDefaultAction();
            }

        }

        internal override bool IsIAccessibleExSupported() {
            if (owner != null) {
                return true;
            }
            else {
                return base.IsIAccessibleExSupported();
            }
        }

        internal override bool IsPatternSupported(int patternId) {
            if (patternId == NativeMethods.UIA_ExpandCollapsePatternId && owner.HasDropDownItems) {
                return true;
            }
            else {
                return base.IsPatternSupported(patternId);
            }
        }

        internal override object GetPropertyValue(int propertyID) {
            if (propertyID == NativeMethods.UIA_IsOffscreenPropertyId && owner != null && owner.Owner is ToolStripDropDown) {
                return !((ToolStripDropDown)owner.Owner).Visible;
            }

            return base.GetPropertyValue(propertyID);
        }

        internal override void Expand() {
            DoDefaultAction();            
        }

        internal override void Collapse() {
            if (owner != null && owner.DropDown != null && owner.DropDown.Visible) {
                owner.DropDown.Close();
            }            
        }

        internal override UnsafeNativeMethods.ExpandCollapseState ExpandCollapseState {
            get {
                return owner.DropDown.Visible ? UnsafeNativeMethods.ExpandCollapseState.Expanded : UnsafeNativeMethods.ExpandCollapseState.Collapsed;                
            }
        }

        public override AccessibleObject GetChild(int index) {
            if ((owner == null) || !owner.HasDropDownItems) {
                return null;
            }
            return owner.DropDown.AccessibilityObject.GetChild(index);
       
        }
        public override int GetChildCount() {
            if ((owner == null) || !owner.HasDropDownItems) {
                return -1;
            }

            // Do not expose child items when the submenu is collapsed to prevent Narrator from announcing
            // invisible menu items when Narrator is in item's mode (CAPSLOCK + Arrow Left/Right) or
            // in scan mode (CAPSLOCK + Space)
            if (ExpandCollapseState == UnsafeNativeMethods.ExpandCollapseState.Collapsed) {
                return 0;
            }

            if (owner.DropDown.LayoutRequired) {
                LayoutTransaction.DoLayout(owner.DropDown, owner.DropDown, PropertyNames.Items);
            }
            return owner.DropDown.AccessibilityObject.GetChildCount();

        }

        internal int GetChildFragmentIndex(ToolStripItem.ToolStripItemAccessibleObject child) {
            if ((owner == null) || (owner.DropDownItems == null)) {
                return -1;
            }

            for (int i = 0; i < owner.DropDownItems.Count; i++) {
                if (owner.DropDownItems[i].Available && child.Owner == owner.DropDownItems[i]) {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Gets the number of children belonging to an accessible object.
        /// </summary>
        /// <returns>The number of children.</returns>
        internal int GetChildFragmentCount() {
            if ((owner == null) || (owner.DropDownItems == null)) {
                return -1;
            }

            int count = 0;
            for (int i = 0; i < owner.DropDownItems.Count; i++) {
                if (owner.DropDownItems[i].Available) {
                    count++;
                }
            }

            return count;
        }

        internal AccessibleObject GetChildFragment(int index) {
            var toolStripAccessibleObject = owner.DropDown.AccessibilityObject as ToolStrip.ToolStripAccessibleObject;
            if (toolStripAccessibleObject != null) {
                return toolStripAccessibleObject.GetChildFragment(index);
            }

            return null;
        }

        internal override UnsafeNativeMethods.IRawElementProviderFragment FragmentNavigate(UnsafeNativeMethods.NavigateDirection direction) {
            if (owner == null || owner.DropDown == null) {
                return null;
            }

            switch (direction) {
                case UnsafeNativeMethods.NavigateDirection.FirstChild:
                    int childCount = GetChildCount();
                    if (childCount > 0) {
                        return GetChildFragment(0);
                    }

                    return null;
                case UnsafeNativeMethods.NavigateDirection.LastChild:
                    childCount = GetChildCount();
                    if (childCount > 0) {
                        return GetChildFragment(childCount - 1);
                    }

                    return null;
                case UnsafeNativeMethods.NavigateDirection.NextSibling:
                case UnsafeNativeMethods.NavigateDirection.PreviousSibling:
                    ToolStripDropDown dropDown = owner.Owner as ToolStripDropDown;

                    if (dropDown == null) {
                        break;
                    }
                    int index = dropDown.Items.IndexOf(owner);

                    if (index == -1) {
                        Debug.Fail("No item matched the index?");
                        return null;
                    }

                    index += direction == UnsafeNativeMethods.NavigateDirection.NextSibling ? 1 : -1;

                    if (index >= 0 && index < dropDown.Items.Count) {
                        var item = dropDown.Items[index];
                        var controlHostItem = item as ToolStripControlHost;
                        if (controlHostItem != null) {
                            return controlHostItem.ControlAccessibilityObject;
                        }

                        return item.AccessibilityObject;
                    }

                    return null;
            }

            return base.FragmentNavigate(direction);
        }
    }
}
