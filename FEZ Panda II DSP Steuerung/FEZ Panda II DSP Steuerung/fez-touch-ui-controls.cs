/*
FEZ TOUCH UI CONTROLS
Modify for Driver 2.3

Important: You need a Font

*/

using System;
using Microsoft.SPOT;
using GHIElectronics.NETMF.Hardware;
using GHIElectronics.NETMF.FEZ;
using System.Threading;

namespace FEZTouch.UIControls
{   
    public abstract class Control
    {
        protected int X_Pos;
        protected int Y_Pos;
        protected int Width;
        protected int Height;
        protected string btnText;
        protected FEZ_Components.FEZTouch touchscreen;
        public FEZ_Components.FEZTouch.Color BGColor;

       // public static FEZ_Components.FEZTouch.Font myFont10 = new FEZTouch.Fonts.FontCourierNew10(); // FONT PUBLIC
        public static FEZ_Components.FEZTouch.Font myFont = new FEZTouch.Fonts.FontCourierNew8(); // FONT PUBLIC
        

        protected virtual void Draw()
        {
            throw new Exception("Draw() not implemented");
        }

        public virtual void Refresh()
        {
            Draw();
        }

        protected virtual Boolean IsPointInControl(int x, int y)
        {
            if ((x >= X_Pos) && (x <= X_Pos + Width) &&
                (y >= Y_Pos) && (y <= Y_Pos + Height))
            {
                return true;
            }
            return false;
        }
    }

    public class Label : Control
    {
        public FEZ_Components.FEZTouch.Color TextColor { get; set; }
        private String myText;
        public String Text
        {
            get { return myText; }
            set
            {
                myText = value;
                Draw();
            }
        }

        public Label(int x, int y, FEZ_Components.FEZTouch ft, String txt)
        {
            Init(x, y, ft, 50, 10, txt);
        }

        public Label(int x, int y, FEZ_Components.FEZTouch ft, int w, int h, String txt)
        {
            Init(x, y, ft, w, h, txt);
        }

        public Label(int x, int y, FEZ_Components.FEZTouch ft, int w, int h, String txt, bool getBytes)
        {
            if (getBytes == true)
            {
                Init(x, y, ft, w, h, txt, getBytes);
            }
            else
            {
                Init(x, y, ft, w, h, txt);
            }
        }

        void Init(int x, int y, FEZ_Components.FEZTouch ft, int w, int h, String txt)
        {
            X_Pos = x;
            Y_Pos = y;
            Width = w;
            Height = h;
            touchscreen = ft;
            Text = txt;
            BGColor = FEZ_Components.FEZTouch.Color.Black;
            TextColor = FEZ_Components.FEZTouch.Color.White;

            Draw();
        }


        byte[][] Init(int x, int y, FEZ_Components.FEZTouch ft, int w, int h, String txt, bool getBytes)
        {
            byte[][] labelTextBuffer = new byte[0][];

            X_Pos = x;
            Y_Pos = y;
            Width = w;
            Height = h;
            touchscreen = ft;
            Text = txt;
            BGColor = FEZ_Components.FEZTouch.Color.Black;
            TextColor = FEZ_Components.FEZTouch.Color.White;

            if (getBytes == true) 
            {
                touchscreen.FillRectangle(X_Pos, Y_Pos, Width, Height, BGColor);
                labelTextBuffer = getDrawBytes();
            }
            else{
                Draw();
            }
            return labelTextBuffer;
        }

        protected override void Draw()
        {
            touchscreen.FillRectangle(X_Pos, Y_Pos, Width, Height, BGColor);
           // touchscreen.DrawString(Text, X_Pos, Y_Pos, TextColor, BGColor);
            //Änderung
            touchscreen.DrawString(X_Pos, Y_Pos, Text, TextColor, BGColor, myFont);

            //byte[][] labelTextBuffer = touchscreen.getDrawStringBuffer(X_Pos, Y_Pos, Text, TextColor, BGColor, myFont);

            //for (int i = 0; i < labelTextBuffer.Length; i++)
            //{
            //    int x = (labelTextBuffer[i][0] << 8 | labelTextBuffer[i][1] << 0);
            //    int y = (labelTextBuffer[i][2] << 8 | labelTextBuffer[i][3] << 0);
            //    int width = (labelTextBuffer[i][4] << 8 | labelTextBuffer[i][5] << 0);
            //    int height = (labelTextBuffer[i][6] << 8 | labelTextBuffer[i][7] << 0);

            //    // mit offset
            //    touchscreen.writeBuffer(x, y, width, height, labelTextBuffer[i], 8); 
            //}

        }

        // GET DRAW BYtes
        protected byte[][] getDrawBytes() 
        {
            byte[][] labelTextBuffer = touchscreen.getDrawStringBuffer(X_Pos, Y_Pos, Text, TextColor, BGColor, myFont);

            for (int i = 0; i < labelTextBuffer.Length; i++)
            {
                int bx = (labelTextBuffer[i][0] << 8 | labelTextBuffer[i][1] << 0);
                int by = (labelTextBuffer[i][2] << 8 | labelTextBuffer[i][3] << 0);
                int bwidth = (labelTextBuffer[i][4] << 8 | labelTextBuffer[i][5] << 0);
                int bheight = (labelTextBuffer[i][6] << 8 | labelTextBuffer[i][7] << 0);

                // mit offset
                touchscreen.writeBuffer(bx, by, bwidth, bheight, labelTextBuffer[i], 8);
            }

            return labelTextBuffer;
        }
    }

    public class Slider : Control
    {
        public enum eOrientation
        {
            Horizontal,
            Vertical
        }

        const int HandleSize = 20;
        int line_size;
        int value_range;

        int Size;

        public int MinValue { get; private set; }
        public int MaxValue { get; private set; }
        public eOrientation Orientation { get; private set; }

        int current_val;
        public int Value
        {
            get { return current_val; }
            set
            {
                if (value < MinValue || value > MaxValue)
                    throw new Exception("Slider value out of range");

                current_val = value;
                if (Orientation == eOrientation.Horizontal)
                {
                    current_pos = X_Pos + (int)(((float)(current_val - MinValue) / (float)value_range) * line_size) + (HandleSize / 2);
                }
                else
                {
                    current_pos = (Y_Pos + (HandleSize / 2) + line_size) - (int)(((float)(current_val - MinValue) / (float)value_range) * line_size);
                }
                Draw();
                DoValueChanged(value);
            }
        }

        public FEZ_Components.FEZTouch.Color LineColor;
        public FEZ_Components.FEZTouch.Color HandleColor;

        // Deaktiv für TouchMoveEvent setzten
        public bool isDeaktive = false;
        
        int prev_pos;
        int current_pos;

        public Slider(int x, int y, FEZ_Components.FEZTouch ft)
        {
            Init(x, y, ft, FEZ_Components.FEZTouch.SCREEN_WIDTH, 0, 99, eOrientation.Horizontal);
        }

        public Slider(int x, int y, FEZ_Components.FEZTouch ft, int min, int max)
        {
            Init(x, y, ft, FEZ_Components.FEZTouch.SCREEN_WIDTH, min, max, eOrientation.Horizontal);
        }

        public Slider(int x, int y, FEZ_Components.FEZTouch ft, int min, int max, eOrientation orient)
        {
            Init(x, y, ft, 150, min, max, orient);
        }

        public Slider(int x, int y, FEZ_Components.FEZTouch ft, int min, int max, eOrientation orient, int currentValue)
        {
            Init(x, y, ft, 150, min, max, orient, currentValue);
        }

        public Slider(int x, int y, FEZ_Components.FEZTouch ft, int sz, int min, int max, eOrientation orient)
        {
            Init(x, y, ft, sz, min, max, orient);
        }

        public Slider(int x, int y, FEZ_Components.FEZTouch ft, int sz, int min, int max, eOrientation orient, int currentValue)
        {
            Init(x, y, ft, sz, min, max, orient, currentValue);
        }

        public delegate void ValueChangedDelegate(int val);
        public event ValueChangedDelegate ValueChanged;
        internal void DoValueChanged(int val)
        {
            if (ValueChanged != null) ValueChanged(val);
        }

        void Init(int x, int y, FEZ_Components.FEZTouch ft, int sz, int min, int max, eOrientation orient)
        {
            X_Pos = x;
            Y_Pos = y;
            Size = sz;
            line_size = Size - HandleSize;
            MinValue = min;
            MaxValue = max;
            value_range = MaxValue - MinValue;
            touchscreen = ft;
            Orientation = orient;
            Width = orient == eOrientation.Horizontal ? sz : HandleSize;
            Height = orient == eOrientation.Horizontal ? HandleSize : sz;
            //Value = MinValue;
            LineColor = FEZ_Components.FEZTouch.Color.White;
            HandleColor = FEZ_Components.FEZTouch.Color.Red;
            BGColor = FEZ_Components.FEZTouch.Color.Black;

            // Set slider to minimum position
            current_val = MinValue;
            if (Orientation == eOrientation.Horizontal)
            {
                current_pos = X_Pos + (int)(((float)(current_val - MinValue) / (float)value_range) * line_size) + (HandleSize / 2);
                prev_pos = current_pos;
            }
            else
            {
                current_pos = (Y_Pos + (HandleSize / 2) + line_size) - (int)(((float)(current_val - MinValue) / (float)value_range) * line_size);
                prev_pos = current_pos;
            }

            Draw();

            // Event
            try
            {
                touchscreen.TouchMoveEvent -= new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchMoveEvent);
            }
            catch (Exception ex) { }
            finally
            {
                touchscreen.TouchMoveEvent += new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchMoveEvent);
            }

        }

        // Custom ohne Draw und Events
        void Init(int x, int y, FEZ_Components.FEZTouch ft, int sz, int min, int max, eOrientation orient, int currentValue)
        {
            X_Pos = x;
            Y_Pos = y;
            Size = sz;
            line_size = Size - HandleSize;
            MinValue = min;
            MaxValue = max;
            value_range = MaxValue - MinValue;
            touchscreen = ft;
            Orientation = orient;
            Width = orient == eOrientation.Horizontal ? sz : HandleSize;
            Height = orient == eOrientation.Horizontal ? HandleSize : sz;
            //Value = MinValue;
            LineColor = FEZ_Components.FEZTouch.Color.White;
            HandleColor = FEZ_Components.FEZTouch.Color.Red;
            BGColor = FEZ_Components.FEZTouch.Color.Black;

            // Set slider to minimum position
            current_val = currentValue;
            if (Orientation == eOrientation.Horizontal)
            {
                current_pos = X_Pos + (int)(((float)(current_val - MinValue) / (float)value_range) * line_size) + (HandleSize / 2);
                prev_pos = current_pos;
            }
            else
            {
                current_pos = (Y_Pos + (HandleSize / 2) + line_size) - (int)(((float)(current_val - MinValue) / (float)value_range) * line_size);
                prev_pos = current_pos;
            }

            //Draw();

            // Event
            //try
            //{
            //    touchscreen.TouchMoveEvent -= new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchMoveEvent);
            //}              
            //catch (Exception ex) { }
            //finally
            //{
            //    touchscreen.TouchMoveEvent += new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchMoveEvent);
            //}
        }

        // Custom
        public void DrawSlider()
        {
            if (Orientation == eOrientation.Horizontal)
            {
                touchscreen.FillRectangle(prev_pos - (HandleSize / 2), Y_Pos, HandleSize, HandleSize, BGColor); // Clear old handle position
                touchscreen.FillRectangle(X_Pos + (HandleSize / 2), Y_Pos + (HandleSize / 2), Size - HandleSize, 1, LineColor); // Draw line
                touchscreen.FillRectangle(current_pos - (HandleSize / 2), Y_Pos, HandleSize, HandleSize, HandleColor);  // Draw new handle position
                prev_pos = current_pos;         // Save for clearing, next time we draw
            }
            else
            {
                touchscreen.FillRectangle(X_Pos, prev_pos - (HandleSize / 2), HandleSize, HandleSize, BGColor);
                touchscreen.FillRectangle(X_Pos + (HandleSize / 2), Y_Pos + (HandleSize / 2), 1, Size - HandleSize, LineColor);
                touchscreen.FillRectangle(X_Pos, current_pos - (HandleSize / 2), HandleSize, HandleSize, HandleColor);
                prev_pos = current_pos;         // Save for clearing, next time we draw
            }
        }

        // Custom Eventhandler
        public void AddEventhandler() 
        {
            try
            {
                touchscreen.TouchMoveEvent -= new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchMoveEvent);
            }              
            catch (Exception ex) { }
            finally
            {
                touchscreen.TouchMoveEvent += new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchMoveEvent);
            }
        }
        public void ClearEventhandler()
        {
            try
            {
                touchscreen.TouchMoveEvent -= new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchMoveEvent);
            }
            catch (Exception ex) { }
        }



        protected override void Draw()
        {
            if (Orientation == eOrientation.Horizontal)
            {
                touchscreen.FillRectangle(prev_pos - (HandleSize / 2), Y_Pos, HandleSize, HandleSize, BGColor); // Clear old handle position
                touchscreen.FillRectangle(X_Pos + (HandleSize / 2), Y_Pos + (HandleSize / 2), Size - HandleSize, 1, LineColor); // Draw line
                touchscreen.FillRectangle(current_pos - (HandleSize / 2), Y_Pos, HandleSize, HandleSize, HandleColor);  // Draw new handle position
                prev_pos = current_pos;         // Save for clearing, next time we draw
            }
            else
            {
                touchscreen.FillRectangle(X_Pos, prev_pos - (HandleSize / 2), HandleSize, HandleSize, BGColor);
                touchscreen.FillRectangle(X_Pos + (HandleSize / 2), Y_Pos + (HandleSize / 2), 1, Size - HandleSize, LineColor); 
                touchscreen.FillRectangle(X_Pos, current_pos - (HandleSize / 2), HandleSize, HandleSize, HandleColor);
                prev_pos = current_pos;         // Save for clearing, next time we draw
            }
        }

        void touchscreen_TouchMoveEvent(int x, int y)
        {
            if (isDeaktive == true) {
                return;
            }

            int pos_delta;      // The number of pixels we are from the pixel position that represents MinValue

            if (IsPointInControl(x, y))
            {
                if (Orientation == eOrientation.Horizontal)
                {
                    // Handle edge cases
                    if (x < (X_Pos + (HandleSize / 2)))
                        current_pos = X_Pos + (HandleSize / 2);
                    else if (x > ((X_Pos + Size) - (HandleSize / 2)))
                        current_pos = (X_Pos + Size) - (HandleSize / 2);
                    else
                        current_pos = x;

                    pos_delta = current_pos - X_Pos - (HandleSize / 2);

                    Value = MinValue + (int)((float)value_range * ((float)pos_delta / (float)line_size));
                }
                else
                {
                    // Handle edge cases
                    if (y < (Y_Pos + (HandleSize / 2)))
                        current_pos = Y_Pos + (HandleSize / 2);
                    else if (y > ((Y_Pos + Size) - (HandleSize / 2)))
                        current_pos = Y_Pos + Size - (HandleSize / 2);
                    else
                        current_pos = y;

                    pos_delta = line_size - ((current_pos - Y_Pos) - (HandleSize / 2));

                    Value = MinValue + (int)((float)value_range * ((float)pos_delta / (float)line_size));
                }
            }
        }

        //protected override Boolean IsPointInControl(int x, int y)
        //{
        //    int extra_space = 20;
        //    int y1_w_extra;
        //    int y2_w_extra;
        //    int x1_w_extra;
        //    int x2_w_extra;

        //    if (Orientation == eOrientation.Horizontal)
        //    {
        //        x1_w_extra = X_Pos - extra_space;
        //        if (x1_w_extra < 0) x1_w_extra = 0;
        //        x2_w_extra = X_Pos + Size + extra_space;
        //        if (x2_w_extra > FEZ_Components.FEZTouch.ScreenWidth) x2_w_extra = FEZ_Components.FEZTouch.ScreenWidth;

        //        if ((x >= x1_w_extra) &&
        //            (x <= x2_w_extra) &&
        //            (y >= Y_Pos) &&
        //            (y <= (Y_Pos + HandleSize)))
        //        {
        //            return true;
        //        }
        //        return false;
        //    }
        //    else
        //    {
        //        y1_w_extra = Y_Pos - extra_space;
        //        if (y1_w_extra < 0) y1_w_extra = 0;
        //        y2_w_extra = Y_Pos + Size + extra_space;
        //        if (y2_w_extra > FEZ_Components.FEZTouch.ScreenHeight) y2_w_extra = FEZ_Components.FEZTouch.ScreenHeight;

        //        if ((x >= X_Pos) &&
        //            (x <= (X_Pos + HandleSize)) &&
        //            (y >= y1_w_extra) &&
        //            (y <= y2_w_extra))
        //        {
        //            return true;
        //        }
        //        return false;
        //    }
        //}
    }

    // NEW LevelMeteScalar
    public class LevelMeterScalar : Control
    {
        public enum eOrientation
        {
            Horizontal,
            Vertical
        }
        public enum textSide
        {
            left,
            right
        }
        
        int Size;

        public int MinValue { get; private set; }
        public int MaxValue { get; private set; }
        public eOrientation Orientation { get; private set; }
        public textSide TextSide { get; private set; }

        public FEZ_Components.FEZTouch.Color LineColor;
        public FEZ_Components.FEZTouch.Color TextColor;

        // für schnelle Text Ausgabe        
        byte[][] drawData30 = new byte[0][];
        byte[][] drawData20 = new byte[0][];
        byte[][] drawData10 = new byte[0][];
        byte[][] drawData00 = new byte[0][];
        byte[][] drawData_10 = new byte[0][];
        byte[][] drawData_20 = new byte[0][];
        byte[][] drawData_30 = new byte[0][];
        byte[][] drawData_40 = new byte[0][];
        byte[][] drawData_50 = new byte[0][];
        byte[][] drawData_60 = new byte[0][]; 
        byte[][] drawData_70 = new byte[0][];
        byte[][] drawData_80 = new byte[0][];
        
        byte[] drawRec30 = new byte[0];
        byte[] drawRec20 = new byte[0];
        byte[] drawRec10 = new byte[0];
        byte[] drawRec00 = new byte[0];
        byte[] drawRec_10 = new byte[0];
        byte[] drawRec_20 = new byte[0];
        byte[] drawRec_30 = new byte[0];
        byte[] drawRec_40 = new byte[0];
        byte[] drawRec_50 = new byte[0];
        byte[] drawRec_60 = new byte[0];
        byte[] drawRec_70 = new byte[0];
        byte[] drawRec_80 = new byte[0];

        bool isDrawed = false;
        bool isBuffered = false;

        // Konstructor
        public LevelMeterScalar(int x, int y, FEZ_Components.FEZTouch ft, int min, int max, eOrientation orient, textSide sideText)
        {
            Init(x, y, ft, 150, min, max, orient, sideText);
        }

        public LevelMeterScalar(int x, int y, FEZ_Components.FEZTouch ft, int min, int max, eOrientation orient, textSide sideText, bool buffer)
        {
            isBuffered = buffer;
            Init(x, y, ft, 150, min, max, orient, sideText);
        }

        public LevelMeterScalar(int x, int y, FEZ_Components.FEZTouch ft, int sz, int min, int max, eOrientation orient, textSide sideText)
        {
            Init(x, y, ft, sz, min, max, orient, sideText);
        }

        // Show
        void Init(int x, int y, FEZ_Components.FEZTouch ft, int sz, int min, int max, eOrientation orient, textSide side)
        {
            X_Pos = x;
            Y_Pos = y;
            Size = sz;
            MinValue = min;
            MaxValue = max;
            touchscreen = ft;
            Orientation = orient;
            TextSide = side;
            LineColor = FEZ_Components.FEZTouch.Color.White;
            TextColor = FEZ_Components.FEZTouch.Color.White;
            BGColor = FEZ_Components.FEZTouch.Color.Black;

            Draw();
        }

        public void showLevelMeterScalar() 
        {
            Draw();            
        }

        protected override void Draw()
        {
            double range=0;
            double steps=0;
            int textOffsetLR=0;
            if (isDrawed != true)
            {
                range = System.Math.Abs(MinValue) + System.Math.Abs(MaxValue);
                steps = Size / range;

                // Prüfung ob Link oder rechts
                textOffsetLR = 6;
                if (this.TextSide == textSide.left)
                {
                    textOffsetLR = -19;
                }
            }                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                     
            if (Orientation == eOrientation.Horizontal)
            {
                // TODO
            }
            else 
            {
                if (isDrawed == false)
                {
                    // Draw Max Val
                    //touchscreen.FillRectangle(X_Pos, Y_Pos+1, 8, 1, LineColor);
                    // Draw Min Val
                    //touchscreen.FillRectangle(X_Pos, Y_Pos+Size+1, 8, 1, LineColor);

                    // Zählt die normalen Nummern
                    int numCounter = MaxValue;
                    int lastTextPost = 0;
                    int indexCounter = 0;
                    // geht von oben nach unter X-Pos
                    for (double i = 0; i < Size; i = i + steps)
                    {
                        switch (numCounter)
                        {
                            case 30:
                            case 20:
                            case 10:
                            case 0:
                            case -10:
                            case -20:
                            case -30:
                            case -40:
                            case -50:
                            case -60:
                            case -70:
                            case -80:
                                int shift = (Int32)System.Math.Round(i);

                                if (isBuffered == false) 
                                {
                                    // Marker
                                    touchscreen.FillRectangle(X_Pos, Y_Pos + 1 + shift, 4, 1, LineColor);
                                    // Text als Nummer
                                    Label l = new Label(X_Pos + textOffsetLR, Y_Pos + 1 + shift - (myFont.Height / 2), this.touchscreen, 10 , 10, System.Math.Abs(numCounter).ToString());
                                }
                                else
                                {
                                    byte[] recBuffer = this.touchscreen.getFillRectangleBuffer(X_Pos, Y_Pos + 1 + shift, 4, 1, LineColor);
                                    // Buffer der Number
                                    byte[][] labelTextBuffer = this.touchscreen.getDrawStringBuffer(X_Pos + textOffsetLR, Y_Pos + 1 + shift - (myFont.Height / 2), System.Math.Abs(numCounter).ToString(), TextColor, BGColor, myFont);
                                
                                    isDrawed = true;
                                    switch (numCounter)
                                    {
                                        case 30:                                       
                                            drawData30 = labelTextBuffer;
                                            drawRec30 = recBuffer;
                                            break;
                                        case 20:
                                            drawData20 = labelTextBuffer;
                                            drawRec20 = recBuffer;
                                            break;
                                        case 10:                                        
                                            drawData10 = labelTextBuffer;
                                            drawRec10 = recBuffer;
                                            break;
                                        case 0:                                      
                                            drawData00 = labelTextBuffer;
                                            drawRec00 = recBuffer;
                                            break;
                                        case -10:                                        
                                            drawData_10 = labelTextBuffer;
                                            drawRec_10 = recBuffer;
                                            break;
                                        case -20:                                        
                                            drawData_20 = labelTextBuffer;
                                            drawRec_20 = recBuffer;
                                            break;
                                        case -30:                                        
                                            drawData_30 = labelTextBuffer;
                                            drawRec_30 = recBuffer;
                                            break;
                                        case -40:                                        
                                            drawData_40 = labelTextBuffer;
                                            drawRec_40 = recBuffer;
                                            break;
                                        case -50:                                        
                                            drawData_50 = labelTextBuffer;
                                            drawRec_50 = recBuffer;
                                            break;
                                        case -60:                                        
                                            drawData_60 = labelTextBuffer;
                                            drawRec_60 = recBuffer;
                                            break;
                                        case -70:                                        
                                            drawData_70 = labelTextBuffer;
                                            drawRec_70 = recBuffer;
                                            break;
                                        case -80:                                        
                                            drawData_80 = labelTextBuffer;
                                            drawRec_80 = recBuffer;
                                            break;
                                    }
                                    lastTextPost = shift;
                                    indexCounter++;
                                }
                                break;                                
                        }
                        numCounter--;
                    }
                }
                else if (isBuffered != false)
                {
                    DrawBuffer(drawRec30);
                    DrawBuffer(drawData30);

                    DrawBuffer(drawRec20);
                    DrawBuffer(drawData20);

                    DrawBuffer(drawRec10);
                    DrawBuffer(drawData10);

                    DrawBuffer(drawRec00);
                    DrawBuffer(drawData00);

                    DrawBuffer(drawRec_10);
                    DrawBuffer(drawData_10);

                    DrawBuffer(drawRec_20);
                    DrawBuffer(drawData_20);

                    DrawBuffer(drawRec_30);
                    DrawBuffer(drawData_30);

                    DrawBuffer(drawRec_40);
                    DrawBuffer(drawData_40);

                    DrawBuffer(drawRec_50);
                    DrawBuffer(drawData_50);

                    DrawBuffer(drawRec_60);
                    DrawBuffer(drawData_60);

                    DrawBuffer(drawRec_70);
                    DrawBuffer(drawData_70);

                    DrawBuffer(drawRec_80);
                    DrawBuffer(drawData_80);
                }
                // Line
                // touchscreen.FillRectangle(X_Pos, Y_Pos, 1, Size, LineColor);                
            }        
        }

        // Draw Text Buffer
        protected void DrawBuffer(byte[][] drawData) 
        {
            // jedes Zeichen
            for (int i = 0; i < drawData.Length; i++)
            {
                int x = (drawData[i][0] << 8 | drawData[i][1] << 0);
                int y = (drawData[i][2] << 8 | drawData[i][3] << 0);
                int width = (drawData[i][4] << 8 | drawData[i][5] << 0);
                int height = (drawData[i][6] << 8 | drawData[i][7] << 0);

                // mit offset
                touchscreen.writeBuffer(x, y, width, height, drawData[i], 8);
            }
        }

        // Draw Rectangle Buffer
        protected void DrawBuffer(byte[] drawData)
        {
            if (drawData.Length > 0)
            {
                int x = (drawData[0] << 8 | drawData[1] << 0);
                int y = (drawData[2] << 8 | drawData[3] << 0);
                int width = (drawData[4] << 8 | drawData[5] << 0);
                int height = (drawData[6] << 8 | drawData[7] << 0);

                // mit offset
                touchscreen.writeBuffer(x, y, width, height, drawData, 8);
            }
        }

    }


    // NEW LevelMeter
    public class LevelMeter : Control
    {
        public enum eOrientation
        {
            Horizontal,
            Vertical
        }

        bool isDrawed = false;
        int HandleSize = 20;
        const int HandleHight = 2;
        int line_size;
        int value_range;       
        int Size;

        // für schnellere Display Ausgabe
        byte[] handelDrawImage;
        byte[] handelClearImage;
        int handelPosX = 0;
        int handelPosY = 0;
        int handelWidth = 0;
        int handelHeight = 0;


        public int MinValue { get; private set; }
        public int MaxValue { get; private set; }
        public eOrientation Orientation { get; private set; }
        public int getSize() { return Size; }

        int current_val;
        public int Value
        {
            get { return current_val; }
            set
            {
                if (value < MinValue || value > MaxValue)
                    throw new Exception("LevelMeter value out of range");

                current_val = value;
                if (Orientation == eOrientation.Horizontal)
                {
                    current_pos = X_Pos + (int)(((float)(current_val - MinValue) / (float)value_range) * line_size) + (HandleSize / 2);
                }
                else
                {
                    current_pos = (Y_Pos + (HandleHight / 2) + line_size) - (int)(((float)(current_val - MinValue) / (float)value_range) * line_size);
                }
                Draw();
                DoValueChanged(value);
            }
        }

        // Colors
        public FEZ_Components.FEZTouch.Color LineColor = FEZ_Components.FEZTouch.Color.White;
        public FEZ_Components.FEZTouch.Color HandleColor = FEZ_Components.FEZTouch.Color.Red;
        public FEZ_Components.FEZTouch.Color BGColor = FEZ_Components.FEZTouch.Color.Black;
        
        int current_pos;
        int prev_pos;
        public LevelMeter(int x, int y, FEZ_Components.FEZTouch ft)
        {
            Init(x, y, ft, FEZ_Components.FEZTouch.SCREEN_WIDTH, 0, 99, eOrientation.Horizontal);
        }

        public LevelMeter(int x, int y, FEZ_Components.FEZTouch ft, int min, int max)
        {
            Init(x, y, ft, FEZ_Components.FEZTouch.SCREEN_WIDTH, min, max, eOrientation.Horizontal);
        }

        public LevelMeter(int x, int y, FEZ_Components.FEZTouch ft, int min, int max, eOrientation orient)
        {
            Init(x, y, ft, 150, min, max, orient);
        }

        public LevelMeter(int x, int y, FEZ_Components.FEZTouch ft, int min, int max, eOrientation orient, int handleSize, FEZ_Components.FEZTouch.Color handleColor, FEZ_Components.FEZTouch.Color borderColor )
        {
            this.HandleSize = handleSize;
            this.HandleColor = borderColor; // Vertauscht aber egal
            this.LineColor = handleColor;
            Init(x, y, ft, 150, min, max, orient);
        }

        public LevelMeter(int x, int y, FEZ_Components.FEZTouch ft, int sz, int min, int max, eOrientation orient)
        {
            Init(x, y, ft, sz, min, max, orient);
        }

        // Delegate
        public delegate void ValueChangedDelegate(int val);
        public event ValueChangedDelegate ValueChanged;
        internal void DoValueChanged(int val)
        {
            if (ValueChanged != null) ValueChanged(val);
        }

        void Init(int x, int y, FEZ_Components.FEZTouch ft, int sz, int min, int max, eOrientation orient)
        {
            X_Pos = x;
            Y_Pos = y;
            Size = sz;
            line_size = Size - HandleHight;
            MinValue = min;
            MaxValue = max;
            value_range = MaxValue - MinValue;
            touchscreen = ft;
            Orientation = orient;
            Width = orient == eOrientation.Horizontal ? sz : HandleSize;
            Height = orient == eOrientation.Horizontal ? HandleSize : sz;
            //Value = MinValue;
        
            // Set slider to minimum position
            current_val = MinValue;
            if (Orientation == eOrientation.Horizontal)
            {
                current_pos = X_Pos + (int)(((float)(current_val - MinValue) / (float)value_range) * line_size) + (HandleHight);
                prev_pos = current_pos;
            }
            else
            {
                current_pos = (Y_Pos + (HandleHight) + line_size) - (int)(((float)(current_val - MinValue) / (float)value_range) * line_size);
                prev_pos = current_pos;
            }

            Draw();

            // Eventhandler, wird benötigt
            //try
            //{
            //    touchscreen.TouchMoveEvent -= new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchMoveEvent);
            //}
            //catch (Exception ex) { }
            //finally
            //{
            //    touchscreen.TouchMoveEvent += new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchMoveEvent);
            //}
        }

        protected override void Draw()
        {
            if (Orientation == eOrientation.Horizontal)
            {
                // TODO

                //touchscreen.FillRectangle(prev_pos - (HandleSize / 2), Y_Pos, HandleSize, HandleSize, BGColor);                 // Clear old handle position
                //touchscreen.FillRectangle(X_Pos + (HandleSize / 2), Y_Pos + (HandleSize / 2), Size - HandleSize, 1, LineColor); // Draw line
                //touchscreen.FillRectangle(current_pos - (HandleSize / 2), Y_Pos, HandleSize, HandleSize, HandleColor);          // Draw new handle position
                //prev_pos = current_pos;         // Save for clearing, next time we draw
            }
            else
            {
                // Lines
                // Muss nur einmal gezeichnet werden
                if (isDrawed == false)
                {                    
                    touchscreen.FillRectangle(X_Pos - 1, Y_Pos + 1, 1, Size + 1, LineColor);                // Line Left                    
                    touchscreen.FillRectangle(X_Pos + HandleSize + 1, Y_Pos + 1, 1, Size + 1, LineColor);   // Right

                    touchscreen.FillRectangle(X_Pos, Y_Pos, HandleSize +1, 1, LineColor);                // TOP
                    touchscreen.FillRectangle(X_Pos, Y_Pos + Size + 2, HandleSize+1, 1, LineColor);                // Bottom

                    // set handle data
                    fillHandleData(X_Pos, current_pos - 1, HandleSize, HandleHight, HandleColor, BGColor);


                    //is Drawed setzten
                    isDrawed = true;
                }

                // 1. Clear old handle position
                // touchscreen.FillRectangle(X_Pos, prev_pos - 1, HandleSize, HandleHight, BGColor);

                // 2. New handle Posizion                
                // touchscreen.FillRectangle(X_Pos, current_pos - 1, HandleSize, HandleHight, HandleColor);


                // Nur wenn anderer Wert
                if (current_pos != prev_pos)
                {
                    // 1. Clear handle Position - Write Handel Data, need func: fillHandleData
                    touchscreen.writeBuffer(prev_pos - 1, this.handelPosY, this.handelWidth, this.handelHeight, this.handelClearImage);

                    // 2. New handle Position - Write Handel Data, need func: fillHandleData
                    touchscreen.writeBuffer(current_pos - 1, this.handelPosY, this.handelWidth, this.handelHeight, this.handelDrawImage);
                }
                prev_pos = current_pos;         // Save for clearing, next time we draw
            }
        }

        void touchscreen_TouchMoveEvent(int x, int y)
        {
            int pos_delta;      // The number of pixels we are from the pixel position that represents MinValue

            if (IsPointInControl(x, y))
            {
                if (Orientation == eOrientation.Horizontal)
                {
                    // Handle edge cases
                    if (x < (X_Pos + (HandleSize / 2)))
                        current_pos = X_Pos + (HandleSize / 2);
                    else if (x > ((X_Pos + Size) - (HandleSize / 2)))
                        current_pos = (X_Pos + Size) - (HandleSize / 2);
                    else
                        current_pos = x;

                    pos_delta = current_pos - X_Pos - (HandleSize / 2);

                    Value = MinValue + (int)((float)value_range * ((float)pos_delta / (float)line_size));
                }
                else
                {
                    // Handle edge cases
                    if (y < (Y_Pos + HandleHight))
                        current_pos = Y_Pos + HandleHight;
                    else if (y > ((Y_Pos + Size) - HandleHight ))
                        current_pos = Y_Pos + Size - HandleHight;
                    else
                        current_pos = y;

                    pos_delta = line_size - ((current_pos - Y_Pos) - HandleHight);

                    Value = MinValue + (int)((float)value_range * ((float)pos_delta / (float)line_size));
                }
            }
        }

        // Init handel Buffer
        protected void fillHandleData(int xPos, int yPos, int rectWidth, int rectHeight, GHIElectronics.NETMF.FEZ.FEZ_Components.FEZTouch.Color forColor, GHIElectronics.NETMF.FEZ.FEZ_Components.FEZTouch.Color clearColor) 
        {
            // Buffer
            this.handelDrawImage = new byte[(rectHeight * rectWidth) * 2];    // every pixel is 2 bytes
            this.handelClearImage = new byte[(rectHeight * rectWidth) * 2];    // every pixel is 2 bytes

            int x = 0;
            int y = 0;
            int width = 0;
            int height = 0;            
            switch (touchscreen.getlcdOrientation() )
            {
                case GHIElectronics.NETMF.FEZ.FEZ_Components.FEZTouch.Orientation.Portrait:
                    x = xPos;
                    y = yPos;
                    width = rectWidth;
                    height = rectHeight;
                    break;
                case GHIElectronics.NETMF.FEZ.FEZ_Components.FEZTouch.Orientation.PortraitInverse:
                    x = GHIElectronics.NETMF.FEZ.FEZ_Components.FEZTouch.SCREEN_WIDTH - xPos - rectWidth;
                    y = GHIElectronics.NETMF.FEZ.FEZ_Components.FEZTouch.SCREEN_HEIGHT - yPos - rectHeight;
                    width = rectWidth;
                    height = rectHeight;
                    break;
                case GHIElectronics.NETMF.FEZ.FEZ_Components.FEZTouch.Orientation.Landscape:
                    x = GHIElectronics.NETMF.FEZ.FEZ_Components.FEZTouch.SCREEN_WIDTH - yPos - rectHeight;
                    y = xPos;
                    width = rectHeight;
                    height = rectWidth;
                    break;
                case GHIElectronics.NETMF.FEZ.FEZ_Components.FEZTouch.Orientation.LandscapeInverse:
                    x = yPos;
                    y = GHIElectronics.NETMF.FEZ.FEZ_Components.FEZTouch.SCREEN_HEIGHT - xPos - rectWidth;
                    width = rectHeight;
                    height = rectWidth;
                    break;
            }

            // Save
            this.handelPosX = x;
            this.handelPosY = y;
            this.handelWidth = width;
            this.handelHeight = height;
        

            // Color Pixel
            byte h = (byte)((int)forColor >> 8);
            byte l = (byte)(forColor);

            // fill Front buffer
            // alle 2 Pixel also 4 und keine Line mit i = i + 2
            for (int i = 0; i < this.handelDrawImage.Length; i = i + 4)
            {
                this.handelDrawImage[i] = h;
                this.handelDrawImage[i + 1] = l;
            }

            // Clear Buffer
            h = (byte)((int)clearColor >> 8);
            l = (byte)(clearColor);

            // fill Front buffer
            for (int i = 0; i < this.handelClearImage.Length; i = i + 4)
            {
                this.handelClearImage[i] = h;
                this.handelClearImage[i + 1] = l;
            }

            // Example Draw
            //touchscreen.writeBuffer(x, y, width, height, this.handelDrawImage);
        }
    }




    public class LED : Control
    {
        protected int Radius;

        public FEZ_Components.FEZTouch.Color OnColor;

        public FEZ_Components.FEZTouch.Color OffColor;

        Boolean is_on;
        public Boolean IsOn
        {
            get { return is_on; }
            set { is_on = value; Draw(); }
        }

        public LED(int x, int y, FEZ_Components.FEZTouch ft)
        {
            Init(x, y, 10, ft, ft.ColorFromRGB(255, 0, 0), ft.ColorFromRGB(120, 0, 0), FEZ_Components.FEZTouch.Color.Black);
        }

        public LED(int x, int y, FEZ_Components.FEZTouch ft, FEZ_Components.FEZTouch.Color on, FEZ_Components.FEZTouch.Color off, FEZ_Components.FEZTouch.Color bg)
        {
            Init(x, y, 10, ft, on, off, bg);
        }

        public LED(int x, int y, int r, FEZ_Components.FEZTouch ft, FEZ_Components.FEZTouch.Color on, FEZ_Components.FEZTouch.Color off, FEZ_Components.FEZTouch.Color bg)
        {
            Init(x, y, r, ft, on, off, bg);
        }


        void Init(int x, int y, int r, FEZ_Components.FEZTouch ts, FEZ_Components.FEZTouch.Color on, FEZ_Components.FEZTouch.Color off, FEZ_Components.FEZTouch.Color bg)
        {
            X_Pos = x;
            Y_Pos = y;
            Radius = r;
            touchscreen = ts;
            OnColor = on;
            OffColor = off;
            BGColor = bg;
            is_on = false;

            Draw();
        }

        protected override void Draw()
        {
            // If a FillCircle() method is available, use it to create a circular LED.
            //if (IsOn)
            //    touchscreen.FillCircle(X_Pos, Y_Pos, Radius, OnColor, BGColor);
            //else
            //    touchscreen.FillCircle(X_Pos, Y_Pos, Radius, OffColor, BGColor);

            if (is_on)
                touchscreen.FillRectangle(X_Pos, Y_Pos, (Radius * 2), (Radius * 2), OnColor);
            else
                touchscreen.FillRectangle(X_Pos, Y_Pos, (Radius * 2), (Radius * 2), OffColor);
        }

        public void Toggle()
        {
            is_on = !is_on;
            Draw();
        }
    }

    public class TextArea : Control
    {
        public FEZ_Components.FEZTouch.Color TextColor;

        int cursor_x_pos;
        int cursor_y_pos;
        int num_chars_per_line;
        int num_lines_in_window;
        String[] text_lines;
        int top_text_line_idx;
        int curr_text_insert_idx;
        int num_lines_written;

        public TextArea(int x, int y, int width, int height, FEZ_Components.FEZTouch ft)
        {
            Init(x, y, width, height, ft, FEZ_Components.FEZTouch.Color.Gray, FEZ_Components.FEZTouch.Color.White);
        }

        public TextArea(int x, int y, int w, int h, FEZ_Components.FEZTouch ft, FEZ_Components.FEZTouch.Color bg, FEZ_Components.FEZTouch.Color fg)
        {
            Init(x, y, w, h, ft, bg, fg);
        }

        void Init(int x, int y, int w, int h, FEZ_Components.FEZTouch ft, FEZ_Components.FEZTouch.Color bg, FEZ_Components.FEZTouch.Color fg)
        {
            touchscreen = ft;
            X_Pos = x;
            Y_Pos = y;

            if ((w <= myFont.AverageWidth) || (h <= myFont.Height))
                throw new ArgumentException("TextArea window size too small");

            Width = w;
            Height = h;
            BGColor = bg;
            TextColor = fg;
            cursor_x_pos = X_Pos + 2;
            cursor_y_pos = Y_Pos + 2;
            num_chars_per_line = (Width / myFont.AverageWidth) - 1;  // Subtract 1 to allow for "margins"
            num_lines_in_window = (Height / myFont.Height ) - 1;
            text_lines = new String[num_lines_in_window];
            top_text_line_idx = 0;      // The 1st element in the text_lines array is at the top of the window
            curr_text_insert_idx = 0;
            num_lines_written = 0;

            Clear();
        }

        public void Clear()
        {
            int i;
            top_text_line_idx = 0;
            curr_text_insert_idx = 0;
            num_lines_written = 0;
            for (i = 0; i < text_lines.Length; i++)
            {
                text_lines[i] = null;
            }
            touchscreen.FillRectangle(X_Pos, Y_Pos, Width, Height, BGColor);
        }

        public void WriteLine(String txt)
        {
            int num_lines;
            int i;
            int cur_idx = 0;
            int num_chars_to_write;
            String s;

            num_lines = txt.Length / num_chars_per_line;
            if (txt.Length % num_chars_per_line != 0) num_lines++;

            for (i = 0; i < num_lines; i++)
            {
                if ((txt.Length - cur_idx) <= num_chars_per_line)
                    num_chars_to_write = (txt.Length - cur_idx);
                else
                    num_chars_to_write = num_chars_per_line;

                s = txt.Substring(cur_idx, num_chars_to_write);
                text_lines[curr_text_insert_idx] = s;

                if (curr_text_insert_idx == (text_lines.Length - 1))
                    curr_text_insert_idx = 0;
                else
                    curr_text_insert_idx++;

                cur_idx += num_chars_to_write;
            }
            num_lines_written += num_lines;

            Draw();

            if (num_lines_written >= num_lines_in_window)
                top_text_line_idx = curr_text_insert_idx + 1;
        }

        protected override void Draw()
        {
            int i;
            int curr_line_num = 0;
            Object lock_obj = new Object();

            lock (lock_obj)
            {
                if (num_lines_written > num_lines_in_window)
                {
                    touchscreen.FillRectangle(X_Pos, Y_Pos, Width, Height, BGColor);
                }

                if (text_lines != null)
                {
                    for (i = top_text_line_idx; i < text_lines.Length; i++)
                    {
                        if ((text_lines[i] != null) && (text_lines[i] != String.Empty))
                        {
                            touchscreen.DrawString(                                 
                                X_Pos, 
                                Y_Pos + ((i - top_text_line_idx) * myFont.Height),
                                text_lines[i],
                                TextColor, 
                                BGColor,
                                myFont);

                            curr_line_num++;
                        }
                    }
                }
                for (i = 0; i < top_text_line_idx; i++)
                {
                    if ((text_lines[i] != null) && (text_lines[i] != String.Empty))
                    {
                        touchscreen.DrawString(                            
                            X_Pos, 
                            Y_Pos + ((i + curr_line_num) * myFont.Height),
                            text_lines[i], 
                            TextColor, 
                            BGColor,
                            myFont);
                    }
                }
            }
        }
    }

    public class StateButton : Control
    {
        public enum eCurrState
        {
            state1,
            state2
        };

        public FEZ_Components.FEZTouch.Color State1Color;

        public FEZ_Components.FEZTouch.Color State2Color;

        public FEZ_Components.FEZTouch.Color Text1Color;

        public FEZ_Components.FEZTouch.Color Text2Color;

        eCurrState curr_state = eCurrState.state1;

        String state1_text;

        public String State1Text
        {
            get { return state1_text; }
            set
            {
                int text_width = value.Length * myFont.AverageWidth;

                if (text_width > Width)
                    throw new ArgumentException();

                state1_text = value;
                touchscreen.DrawString(
                    X_Pos + (Width - text_width) / 2, 
                    Y_Pos + (Height - myFont.Height) / 2,
                    value, 
                    Text1Color, 
                    State1Color,
                    myFont);
            }
        }

        String state2_text;

        public String State2Text
        {
            get { return state2_text; }
            set
            {
                int text_width = value.Length * myFont.AverageWidth;

                if (text_width > Width)
                    throw new ArgumentException();

                state2_text = value;
                touchscreen.DrawString( 
                    X_Pos + (Width - text_width) / 2, 
                    Y_Pos + (Height - myFont.Height) / 2,
                    value,
                    Text2Color, 
                    State2Color,
                    myFont);
            }
        }

        public delegate void State1ActiveDelegate(eCurrState new_state);

        public event State1ActiveDelegate StateChanged;

        internal void DoStateChanged(eCurrState new_state)
        {
            if (StateChanged != null) StateChanged(new_state);
        }

        public StateButton(int x, int y, FEZ_Components.FEZTouch ft, int width, int height, String t1, String t2)
        {
            Init(x, y, ft, width, height, FEZ_Components.FEZTouch.Color.Gray, FEZ_Components.FEZTouch.Color.Red, FEZ_Components.FEZTouch.Color.White, FEZ_Components.FEZTouch.Color.White, t1, t2);
        }

        // Cusrtom
        public StateButton(int x, int y, FEZ_Components.FEZTouch ft, int width, int height, String t1, String t2, bool active)
        {
            if (active == true)
            {
                this.curr_state = eCurrState.state2;
            }
            else 
            {
                this.curr_state = eCurrState.state1;
            }
            InitCustom(x, y, ft, width, height, FEZ_Components.FEZTouch.Color.Gray, FEZ_Components.FEZTouch.Color.Red, FEZ_Components.FEZTouch.Color.White, FEZ_Components.FEZTouch.Color.White, t1, t2);
        }


        public StateButton(int x, int y, FEZ_Components.FEZTouch ft, String t1, String t2)
        {
            Init(x, y, ft, 50, 50, FEZ_Components.FEZTouch.Color.Gray, FEZ_Components.FEZTouch.Color.Red, FEZ_Components.FEZTouch.Color.White, FEZ_Components.FEZTouch.Color.White, t1, t2);
        }

        void Init(int x, int y, FEZ_Components.FEZTouch ft, int w, int h,
                  FEZ_Components.FEZTouch.Color st1_c, FEZ_Components.FEZTouch.Color st2_c, FEZ_Components.FEZTouch.Color t1_c, FEZ_Components.FEZTouch.Color t2_c,
                  String text1, String text2)
        {
            touchscreen = ft;
            X_Pos = x;
            Y_Pos = y;
            Width = w;
            Height = h;
            State1Color = st1_c;
            State2Color = st2_c;
            Text1Color = t1_c;
            Text2Color = t2_c;
            touchscreen.FillRectangle(X_Pos, Y_Pos, Width, Height, State1Color);
            state1_text = text1;
            state2_text = text2;
            touchscreen.DrawString(                 
                X_Pos + (Width - (state1_text.Length * myFont.AverageWidth)) / 2, 
                Y_Pos + (Height - myFont.Height) / 2,
                state1_text,
                Text1Color, 
                State1Color,
                myFont
                );

            touchscreen.TouchDownEvent += new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchDownEvent);
        }

        // Custom
        void InitCustom(int x, int y, FEZ_Components.FEZTouch ft, int w, int h,
          FEZ_Components.FEZTouch.Color st1_c, FEZ_Components.FEZTouch.Color st2_c, FEZ_Components.FEZTouch.Color t1_c, FEZ_Components.FEZTouch.Color t2_c,
          String text1, String text2)
        {
            touchscreen = ft;
            X_Pos = x;
            Y_Pos = y;
            Width = w;
            Height = h;
            State1Color = st1_c;
            State2Color = st2_c;
            Text1Color = t1_c;
            Text2Color = t2_c;
            //touchscreen.FillRectangle(X_Pos, Y_Pos, Width, Height, State1Color);
            state1_text = text1;
            state2_text = text2;
            //touchscreen.DrawString(
            //    X_Pos + (Width - (state1_text.Length * myFont.AverageWidth)) / 2,
            //    Y_Pos + (Height - myFont.Height) / 2,
            //    state1_text,
            //    Text1Color,
            //    State1Color,
            //    myFont
            //    );

            //touchscreen.TouchDownEvent += new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchDownEvent);
        }

        public void DrawStateButton() 
        {
            if (this.curr_state == eCurrState.state1)
            {
                touchscreen.FillRectangle(X_Pos, Y_Pos, Width, Height, State1Color);
                touchscreen.DrawString(
                    X_Pos + (Width - (state1_text.Length * myFont.AverageWidth)) / 2,
                    Y_Pos + (Height - myFont.Height) / 2,
                    state1_text,
                    Text1Color,
                    State1Color,
                    myFont
                    );
            }
            else 
            {
                touchscreen.FillRectangle(X_Pos, Y_Pos, Width, Height, State2Color);
                touchscreen.DrawString(
                    X_Pos + (Width - (state2_text.Length * myFont.AverageWidth)) / 2,
                    Y_Pos + (Height - myFont.Height) / 2,
                    state2_text,
                    Text2Color,
                    State2Color,
                    myFont
                    );
            }
        }

        public void AddEventhandler()
        {
            try
            {
                touchscreen.TouchDownEvent -= new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchDownEvent);
            }
            catch (Exception ex) { }
            finally {
                touchscreen.TouchDownEvent += new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchDownEvent);
            }
        }
        public void ClearEventhandler() 
        {
            try
            {
                touchscreen.TouchDownEvent -= new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchDownEvent);
            }
            catch (Exception ex) { }
        }



        void touchscreen_TouchDownEvent(int x, int y)
        {
            FEZ_Components.FEZTouch.Color new_btn_color;
            FEZ_Components.FEZTouch.Color new_txt_color;
            String new_text;

            if (IsPointInControl(x, y) == true)
            {
                if (curr_state == eCurrState.state1)
                {
                    new_btn_color = State2Color;
                    new_txt_color = Text2Color;
                    new_text = state2_text;
                    curr_state = eCurrState.state2;
                }
                else
                {
                    new_btn_color = State1Color;
                    new_txt_color = Text1Color;
                    new_text = state1_text;
                    curr_state = eCurrState.state1;
                }
                touchscreen.FillRectangle(X_Pos, Y_Pos, Width, Height, new_btn_color);
                touchscreen.DrawString(
                    X_Pos + (Width - (new_text.Length * myFont.AverageWidth)) / 2, 
                    Y_Pos + (Height - myFont.Height) / 2,
                    new_text, new_txt_color, new_btn_color, myFont);

                DoStateChanged(curr_state);
            }
        }
    }

    public class MomentaryButton : Control
    {
        public FEZ_Components.FEZTouch.Color NotPressedColor;

        public FEZ_Components.FEZTouch.Color PressedColor;

        public FEZ_Components.FEZTouch.Color TextColor;

        String button_text;
        public bool isDeaktive = false;

        public String Text
        {
            get { return button_text; }
            set
            {
                int text_width = value.Length * myFont.AverageWidth;

                if (text_width > Width)
                    throw new ArgumentException("Label width too long");

                button_text = value;
                touchscreen.DrawString(
                    X_Pos + (Width - text_width) / 2, 
                    Y_Pos + (Height - myFont.Height) / 2,
                    value,
                    TextColor, 
                    NotPressedColor,
                    myFont);
            }
        }

        public delegate void ButtonPressedDelegate();

        public event ButtonPressedDelegate Pressed;

        internal void DoButtonPressed()
        {
            if (Pressed != null) Pressed();
        }

        public delegate void ButtonReleasedDelegate();

        public event ButtonReleasedDelegate Released;

        internal void DoButtonReleased()
        {
            if (Released != null) Released();
        }

        public MomentaryButton(int x, int y, FEZ_Components.FEZTouch ft, int width, int height, String txt)
        {
            Init(x, y, ft, width, height, FEZ_Components.FEZTouch.Color.Gray, FEZ_Components.FEZTouch.Color.Red, txt);
        }

        // Custom
        public MomentaryButton(int x, int y, FEZ_Components.FEZTouch ft, int width, int height, String txt, bool active)
        {
            InitCustom(x, y, ft, width, height, FEZ_Components.FEZTouch.Color.Gray, FEZ_Components.FEZTouch.Color.Red, txt);
        }

        public MomentaryButton(int x, int y, FEZ_Components.FEZTouch ft, int width, int height, FEZ_Components.FEZTouch.Color pc, FEZ_Components.FEZTouch.Color rc, String txt)
        {
            Init(x, y, ft, width, height, pc, rc, txt);
        }

        // Custom
        public MomentaryButton(int x, int y, FEZ_Components.FEZTouch ft, int width, int height, FEZ_Components.FEZTouch.Color pc, FEZ_Components.FEZTouch.Color rc, String txt, bool active)
        {
            InitCustom(x, y, ft, width, height, pc, rc, txt);
        }

        public MomentaryButton(int x, int y, FEZ_Components.FEZTouch ft, String txt)
        {
            Init(x, y, ft, 50, 50, FEZ_Components.FEZTouch.Color.Gray, FEZ_Components.FEZTouch.Color.Red, txt);
        }

        void Init(int x, int y, FEZ_Components.FEZTouch ft, int w, int h, FEZ_Components.FEZTouch.Color pc, FEZ_Components.FEZTouch.Color rc, String txt)
        {
            touchscreen = ft;
            X_Pos = x;
            Y_Pos = y;
            Width = w;
            Height = h;
            PressedColor = pc;
            NotPressedColor = rc;
            TextColor = FEZ_Components.FEZTouch.Color.White;
            touchscreen.FillRectangle(X_Pos, Y_Pos, Width, Height, NotPressedColor);
            Text = txt;
            touchscreen.DrawString(                 
                X_Pos + (Width - (Text.Length * myFont.AverageWidth)) / 2, 
                Y_Pos + (Height - myFont.Height) / 2,
                Text,
                TextColor, 
                NotPressedColor,
                myFont);

            touchscreen.TouchDownEvent += new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchDownEvent);
            touchscreen.TouchUpEvent += new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchUpEvent);
        }

        // Custom
        void InitCustom(int x, int y, FEZ_Components.FEZTouch ft, int w, int h, FEZ_Components.FEZTouch.Color pc, FEZ_Components.FEZTouch.Color rc, String txt)
        {
            touchscreen = ft;
            X_Pos = x;
            Y_Pos = y;
            Width = w;
            Height = h;
            PressedColor = pc;
            NotPressedColor = rc;
            TextColor = FEZ_Components.FEZTouch.Color.White;
            btnText = txt;
            //touchscreen.FillRectangle(X_Pos, Y_Pos, Width, Height, NotPressedColor);
            //Text = txt;
            //touchscreen.DrawString(
            //    X_Pos + (Width - (Text.Length * myFont.AverageWidth)) / 2,
            //    Y_Pos + (Height - myFont.Height) / 2,
            //    Text,
            //    TextColor,
            //    NotPressedColor,
            //    myFont);

            //touchscreen.TouchDownEvent += new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchDownEvent);
            //touchscreen.TouchUpEvent += new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchUpEvent);
        }

        // Custom
        public void AddEventhandler()
        {
            try
            {
                touchscreen.TouchDownEvent -= new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchDownEvent);
                touchscreen.TouchUpEvent -= new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchUpEvent);
            }
            catch (Exception ex) { }
            finally {
                touchscreen.TouchDownEvent += new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchDownEvent);
                touchscreen.TouchUpEvent += new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchUpEvent);
            }        
        }
        // Clear
        public void ClearEventhandler() 
        {
            try
            {
                touchscreen.TouchDownEvent -= new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchDownEvent);
                touchscreen.TouchUpEvent -= new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchUpEvent);
            }
            catch (Exception ex) { }        
        }
        public void DrawMomentaryButton() 
        {
            touchscreen.FillRectangle(X_Pos, Y_Pos, Width, Height, NotPressedColor);
            Text = this.btnText;
            touchscreen.DrawString(
                X_Pos + (Width - (Text.Length * myFont.AverageWidth)) / 2,
                Y_Pos + (Height - myFont.Height) / 2,
                Text,
                TextColor,
                NotPressedColor,
                myFont);        
        }


        void touchscreen_TouchUpEvent(int x, int y)
        {
            if (isDeaktive == true)
            {
                return;
            }

            if (IsPointInControl(x, y) == true)
            {
                touchscreen.FillRectangle(X_Pos, Y_Pos, Width, Height, NotPressedColor);
                touchscreen.DrawString(
                    X_Pos + (Width - (Text.Length * myFont.AverageWidth)) / 2, 
                    Y_Pos + (Height - myFont.Height) / 2,
                    Text,
                    TextColor, 
                    NotPressedColor,
                    myFont);
                DoButtonReleased();
            }
        }

        void touchscreen_TouchDownEvent(int x, int y)
        {
            if (isDeaktive == true)
            {
                return;
            }

            if (IsPointInControl(x, y) == true)
            {
                touchscreen.FillRectangle(X_Pos, Y_Pos, Width, Height, PressedColor);
                touchscreen.DrawString( 
                    X_Pos + (Width - (Text.Length * myFont.AverageWidth)) / 2, 
                    Y_Pos + (Height - myFont.Height) / 2,
                    Text,
                    TextColor, 
                    PressedColor,
                    myFont);
                DoButtonPressed();
            }
        }
    }

    public class ScrollingGraph : Control
    {
        public delegate int DataGrabberDelegate();

        public FEZ_Components.FEZTouch.Color BarColor;

        public int MinValue;

        public int MaxValue;

        Timer refresh_timer;
        int next_idx;
        int num_bars = 50;
        int bar_width;
        int[] bar_vals;

        public DataGrabberDelegate DataGrabberFunction;

        public ScrollingGraph(int x, int y, int w, int h, FEZ_Components.FEZTouch ft)
        {
            Init(x, y, w, h, ft);
        }

        void Init(int x, int y, int w, int h, FEZ_Components.FEZTouch ft)
        {
            X_Pos = x;
            Y_Pos = y;
            Width = w;
            Height = h;
            touchscreen = ft;
            MinValue = 0;
            MaxValue = 99;
            BGColor = FEZ_Components.FEZTouch.Color.Black;
            BarColor = FEZ_Components.FEZTouch.Color.White;
            refresh_timer = new Timer(new TimerCallback(Refresh), null, 1000, 1000);
            bar_width = Width / num_bars;
            bar_vals = new int[num_bars];
            next_idx = 0;
            Clear();
        }

        void Clear()
        {
            touchscreen.FillRectangle(X_Pos, Y_Pos, Width, Height, BGColor);
        }

        void Refresh(Object o)
        {
            int val;

            val = DataGrabberFunction();
            bar_vals[next_idx] = val;
            //Debug.Print("Add " + val.ToString() + " at idx " + next_idx.ToString());
            next_idx = (next_idx + 1) % num_bars;
            Draw();
        }

        protected override void Draw()
        {
            int i;
            int val;
            int cur_idx;

            touchscreen.FillRectangle(X_Pos, Y_Pos, Width, Height, BGColor);

            for (i = 0; i < num_bars; i++)
            {
                cur_idx = (next_idx + i) % num_bars;
                if (bar_vals[cur_idx] == 0)
                    val = 0;
                else
                    val = (int)((float)Height * ((float)(bar_vals[cur_idx]) / (float)MaxValue));
                //Debug.Print(i.ToString() + ":" + val.ToString());
                touchscreen.FillRectangle(X_Pos + (i * bar_width), (Y_Pos + (Height - val)), bar_width, val, BarColor);
            }
        }
    }

    public class CheckBox : Control
    {
        public delegate void CheckStateChangedDelegate(Boolean check);

        public event CheckStateChangedDelegate CheckStateChanged;

        internal void DoCheckStateChanged(Boolean check)
        {
            if (CheckStateChanged != null) CheckStateChanged(check);
        }

        const int box_size_px = 20;

        Boolean is_checked;

        public Boolean IsChecked
        {
            get { return is_checked; }
            set
            {
                is_checked = value;
                Draw();
                DoCheckStateChanged(IsChecked);
            }
        }

        public FEZ_Components.FEZTouch.Color TextColor;

        public FEZ_Components.FEZTouch.Color CheckColor;

        public String Text;

        public CheckBox(int x, int y, FEZ_Components.FEZTouch ft, String txt)
        {
            Init(x, y, ft, txt, FEZ_Components.FEZTouch.Color.White, FEZ_Components.FEZTouch.Color.Black, FEZ_Components.FEZTouch.Color.Blue);
        }

        public CheckBox(int x, int y, FEZ_Components.FEZTouch ft, String txt, FEZ_Components.FEZTouch.Color tc, FEZ_Components.FEZTouch.Color bgc, FEZ_Components.FEZTouch.Color cc)
        {
            Init(x, y, ft, txt, tc, bgc, cc);
        }

        void Init(int x, int y, FEZ_Components.FEZTouch ft, String txt, FEZ_Components.FEZTouch.Color tc, FEZ_Components.FEZTouch.Color bgc, FEZ_Components.FEZTouch.Color cc)
        {
            
            X_Pos = x;
            Y_Pos = y;
            touchscreen = ft;
            Text = txt;
            Width = box_size_px + 3 + (txt.Length * myFont.AverageWidth);
            Height = box_size_px;
            BGColor = bgc;
            TextColor = tc;
            CheckColor = cc;

            // Draw background
            touchscreen.FillRectangle(X_Pos, Y_Pos, Width, Height, BGColor); 

            // Draw text
            touchscreen.DrawString(                
                X_Pos + box_size_px + 3, 
                Y_Pos + (Height / 2) - (myFont.Height / 2), 
                Text, 
                TextColor, 
                BGColor,
                myFont);

            // Draw frame
            touchscreen.FillRectangle(X_Pos, Y_Pos, box_size_px, box_size_px, TextColor);
            touchscreen.FillRectangle(X_Pos + 1, Y_Pos + 1, box_size_px - 2, box_size_px - 2, BGColor);

            Draw();

            // Event ist schon ausgelöst und muss nicht neu erstellt werden - führt zu dopplung bei Buttons "MomentaryButton"
            // touchscreen.TouchDownEvent += new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchDownEvent);
        }

        void touchscreen_TouchDownEvent(int x, int y)
        {
            if (IsPointInControl(x, y))
            {
                is_checked = !is_checked;
                Draw();
                DoCheckStateChanged(is_checked);
            }
        }

        protected override void Draw()
        {
            touchscreen.FillRectangle(X_Pos + 2, Y_Pos + 2, box_size_px - 4, box_size_px - 4, IsChecked ? CheckColor : BGColor);
        }
    }

    public class RadioButton : Control
    {
        public delegate void SelectedIndexChangedDelegate(int idx);

        public event SelectedIndexChangedDelegate SelectedIndexChanged;

        internal void DoIndexChanged(int idx, String lbl)
        {
            if (SelectedIndexChanged != null) SelectedIndexChanged(idx);
            if (SelectedButtonChanged != null) SelectedButtonChanged(lbl);
        }

        public delegate void SelectedButtonChangedDelegate(String lbl);

        public event SelectedButtonChangedDelegate SelectedButtonChanged;

        const int box_width_px = 20;

        public FEZ_Components.FEZTouch.Color TextColor;

        public FEZ_Components.FEZTouch.Color ButtonColor;

        String[] button_labels;
        int selected_idx;

        public RadioButton(int x, int y, FEZ_Components.FEZTouch ft, String[] lbls)
        {
            int max_len = 0;

            foreach (String s in lbls)
            {
                if (s.Length > max_len)
                    max_len = s.Length;
            }
            max_len += (box_width_px + 3);
            Init(x, y, max_len, (lbls.Length * (box_width_px + 1)), ft, lbls, FEZ_Components.FEZTouch.Color.White, FEZ_Components.FEZTouch.Color.Black, FEZ_Components.FEZTouch.Color.Green);
        }

        public RadioButton(int x, int y, FEZ_Components.FEZTouch ft, String[] lbls, FEZ_Components.FEZTouch.Color tc, FEZ_Components.FEZTouch.Color bgc, FEZ_Components.FEZTouch.Color bc)
        {
            int max_len = 0;

            foreach (String s in lbls)
            {
                if ((s.Length * myFont.AverageWidth) > max_len)
                    max_len = (s.Length * myFont.AverageWidth);
            }
            max_len += (box_width_px + 3);
            Init(x, y, max_len, (lbls.Length * (box_width_px + 1)), ft, lbls, tc, bgc, bc);
        }

        void Init(int x, int y, int w, int h, FEZ_Components.FEZTouch ft, String[] lbls, FEZ_Components.FEZTouch.Color tc, FEZ_Components.FEZTouch.Color bgc, FEZ_Components.FEZTouch.Color bc)
        {
            X_Pos = x;
            Y_Pos = y;

            Width = w;
            Height = h;
            touchscreen = ft;
            button_labels = lbls;
            selected_idx = 0;
            BGColor = bgc;
            TextColor = tc;
            ButtonColor = bc;

            touchscreen.TouchDownEvent += new FEZ_Components.FEZTouch.TouchEventHandler(touchscreen_TouchDownEvent);
            //Draw Background
            touchscreen.FillRectangle(X_Pos, Y_Pos, Width, Height, BGColor);

            //Draw Text
            int i;
            int y_offset;

            for (i = 0; i < button_labels.Length; i++)
            {
                y_offset = (i * box_width_px) + (box_width_px / 2) - (myFont.Height / 2) + i;
                touchscreen.DrawString( 
                    X_Pos + box_width_px + 3, 
                    Y_Pos + y_offset, 
                    button_labels[i],
                    TextColor, 
                    BGColor,
                    myFont);
            }

            // Draw Frames
            for (i = 0; i < button_labels.Length; i++)
            {
                touchscreen.FillRectangle(X_Pos, Y_Pos + (box_width_px * i) + 1 + i, box_width_px, box_width_px, TextColor);
                touchscreen.FillRectangle(X_Pos + 1, (Y_Pos + (box_width_px * i)) + 2 + i, box_width_px - 2, box_width_px - 2, BGColor);
            }

            Draw();
        }

        void touchscreen_TouchDownEvent(int x, int y)
        {
            int idx;
            if (IsPointInControl(x, y))
            {
                idx = (y - Y_Pos) / (box_width_px + 2);
                if (idx != selected_idx)
                {
                    selected_idx = idx;
                    Draw();
                    DoIndexChanged(selected_idx, button_labels[selected_idx]);
                }
            }
        }

        protected override void Draw()
        {
            int i;

            for (i = 0; i < button_labels.Length; i++)
            {
                if (i == selected_idx)
                {
                    touchscreen.FillRectangle(X_Pos + 2, (Y_Pos + (box_width_px * i)) + 3 + i, box_width_px - 4, box_width_px - 4, ButtonColor);
                }
                else
                {
                    touchscreen.FillRectangle(X_Pos + 2, (Y_Pos + (box_width_px * i)) + 3 + i, box_width_px - 4, box_width_px - 4, BGColor);
                }
            }
        }
    }

    public class ListBox
    {
        CheckBox[] items;

        public ListBox(CheckBox[] cb_items)
        {
            items = cb_items;
        }

        public int[] SelectedIndices
        {
            get
            {
                int i;
                int[] tmp;
                int cnt = 0;

                foreach (CheckBox cb in items)
                {
                    if (cb.IsChecked)
                        cnt++;
                }

                tmp = new int[cnt];
                cnt = 0;

                for (i = 0; i < items.Length; i++)
                {
                    if (items[i].IsChecked)
                        tmp[cnt++] = i;
                }

                return tmp;
            }
        }

        public String[] SelectedValues
        {
            get
            {
                int i;
                String[] tmp;
                int cnt = 0;

                foreach (CheckBox cb in items)
                {
                    if (cb.IsChecked)
                        cnt++;
                }

                tmp = new String[cnt];
                cnt = 0;

                for (i = 0; i < items.Length; i++)
                {
                    if (items[i].IsChecked)
                        tmp[cnt++] = items[i].Text;
                }

                return tmp;
            }
        }

        public void ClearAll()
        {
            foreach (CheckBox cb in items)
                cb.IsChecked = false;
        }

        public void SelectAll()
        {
            foreach (CheckBox cb in items)
                cb.IsChecked = true;
        }
    }
}

