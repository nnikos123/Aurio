﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace AudioAlign.WaveControls {
    [TemplatePart(Name="PART_ResizeThumb", Type=typeof(Thumb))]
    public class ResizeDecorator : ContentControl {

        static ResizeDecorator() {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ResizeDecorator), new FrameworkPropertyMetadata(typeof(ResizeDecorator)));
        }

        public event DragDeltaEventHandler ResizeDelta;

        public ResizeDecorator() {
            this.Loaded += new RoutedEventHandler(ResizeDecorator_Loaded);
        }

        private void ResizeDecorator_Loaded(object sender, RoutedEventArgs e) {
            Thumb resizeThumb = GetTemplateChild("PART_ResizeThumb") as Thumb;
            if (resizeThumb != null) {
                resizeThumb.DragDelta += new DragDeltaEventHandler(thumb_DragDelta);
            }
        }

        private void thumb_DragDelta(object sender, DragDeltaEventArgs e) {
            Debug.WriteLine("ResizeDecorator ResizeThumb DragDelta: " + e.VerticalChange);

            double newHeight = Math.Max(MinHeight, ActualHeight + e.VerticalChange);
            SetValue(HeightProperty, newHeight);
            e.Handled = true;
        }

        private void OnResizeDelta(DragDeltaEventArgs e) {
            if (ResizeDelta != null) {
                ResizeDelta(this, e);
            }
        }

    }
}