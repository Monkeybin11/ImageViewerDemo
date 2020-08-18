﻿using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageViewer.ImageViewerControl.Extensions;
using ImageViewer.ImageViewerControl.RoiControls;

namespace ImageViewer.ImageViewerControl
{
    public class ImageViewer : ContentControl
    {
        /// <summary>
        /// 是否修改ROI 
        /// </summary>
        public static readonly DependencyProperty IsModifyRoiProperty =
            DependencyProperty.Register(nameof(IsModifyRoi),
                typeof(bool),
                typeof(ImageViewer),
                new PropertyMetadata(default(bool)));

        /// <summary>
        /// 是否移动与缩放ROI
        /// </summary>
        public static readonly DependencyProperty IsMoveAndScaleProperty =
            DependencyProperty.Register(nameof(IsMoveAndScale),
                typeof(bool),
                typeof(ImageViewer),
                new PropertyMetadata(default(bool)));

        /// <summary>
        /// 是否画矩形ROI
        /// </summary>
        public static readonly DependencyProperty IsRectangleProperty =
            DependencyProperty.Register(nameof(IsRectangle),
                typeof(bool),
                typeof(ImageViewer),
                new PropertyMetadata(default(bool)));

        /// <summary>
        /// 是否画旋转矩形ROI
        /// </summary>
        public static readonly DependencyProperty IsRotateRectangleProperty =
            DependencyProperty.Register(nameof(IsRotateRectangle),
                typeof(bool),
                typeof(ImageViewer),
                new PropertyMetadata(default(bool)));

        /// <summary>
        /// 最大缩放倍数
        /// </summary>
        public static readonly DependencyProperty MaxScaleProperty =
            DependencyProperty.Register(nameof(MaxScale),
                typeof(double),
                typeof(ImageViewer),
                new PropertyMetadata((double) 10));

        /// <summary>
        /// 最小缩放倍数
        /// </summary>
        public static readonly DependencyProperty MinScaleProperty =
            DependencyProperty.Register(nameof(MinScale),
                typeof(double),
                typeof(ImageViewer),
                new PropertyMetadata(0.00000000001));

        /// <summary>
        ///     缩放系数,一次缩放多少倍
        /// </summary>
        public static readonly DependencyProperty ScaleFactorProperty =
            DependencyProperty.Register(nameof(ScaleFactor),
                typeof(double),
                typeof(ImageViewer),
                new PropertyMetadata(0.05));
        /// <summary>
        /// 缩放倍数
        /// </summary>
        internal static readonly DependencyProperty ScaleProperty =
            DependencyProperty.Register(nameof(Scale),
                typeof(double),
                typeof(ImageViewer),
                new PropertyMetadata((double) 1));
        /// <summary>
        /// 图片是否已经加载
        /// </summary>
        private static readonly DependencyProperty IsImageLoadedProperty =
            DependencyProperty.Register(nameof(IsImageLoaded),
                typeof(bool),
                typeof(ImageViewer),
                new PropertyMetadata(default(bool)));

        internal Image Image;
        internal InCanvas InCanvas;
        internal OutCanvas OutCanvas;

        public bool IsImageLoaded
        {
            get => (bool) GetValue(IsImageLoadedProperty);
            private set => SetValue(IsImageLoadedProperty, value);
        }

        public bool IsModifyRoi
        {
            get => (bool) GetValue(IsModifyRoiProperty);
            set => SetValue(IsModifyRoiProperty, value);
        }

        public bool IsMoveAndScale
        {
            get => (bool) GetValue(IsMoveAndScaleProperty);
            set => SetValue(IsMoveAndScaleProperty, value);
        }

        public bool IsRectangle
        {
            get => (bool) GetValue(IsRectangleProperty);
            set => SetValue(IsRectangleProperty, value);
        }

        public bool IsRotateRectangle
        {
            get => (bool) GetValue(IsRotateRectangleProperty);
            set => SetValue(IsRotateRectangleProperty, value);
        }

        public double MaxScale
        {
            get => (double) GetValue(MaxScaleProperty);
            set => SetValue(MaxScaleProperty, value);
        }

        public double MinScale
        {
            get => (double) GetValue(MinScaleProperty);
            set => SetValue(MinScaleProperty, value);
        }

        public double Scale
        {
            get => (double) GetValue(ScaleProperty);
            internal set => SetValue(ScaleProperty, value);
        }

        /// <summary>
        ///     缩放系数
        /// </summary>
        public double ScaleFactor
        {
            get => (double) GetValue(ScaleFactorProperty);
            set => SetValue(ScaleFactorProperty, value);
        }

        public void AddRoi(RoiControl roi)
        {
            roi.ImageViewer = this;
            InCanvas.Children.Add(roi);
        }

        public void ClearRoi()
        {
            foreach (RoiControl roiShape in GetRoi())
                InCanvas.Children.Remove(roiShape);
        }

        /// <summary>
        ///     获取控件内部的两个Canvas的RenderSize
        /// </summary>
        /// <returns></returns>
        public (Size outCanvasRenderSize, Size inCanvasRenderSize)
            GetCanvasesRenderSize()
        {
            Size outCanvasRenderSize = OutCanvas.RenderSize;
            Size inCanvasRenderSize = InCanvas.RenderSize;
            return (outCanvasRenderSize, inCanvasRenderSize);
        }

        public ImmutableArray<RoiControl> GetRoi()
        {
            return InCanvas.Children.OfType<RoiControl>().ToImmutableArray();
        }

        public (double scale, double scaleFactor, double minScale, double maxScale)
            GetScaleInfo()
        {
            return (Scale, ScaleFactor, MinScale, MaxScale);
        }

        /// <summary>
        ///     在控件中显示图片
        /// </summary>
        /// <param name="filePath"> 图片路径 </param>
        public void LoadImage(string filePath)
        {
            var bitmapImage = new BitmapImage(new Uri(filePath));
            SetInsideControlsInfo_When_LoadImage(bitmapImage);
            IsImageLoaded = true;
            ClearRoi();
        }

        public void RemoveLastRoi()
        {
            ImmutableArray<RoiControl> _roiShapes = GetRoi();
            if (_roiShapes.Length != 0)
            {
                InCanvas.Children.Remove(_roiShapes.Last());
            }
        }

        /// <summary>
        ///     override OnInitialized
        /// </summary>
        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            Initialization_Layout_ChildrenControl();
        }

        /// <summary>
        /// 初始化并布局子空间
        /// <remarks>
        ///     初始化此控件。 此控件的结构为： Gird分为两行，第一行存放着_outCanvas，第二行存放着该控件的一些功能选择按钮，例如是否缩放、移动图片等等。
        ///     _outCanvas包含着_inCanvas。
        ///     _inCanvas包含着_image。
        /// </remarks>
        /// </summary>
        private void Initialization_Layout_ChildrenControl()
        {
            var grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(),
                    new RowDefinition {Height = GridLength.Auto}
                }
            };
            Content = grid;
            InCanvas = new InCanvas(this) {Background = Brushes.Green};
            OutCanvas = new OutCanvas(this)
            {
                Background = Brushes.Black, ClipToBounds = true
            };
            grid.Children.Add(OutCanvas);
            OutCanvas.Children.Add(InCanvas);
            ControlPanel controlPanel = new ControlPanel(this);
            grid.Children.Add(controlPanel);
            Grid.SetRow(controlPanel, 1);
        }

        /// <summary>
        ///     当加载图片时设置内部控件的属性。
        /// </summary>
        /// <remarks>
        ///     _image的长宽与其Canvas.Left、Canvas.Top。
        ///     _inCanvas的长宽与其Canvas.Left、Canvas.Top。
        /// </remarks>
        /// <param name="bitmapImage"> </param>
        [SuppressMessage("Readability",
            "RCS1123:Add parentheses when necessary.",
            Justification = "<挂起>")]
        private void SetInsideControlsInfo_When_LoadImage(BitmapImage bitmapImage)
        {
            Scale = 1;
            if (Image == null)
            {
                Image = new Image {Source = bitmapImage};
                InCanvas.Children.Add(Image);
            }
            else
                Image.Source = bitmapImage;

            (double width, double height) = bitmapImage.GetWH();
            Image.SetWH(width, height);
            InCanvas.RenderTransform = new ScaleTransform(1, 1);
            (double inW, double inH) = (width * 100, height * 100);
            InCanvas.SetWH(inW, inH);
            (double x, double y) = (inW / 2 - width / 2, inH / 2 - height / 2);
            Image.SetCanvasXY(x, y);
            InCanvas.SetCanvasXY(-x, -y);
        }
    }
}