using SharpDX.DirectInput;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using Mouse = SharpDX.DirectInput.Mouse;

namespace up_game_pad
{
    /// <summary>
    /// Logika interakcji dla klasy MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
	{
		// Initialize DirectInput
		DirectInput directInput = new DirectInput();

		Boolean keepWorking = false;
		Joystick joystick;
		Mouse mouse;

		double oldPointX = 0;
		double oldPointY = 0;
		double pointX = 0;
		double pointY = 0;


		double pointXforPosition = 0;
		double pointYforPosition = 0;


		bool isDrawing = false;
		bool startDrawingOnceAgain = true;
		bool useAsAMouseBool;

		Brush currentBrush = Brushes.Black;


		public MainWindow()
		{
			InitializeComponent();
			//startMouse();
			initializeJoystick();
		}


		[DllImport("User32.dll")]
		private static extern bool SetCursorPos(int X, int Y);

		[DllImport("user32.dll")]
		static extern void mouse_event(int dwFlags, int dx, int dy,
					  int dwData, int dwExtraInfo);

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			SetCursor(200, 200);
		}

		private static void SetCursor(int x, int y)
		{
			// Left boundary
			var xL = (int)App.Current.MainWindow.Left;
			// Top boundary
			var yT = (int)App.Current.MainWindow.Top;

			SetCursorPos(x + xL, y + yT);
		}

		[Flags]
		public enum MouseEventFlags
		{
			LEFTDOWN = 0x00000002,
			LEFTUP = 0x00000004,
			MIDDLEDOWN = 0x00000020,
			MIDDLEUP = 0x00000040,
			MOVE = 0x00000001,
			ABSOLUTE = 0x00008000,
			RIGHTDOWN = 0x00000008,
			RIGHTUP = 0x00000010
		}

		public static void LeftClick(int x, int y)
		{
			mouse_event((int)(MouseEventFlags.LEFTDOWN), x, y, 0, 0);
			mouse_event((int)(MouseEventFlags.LEFTUP), x, y, 0, 0);
		}

		public static void RightClick(int x, int y)
		{
			mouse_event((int)(MouseEventFlags.RIGHTDOWN), x, y, 0, 0);
			mouse_event((int)(MouseEventFlags.RIGHTUP), x, y, 0, 0);
		}



		public void initializeJoystick()
		{
			// Find a Joystick Guid
			var joystickGuid = Guid.Empty;

			foreach (var deviceInstance in directInput.GetDevices(DeviceType.Gamepad,
						DeviceEnumerationFlags.AllDevices))
				joystickGuid = deviceInstance.InstanceGuid;

			// If Gamepad not found, look for a Joystick
			if (joystickGuid == Guid.Empty)
				foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick,
						DeviceEnumerationFlags.AllDevices))
					joystickGuid = deviceInstance.InstanceGuid;

			// If Joystick not found, throws an error
			if (joystickGuid == Guid.Empty)
			{
				Console.WriteLine("No joystick/Gamepad found.");
				Console.ReadKey();
				Environment.Exit(1);
			}

			// Instantiate the joystick
			joystick = new Joystick(directInput, joystickGuid);

			Console.WriteLine("Found Joystick/Gamepad with GUID: {0}", joystickGuid);

			// Query all suported ForceFeedback effects
			var allEffects = joystick.GetEffects();
			foreach (var effectInfo in allEffects)
				Console.WriteLine("Effect available {0}", effectInfo.Name);

			// Set BufferSize in order to use buffered data.
			joystick.Properties.BufferSize = 128;

			// Acquire the joystick
			joystick.Acquire();

		}

		public void joyStick()
		{

			var stateX = new JoystickUpdate();
			var stateY = new JoystickUpdate();
			// Poll events from joystick
			while (keepWorking)
			{
				joystick.Poll();
				var datas = joystick.GetBufferedData();
				foreach (var state in datas)
				{
					Console.WriteLine(state);
					myGrid.Dispatcher.Invoke(() => showUpdate(state), DispatcherPriority.Normal);

					if (state.Offset == JoystickOffset.X || state.Offset == JoystickOffset.RotationX)
					{
						stateX = state;
						//continue;
					}

					if (state.Offset == JoystickOffset.Y || state.Offset == JoystickOffset.RotationY)
					{
						stateY = state;
						//continue;
					}


					// Ustawienie czy rysować czy nie
					if (state.Offset == JoystickOffset.PointOfViewControllers0 && state.Value != -1)
					{
						if (isDrawing == false)
						{
							startDrawingOnceAgain = true;
							isDrawing = true;
						}
					}
					else if (state.Offset == JoystickOffset.PointOfViewControllers0 && state.Value == -1)
					{
						isDrawing = false;
						Console.WriteLine(isDrawing);
					}

					// Jeśli rusza się joystick lewy
					if (state.Offset == JoystickOffset.Y || state.Offset == JoystickOffset.X)
					{
						setCoordinates(stateX, stateY);
						canvaL.Dispatcher.Invoke(() => drawCanvaJoystick(state, canvaL), DispatcherPriority.Normal);
					}
					// jeśli rusza się prawy
					else if (state.Offset == JoystickOffset.RotationX || state.Offset == JoystickOffset.RotationY)
					{
						setCoordinates(stateX, stateY);
						canvaR.Dispatcher.Invoke(() => drawCanvaJoystick(state, canvaR), DispatcherPriority.Normal);
					}


					// Czyszczenie płótna
					if (state.Offset == JoystickOffset.Buttons4)
					{
						canva.Dispatcher.Invoke(() => clearCanva(), DispatcherPriority.Normal);
						Console.WriteLine("Clean canva");
					}

					// Zmiana pędzla
					if (state.Offset == JoystickOffset.Buttons0)
					{
						currentBrush = Brushes.Red;
					}
					if (state.Offset == JoystickOffset.Buttons3)
					{
						currentBrush = Brushes.Blue;
					}
					if (state.Offset == JoystickOffset.Z)
					{
						currentBrush = Brushes.Black;
					}

					// Przerwij rysowanie
					if (state.Offset == JoystickOffset.Buttons5)
					{
						keepWorking = false;
					}



					// Rysuj jeśli można
					if (isDrawing == true)
					{
						canva.Dispatcher.Invoke(() => drawLine(state), DispatcherPriority.Normal);
					}
				}
			}
		}


		public void setCoordinates(JoystickUpdate stateX, JoystickUpdate stateY)
		{
			pointX = stateX.Value / 140.0;
			pointY = stateY.Value / 140.0;

			pointXforPosition = stateX.Value / 282.0;
			pointYforPosition = stateY.Value / 282.0;
		}


		public void drawCanvaJoystick(JoystickUpdate state, Canvas canvaJoystick)
		{
			canvaJoystick.Children.Clear();
			canvaJoystick.UpdateLayout();


			Line myLine = new Line();
			myLine.Stroke = Brushes.Red;


			//if (state.Offset == JoystickOffset.X || state.Offset == JoystickOffset.RotationX)
			//{
			//	pointXforPosition = state.Value / 282.0;
			//}
			//else if (state.Offset == JoystickOffset.Y || state.Offset == JoystickOffset.RotationY)
			//{
			//	pointYforPosition = state.Value / 282.0;
			//}

			myLine.X1 = pointXforPosition + 4;
			myLine.Y1 = pointYforPosition + 4;
			myLine.X2 = pointXforPosition;
			myLine.Y2 = pointYforPosition;

			myLine.StrokeThickness = 1;

			canvaJoystick.Children.Add(myLine);

			Console.WriteLine("Rysuje mała kanva " + myLine.X1 + " " + myLine.Y1);
			Console.WriteLine("#############################################################################");
			canvaJoystick.Dispatcher.Invoke(() => canvaJoystick.UpdateLayout(), DispatcherPriority.Background);
		}


		private void clearCanva()
		{
			canva.Children.Clear();
			canva.UpdateLayout();
			// canva.Dispatcher.Invoke(() => canva.UpdateLayout(), DispatcherPriority.Normal);
		}


		private void showUpdate(JoystickUpdate state)
		{
			elementTB.Dispatcher.Invoke(() => elementTB.Text = state.Offset.ToString(), DispatcherPriority.Background);
			valueTB.Dispatcher.Invoke(() => valueTB.Text = state.Value.ToString(), DispatcherPriority.Background);
		}


		private void drawLine(JoystickUpdate state)
		{
			Line myLine = new Line();
			myLine.Stroke = currentBrush;


			//         if (state.Offset == JoystickOffset.X || state.Offset == JoystickOffset.RotationX)
			//         {
			//             pointX = state.Value / 150.0;
			//         }
			//         else if (state.Offset == JoystickOffset.Y || state.Offset == JoystickOffset.RotationY)
			//{
			//             pointY = state.Value / 150.0;
			//         }


			if (startDrawingOnceAgain == true)
			{
				oldPointX = pointX - 1;
				oldPointY = pointY - 1;
				startDrawingOnceAgain = false;
			}

			myLine.X1 = oldPointX;
			myLine.Y1 = oldPointY;
			myLine.X2 = pointX;
			myLine.Y2 = pointY;

			oldPointX = pointX;
			oldPointY = pointY;

			myLine.StrokeThickness = 2;

			canva.Children.Add(myLine);

			Console.WriteLine("Rysuje duża kanva " + myLine.X1 + " " + myLine.Y1);
			canva.Dispatcher.Invoke(() => canva.UpdateLayout(), DispatcherPriority.Background);
		}


		private void btnStartClick(object sender, RoutedEventArgs e)
		{
			keepWorking = !keepWorking;
			joyStick();
		}

		private void drawLineBtnClick(object sender, RoutedEventArgs e)
		{
			// Add a Line Element
			Line myLine = new Line();
			myLine.Stroke = Brushes.Black;
			myLine.X1 = 100;
			myLine.Y1 = 100;

			myLine.X2 = 101;
			myLine.Y2 = 101;
			myLine.StrokeThickness = 2;

			canva.Children.Add(myLine);


			canva.UpdateLayout();
			canva.Dispatcher.Invoke(() => DispatcherPriority.Background);
			Console.WriteLine("Rysuje");
		}

		//private void startMouse()
		//{
		//	// Find a mouse
		//	var mouseGuid = Guid.Empty;

		//	foreach (var deviceInstance in directInput.GetDevices(DeviceType.Mouse,
		//				DeviceEnumerationFlags.AllDevices))
		//		mouseGuid = deviceInstance.InstanceGuid;

		//	// If Mouse not found, look for a Mouse
		//	if (mouseGuid == Guid.Empty)
		//		foreach (var deviceInstance in directInput.GetDevices(DeviceType.Joystick,
		//				DeviceEnumerationFlags.AllDevices))
		//			mouseGuid = deviceInstance.InstanceGuid;

		//	// If Mouse not found, throws an error
		//	if (mouseGuid == Guid.Empty)
		//	{
		//		Console.WriteLine("No Mouse found.");
		//		Console.ReadKey();
		//		Environment.Exit(1);
		//	}

		//	// Instantiate the mouse
		//	mouse = new Mouse(directInput);

		//	Console.WriteLine("Found Mouse with GUID: {0}", mouseGuid);


		//	//Acquire devices for capturing.
		//	mouse.Acquire();

		//	Console.WriteLine(mouse.GetCurrentState().ToString());
		//}

		private void useAsAMouse(object sender, RoutedEventArgs e)
		{
			//this.Cursor = new Cursor(Cursor.Current.Handle);
			//Cursor.Position = new Point(Cursor.Position.X - 50, Cursor.Position.Y - 50);
			//Cursor.Clip = new Rectangle(this.Location, this.Size);

			//SetCursorPos(300, 300);
			useAsAMouseBool = true;

			int mouseX = 0;
			int mouseY = 0;


            while (useAsAMouseBool)
            {
				joystick.Poll();
				var datas = joystick.GetBufferedData();
				foreach (var state in datas)
				{
					if (state.Offset == JoystickOffset.X || state.Offset == JoystickOffset.RotationX)
					{
						mouseX = (int)(state.Value / 20.0);
					}
					else if (state.Offset == JoystickOffset.Y || state.Offset == JoystickOffset.RotationY)
					{
						mouseY = (int)(state.Value / 20.0);
					}

					SetCursorPos(mouseX, mouseY);

					if (state.Offset == JoystickOffset.Buttons5)
                    {
						useAsAMouseBool = false;
                    }

					if (state.Offset == JoystickOffset.Buttons0)
					{
						LeftClick(mouseX, mouseY);
					}

					if (state.Offset == JoystickOffset.Buttons3)
					{
						RightClick(mouseX, mouseY);
					}

				}
			}
			

		}
	}
}
