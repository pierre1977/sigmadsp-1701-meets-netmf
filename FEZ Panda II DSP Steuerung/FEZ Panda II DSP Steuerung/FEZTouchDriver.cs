/*
Touch Driver 2.3 Work with UI-Controls
 
Copyright 2011 GHI Electronics LLC
Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except in compliance with the License. 
You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
Unless required by applicable law or agreed to in writing, software distributed under the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, 
either express or implied. See the License for the specific language governing permissions and limitations under the License. 
*/

using System;
using System.Threading;

using Microsoft.SPOT.Hardware;

using GHIElectronics.NETMF.Hardware;


namespace GHIElectronics.NETMF.FEZ
{
    public static partial class FEZ_Components
    {
        public class FEZTouch : IDisposable
        {
            #region Enumerated Values

            public enum Orientation
            {
                Portrait = 0,
				PortraitInverse = 1,
				Landscape = 2,
				LandscapeInverse = 3
            }

            public enum Color : ushort
            {
                White = 0xFFFF,
                Black = 0x0000,
                Red = (0xFF >> 3) | ((0 & 0xFC) << 3) | ((0 & 0xF8) << 8),
                Blue = (0 >> 3) | ((0 & 0xFC) << 3) | ((0xFF & 0xF8) << 8),
				Green = (0 >> 3) | ((0xFF & 0xFC) << 3) | ((0 & 0xF8) << 8),
				Cyan = (0 >> 3) | ((0xFF & 0xFC) << 3) | ((0xFF & 0xF8) << 8),
                Gray = (0x80 >> 3) | ((0x80 & 0xFC) << 3) | ((0x80 & 0xF8) << 8),
				Magneta = (0xFF >> 3) | ((0 & 0xFC) << 3) | ((0xFF & 0xF8) << 8),
				Yellow = (0xFF >> 3) | ((0xFF & 0xFC) << 3) | ((0 & 0xF8) << 8),
            }

			public enum PowerMode
			{
				Normal = 0,
				Sleep = 1,
				StandBy = 2,
				DeepStandBy = 3
			}

			public enum DisplayMode
			{
				Normal = 0,
				Dim = 1,
				Off = 2
			}

			#endregion


			#region Events

			public delegate void TouchEventHandler(int x, int y);

			public event TouchEventHandler TouchDownEvent = delegate { };
			public event TouchEventHandler TouchMoveEvent = delegate { };
			public event TouchEventHandler TouchUpEvent = delegate { };

			#endregion


			#region Constructors

            protected FEZTouch()
            {
                // not allowed to call this constructor
			}

            public FEZTouch(LCDConfiguration lcdConfig)
            {
                this.InitLCD(lcdConfig);
            }

            public FEZTouch(LCDConfiguration lcdConfig, TouchConfiguration touchConfig)
            {
				this.InitLCD(lcdConfig);
                this.InitTouch(touchConfig);
            }

            #endregion


            #region Destructors

            ~FEZTouch()
            {
                this.Dispose();
            }

            public void Dispose()
            {
                if (this.disposed == false)
                {
                    this.disposed = true;

                    if (this.spi != null)
                    {
                        this.terminateTouchThread = true;
                        this.touchThread.Join();

                        this.spi.Dispose();
                        this.touchIRQ.Dispose();
                    }

                    this.TouchDownEvent = null;
					this.TouchMoveEvent = null;
					this.TouchUpEvent = null;

                    this.parallelPort.Dispose();
                    this.lcdReset.Dispose();
                    this.lcdChipSelect.Dispose();
                    this.lcdRegSelect.Dispose();
                    this.lcdBackLight.Dispose();
                }
            }

            #endregion


            #region Properties

            public int ScreenWidth
            {
				get
				{
					switch (this.lcdOrientation)
					{
						case Orientation.Landscape:
						case Orientation.LandscapeInverse:
							return 320;
						default:
							return 240;
					}
				}
			}

            public int ScreenHeight
            {
				get
				{
					switch (this.lcdOrientation)
					{
						case Orientation.Landscape:
						case Orientation.LandscapeInverse:
							return 240;
						default:
							return 320;
					}
				}
			}

			#endregion


			#region Control Methods

			private void InitLCD(LCDConfiguration lcdConfig)
			{
				// save config values
				this.parallelPort = new ParallelPort(lcdConfig.DataPins, lcdConfig.WritePin, lcdConfig.ReadPin);
				this.lcdReset = new OutputPort(lcdConfig.Reset, true);
				this.lcdChipSelect = new OutputPort(lcdConfig.ChipSelect, true);
				this.lcdRegSelect = new OutputPort(lcdConfig.RS, true);
				this.lcdBackLight = new OutputCompare(lcdConfig.BackLight, true, 2);
				this.lcdOrientation = lcdConfig.LCDOrientation;

				// set initial power and display modes
				this.displayMode = DisplayMode.Normal;
				this.backlightLevel = 100;

				// toggle reset pin
				this.lcdReset.Write(true);
				Thread.Sleep(5);
				this.lcdReset.Write(false);
				Thread.Sleep(5);
				this.lcdReset.Write(true);
				Thread.Sleep(5);

				this.lcdChipSelect.Write(false);

				//************* Start Initial Sequence **********//
				this.WriteRegister(0x01, 0x0100); // set SS and SM bit
				this.WriteRegister(0x02, 0x0200); // set 1 line inversion
				switch (this.lcdOrientation)
				{
					case Orientation.Portrait:
						// AM		=	0
						// ID1-ID0	=	10
						// ORG		=	1
						// HWM		=	1
						this.WriteRegister(0x03, 0x0230);
						break;
					case Orientation.PortraitInverse:
						// AM		=	0
						// ID1-ID0	=	00
						// ORG		=	0
						// HWM		=	1
						this.WriteRegister(0x03, 0x0200);
						break;
					case Orientation.Landscape:
						// AM		=	1
						// ID1-ID0	=	11
						// ORG		=	0
						// HWM		=	1
						this.WriteRegister(0x03, 0x02A8);
						break;
					case Orientation.LandscapeInverse:
						// AM		=	1
						// ID1-ID0	=	01
						// ORG		=	0
						// HWM		=	1
						this.WriteRegister(0x03, 0x0298);
						break;
				}
				this.WriteRegister(0x04, 0x0000); // Resize register
				this.WriteRegister(0x08, 0x0207); // set the back porch and front porch
				this.WriteRegister(0x09, 0x0000); // set non-display area refresh cycle ISC[3:0]
				this.WriteRegister(0x0A, 0x0000); // FMARK function
                this.WriteRegister(0x0C, 0x0000); // RGB Display Interface Control 1 , 18-bit RGB interface (1 transfer/pixel), DB[17:0]
				this.WriteRegister(0x0D, 0x0000); // Frame marker Position
				this.WriteRegister(0x0F, 0x0000); // RGB interface polarity

				//*************Power On sequence ****************//
				this.WriteRegister(0x10, 0x0000); // SAP, BT[3:0], AP, DSTB, SLP, STB
				this.WriteRegister(0x11, 0x0007); // DC1[2:0], DC0[2:0], VC[2:0]
				this.WriteRegister(0x12, 0x0000); // VREG1OUT voltage
				this.WriteRegister(0x13, 0x0000); // VDV[4:0] for VCOM amplitude
                this.WriteRegister(0x07, 0x0001); // Display Control 1 
				Thread.Sleep(200); // Dis-charge capacitor power voltage
				this.WriteRegister(0x10, 0x1690); // SAP, BT[3:0], AP, DSTB, SLP, STB
				this.WriteRegister(0x11, 0x0227); // Set DC1[2:0], DC0[2:0], VC[2:0]
				Thread.Sleep(50); // Delay 50ms
				this.WriteRegister(0x12, 0x000D); // 0012
				Thread.Sleep(50); // Delay 50ms
				this.WriteRegister(0x13, 0x1200); // VDV[4:0] for VCOM amplitude
				this.WriteRegister(0x29, 0x000A); // 04 VCM[5:0] for VCOMH
				this.WriteRegister(0x2B, 0x000D); // Set Frame Rate
				Thread.Sleep(50); // delay 50ms
				this.WriteRegister(0x20, 0x0000); // GRAM horizontal Address
				this.WriteRegister(0x21, 0x0000); // GRAM Vertical Address

				// ----------- Adjust the Gamma Curve ----------//
				this.WriteRegister(0x30, 0x0000);
				this.WriteRegister(0x31, 0x0404);
				this.WriteRegister(0x32, 0x0003);
				this.WriteRegister(0x35, 0x0405);
				this.WriteRegister(0x36, 0x0808);
				this.WriteRegister(0x37, 0x0407);
				this.WriteRegister(0x38, 0x0303);
				this.WriteRegister(0x39, 0x0707);
				this.WriteRegister(0x3C, 0x0504);
				this.WriteRegister(0x3D, 0x0808);

				//------------------ Set GRAM area ---------------//
				this.WriteRegister(0x50, 0x0000); // Horizontal GRAM Start Address
				this.WriteRegister(0x51, 0x00EF); // Horizontal GRAM End Address
				this.WriteRegister(0x52, 0x0000); // Vertical GRAM Start Address
				this.WriteRegister(0x53, 0x013F); // Vertical GRAM Start Address
				this.WriteRegister(0x60, 0xA700); // Gate Scan Line
				this.WriteRegister(0x61, 0x0001); // NDL, VLE, REV
				this.WriteRegister(0x6A, 0x0000); // set scrolling line

				//-------------- Partial Display Control ---------//
                this.WriteRegister(0x80, 0x0000);   // Partial Image 1 Display Position
                this.WriteRegister(0x81, 0x0000);   // Partial Image 1 RAM Start Address
                this.WriteRegister(0x82, 0x0000);   // Partial Image 1 RAM End Address
                this.WriteRegister(0x83, 0x0000);   // Partial Image 2 Display Position
                this.WriteRegister(0x84, 0x0000);   // Partial Image 2 RAM Start Address
                this.WriteRegister(0x85, 0x0000);   // Partial Image 2 RAM End Address

				//-------------- Panel Control -------------------//
				this.WriteRegister(0x90, 0x0010);
				this.WriteRegister(0x92, 0x0000);
				this.WriteRegister(0x07, 0x0133); // 262K color and display ON

				this.lcdChipSelect.Write(true);
			}

			private void InitTouch(TouchConfiguration touchConfig)
			{
				this.spi = new SPI(new SPI.Configuration(touchConfig.ChipSelect, false, 1, 1, false, true, 2000, touchConfig.Channel));
				this.touchIRQ = new InputPort(touchConfig.TouchIRQ, false, Port.ResistorMode.Disabled);
				this.terminateTouchThread = false;
				this.touchThread = new Thread(this.TouchThread);
				this.touchThread.Priority = ThreadPriority.Highest;
				this.touchThread.Start();
			}

			/*
			public void SetPowerMode(PowerMode powerMode)
			{
				if (this.powerMode != powerMode)
				{
					// save power mode
					this.powerMode = powerMode;

					// set new power mode
					this.lcdChipSelect.Write(false);

					switch (this.powerMode)
					{
						case PowerMode.Normal:
							this.WriteRegister(0x10, 0x1690);
							break;
						case PowerMode.Sleep:
							this.WriteRegister(0x10, 0x1691);
							break;
						case PowerMode.StandBy:
							this.WriteRegister(0x10, 0x1692);
							break;
						case PowerMode.DeepStandBy:
							this.WriteRegister(0x10, 0x1694);
							break;
					}

					this.lcdChipSelect.Write(true);
				}
			}
			*/

			public void SetDisplayMode(DisplayMode displayMode)
			{
				if (this.displayMode != displayMode)
				{
					// save display mode
					this.displayMode = displayMode;

					// set new display mode
					this.lcdChipSelect.Write(false);

					switch (this.displayMode)
					{
						case DisplayMode.Normal:
							this.SetBackLightLevel(100);
							this.WriteRegister(0x07, 0x0133);
							break;
						case DisplayMode.Dim:
							this.SetBackLightLevel(5);
							this.WriteRegister(0x07, 0x0133);
							break;
						case DisplayMode.Off:
							this.SetBackLightLevel(0);
							this.WriteRegister(0x07, 0x0131);
							break;
					}

					this.lcdChipSelect.Write(true);
				}
			}

			public void SetBackLightLevel(int backlightLevel)
			{
				// check for valid parameters
				if (backlightLevel < 0 || backlightLevel > 100)
				{
					throw new ArgumentException();
				}

				if (this.backlightLevel != backlightLevel)
				{
					// save back light level
					this.backlightLevel = backlightLevel;

					// set new back light level
					if (this.backlightLevel == 0)
					{
						this.lcdBackLight.Set(false);
					}
					else if (this.backlightLevel == 100)
					{
						this.lcdBackLight.Set(true);
					}
					else
					{
						const int PERIOD = 10000;
						int highTime = PERIOD * this.backlightLevel / 100;
						this.lcdBackLight.Set(true, new uint[2] { (uint)highTime, (uint)(PERIOD - highTime) }, 0, 2, true);
					}
				}
			}

			#endregion


			#region Drawing Methods

			public void ClearScreen()
            {
                this.FillRectangle(0, 0, this.ScreenWidth, this.ScreenHeight, FEZ_Components.FEZTouch.Color.Black);  // fill screen with black
            }

            public Color ColorFromRGB(byte red, byte green, byte blue)
            {
                return (Color)((red >> 3) | ((green & 0xFC) << 3) | ((blue & 0xF8) << 8));
            }

			private void SetPixelAddress(int x, int y)
			{
				this.WriteRegister(0x20, (ushort)x);
				this.WriteRegister(0x21, (ushort)y);
			}

			private void SetDrawingWindow(int x, int y, int width, int height)
			{
				// pixel address
				this.SetPixelAddress(x, y);

				// window
				this.WriteRegister(0x50, (ushort)x);
				this.WriteRegister(0x52, (ushort)y);
				this.WriteRegister(0x51, (ushort)(x + width - 1));
				this.WriteRegister(0x53, (ushort)(y + height - 1));
			}

            public void SetPixel(int x, int y, Color color)
            {
                if (x < 0 || y < 0 || x >= this.ScreenWidth || y >= this.ScreenHeight)
                {
                    throw new ArgumentException("SetPixel: Invalid parameter values.");
                }

                this.lcdChipSelect.Write(false);

                this.buffer[0] = (byte)((int)color >> 8);
                this.buffer[1] = (byte)(color);

                this.SetDrawingWindow(x, y, 1, 1);

                this.SetRegister(REGISTER_WRITE_GRAM);
                this.parallelPort.Write(this.buffer, 0, 2);

                this.lcdChipSelect.Write(true);
            }

            public void DrawLine(int xStart, int yStart, int xEnd, int yEnd, Color color)
            {
				if (xStart < 0 || yStart < 0 || xStart >= this.ScreenWidth || yStart >= this.ScreenHeight)
                {
                    throw new ArgumentException("DrawLine: Invalid start position values.");
                }

				if (xEnd < 0 || yEnd < 0 || xEnd >= this.ScreenWidth || yEnd >= this.ScreenHeight)
                {
					throw new ArgumentException("DrawLine: Invalid end position values.");
				}

				// translate coordinates based on display orientation
				int x0 = 0;
				int y0 = 0;
				int x1 = 0;
				int y1 = 0;
				switch (this.lcdOrientation)
				{
					case Orientation.Portrait:
						x0 = xStart;
						y0 = yStart;
						x1 = xEnd;
						y1 = yEnd;
						break;
					case Orientation.PortraitInverse:
						x0 = SCREEN_WIDTH - xStart - 1;
						y0 = SCREEN_HEIGHT - yStart - 1;
						x1 = SCREEN_WIDTH - xEnd - 1;
						y1 = SCREEN_HEIGHT - yEnd - 1;
						break;
					case Orientation.Landscape:
						x0 = SCREEN_WIDTH - yStart - 1;
						y0 = xStart;
						x1 = SCREEN_WIDTH - yEnd - 1;
						y1 = xEnd;
						break;
					case Orientation.LandscapeInverse:
						x0 = yStart;
						y0 = SCREEN_HEIGHT - xStart - 1;
						x1 = yEnd;
						y1 = SCREEN_HEIGHT - xEnd - 1;
						break;
				}

				this.lcdChipSelect.Write(false);

                this.buffer[0] = (byte)((int)color >> 8);
                this.buffer[1] = (byte)(color);

                int dy = y1 - y0;
                int dx = x1 - x0;

                float m = 0;
                int b = 0;

                if (dx != 0)
                {
                    m = ((float)(dy)) / (dx);
                    b = y0 - (int)(m * x0);
                }

				if (global::System.Math.Abs(dx) >= global::System.Math.Abs(dy))
                {
					if (x0 > x1)
					{
                        this.Swap(ref x0, ref x1);
                        this.Swap(ref y0, ref y1);
                    }

                    while (x0 <= x1)
                    {
						this.SetDrawingWindow(x0, y0, 1, 1);

                        this.SetRegister(REGISTER_WRITE_GRAM);
                        this.parallelPort.Write(this.buffer, 0, 2);

                        x0++;

                        if (x0 <= x1)
                        {
                            y0 = (int)(m * x0) + b;
                        }
                    }
                }
                else
                {
					if (y0 > y1)
                    {
                        this.Swap(ref x0, ref x1);
                        this.Swap(ref y0, ref y1);
                    }

                    while (y0 <= y1)
                    {
						this.SetDrawingWindow(x0, y0, 1, 1);

                        this.SetRegister(REGISTER_WRITE_GRAM);
                        this.parallelPort.Write(this.buffer, 0, 2);

                        y0++;

                        if (y0 <= y1)
                        {
							if (dx != 0)
							{
								x0 = (int)((float)(y0 - b) / m);
							}
                        }
                    }
                }

                this.lcdChipSelect.Write(true);
            }

            public void FillRectangle(int xPos, int yPos, int rectWidth, int rectHeight, Color color)
            {
				// validate parameter values
                if (xPos < 0 || yPos < 0 || (xPos + rectWidth) > this.ScreenWidth || (yPos + rectHeight) > this.ScreenHeight)
                {
                    throw new ArgumentException("FillRectangle: Invalid parameter values.");
                }

				// translate coordinates based on display orientation
				int x = 0;
				int y = 0;
				int width = 0;
				int height = 0;
				switch (this.lcdOrientation)
				{
					case Orientation.Portrait:
						x = xPos;
						y = yPos;
						width = rectWidth;
						height = rectHeight;
						break;
					case Orientation.PortraitInverse:
						x = SCREEN_WIDTH - xPos - rectWidth;
						y = SCREEN_HEIGHT - yPos - rectHeight;
						width = rectWidth;
						height = rectHeight;
						break;
					case Orientation.Landscape:
						x = SCREEN_WIDTH - yPos - rectHeight;
						y = xPos;
						width = rectHeight;
						height = rectWidth;
						break;
					case Orientation.LandscapeInverse:
						x = yPos;
						y = SCREEN_HEIGHT - xPos - rectWidth;
						width = rectHeight;
						height = rectWidth;
						break;
				}

                this.lcdChipSelect.Write(false);

                int pixelCount = width * height;
                int bufferPixels = this.buffer.Length / 2; // every pixel is 2 bytes
                byte h = (byte)((int)color >> 8);
                byte l = (byte)(color);

                // fill buffer
                for (int i = 0; i < this.buffer.Length; i = i + 2)
                {
                    this.buffer[i] = h;
                    this.buffer[i + 1] = l;
                }

                this.SetDrawingWindow(x, y, width, height);

                this.SetRegister(REGISTER_WRITE_GRAM);

                int loops = pixelCount / bufferPixels;

                for (int i = 0; i < loops; i++)
                {
                    this.parallelPort.Write(this.buffer, 0, this.buffer.Length);
                }

                int pixelsLeft = pixelCount % bufferPixels;
                if (pixelsLeft > 0)
                {
                    // every pixel is 2 bytes
                    this.parallelPort.Write(this.buffer, 0, pixelsLeft * 2);
                }

                this.lcdChipSelect.Write(true);
            }

            public byte[] getFillRectangleBuffer(int xPos, int yPos, int rectWidth, int rectHeight, Color color)
            {
                // validate parameter values
                if (xPos < 0 || yPos < 0 || (xPos + rectWidth) > this.ScreenWidth || (yPos + rectHeight) > this.ScreenHeight)
                {
                    throw new ArgumentException("FillRectangle: Invalid parameter values.");
                }                
                
                // translate coordinates based on display orientation
                int x = 0;
                int y = 0;
                int width = 0;
                int height = 0;
                byte[] retBuffer;// = new byte[];

                switch (this.lcdOrientation)
                {
                    case Orientation.Portrait:
                        x = xPos;
                        y = yPos;
                        width = rectWidth;
                        height = rectHeight;
                        break;
                    case Orientation.PortraitInverse:
                        x = SCREEN_WIDTH - xPos - rectWidth;
                        y = SCREEN_HEIGHT - yPos - rectHeight;
                        width = rectWidth;
                        height = rectHeight;
                        break;
                    case Orientation.Landscape:
                        x = SCREEN_WIDTH - yPos - rectHeight;
                        y = xPos;
                        width = rectHeight;
                        height = rectWidth;
                        break;
                    case Orientation.LandscapeInverse:
                        x = yPos;
                        y = SCREEN_HEIGHT - xPos - rectWidth;
                        width = rectHeight;
                        height = rectWidth;
                        break;
                }


                int pixelCount = width * height;
                int bufferPixels = this.buffer.Length / 2; // every pixel is 2 bytes
                byte h = (byte)((int)color >> 8);
                byte l = (byte)(color);

                // fill buffer
                for (int i = 0; i < this.buffer.Length; i = i + 2)
                {
                    this.buffer[i] = h;
                    this.buffer[i + 1] = l;
                }
                int loops = pixelCount / bufferPixels;
                int pixelsLeft = pixelCount % bufferPixels;

                // set return buffer
                retBuffer = new byte[ 8 + (loops * this.buffer.Length) + (pixelsLeft * 2)];
                retBuffer[0] = (byte)(x >> 8);
                retBuffer[1] = (byte)(x >> 0);

                retBuffer[2] = (byte)(y >> 8);
                retBuffer[3] = (byte)(y >> 0);

                retBuffer[4] = (byte)(width >> 8);
                retBuffer[5] = (byte)(width >> 0);

                retBuffer[6] = (byte)(height >> 8);
                retBuffer[7] = (byte)(height >> 0);
                
                
                for (int i = 0; i < loops; i++)
                {
                    //this.parallelPort.Write(this.buffer, 0, this.buffer.Length);
                    for(int j = 0; j< this.buffer.Length; j++)
                    {
                        retBuffer[(j*i)+8] = this.buffer[j];
                    }
                    
                }
                if (pixelsLeft > 0)
                {
                    // every pixel is 2 bytes
                    //this.parallelPort.Write(this.buffer, 0, pixelsLeft * 2);
                    for (int j = 0; j < pixelsLeft * 2; j++) 
                    {
                        retBuffer[8 + (loops * this.buffer.Length) + j] = this.buffer[j];
                    }
                }

                return retBuffer;
            }



            public void DrawImage(int xPos, int yPos, Image image)
            {
				// validate parameter values
				if (xPos < 0 || yPos < 0 || (xPos + image.Width) > this.ScreenWidth || (yPos + image.Height) > this.ScreenHeight)
				{
					throw new ArgumentException("DrawImage: Invalid parameter values.");
				}

				// translate coordinates based on display orientation
				int x = 0;
				int y = 0;
				int width = 0;
				int height = 0;
				switch (this.lcdOrientation)
				{
					case Orientation.Portrait:
						x = xPos;
						y = yPos;
						width = image.Width;
						height = image.Height;
						break;
					case Orientation.PortraitInverse:
						x = SCREEN_WIDTH - xPos - image.Width;
						y = SCREEN_HEIGHT - yPos - image.Height;
						width = image.Width;
						height = image.Height;
						break;
					case Orientation.Landscape:
						x = SCREEN_WIDTH - yPos - image.Height;
						y = xPos;
						width = image.Height;
						height = image.Width;
						break;
					case Orientation.LandscapeInverse:
						x = yPos;
						y = SCREEN_HEIGHT - xPos - image.Width;
						width = image.Height;
						height = image.Width;
						break;
				}

				this.lcdChipSelect.Write(false);
                this.SetDrawingWindow(x, y, width, height);
                this.SetRegister(REGISTER_WRITE_GRAM);
                this.parallelPort.Write(image.ImageBytes, Image.IMG_PIXELS_INDEX, image.ImageBytes.Length - Image.IMG_PIXELS_INDEX);
                this.lcdChipSelect.Write(true);
            }

			public void DrawString(int xPos, int yPos, string text, Color fgColor, Color bgColor, Font font)
			{
				// validate parameter values
				if (xPos < 0 || yPos < 0)
				{
					throw new ArgumentException("DrawString: Screen position (x,y) is invalid.");
				}

				if ((xPos + font.GetTextWidth(text)) > this.ScreenWidth || (yPos + font.Height) > this.ScreenHeight)
				{
					throw new ArgumentException("DrawString: Text string is too wide or too high to fit on the screen.");
				}

				// translate coordinates based on display orientation
				int x = 0;
				int y = 0;
				int width = 0;
				int height = 0;
				switch (this.lcdOrientation)
				{
					case Orientation.Portrait:
						x = xPos;
						y = yPos;
						break;
					case Orientation.PortraitInverse:
						x = SCREEN_WIDTH - xPos - font.AverageWidth;
						y = SCREEN_HEIGHT - yPos - font.Height;
						break;
					case Orientation.Landscape:
						x = SCREEN_WIDTH - yPos - font.Height;
						y = xPos;
						break;
					case Orientation.LandscapeInverse:
						x = yPos;
						y = SCREEN_HEIGHT - xPos - font.AverageWidth;
						break;
				}

				// split colors into bytes
				byte fgColorHigh = (byte)((int)fgColor >> 8);
				byte fgColorLow = (byte)(fgColor);
				byte bgColorHigh = (byte)((int)bgColor >> 8);
				byte bgColorLow = (byte)(bgColor);

				// draw each character
				char currentChar = ' ';
				CharInfo charInfo = null;
				int bytesInBuffer = 0;
				for (int charIndex = 0; charIndex < text.Length; charIndex++)
				{
					// get character
					currentChar = text[charIndex];

					// get character info
					charInfo = font[currentChar];

					// translate width and height based on display orientation
					switch (this.lcdOrientation)
					{
						case Orientation.Portrait:
						case Orientation.PortraitInverse:
							width = charInfo.Width;
							height = charInfo.Height;
							break;
						case Orientation.Landscape:
						case Orientation.LandscapeInverse:
							width = charInfo.Height;
							height = charInfo.Width;
							break;
					}

					// get character pixels
					bytesInBuffer = font.FillBitmapBuffer(charInfo, this.buffer, fgColorLow, fgColorHigh, bgColorLow, bgColorHigh);

					// output character pixels
					this.lcdChipSelect.Write(false);
					this.SetDrawingWindow(x, y, width, height);
					this.SetRegister(REGISTER_WRITE_GRAM);
					this.parallelPort.Write(this.buffer, 0, bytesInBuffer);
					this.lcdChipSelect.Write(true);

					// update x position
					switch (this.lcdOrientation)
					{
						case Orientation.Portrait:
							x += width;
							break;
						case Orientation.PortraitInverse:
							x -= width;
							break;
						case Orientation.Landscape:
							y += height;
							break;
						case Orientation.LandscapeInverse:
							y -= height;
							break;
					}
				}
			}

            // Create Buffer from Text-String
            public byte[][] getDrawStringBuffer(int xPos, int yPos, string text, Color fgColor, Color bgColor, Font font) 
            {
                byte[][] retBuffer = new byte[text.Length][];

                // validate parameter values
                if (xPos < 0 || yPos < 0)
                {
                    throw new ArgumentException("DrawString: Screen position (x,y) is invalid.");
                }

                if ((xPos + font.GetTextWidth(text)) > this.ScreenWidth || (yPos + font.Height) > this.ScreenHeight)
                {
                    throw new ArgumentException("DrawString: Text string is too wide or too high to fit on the screen.");
                }

                // translate coordinates based on display orientation
                int x = 0;
                int y = 0;
                int width = 0;
                int height = 0;
                switch (this.lcdOrientation)
                {
                    case Orientation.Portrait:
                        x = xPos;
                        y = yPos;
                        break;
                    case Orientation.PortraitInverse:
                        x = SCREEN_WIDTH - xPos - font.AverageWidth;
                        y = SCREEN_HEIGHT - yPos - font.Height;
                        break;
                    case Orientation.Landscape:
                        x = SCREEN_WIDTH - yPos - font.Height;
                        y = xPos;
                        break;
                    case Orientation.LandscapeInverse:
                        x = yPos;
                        y = SCREEN_HEIGHT - xPos - font.AverageWidth;
                        break;
                }

                // split colors into bytes
                byte fgColorHigh = (byte)((int)fgColor >> 8);
                byte fgColorLow = (byte)(fgColor);
                byte bgColorHigh = (byte)((int)bgColor >> 8);
                byte bgColorLow = (byte)(bgColor);

                // draw each character
                char currentChar = ' ';
                CharInfo charInfo = null;
                int bytesInBuffer = 0;
                for (int charIndex = 0; charIndex < text.Length; charIndex++)
                {
                    // get character
                    currentChar = text[charIndex];

                    // get character info
                    charInfo = font[currentChar];

                    // translate width and height based on display orientation
                    switch (this.lcdOrientation)
                    {
                        case Orientation.Portrait:
                        case Orientation.PortraitInverse:
                            width = charInfo.Width;
                            height = charInfo.Height;
                            break;
                        case Orientation.Landscape:
                        case Orientation.LandscapeInverse:
                            width = charInfo.Height;
                            height = charInfo.Width;
                            break;
                    }

                    // get character pixels
                    bytesInBuffer = font.FillBitmapBuffer(charInfo, this.buffer, fgColorLow, fgColorHigh, bgColorLow, bgColorHigh);
                    
                    // init Buffer
                    retBuffer[charIndex] = new byte[bytesInBuffer + 8];
                    //for (int bcount = 0; bcount < bytesInBuffer+8; bcount++) 
                    //{
                    //    retBuffer[charIndex] = new byte[bytesInBuffer + 8];
                    //}

                   // byte[] tmpBuffer = new byte[bytesInBuffer];                    
                    retBuffer[charIndex][0] = (byte)(x >> 8);                    
                    retBuffer[charIndex][1] = (byte)(x >> 0);

                    //Microsoft.SPOT.Debug.Print("X: " + retBuffer[charIndex][0] );
                    //Microsoft.SPOT.Debug.Print("X2: " + retBuffer[charIndex][1]);

                    retBuffer[charIndex][2] = (byte)(y >> 8);
                    retBuffer[charIndex][3] = (byte)(y >> 0);

                    retBuffer[charIndex][4] = (byte)(width >> 8);
                    retBuffer[charIndex][5] = (byte)(width >> 0);

                    retBuffer[charIndex][6] = (byte)(height >> 8);
                    retBuffer[charIndex][7] = (byte)(height >> 0);


                    for( int bcount=0; bcount < bytesInBuffer; bcount++)
                    {
                       // tmpBuffer[bcount] = this.buffer[bcount];
                        retBuffer[charIndex][bcount+8] = this.buffer[bcount];
                       // Microsoft.SPOT.Debug.Print("buffer: " + bcount + " Arry: " + this.buffer[bcount] );
                    }

                    
                    

                   // retBuffer[charIndex][0] = tmpBuffer;

                    // output character pixels
                    //this.lcdChipSelect.Write(false);
                    //this.SetDrawingWindow(x, y, width, height);
                    //this.SetRegister(REGISTER_WRITE_GRAM);
                    //this.parallelPort.Write(this.buffer, 0, bytesInBuffer);
                    //this.lcdChipSelect.Write(true);

                    // update x position
                    switch (this.lcdOrientation)
                    {
                        case Orientation.Portrait:
                            x += width;
                            break;
                        case Orientation.PortraitInverse:
                            x -= width;
                            break;
                        case Orientation.Landscape:
                            y += height;
                            break;
                        case Orientation.LandscapeInverse:
                            y -= height;
                            break;
                    }
                }

                return retBuffer;
            }

            // Custom GET Orientation(
            public Orientation getlcdOrientation()
            {
                return this.lcdOrientation;
            }

            // NOT USED
            public int getBufferSize() 
            {
                return this.buffer.Length;
            }

            // Write Buffer
            public void writeBuffer(int x, int y, int width, int height, byte[] buffer)
            {
                this.lcdChipSelect.Write(false);

                this.SetDrawingWindow(x, y, width, height);                
                this.SetRegister(REGISTER_WRITE_GRAM);
                this.parallelPort.Write(buffer, 0, buffer.Length);
                
                this.lcdChipSelect.Write(true);            
            }

            // Write Buffer mit Offset
            public void writeBuffer(int x, int y, int width, int height, byte[] buffer, int offset)
            {
                this.lcdChipSelect.Write(false);

                this.SetDrawingWindow(x, y, width, height);
                this.SetRegister(REGISTER_WRITE_GRAM);
                this.parallelPort.Write(buffer, offset, buffer.Length - offset);

                this.lcdChipSelect.Write(true);
            }


            // NOT USED
            /// <summary>
            /// Partial Image 1, to GRAM, only for TEST 
            /// </summary>
            public void SetPartialRectangle(int xPos, int yPos, int rectWidth, int rectHeight, Color color)
            {
                // validate parameter values
                if (xPos < 0 || yPos < 0 || (xPos + rectWidth) > this.ScreenWidth || (yPos + rectHeight) > this.ScreenHeight)
                {
                    throw new ArgumentException("FillRectangle: Invalid parameter values.");
                }

                // translate coordinates based on display orientation
                int x = 0;
                int y = 0;
                int width = 0;
                int height = 0;
                switch (this.lcdOrientation)
                {
                    case Orientation.Portrait:
                        x = xPos;
                        y = yPos;
                        width = rectWidth;
                        height = rectHeight;
                        break;
                    case Orientation.PortraitInverse:
                        x = SCREEN_WIDTH - xPos - rectWidth;
                        y = SCREEN_HEIGHT - yPos - rectHeight;
                        width = rectWidth;
                        height = rectHeight;
                        break;
                    case Orientation.Landscape:
                        x = SCREEN_WIDTH - yPos - rectHeight;
                        y = xPos;
                        width = rectHeight;
                        height = rectWidth;
                        break;
                    case Orientation.LandscapeInverse:
                        x = yPos;
                        y = SCREEN_HEIGHT - xPos - rectWidth;
                        width = rectHeight;
                        height = rectWidth;
                        break;
                }

                int pixelCount = width * height;
                int bufferPixels = this.buffer.Length / 2; // every pixel is 2 bytes
                byte h = (byte)((int)color >> 8);
                byte l = (byte)(color);

                // fill buffer
                for (int i = 0; i < this.buffer.Length; i = i + 2)
                {
                    this.buffer[i] = h;
                    this.buffer[i + 1] = l;
                }

                               


                // Start Write
                this.lcdChipSelect.Write(false);

                

                // Setting, BASEE = 0, PTDE0 = 1 
                //this.WriteRegister(0x07, 0x1133); // 262K color and display ON and PTDE0 = 1, BASEE = 0
                this.WriteRegister(0x07, 0x0033); // 262K color and display ON and PTDE0 = 1, BASEE = 0

                // Setting, NL = 27
                this.WriteRegister(0x60, 0x2700); // Driver Output Control 2 (ILI9325_DRIVER_OUTPUT_CTRL2) 

                this.WriteRegister(0x07, 0x1033);


                // PTSA0  
                this.WriteRegister(0x81, 0x0000);   // Partial Image 1 RAM Start Address
                // PTEA0
                this.WriteRegister(0x82, 0x000F);   // Partial Image 1 RAM End Address, LINE 15
                // PTDP0
                this.WriteRegister(0x80, 0x0000);   // Partial Image 1 Display Position

                // GRAM Settings
                //this.SetDrawingWindow(x, y, width, height);
              //  this.WriteRegister(0x20, (ushort)x); /* GRAM horizontal Address */
              //  this.WriteRegister(0x21, (ushort)y); /* GRAM Vertical Address */

                this.WriteRegister(0x20, 0); /* GRAM horizontal Address */
                this.WriteRegister(0x21, 0); /* GRAM Vertical Address */

                // Prepare
                //LCD_IR() = ILI9325_R22H; /* Write Data to GRAM (R22h)  */
                this.SetRegister(REGISTER_WRITE_GRAM);



                // Write PixelData
                int loops = pixelCount / bufferPixels;
                for (int i = 0; i < loops; i++)
                {
                    this.parallelPort.Write(this.buffer, 0, this.buffer.Length);
                }
                int pixelsLeft = pixelCount % bufferPixels;
                if (pixelsLeft > 0)
                {
                    // every pixel is 2 bytes
                    this.parallelPort.Write(this.buffer, 0, pixelsLeft * 2);
                }



                //this.buffer[0] = (byte)((int)color >> 8);
                //this.buffer[1] = (byte)(color);

                //for (int i = 0; i < 10; i++)
                //{
                //    this.parallelPort.Write(this.buffer, 0, 2);
                //}




                // MOVE 
                for (int i = 0; i < 200; i++) 
                {
                    // Partial Image 1 Display Position (R80h)  
                    this.WriteRegister(0x80, (ushort)i);
                    Thread.Sleep(10);
                }



                // End
                this.lcdChipSelect.Write(true);

            }


			#endregion


			#region Interface Methods

			private void Swap(ref int a1, ref int a2)
            {
                int temp = a1;
                a1 = a2;
                a2 = temp;
            }

            private void SetRegister(byte register)
            {
                 this.lcdRegSelect.Write(false);

                this.regBuffer[0] = 0;
				this.regBuffer[1] = register;
                this.parallelPort.Write(this.regBuffer, 0, 2);

                 this.lcdRegSelect.Write(true);
            }

			private void WriteRegister(byte register, ushort value)
            {
				this.SetRegister(register);

                this.regBuffer[0] = (byte)(value >> 8);
                this.regBuffer[1] = (byte)(value);
                this.parallelPort.Write(regBuffer, 0, 2);
            }

            #endregion


            #region Touch Thread & Methods

            private void TouchThread()
            {
                int lastX = 0;
                int lastY = 0;
                bool lastStatus = false; // true means there are touches

                int x;
                int y;
                bool status;

                byte[] writeBuffer = new byte[] { 0, 0, 0, 0 };
                byte[] readBuffer = new byte[2];

                while (this.terminateTouchThread == false)
                {
                    Thread.Sleep(TOUCH_SAMPLING_TIME);

                    status = !this.touchIRQ.Read();

                    if (status == true)
                    {
                        writeBuffer[0] = 0x90;
                        this.spi.WriteRead(writeBuffer, readBuffer, 1);
                        y = readBuffer[0];
                        y <<= 8;
                        y |= readBuffer[1];
                        y >>= 3;

                        writeBuffer[0] = 0xD0;
                        this.spi.WriteRead(writeBuffer, readBuffer, 1);
                        x = readBuffer[0];
                        x <<= 8;
                        x |= readBuffer[1];
                        x >>= 3;

                        // calibrate 
                        if (x > 3750)
                            x = 3750;
                        else if (x < 280)
                            x = 280;

                        if (y > 3850)
                            y = 3850;
                        else if (y < 450)
                            y = 450;

                        x = (3750 - x) * (SCREEN_WIDTH - 1) / (3750 - 280);
                        y = (3850 - y) * (SCREEN_HEIGHT - 1) / (3850 - 450);

                        if (lastStatus == false)
                        {
							this.FireTouchDownEvent(x, y);

                            lastStatus = true;
                            lastX = x;
                            lastY = y;
                        }
                        else
                        {
                            // filter small changes
                            if (global::System.Math.Abs(x - lastX) > 5 || global::System.Math.Abs(y - lastY) > 5)
                            {
								this.FireTouchMoveEvent(x, y);
                                lastX = x;
                                lastY = y;
                            }
                        }
                    }
                    else if (lastStatus == true)
                    {
						this.FireTouchUpEvent(lastX, lastY);
                        lastStatus = false;
                    }
                }
            }

			private void FireTouchDownEvent(int xPos, int yPos)
			{
				if (this.TouchDownEvent != null)
				{
					// translate coordinates based on display orientation
					int x = 0;
					int y = 0;
					switch (this.lcdOrientation)
					{
						case Orientation.Portrait:
							x = xPos;
							y = yPos;
							break;
						case Orientation.PortraitInverse:
							x = SCREEN_WIDTH - xPos - 1;
							y = SCREEN_HEIGHT - yPos - 1;
							break;
						case Orientation.Landscape:
							x = yPos;
							y = SCREEN_WIDTH - xPos - 1;
							break;
						case Orientation.LandscapeInverse:
							x = SCREEN_HEIGHT - yPos - 1;
							y = xPos;
							break;
					}

					// fire the event
					this.TouchDownEvent(x, y);
				}
			}

			private void FireTouchMoveEvent(int xPos, int yPos)
			{
				if (this.TouchMoveEvent != null)
				{
					// translate coordinates based on display orientation
					int x = 0;
					int y = 0;
					switch (this.lcdOrientation)
					{
						case Orientation.Portrait:
							x = xPos;
							y = yPos;
							break;
						case Orientation.PortraitInverse:
							x = SCREEN_WIDTH - xPos - 1;
							y = SCREEN_HEIGHT - yPos - 1;
							break;
						case Orientation.Landscape:
							x = yPos;
							y = SCREEN_WIDTH - xPos - 1;
							break;
						case Orientation.LandscapeInverse:
							x = SCREEN_HEIGHT - yPos - 1;
							y = xPos;
							break;
					}

					// fire the event
					this.TouchMoveEvent(x, y);
				}
			}

			private void FireTouchUpEvent(int xPos, int yPos)
			{
				if (this.TouchUpEvent != null)
				{
					// translate coordinates based on display orientation
					int x = 0;
					int y = 0;
					switch (this.lcdOrientation)
					{
						case Orientation.Portrait:
							x = xPos;
							y = yPos;
							break;
						case Orientation.PortraitInverse:
							x = SCREEN_WIDTH - xPos - 1;
							y = SCREEN_HEIGHT - yPos - 1;
							break;
						case Orientation.Landscape:
							x = yPos;
							y = SCREEN_WIDTH - xPos - 1;
							break;
						case Orientation.LandscapeInverse:
							x = SCREEN_HEIGHT - yPos - 1;
							y = xPos;
							break;
					}

					// fire the event
					this.TouchUpEvent(x, y);
				}
			}

            #endregion


            #region Font Classes
                      
            public class Font
            {
                // CONSTRUCTORS
                protected Font()
                {
                    // this constructor is not for external access
                    this.avgWidth = 0;
                    this.maxWidth = 0;
                    this.height = 0;
                    this.startChar = '\x00';
                    this.endChar = '\x00';
                    this.charDescriptors = null;
                    this.charBitmaps = null;
                }


                // PROPERTIES
                public ushort AverageWidth
                {
                    get { return this.avgWidth; }
                }

				public ushort MaxWidth
                {
                    get { return this.maxWidth; }
                }

				public ushort Height
                {
                    get { return this.height; }
                }

				public CharInfo this[char newChar]
				{
					get
					{
						// validate parameter
						if (newChar < this.startChar || newChar > this.endChar)
						{
							throw new ArgumentException("Font class: character not found.");
						}

						ushort bitMask = 0x7FFF;
						int descriptorIndex = (newChar - this.startChar) * constSizeOfCharDescriptor;
						ushort charWidth = (ushort)(this.charDescriptors[descriptorIndex] & bitMask);
						ushort charHeight = (ushort)(this.charDescriptors[descriptorIndex + 1] & bitMask);
						ushort bitmapStartIndex = (ushort)(this.charDescriptors[descriptorIndex + 2] & bitMask);
						ushort bitmapLength = (ushort)(this.charDescriptors[descriptorIndex + 3] & bitMask);

						return new CharInfo(newChar, charWidth, charHeight, bitmapStartIndex, bitmapLength);
					}
				}


				// METHODS
				public int GetTextWidth(string text)
				{
					int totalPixels = 0;

					foreach (char character in text)
					{
						totalPixels += this[character].Width;
					}

					return totalPixels;

				}

				public int FillBitmapBuffer(CharInfo charInfo, byte[] buffer, byte fgColorLow, byte fgColorHigh, byte bgColorLow, byte bgColorHigh)
				{
					// check the buffer size
					int charBytesReq = charInfo.Width * charInfo.Height * 2;
					int totalBytesReq = charInfo.Width * this.height * 2;
					if (buffer.Length < totalBytesReq)
					{
						throw new ArgumentException("Buffer length is not large enough for this character.");
					}

					// fill the buffer
					ushort bitmapValue = 0;
					int bufferIndex = 0;
					ushort bitmapIndex = charInfo.BitmapStartIndex;
					for (int i = 0; i < charInfo.BitmapLength; i++, bitmapIndex++)
					{
						bitmapValue = (ushort)this.charBitmaps[bitmapIndex];
						for (ushort bitMask = (ushort)0x0001; bitMask < (ushort)0x8000; )
						{
							// add pixel color
							if ((bitmapValue & bitMask) == bitMask)
							{
								// add foreground color
								buffer[bufferIndex] = fgColorHigh;
								buffer[bufferIndex + 1] = fgColorLow;
							}
							else
							{
								// add background color
								buffer[bufferIndex] = bgColorHigh;
								buffer[bufferIndex + 1] = bgColorLow;
							}

							// adjust bitMask, increment pixelsRead, and increment bufferIndex
							bitMask = (ushort)(bitMask << 1);
							bufferIndex += 2;

                            //Microsoft.SPOT.Debug.Print("bufffilll " + i + " bindex " + bufferIndex + " Val " + buffer[bufferIndex]);

							// check if we are done
							if (bufferIndex >= charBytesReq)
							{
								break;
							}
						}
					}

					// add blank rows
					while (bufferIndex < totalBytesReq)
					{
						// add background color
						buffer[bufferIndex] = bgColorHigh;
						buffer[bufferIndex + 1] = bgColorLow;
						bufferIndex += 2;

                        //Microsoft.SPOT.Debug.Print("blackroww " + bufferIndex + "  Val " + buffer[bufferIndex]);

					}

					// return num bytes in buffer
					return bufferIndex;
				}


                // MEMBER FIELDS
                protected ushort avgWidth;
                protected ushort maxWidth;
                protected ushort height;
                protected char startChar;
                protected char endChar;
                protected string charDescriptors;
                protected string charBitmaps;
				protected const int constSizeOfCharDescriptor = 4;	// number of chars in each descriptor
            }

			public class CharInfo
			{
				// CONSTRUCTORS
				public CharInfo(char character, ushort charWidth, ushort charHeight, ushort bitmapStartIndex, ushort bitmapLength)
				{
					this.character = character;
					this.charWidth = charWidth;
					this.charHeight = charHeight;
					this.bitmapStartIndex = bitmapStartIndex;
					this.bitmapLength = bitmapLength;
				}

				// PROPERTIES
				public char Character
				{
					get { return this.character; }
				}

				public ushort Width
				{
					get { return this.charWidth; }
				}

				public ushort Height
				{
					get { return this.charHeight; }
				}

				public ushort BitmapStartIndex
				{
					get { return this.bitmapStartIndex; }
				}

				public ushort BitmapLength
				{
					get { return this.bitmapLength; }
				}


				// MEMBER FIELDS
				private char character;
				private ushort charWidth;
				private ushort charHeight;
				private ushort bitmapStartIndex;
				private ushort bitmapLength;
			}

            #endregion


            #region Image Class

            public class Image
            {
                // CONSTRUCTORS
                public Image(byte[] imgBytes)
                {
					if (Utility.ExtractValueFromArray(imgBytes, 0, 4) != SIGNATURE)
					{
						throw new ArgumentException("Image Class: Signature bytes not found.");
					}

                    int width = (int)Utility.ExtractValueFromArray(imgBytes, 4, 2);
                    int height = (int)Utility.ExtractValueFromArray(imgBytes, 6, 2);

					if (width * height * 2 + 8 != imgBytes.Length)
					{
						throw new ArgumentException("Image class: Width and height do not match size of byte array.");
					}

					this.ImageBytes = imgBytes;
                    this.Width = width;
                    this.Height = height;
                }

                // PROPERTIES
                public readonly int Width;
                public readonly int Height;
                public const uint SIGNATURE = 0x354A82B8;
                public const int IMG_PIXELS_INDEX = 8;
                public byte[] ImageBytes;
            }

            #endregion


            #region LCD Configuration Class

            public class LCDConfiguration
            {
                // CONSTRUCTORS
                public LCDConfiguration(FEZ_Pin.Digital reset,
                    FEZ_Pin.Digital chipSelect,
                    FEZ_Pin.Digital RS,
                    FEZ_Pin.Digital lcdBackLight,
                    FEZ_Pin.Digital[] dataPins,
                    FEZ_Pin.Digital writePin,
                    FEZ_Pin.Digital readPin,
                    Orientation lcdOrientation)
                {
                    this.DataPins = new Cpu.Pin[8];

					for (int i = 0; i < 8; i++)
					{
						this.DataPins[i] = (Cpu.Pin)dataPins[i];
					}

                    this.WritePin = (Cpu.Pin)writePin;
                    this.ReadPin = (Cpu.Pin)readPin;
                    this.ChipSelect = (Cpu.Pin)chipSelect;
                    this.Reset = (Cpu.Pin)reset;
                    this.RS = (Cpu.Pin)RS;
                    this.BackLight = (Cpu.Pin)lcdBackLight;
                    this.LCDOrientation = lcdOrientation;
                }

                // PROPERTIES
                public Cpu.Pin[] DataPins;
                public Cpu.Pin WritePin;
                public Cpu.Pin ReadPin;
                public Cpu.Pin ChipSelect;
                public Cpu.Pin Reset;
                public Cpu.Pin RS;
                public Cpu.Pin BackLight;
                public Orientation LCDOrientation;
            }

            #endregion


            #region Touch Configuration Class

            public class TouchConfiguration
            {
                public SPI.SPI_module Channel;
                public Cpu.Pin ChipSelect;
                public Cpu.Pin TouchIRQ;

                public TouchConfiguration(SPI.SPI_module channel, FEZ_Pin.Digital chipSelect, FEZ_Pin.Digital touchIRQ)
                {
                    this.Channel = channel;
                    this.ChipSelect = (Cpu.Pin)chipSelect;
                    this.TouchIRQ = (Cpu.Pin)touchIRQ;
                }
            }

            #endregion


            #region Member Fields

			// constants
			public const int SCREEN_WIDTH = 240;            // PUBLIC Änderung
            public const int SCREEN_HEIGHT = 320;           // PUBLIC Änderung
            private const int BUFFER_SIZE = 2024;
			private const byte REGISTER_WRITE_GRAM = 0x22;
			private const int TOUCH_SAMPLING_TIME = 10;

            private bool disposed = false;
            private byte[] buffer = new byte[BUFFER_SIZE];
            private byte[] regBuffer = new byte[2];

            // lcd configuration
            private ParallelPort parallelPort;
            private OutputPort lcdReset;
            private OutputPort lcdChipSelect;
            private OutputPort lcdRegSelect;
            private OutputCompare lcdBackLight;
            private Orientation lcdOrientation;

            // touch configuration
            private SPI spi;
            private InputPort touchIRQ;
            private bool terminateTouchThread;
            private Thread touchThread;

			// display mode
			private DisplayMode displayMode;
			private int backlightLevel;

            #endregion
        }
    }
}
