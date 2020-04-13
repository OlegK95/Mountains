using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.IO;

namespace wpf_scrollviewer
{
	/// <summary>
	/// Interaction logic for ScrollViewer.xaml
	/// </summary>
	public partial class ScrollViewer : UserControl
	{
		public System.Windows.FrameworkElement Content
		{
			get => (System.Windows.FrameworkElement)GridContent.Children[0];
			set 
			{
				GridContent.Children.Clear();
				GridContent.Children.Add(value);
				Rect view = new Rect(0, 0, value.ActualWidth, value.ActualHeight);
				ViewArea = view;
				value.SizeChanged += Value_SizeChanged;

				Scale = -1;
			}
		}

		private void Value_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			//ZoomInFull();
		}

		System.Windows.Point? lastCenterPositionOnTarget;
		System.Windows.Point? lastMousePositionOnTarget;
		System.Windows.Point? lastDragPoint;

		public ScrollViewer()
		{
			InitializeComponent();

			//Левая кнопка нажата
			GridContent.MouseLeftButtonDown += OnMouseLeftButtonDown;

			//Мышь переместилась
			GridContent.MouseMove += OnMouseMove;

			//   Полоса прокрутки
			//Перемещение полосы прокрутки полосе прокрутки
			scrollViewer.ScrollChanged += OnScrollViewerScrollChanged;
			scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;


			//Кнопка нажата и переместилась по полосе прокрутки
			scrollViewer.MouseMove += ScrollViewer_MouseMove;

			//Кнопка отпущенна на полосе прокрутки
			scrollViewer.PreviewMouseLeftButtonUp += OnMouseLeftButtonUp;			


			slider.ValueChanged += OnSliderValueChanged;

			
			ZoomInFull();
			scrollViewerBitmap = new ScrollViewerBitmap();

			Scale = -1;
		}

		//		Bitmap bitmap;
		ScrollViewerBitmap scrollViewerBitmap;
		BitmapImage bitmapimage;

		//creating
		System.Windows.Point creatingStart;

		private void ScrollViewer_MouseMove(object sender, MouseEventArgs e)
		{
			if (lastDragPoint.HasValue)
			{
				if (moveMode == MoveModes.MoveAll)
				{
					System.Windows.Point posNow = e.GetPosition(scrollViewer);

					double dX = posNow.X - lastDragPoint.Value.X;
					double dY = posNow.Y - lastDragPoint.Value.Y;

					lastDragPoint = posNow;

					Rect rect = ViewArea;

					rect.X -= dX / Scale;
					rect.Y -= dY / Scale;

					ViewArea = rect;

					System.Windows.Point pos = e.GetPosition(GridContent);
				}
				else if (moveMode == MoveModes.Creating)
				{
					System.Windows.Point posNow = e.GetPosition(GridContent);

					double x = Math.Min(creatingStart.X, posNow.X);
					double y = Math.Min(creatingStart.Y, posNow.Y);
					double width = Math.Abs(creatingStart.X - posNow.X);
					double height = Math.Abs(creatingStart.Y - posNow.Y);
				}
			}
		}

		

		Rect viewArea = new Rect();

		public double Scale
		{
			get => scaleTransform.ScaleX;
			set
			{
				scaleTransform.ScaleX = value;
				scaleTransform.ScaleY = value;
			}
		}

		public Rect ViewArea
		{
			set
			{
				double windowWidth = scrollViewer.ViewportWidth;
				double windowHeight = scrollViewer.ViewportHeight;
				double windowRate = windowWidth / windowHeight;

				if (windowWidth == 0)
				{
					windowWidth = scrollViewer.ActualWidth;
					windowHeight = scrollViewer.ActualHeight;
				}

				double a = GridContent.Width;
				double b = GridContent.Height;

				//double contentWidth = scrollViewer.ExtentWidth;
				//double contentHeight = scrollViewer.ExtentHeight; 
				double contentWidth = grid.ActualWidth;
				double contentHeight = grid.ActualHeight;
				double contentRate = contentWidth / contentHeight;

				//oriented in content.
				Rect rect = value;

				if (rect.Width == 0 || contentWidth == 0 || windowWidth == 0)
				{
					viewArea = rect;
					//Application.Current.MainWindow.Title = Scale.ToString() +"  " +viewArea.ToString();

					return;
				}

				//--decide scale
				//allowed by scrollViewer
				double minScale = Math.Min(windowWidth / contentWidth, windowHeight / contentHeight);
				

				double scaleX = Math.Max(windowWidth / rect.Width, minScale);
				double scaleY = Math.Max(windowHeight / rect.Height, minScale);

				double scale;
				//(x or y) axis should be extended.
				if (scaleX > scaleY)
				{
					scale = scaleY;
					double oldWidth = rect.Width;
					rect.Width = windowWidth / scale;
					rect.X -= (rect.Width - oldWidth) / 2;//extend from center
				}
				else
				{
					scale = scaleX;
					double oldHeight = rect.Height;
					rect.Height = windowHeight / scale;
					rect.Y -= (rect.Height - oldHeight) / 2;
				}

				Scale = scale;

				//double extendedWidth = contentWidth * scale;
				//double extendedHeight = contentHeight * scale;

				scrollViewer.ScrollToHorizontalOffset(rect.X * scale);
				scrollViewer.ScrollToVerticalOffset(rect.Y * scale);

				//viewArea = rect;
			}

			get
			{
				return viewArea;
			}
		}

		void ZoomInFull()
		{
			ViewArea = new Rect(0, 0, GridContent.ActualWidth, GridContent.ActualHeight);
		}

		void OnMouseMove(object sender, MouseEventArgs e)
		{
			if (lastDragPoint.HasValue)
			{
				System.Windows.Point posNow = e.GetPosition(scrollViewer);

				double dX = posNow.X - lastDragPoint.Value.X;
				double dY = posNow.Y - lastDragPoint.Value.Y;

				lastDragPoint = posNow;

				Rect rect = ViewArea;

				rect.X -= dX / Scale;
				rect.Y -= dY / Scale;

				ViewArea = rect;

				System.Windows.Point pos = e.GetPosition(GridContent);
			}
			else
			{
				MoveMode = MoveModes.MoveAll;
			}
		}

		void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			var mousePos = e.GetPosition(scrollViewer);
			if (mousePos.X <= scrollViewer.ViewportWidth && mousePos.Y <
				scrollViewer.ViewportHeight) //make sure we still can use the scrollbars
			{
				lastDragPoint = mousePos;
				Mouse.Capture(scrollViewer);
			}
		}

		void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			double scale = 1;
			if (e.Delta > 0)
			{
				scale /= 1.01;
			}
			if (e.Delta < 0)
			{
				scale *= 1.01;
			}

			lastMousePositionOnTarget = Mouse.GetPosition(grid);

			System.Windows.Point pos = e.GetPosition(GridContent);

			Rect view = ViewArea;

			double nuWidth = view.Width * scale;
			double nuHeight = view.Height * scale;

			// leftSide / total width
			double rateX = (pos.X - view.X) / view.Width;
			view.X -= (nuWidth - view.Width) * rateX;

			//topSide / total height
			double rateY = (pos.Y - view.Y) / view.Height;
			view.Y -= (nuHeight - view.Height) * rateY;

			view.Width = nuWidth;
			view.Height = nuHeight;

			ViewArea = view;
		}

		void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
		{
			scrollViewer.ReleaseMouseCapture();
			lastDragPoint = null;
		}

		void OnSliderValueChanged(object sender,
			 RoutedPropertyChangedEventArgs<double> e)
		{
			Scale = e.NewValue;

			var centerOfViewport = new System.Windows.Point(scrollViewer.ViewportWidth / 2,
											 scrollViewer.ViewportHeight / 2);
			lastCenterPositionOnTarget = scrollViewer.TranslatePoint(centerOfViewport, grid);


		}

		void OnScrollViewerScrollChanged(object sender, ScrollChangedEventArgs e)
		{
			double scale = Scale;
			if (scale < 0)
			{
				double dX = scrollViewer.ActualHeight;
				double dY = scrollViewer.ActualWidth;
				double contentFerstWidth = GridContent.ActualWidth;
				double contentFerstHeight = GridContent.ActualHeight;

				bitmapimage  = (BitmapImage)((System.Windows.Controls.Image)GridContent.Children[0]).Source;
				//bitmapimage = (BitmapImage)((System.Windows.Controls.Image)GridContent.Children[0]).Source;

				scrollViewerBitmap.InitializeBitmap(bitmapimage);


				var imHeigh = bitmapimage.Height;
				var imWidh  = bitmapimage.Width;

				var imPixelHeight = bitmapimage.PixelHeight;
				var imPixelWidth  = bitmapimage.PixelWidth;

				var imActualHeight = scrollViewer.ActualHeight;
				var imActualWidth  = scrollViewer.ActualWidth;

				var imViewportHeight = scrollViewer.ViewportHeight;
				var imViewportWidth  = scrollViewer.ViewportWidth;

				var imExtentHeight = scrollViewer.ExtentHeight;
				var imExtentWidth  = scrollViewer.ExtentWidth;

				Scale = Math.Min(imViewportHeight / imHeigh, imViewportWidth / imWidh);

				//Scale = 0.5;

				//Scale = Math.Min(scrollViewer.ActualHeight / scrollViewer.ExtentHeight, scrollViewer.ActualWidth / scrollViewer.ExtentWidth);

				//Scale = Math.Min(imViewportHeight / imPixelHeight, imViewportWidth / imPixelWidth);

				//Scale = Math.Min(imViewportHeight / imExtentHeight, imViewportWidth / imExtentWidth);

				//slider.Minimum = Scale - 0.01;
				if (Scale > 1) Scale = 1;
				slider.Minimum = Scale;
				slider.Maximum = 1;

				var imVerticalScrollBarWidth = SystemParameters.VerticalScrollBarWidth;
				scale = Scale;

			}


			if (scale != 0)
			{
				viewArea.X = scrollViewer.HorizontalOffset / scale;
				viewArea.Y = scrollViewer.VerticalOffset / scale;
				viewArea.Width = scrollViewer.ViewportWidth / scale;
				viewArea.Height = scrollViewer.ViewportHeight / scale;

				double contentWidth = GridContent.ActualWidth;
				double contentHeight = GridContent.ActualHeight;

				if (viewArea.Width > contentWidth)
				{
					viewArea.X -= (viewArea.Width - contentWidth) / 2;
				}

				if (viewArea.Height > contentHeight)
				{
					viewArea.Y -= (viewArea.Height - contentHeight) / 2;
				}
			}

			Application.Current.MainWindow.Title = Scale.ToString() + "  " + viewArea.ToString();
		}

		//---------------------------------------------------------------------------------------
		public enum MoveModes : int
		{
			LeftTop = 0,
			Top = 1,
			RightTop = 2,
			Left = 3,
			Right = 4,
			LeftBottom = 5,
			Bottom = 6,
			RightBottom = 7,
			MoveSelected = 8,

			MoveAll,
			None,
			Creating
		}

		MoveModes moveMode;
		public MoveModes MoveMode
		{
			set
			{
				if (lastDragPoint.HasValue)
				{
					return;
				}

				Console.WriteLine(value.ToString());
				if (value == MoveModes.LeftTop)
				{
					Cursor = Cursors.SizeNWSE;
				}
				else if (value == MoveModes.Top)
				{
					Cursor = Cursors.SizeNS;
				}
				else if (value == MoveModes.RightTop)
				{
					Cursor = Cursors.SizeNESW;
				}
				else if (value == MoveModes.Left)
				{
					Cursor = Cursors.SizeWE;
				}
				else if (value == MoveModes.Right)
				{
					Cursor = Cursors.SizeWE;
				}
				else if (value == MoveModes.LeftBottom)
				{
					Cursor = Cursors.SizeNESW;
				}
				else if (value == MoveModes.Bottom)
				{
					Cursor = Cursors.SizeNS;
				}
				else if (value == MoveModes.RightBottom)
				{
					Cursor = Cursors.SizeNWSE;
				}
				else if (value == MoveModes.MoveSelected)
				{
					Cursor = Cursors.SizeAll;
				}
				else 
				{
					Cursor = Cursors.Arrow;
				}
				moveMode = value;
			}

			get
			{
				return moveMode;
			}
		}

		private void GridContent_MouseMove(object sender, MouseEventArgs e)
		{
			if (e.RightButton == MouseButtonState.Pressed)
			{

				return;
			}

			System.Windows.Point position = Mouse.GetPosition(GridContent);
			
			//BitmapImage vars = (BitmapImage)((System.Windows.Controls.Image)GridContent.Children[0]).Source;

			var imHeight = bitmapimage.PixelHeight;
			var imWidth = bitmapimage.PixelWidth;

			Int32 x = (Int32)(imWidth * position.X / GridContent.ActualWidth);
			Int32 y = (Int32)(imHeight * position.Y / GridContent.ActualHeight);

			
			var a = this.Content;

			//Color pixelColor = bitmapimage.GetPixel(50, 50);

			//	Int32Rect sourceRect = new Int32Rect((Int32)x, (Int32) y, 1, 1);

			//Array buffer = new Array (100);
			//	Int32[] buffer = new Int32 [10];
			//	bitmapimage.CopyPixels(sourceRect, buffer, buffer.Length, 0);

//			scrollViewerBitmap.GetHeight((Int32)x, (Int32)y);
			Application.Current.MainWindow.Title = "(X: " + x + " Y: " + y + ")"
				                              //     + "R: " + pixel.R + " G: " + pixel.G + " B: " + pixel.B +" A: "+ pixel.A;
			                                     +" Height " + scrollViewerBitmap.GetHeight((Int32)x, (Int32)y);
		}

		private static Bitmap BitmapImage2Bitmap(BitmapSource bitmapImage)
		{
			using (var outStream = new MemoryStream())
			{
				BitmapEncoder enc = new BmpBitmapEncoder();
				enc.Frames.Add(BitmapFrame.Create(bitmapImage));
				enc.Save(outStream);
				var bitmap = new Bitmap(outStream);
				return bitmap;
			}
		}


	}
}
