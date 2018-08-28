using System;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using GHIElectronics.NETMF.FEZ;

using FEZTouch.UIControls;
using FEZTouch;
using SigmaDSP;
using SigmaDSP.Register;
using SigmaDSP.Eeprom;

namespace FEZ_Panda_II_DSP_Steuerung
{
    public enum MenuPages
    {
        pageInput,
        pageEQ,
        pageAnalyser
    };

    public class Program
    {
        /////////////////////////
        // VARS
        /////////////////////////


        // EEPROM
        static Eeprom_24kI2C Eeprom;

        // Display & Touch Variables
        public static FEZ_Components.FEZTouch ft;       // FEZTouch
        const int IDLE_TIME = 40000;
        static int idleTimer;
        static FEZ_Components.FEZTouch.DisplayMode displayMode;

        // DSP
        static SigmaDsp dsp;
        static ushort dspAdress = 0x34;

        // DSP Level Meter Send Bytes
        public static byte[] askLM_In_L1 = new byte[] { 0x08, 0x1a, 0x03, 0x6E };
        public static byte[] askLM_In_L2 = new byte[] { 0x08, 0x1a };

        public static byte[] askLM_In_R1 = new byte[] { 0x08, 0x1a, 0x03, 0x7A };
        public static byte[] askLM_In_R2 = new byte[] { 0x08, 0x1a };

        // Analyser Request Bytes
        public static byte[] askLM_A0_L2 = new byte[] { 0x08, 0x1a };   // Line 2, immer gleich

        public static byte[] askLM_A0_L1 = new byte[] { 0x08, 0x1a, 0x0E, 0xEA };
        public static byte[] askLM_A1_L1 = new byte[] { 0x08, 0x1a, 0x0E, 0xF6 };
        public static byte[] askLM_A2_L1 = new byte[] { 0x08, 0x1a, 0x0F, 0x02 };
        public static byte[] askLM_A3_L1 = new byte[] { 0x08, 0x1a, 0x0F, 0x0E };
        public static byte[] askLM_A4_L1 = new byte[] { 0x08, 0x1a, 0x0F, 0x1A };
        public static byte[] askLM_A5_L1 = new byte[] { 0x08, 0x1a, 0x0F, 0x26 };
        public static byte[] askLM_A6_L1 = new byte[] { 0x08, 0x1a, 0x0F, 0x32 };
        public static byte[] askLM_A7_L1 = new byte[] { 0x08, 0x1a, 0x0F, 0x3E };
        public static byte[] askLM_A8_L1 = new byte[] { 0x08, 0x1a, 0x0F, 0x4A };
        public static byte[] askLM_A9_L1 = new byte[] { 0x08, 0x1a, 0x0F, 0x56 };

        // Threads
        public static Thread ThreadInputLevelMeter;
        public static bool readLevelStartPage = false;

        public static Thread myDimThread;

        public static Thread ThreadAnalyserMeter;
        public static bool readAnalyserPage = false;

        // Menu Elemente
        public static MomentaryButton pageInput;
        public static MomentaryButton pageEQ;
        public static MomentaryButton pageAnalyser;

        public static MenuPages currentMenu = MenuPages.pageInput;

        public static FEZ_Components.FEZTouch.Color colorTouch = FEZ_Components.FEZTouch.Color.Gray;
        public static FEZ_Components.FEZTouch.Color colorActiv = FEZ_Components.FEZTouch.Color.Green;
        public static FEZ_Components.FEZTouch.Color colorInactiv = FEZ_Components.FEZTouch.Color.Red;


        // Settings
        public static int setting_GainL = 1;
        public static int setting_GainR = 1;

        public static int setting_Volumen = -5;

        public static int setting_Eq0 = 0;
        public static int setting_Eq1 = 0;
        public static int setting_Eq2 = 0;
        public static int setting_Eq3 = 0;
        public static int setting_Eq4 = 0;
        public static int setting_Eq5 = 0;
        public static int setting_Eq6 = 0;
        public static int setting_Eq7 = 0;
        public static int setting_Eq8 = 0;
        public static int setting_Eq9 = 0;

        public static bool setting_btn_muteSpeaker = false;
        public static bool setting_btn_muteLine = true;
        public static bool setting_dimThreadOnOff = true;

        // UI Elemente
        public static Slider sl_GainL;
        public static Slider sl_GainR;

        public static Slider sl_volume;
        
        public static Label txt_GainL;
        public static Label txt_GainR;
        public static Label txt_Volumen;
        public static Label txt_Volumen_Value;

        public static LevelMeter lm_L;
        public static LevelMeter lm_R;
        //public static LevelMeterScalar lms_L_Input;
        //public static LevelMeterScalar lms_R_Input;       // out of memory
        public static LevelMeterScalar lms_L_EQ;
        //public static LevelMeterScalar lms_R_EQ;          // out of memory
        public static LevelMeterScalar lms_L_Analyser;
        //public static LevelMeterScalar lms_R_Analyser;    // out of memory

        public static LevelMeterScalar lms_L;               // for all pages
        public static LevelMeterScalar lms_R;               // for all pages


        public static Slider sl_eq0;
        public static Slider sl_eq1;
        public static Slider sl_eq2;
        public static Slider sl_eq3;
        public static Slider sl_eq4;
        public static Slider sl_eq5;
        public static Slider sl_eq6;
        public static Slider sl_eq7;
        public static Slider sl_eq8;
        public static Slider sl_eq9;
        public static MomentaryButton txt_btn_eq0;  // set auf 0 des EQ
        public static MomentaryButton txt_btn_eq1;
        public static MomentaryButton txt_btn_eq2;
        public static MomentaryButton txt_btn_eq3;
        public static MomentaryButton txt_btn_eq4;
        public static MomentaryButton txt_btn_eq5;
        public static MomentaryButton txt_btn_eq6;
        public static MomentaryButton txt_btn_eq7;
        public static MomentaryButton txt_btn_eq8;
        public static MomentaryButton txt_btn_eq9;

        public static StateButton dimThreadOnOff;
        public static StateButton btn_muteLine;
        public static StateButton btn_muteSpeaker;

        // Analyser
        public static LevelMeter lm_a0;
        public static LevelMeter lm_a1;
        public static LevelMeter lm_a2;
        public static LevelMeter lm_a3;
        public static LevelMeter lm_a4;
        public static LevelMeter lm_a5;
        public static LevelMeter lm_a6;
        public static LevelMeter lm_a7;
        public static LevelMeter lm_a8;
        public static LevelMeter lm_a9;
        
        // Eeprom Settings Adress
        private static uint EepromSettingAdress = 63300;    // Basis Adresse, 

        /// <summary>
        /// MAIN
        /// </summary>
        public static void Main()
        {
            // Device 0x68, 400kHZ, SamplingRate 96000
            dsp = new SigmaDsp(dspAdress, 100, 96000);
            // Mute DSP
            dsp.send_Mute(DSP_Register.ADDR_MUTE_Line, false);
            dsp.send_Mute(DSP_Register.ADDR_MUTE_Speaker, false);
            Thread.Sleep(20);
            
            // Init Eeprom Load and Save Settings and INIT DSP Register
            Eeprom = new Eeprom_24kI2C(dsp.get_I2CBus());
            loadAllSettings();
            dsp.initI2CBus();


            // INIT Display
            InitGraphics();                   
            
            #region Region Threads

            // Display            
            myDimThread = new Thread(Thread_DIM);
            myDimThread.Priority = ThreadPriority.Lowest;
            myDimThread.Start();
            if (setting_dimThreadOnOff == false)
            {
                myDimThread.Suspend();
            }

            // Input Level Meter
            ThreadInputLevelMeter = new Thread(thread_Read_Input);
            ThreadInputLevelMeter.Priority = ThreadPriority.BelowNormal;
            ThreadInputLevelMeter.Start();
            ThreadInputLevelMeter.Suspend();

            // Analyser Thread
            ThreadAnalyserMeter = new Thread(thread_AnalyserRead);
            ThreadAnalyserMeter.Priority = ThreadPriority.BelowNormal;
            ThreadAnalyserMeter.Start();
            ThreadAnalyserMeter.Suspend();


            #endregion
            
            // Init all User Controlls            
            initControlls();

            // Show Input Page
            pageInput_Pressed();
            //pageAnalyser_Pressed();
            //pageEQ_Pressed();

        }

        //------------------------------------------------------------------------------
        // MENU
        //------------------------------------------------------------------------------
        public static void updateMenu() 
        {
            // Achtung in Zeile 930: touchscreen.TouchDownEvent deaktiviert!

            // Clean Page bei Update
            ft.FillRectangle(0, 0, ft.ScreenWidth, 220, FEZ_Components.FEZTouch.Color.Black);

            // Show Menu
            switch (currentMenu)
            {
                case MenuPages.pageInput:

                    pageInput.PressedColor = colorTouch;
                    pageInput.NotPressedColor = colorActiv;
                    pageEQ.PressedColor = FEZ_Components.FEZTouch.Color.Gray;
                    pageEQ.NotPressedColor = FEZ_Components.FEZTouch.Color.Red;
                    pageAnalyser.PressedColor = FEZ_Components.FEZTouch.Color.Gray;
                    pageAnalyser.NotPressedColor = FEZ_Components.FEZTouch.Color.Red;                    
                    break;

                case MenuPages.pageEQ:
                    
                    pageInput.PressedColor = FEZ_Components.FEZTouch.Color.Gray;
                    pageInput.NotPressedColor = FEZ_Components.FEZTouch.Color.Red;
                    pageEQ.PressedColor = colorTouch;
                    pageEQ.NotPressedColor = colorActiv;
                    pageAnalyser.PressedColor = FEZ_Components.FEZTouch.Color.Gray;
                    pageAnalyser.NotPressedColor = FEZ_Components.FEZTouch.Color.Red;                    
                    break;

                case MenuPages.pageAnalyser:
                    
                    pageInput.PressedColor = FEZ_Components.FEZTouch.Color.Gray;
                    pageInput.NotPressedColor = FEZ_Components.FEZTouch.Color.Red;
                    pageEQ.PressedColor = FEZ_Components.FEZTouch.Color.Gray;
                    pageEQ.NotPressedColor = FEZ_Components.FEZTouch.Color.Red;
                    pageAnalyser.PressedColor = colorTouch;
                    pageAnalyser.NotPressedColor = colorActiv;
                    break;
            }

            // Draw Buttons
            pageInput.DrawMomentaryButton();
            pageEQ.DrawMomentaryButton();
            pageAnalyser.DrawMomentaryButton();

        }

        static void pageAnalyser_Pressed()
        {
            // hide other Pages
            showInput(false);
            showEQ(false);

            // change menu
            currentMenu = MenuPages.pageAnalyser;
            updateMenu();

            showAnalyser(true);
        }

        static void pageEQ_Pressed()
        {
            // hide other Pages
            showInput(false);
            showAnalyser(false);
                        
            // change menu
            currentMenu = MenuPages.pageEQ;
            updateMenu();

            // Show Page
            showEQ(true);
        }

        static void pageInput_Pressed()
        {
            // hide other Pages
            showEQ(false);
            showAnalyser(false);
            
            // change menu
            currentMenu = MenuPages.pageInput;
            updateMenu();

            // Show Page
            showInput(true);   
        }


        //------------------------------------------------------------------------------
        // Init Controlls
        //------------------------------------------------------------------------------
        public static void initControlls() 
        {
            // MENU
            #region Menu

            pageInput = new MomentaryButton(20, 220, ft, 80, 20, "Input", false);
            pageEQ = new MomentaryButton(120, 220, ft, 80, 20, "EQ", false);
            pageAnalyser = new MomentaryButton(220, 220, ft, 80, 20, "Analyser", false);

            pageInput.AddEventhandler();
            pageEQ.AddEventhandler();
            pageAnalyser.AddEventhandler();
            
            // Eventhandler
            pageInput.Pressed += new MomentaryButton.ButtonPressedDelegate(pageInput_Pressed);
            pageEQ.Pressed += new MomentaryButton.ButtonPressedDelegate(pageEQ_Pressed);
            pageAnalyser.Pressed += new MomentaryButton.ButtonPressedDelegate(pageAnalyser_Pressed);

            #endregion



            // Level Input Page
            #region Input Page

            // Init Gain
            sl_GainL = new Slider(20, 20, ft, 1, 10, Slider.eOrientation.Vertical, setting_GainL);
            sl_GainR = new Slider(280, 20, ft, 1, 10, Slider.eOrientation.Vertical, setting_GainR);

            sl_volume = new Slider(175, 10, ft, 100, -35, 5, Slider.eOrientation.Vertical, setting_Volumen);

            btn_muteSpeaker = new StateButton(125, 120, ft, 70, 25, "Spk On", "Spk Off", setting_btn_muteSpeaker);
            btn_muteLine = new StateButton(125, 150, ft, 70, 25, "Lin On", "Lin Off", setting_btn_muteLine);    // TODO Active true auswerten
            dimThreadOnOff = new StateButton(125, 180, ft, 70, 25, "Dim On", "Dim Off", setting_dimThreadOnOff);

            // out of Memory 
            //lms_L_Input = new LevelMeterScalar(83, 25, ft, -60, 5, LevelMeterScalar.eOrientation.Vertical, LevelMeterScalar.textSide.right, true);            
            // lms_R_Input = new LevelMeterScalar(234, 25, ft, -60, 5, LevelMeterScalar.eOrientation.Vertical, LevelMeterScalar.textSide.left);
/*
            lm_L = new LevelMeter(60, 25, ft, -60, 5, LevelMeter.eOrientation.Vertical);
            lms_L = new LevelMeterScalar(83, 25, ft, -60, 5, LevelMeterScalar.eOrientation.Vertical, LevelMeterScalar.textSide.right);

            lm_R = new LevelMeter(240, 25, ft, -60, 5, LevelMeter.eOrientation.Vertical);
            lms_R = new LevelMeterScalar(234, 25, ft, -60, 5, LevelMeterScalar.eOrientation.Vertical, LevelMeterScalar.textSide.left);
*/
            #endregion


            // EQ Page
            #region EQ PAGE

            // Init Settings, Funktion erweitert
            sl_eq0 = new Slider(35, 20, ft, -25, 25, Slider.eOrientation.Vertical, setting_Eq0);
            sl_eq1 = new Slider(60, 20, ft, -25, 25, Slider.eOrientation.Vertical, setting_Eq1);
            sl_eq2 = new Slider(85, 20, ft, -25, 25, Slider.eOrientation.Vertical, setting_Eq2);
            sl_eq3 = new Slider(110, 20, ft, -25, 25, Slider.eOrientation.Vertical, setting_Eq3);
            sl_eq4 = new Slider(135, 20, ft, -25, 25, Slider.eOrientation.Vertical, setting_Eq4);
            sl_eq5 = new Slider(160, 20, ft, -25, 25, Slider.eOrientation.Vertical, setting_Eq5);
            sl_eq6 = new Slider(185, 20, ft, -25, 25, Slider.eOrientation.Vertical, setting_Eq6);
            sl_eq7 = new Slider(210, 20, ft, -25, 25, Slider.eOrientation.Vertical, setting_Eq7);
            sl_eq8 = new Slider(235, 20, ft, -25, 25, Slider.eOrientation.Vertical, setting_Eq8);
            sl_eq9 = new Slider(260, 20, ft, -25, 25, Slider.eOrientation.Vertical, setting_Eq9);

            // Text
            txt_btn_eq0 = new MomentaryButton(33, 180, ft, 25, 18, "46", false);
            txt_btn_eq1 = new MomentaryButton(58, 180, ft, 25, 18, "61", false);
            txt_btn_eq2 = new MomentaryButton(83, 180, ft, 25, 18, "168", false);
            txt_btn_eq3 = new MomentaryButton(108, 180, ft, 25, 18, "343", false);
            txt_btn_eq4 = new MomentaryButton(133, 180, ft, 25, 18, "615", false);
            txt_btn_eq5 = new MomentaryButton(158, 180, ft, 25, 18, "1.0", false);
            txt_btn_eq6 = new MomentaryButton(183, 180, ft, 25, 18, "3.4", false);
            txt_btn_eq7 = new MomentaryButton(208, 180, ft, 25, 18, "6.1", false);
            txt_btn_eq8 = new MomentaryButton(233, 180, ft, 25, 18, "10.", false);
            txt_btn_eq9 = new MomentaryButton(258, 180, ft, 25, 18, "16.", false);

            txt_btn_eq0.PressedColor = FEZ_Components.FEZTouch.Color.Gray;
            txt_btn_eq0.NotPressedColor = FEZ_Components.FEZTouch.Color.Red;

            lms_L_EQ = new LevelMeterScalar(25, 18, ft, -25, 25, LevelMeterScalar.eOrientation.Vertical, LevelMeterScalar.textSide.left, true);
            // out of Memory 
            //lms_R_EQ = new LevelMeterScalar(285, 18, ft, -25, 25, LevelMeterScalar.eOrientation.Vertical, LevelMeterScalar.textSide.right);

            #endregion

            // Analyser
            #region Analyser

            // LevelMeter Scalar
            lms_L_Analyser = new LevelMeterScalar(28, 25, ft, -60, 5, LevelMeterScalar.eOrientation.Vertical, LevelMeterScalar.textSide.left, true);
            // out of Memory 
            //lms_R_Analyser = new LevelMeterScalar(288, 25, ft, -60, 5, LevelMeterScalar.eOrientation.Vertical, LevelMeterScalar.textSide.right);


            #endregion

        } 

        /// <summary>
        /// Load Data from Eeprom, if is set
        /// </summary>
        private static void loadAllSettings()
        {
            // Read Data from Eeprom, 16 bytes
            byte[] data = Eeprom.readRegister(EepromSettingAdress, 16);
            dsp.initI2CBus();
            Thread.Sleep(20);

            // Prüfen ob daten vorhanden sind, sonst settings setzten

            // Vol
            if (data[0] != 255)
            {
                setting_Volumen = (sbyte)data[0];
                // Send Volumen DSP
                dsp.send_DB(DSP_Register.ADDR_GAIN_VOL, setting_Volumen, true);
            }

            // Gain L&R
            if (data[1] != 255)
            {
                setting_GainL = (sbyte)data[1];
                // Send Volumen DSP
                dsp.send_Gain(DSP_Register.ADDR_GAIN1, setting_GainL, true);
            }
            if (data[2] != 255)
            {
                setting_GainR = (sbyte)data[2];
                // Send Gain DSP
                dsp.send_Gain(DSP_Register.ADDR_GAIN2, setting_GainR, true);

            }

            // EQ 0-9
            if (data[3] != 255)
            {
                setting_Eq0 = (sbyte)data[3];
                dsp.sendEQSafeLoad(DSP_Register.EQ1940DualS10B1, 46f, (float)setting_Eq0);
            }
            if (data[4] != 255)
            {
                setting_Eq1 = (sbyte)data[4];
                dsp.sendEQSafeLoad(DSP_Register.EQ1940DualS10B2, 61.5f, (float)setting_Eq1);
            }
            if (data[5] != 255)
            {
                setting_Eq2 = (sbyte)data[5];
                dsp.sendEQSafeLoad(DSP_Register.EQ1940DualS10B3, 168.7f, (float)setting_Eq2);
            }
            if (data[6] != 255)
            {
                setting_Eq3 = (sbyte)data[6];
                dsp.sendEQSafeLoad(DSP_Register.EQ1940DualS10B4, 343f, (float)setting_Eq3);
            }
            if (data[7] != 255)
            {
                setting_Eq4 = (sbyte)data[7];
                dsp.sendEQSafeLoad(DSP_Register.EQ1940DualS10B5, 615f, (float)setting_Eq4);
            }
            if (data[8] != 255)
            {
                setting_Eq5 = (sbyte)data[8];
                dsp.sendEQSafeLoad(DSP_Register.EQ1940DualS10B6, 1080f, (float)setting_Eq5);
            }
            if (data[9] != 255)
            {
                setting_Eq6 = (sbyte)data[9];
                dsp.sendEQSafeLoad(DSP_Register.EQ1940DualS10B7, 3430f, (float)setting_Eq6);
            }
            if (data[10] != 255)
            {
                setting_Eq7 = (sbyte)data[10];
                dsp.sendEQSafeLoad(DSP_Register.EQ1940DualS10B8, 6150f, (float)setting_Eq7);
            }
            if (data[11] != 255)
            {
                setting_Eq8 = (sbyte)data[11];
                dsp.sendEQSafeLoad(DSP_Register.EQ1940DualS10B9, 10800f, (float)setting_Eq8);
            }
            if (data[12] != 255)
            {
                setting_Eq9 = (sbyte)data[12];
                dsp.sendEQSafeLoad(DSP_Register.EQ1940DualS10B10, 15870f, (float)setting_Eq9);

            }

            // Button            
            bool val = (data[13] == 1) ? true : false;
            if (data[13] != 255)
            {
                setting_btn_muteLine = val;
                dsp.send_Mute(DSP_Register.ADDR_MUTE_Line, setting_btn_muteLine);
            }

            val = (data[14] == 1) ? true : false;
            if (data[14] != 255)
            {
                setting_btn_muteSpeaker = val;
                dsp.send_Mute(DSP_Register.ADDR_MUTE_Speaker, setting_btn_muteSpeaker);
            }

            val = (data[15] == 1) ? true : false;
            if (data[15] != 255)
            {
                setting_dimThreadOnOff = val;
            }
        }

        /// <summary>
        /// Debug and Testing only
        /// </summary>
        private static void saveAllSettings() 
        {
            int sleep = 10;

            // Vol
            Eeprom.writeRegister(EepromSettingAdress, (byte)setting_Volumen);
            Thread.Sleep(sleep);

            // Gain L, 
            Eeprom.writeRegister(EepromSettingAdress + 1, (byte)setting_GainL);
            Thread.Sleep(sleep);

            // Gain R
            Eeprom.writeRegister(EepromSettingAdress + 2, (byte)setting_GainR);
            Thread.Sleep(sleep);

            // EQ 0-9
            Eeprom.writeRegister(EepromSettingAdress + 3, (byte)setting_Eq0);
            Thread.Sleep(sleep);
            Eeprom.writeRegister(EepromSettingAdress + 4, (byte)setting_Eq1);
            Thread.Sleep(sleep);
            Eeprom.writeRegister(EepromSettingAdress + 5, (byte)setting_Eq2);
            Thread.Sleep(sleep);
            Eeprom.writeRegister(EepromSettingAdress + 6, (byte)setting_Eq3);
            Thread.Sleep(sleep);
            Eeprom.writeRegister(EepromSettingAdress + 7, (byte)setting_Eq4);
            Thread.Sleep(sleep);
            Eeprom.writeRegister(EepromSettingAdress + 8, (byte)setting_Eq5);
            Thread.Sleep(sleep);
            Eeprom.writeRegister(EepromSettingAdress + 9, (byte)setting_Eq6);
            Thread.Sleep(sleep);
            Eeprom.writeRegister(EepromSettingAdress + 10, (byte)setting_Eq7);
            Thread.Sleep(sleep);
            Eeprom.writeRegister(EepromSettingAdress + 11, (byte)setting_Eq8);
            Thread.Sleep(sleep);
            Eeprom.writeRegister(EepromSettingAdress + 12, (byte)setting_Eq9);
            Thread.Sleep(sleep);

            // Buttons
            byte val = setting_btn_muteLine ? (byte) 1 : (byte) 0;
            Eeprom.writeRegister(EepromSettingAdress + 13, val);
            Thread.Sleep(sleep);

            val = setting_btn_muteSpeaker ? (byte)1 : (byte)0;
            Eeprom.writeRegister(EepromSettingAdress + 14, val);
            Thread.Sleep(sleep);

            val = setting_dimThreadOnOff ? (byte)1 : (byte)0;
            Eeprom.writeRegister(EepromSettingAdress + 15, val);

        }


        //------------------------------------------------------------------------------
        // Pages
        //------------------------------------------------------------------------------

        // Page INPUT
        // Gain und Level IN
        public static void showInput(bool show)
        {
            // Input ON
            if (show == true)
            {
                // Draw
                sl_volume.DrawSlider();
                                                
                btn_muteSpeaker.DrawStateButton();
                btn_muteLine.DrawStateButton();
                dimThreadOnOff.DrawStateButton();

                sl_GainL.DrawSlider();
                sl_GainR.DrawSlider();                               

                // Eventhandler
                dimThreadOnOff.AddEventhandler();

                sl_GainL.AddEventhandler();
                sl_GainR.AddEventhandler();

                sl_volume.AddEventhandler();
                btn_muteSpeaker.AddEventhandler();
                btn_muteLine.AddEventhandler();

                // Text
                txt_GainL = new Label(20, 185, ft, "L: " + setting_GainL.ToString());
                txt_GainR = new Label(270, 185, ft, "R: " + setting_GainR.ToString());

                txt_Volumen = new Label(125, 60, ft, "Vol:");
                txt_Volumen_Value = new Label(140, 80, ft, 20 , 8, setting_Volumen.ToString());

                // LevelMeter
                lm_L = new LevelMeter(60, 25, ft, -60, 5, LevelMeter.eOrientation.Vertical);
                //lms_L_Input.showLevelMeterScalar();
                lms_L = new LevelMeterScalar(83, 25, ft, -60, 5, LevelMeterScalar.eOrientation.Vertical, LevelMeterScalar.textSide.right);

                lm_R = new LevelMeter(240, 25, ft, -60, 5, LevelMeter.eOrientation.Vertical);
                lms_R = new LevelMeterScalar(234, 25, ft, -60, 5, LevelMeterScalar.eOrientation.Vertical, LevelMeterScalar.textSide.left);
                // lms_R_Input.showLevelMeterScalar();
                

                // eventhandler
                try
                {
                    sl_GainL.ValueChanged -= new Slider.ValueChangedDelegate(sl_GainL_ValueChanged);
                    sl_GainR.ValueChanged -= new Slider.ValueChangedDelegate(sl_GainR_ValueChanged);
                    sl_volume.ValueChanged -= new Slider.ValueChangedDelegate(sl_volume_ValueChanged);

                    btn_muteLine.StateChanged -= new StateButton.State1ActiveDelegate(btn_muteLine_StateChanged);
                    btn_muteSpeaker.StateChanged -= new StateButton.State1ActiveDelegate(btn_muteSpeaker_StateChanged);
                    dimThreadOnOff.StateChanged -= new StateButton.State1ActiveDelegate(dimThreadOnOff_StateChanged);
                }
                catch (Exception ex) { }
                finally
                {
                    sl_GainL.ValueChanged += new Slider.ValueChangedDelegate(sl_GainL_ValueChanged);
                    sl_GainR.ValueChanged += new Slider.ValueChangedDelegate(sl_GainR_ValueChanged);
                    sl_volume.ValueChanged += new Slider.ValueChangedDelegate(sl_volume_ValueChanged);
                                        
                    btn_muteLine.StateChanged += new StateButton.State1ActiveDelegate(btn_muteLine_StateChanged);
                    btn_muteSpeaker.StateChanged += new StateButton.State1ActiveDelegate(btn_muteSpeaker_StateChanged);
                    dimThreadOnOff.StateChanged += new StateButton.State1ActiveDelegate(dimThreadOnOff_StateChanged);
                }

                // Level Meter ON        
                ThreadInputLevelMeter.Resume();
                readLevelStartPage = true;

            }
            else
            {
                // Clear Eventhandler
                sl_GainL.ClearEventhandler();
                sl_GainR.ClearEventhandler();
                sl_volume.ClearEventhandler();

                sl_volume.ClearEventhandler();
                btn_muteLine.ClearEventhandler();
                btn_muteSpeaker.ClearEventhandler();

                dimThreadOnOff.ClearEventhandler();

                // Level Meter OFF
                readLevelStartPage = false;
                ThreadInputLevelMeter.Suspend();
            }
        }


        // Page EQ
        // 10 Band EQ und ON/OFF
        public static void showEQ(bool show) 
        {
            // EQ ON
            if (show == true)
            {
                // Draw EQ
                sl_eq0.DrawSlider();
                sl_eq1.DrawSlider();
                sl_eq2.DrawSlider();
                sl_eq3.DrawSlider();
                sl_eq4.DrawSlider();
                sl_eq5.DrawSlider();
                sl_eq6.DrawSlider();
                sl_eq7.DrawSlider();
                sl_eq8.DrawSlider();
                sl_eq9.DrawSlider();

                // Eventhandler
                sl_eq0.AddEventhandler();
                sl_eq1.AddEventhandler();
                sl_eq2.AddEventhandler();
                sl_eq3.AddEventhandler();
                sl_eq4.AddEventhandler();
                sl_eq5.AddEventhandler();
                sl_eq6.AddEventhandler();
                sl_eq7.AddEventhandler();
                sl_eq8.AddEventhandler();
                sl_eq9.AddEventhandler();

                // Buttons Draw
                txt_btn_eq0.DrawMomentaryButton();
                txt_btn_eq1.DrawMomentaryButton();
                txt_btn_eq2.DrawMomentaryButton();
                txt_btn_eq3.DrawMomentaryButton();
                txt_btn_eq4.DrawMomentaryButton();
                txt_btn_eq5.DrawMomentaryButton();
                txt_btn_eq6.DrawMomentaryButton();
                txt_btn_eq7.DrawMomentaryButton();
                txt_btn_eq8.DrawMomentaryButton();
                txt_btn_eq9.DrawMomentaryButton();

                // Add Eventhandler
                txt_btn_eq0.AddEventhandler();
                txt_btn_eq1.AddEventhandler();
                txt_btn_eq2.AddEventhandler();
                txt_btn_eq3.AddEventhandler();
                txt_btn_eq4.AddEventhandler();
                txt_btn_eq5.AddEventhandler();
                txt_btn_eq6.AddEventhandler();
                txt_btn_eq7.AddEventhandler();
                txt_btn_eq8.AddEventhandler();
                txt_btn_eq9.AddEventhandler();

                //lms_L = new LevelMeterScalar(25, 18, ft, -25, 25, LevelMeterScalar.eOrientation.Vertical, LevelMeterScalar.textSide.left);
                lms_R = new LevelMeterScalar(285, 18, ft, -25, 25, LevelMeterScalar.eOrientation.Vertical, LevelMeterScalar.textSide.right);
                lms_L_EQ.showLevelMeterScalar();
                //lms_R_EQ.showLevelMeterScalar();

                // eventhandler
                try
                {
                    sl_eq0.ValueChanged -= new Slider.ValueChangedDelegate(sl_eq0_ValueChanged);
                    sl_eq1.ValueChanged -= new Slider.ValueChangedDelegate(sl_eq1_ValueChanged);
                    sl_eq2.ValueChanged -= new Slider.ValueChangedDelegate(sl_eq2_ValueChanged);
                    sl_eq3.ValueChanged -= new Slider.ValueChangedDelegate(sl_eq3_ValueChanged);
                    sl_eq4.ValueChanged -= new Slider.ValueChangedDelegate(sl_eq4_ValueChanged);
                    sl_eq5.ValueChanged -= new Slider.ValueChangedDelegate(sl_eq5_ValueChanged);
                    sl_eq6.ValueChanged -= new Slider.ValueChangedDelegate(sl_eq6_ValueChanged);
                    sl_eq7.ValueChanged -= new Slider.ValueChangedDelegate(sl_eq7_ValueChanged);
                    sl_eq8.ValueChanged -= new Slider.ValueChangedDelegate(sl_eq8_ValueChanged);
                    sl_eq9.ValueChanged -= new Slider.ValueChangedDelegate(sl_eq9_ValueChanged);

                    txt_btn_eq0.Pressed -= new MomentaryButton.ButtonPressedDelegate(txt_btn_eq0_SetNull);
                    txt_btn_eq1.Pressed -= new MomentaryButton.ButtonPressedDelegate(txt_btn_eq1_SetNull);
                    txt_btn_eq2.Pressed -= new MomentaryButton.ButtonPressedDelegate(txt_btn_eq2_SetNull);
                    txt_btn_eq3.Pressed -= new MomentaryButton.ButtonPressedDelegate(txt_btn_eq3_SetNull);
                    txt_btn_eq4.Pressed -= new MomentaryButton.ButtonPressedDelegate(txt_btn_eq4_SetNull);
                    txt_btn_eq5.Pressed -= new MomentaryButton.ButtonPressedDelegate(txt_btn_eq5_SetNull);
                    txt_btn_eq6.Pressed -= new MomentaryButton.ButtonPressedDelegate(txt_btn_eq6_SetNull);
                    txt_btn_eq7.Pressed -= new MomentaryButton.ButtonPressedDelegate(txt_btn_eq7_SetNull);
                    txt_btn_eq8.Pressed -= new MomentaryButton.ButtonPressedDelegate(txt_btn_eq8_SetNull);
                    txt_btn_eq9.Pressed -= new MomentaryButton.ButtonPressedDelegate(txt_btn_eq9_SetNull);
                }
                catch (Exception ex) { }
                finally
                {
                    sl_eq0.ValueChanged += new Slider.ValueChangedDelegate(sl_eq0_ValueChanged);
                    sl_eq1.ValueChanged += new Slider.ValueChangedDelegate(sl_eq1_ValueChanged);
                    sl_eq2.ValueChanged += new Slider.ValueChangedDelegate(sl_eq2_ValueChanged);
                    sl_eq3.ValueChanged += new Slider.ValueChangedDelegate(sl_eq3_ValueChanged);
                    sl_eq4.ValueChanged += new Slider.ValueChangedDelegate(sl_eq4_ValueChanged);
                    sl_eq5.ValueChanged += new Slider.ValueChangedDelegate(sl_eq5_ValueChanged);
                    sl_eq6.ValueChanged += new Slider.ValueChangedDelegate(sl_eq6_ValueChanged);
                    sl_eq7.ValueChanged += new Slider.ValueChangedDelegate(sl_eq7_ValueChanged);
                    sl_eq8.ValueChanged += new Slider.ValueChangedDelegate(sl_eq8_ValueChanged);
                    sl_eq9.ValueChanged += new Slider.ValueChangedDelegate(sl_eq9_ValueChanged);

                    txt_btn_eq0.Pressed += new MomentaryButton.ButtonPressedDelegate(txt_btn_eq0_SetNull);
                    txt_btn_eq1.Pressed += new MomentaryButton.ButtonPressedDelegate(txt_btn_eq1_SetNull);
                    txt_btn_eq2.Pressed += new MomentaryButton.ButtonPressedDelegate(txt_btn_eq2_SetNull);
                    txt_btn_eq3.Pressed += new MomentaryButton.ButtonPressedDelegate(txt_btn_eq3_SetNull);
                    txt_btn_eq4.Pressed += new MomentaryButton.ButtonPressedDelegate(txt_btn_eq4_SetNull);
                    txt_btn_eq5.Pressed += new MomentaryButton.ButtonPressedDelegate(txt_btn_eq5_SetNull);
                    txt_btn_eq6.Pressed += new MomentaryButton.ButtonPressedDelegate(txt_btn_eq6_SetNull);
                    txt_btn_eq7.Pressed += new MomentaryButton.ButtonPressedDelegate(txt_btn_eq7_SetNull);
                    txt_btn_eq8.Pressed += new MomentaryButton.ButtonPressedDelegate(txt_btn_eq8_SetNull);
                    txt_btn_eq9.Pressed += new MomentaryButton.ButtonPressedDelegate(txt_btn_eq9_SetNull);
                }
            }
            // EQ OFF
            else 
            {
                // Clear EQ- Page
                sl_eq0.ClearEventhandler();
                sl_eq1.ClearEventhandler();
                sl_eq2.ClearEventhandler();
                sl_eq3.ClearEventhandler();
                sl_eq4.ClearEventhandler();
                sl_eq5.ClearEventhandler();
                sl_eq6.ClearEventhandler();
                sl_eq7.ClearEventhandler();
                sl_eq8.ClearEventhandler();
                sl_eq9.ClearEventhandler();

                txt_btn_eq0.ClearEventhandler();
                txt_btn_eq1.ClearEventhandler();
                txt_btn_eq2.ClearEventhandler();
                txt_btn_eq3.ClearEventhandler();
                txt_btn_eq4.ClearEventhandler();
                txt_btn_eq5.ClearEventhandler();
                txt_btn_eq6.ClearEventhandler();
                txt_btn_eq7.ClearEventhandler();
                txt_btn_eq8.ClearEventhandler();
                txt_btn_eq9.ClearEventhandler();
            }
        }


        // Page Analyser
        // 7 Band und ON/OFF
        public static void showAnalyser(bool show)
        {
            if (show == true)
            {
                // Level Meter
                lm_a0 = new LevelMeter(35, 25, ft, -60, 5, LevelMeter.eOrientation.Vertical, 22, FEZ_Components.FEZTouch.Color.Gray, FEZ_Components.FEZTouch.Color.Green);
                lm_a1 = new LevelMeter(60, 25, ft, -60, 5, LevelMeter.eOrientation.Vertical, 22, FEZ_Components.FEZTouch.Color.Gray, FEZ_Components.FEZTouch.Color.Green);
                lm_a2 = new LevelMeter(85, 25, ft, -60, 5, LevelMeter.eOrientation.Vertical, 22, FEZ_Components.FEZTouch.Color.Gray, FEZ_Components.FEZTouch.Color.Green);
                lm_a3 = new LevelMeter(110, 25, ft, -60, 5, LevelMeter.eOrientation.Vertical, 22, FEZ_Components.FEZTouch.Color.Gray, FEZ_Components.FEZTouch.Color.Green);
                lm_a4 = new LevelMeter(135, 25, ft, -60, 5, LevelMeter.eOrientation.Vertical, 22, FEZ_Components.FEZTouch.Color.Gray, FEZ_Components.FEZTouch.Color.Green);
                lm_a5 = new LevelMeter(160, 25, ft, -60, 5, LevelMeter.eOrientation.Vertical, 22, FEZ_Components.FEZTouch.Color.Gray, FEZ_Components.FEZTouch.Color.Green);
                lm_a6 = new LevelMeter(185, 25, ft, -60, 5, LevelMeter.eOrientation.Vertical, 22, FEZ_Components.FEZTouch.Color.Gray, FEZ_Components.FEZTouch.Color.Green);
                lm_a7 = new LevelMeter(210, 25, ft, -60, 5, LevelMeter.eOrientation.Vertical, 22, FEZ_Components.FEZTouch.Color.Gray, FEZ_Components.FEZTouch.Color.Green);
                lm_a8 = new LevelMeter(235, 25, ft, -60, 5, LevelMeter.eOrientation.Vertical, 22, FEZ_Components.FEZTouch.Color.Gray, FEZ_Components.FEZTouch.Color.Green);
                lm_a9 = new LevelMeter(260, 25, ft, -60, 5, LevelMeter.eOrientation.Vertical, 22, FEZ_Components.FEZTouch.Color.Gray, FEZ_Components.FEZTouch.Color.Green);

                // LevelMeter Scalar
                //lms_L = new LevelMeterScalar(28, 25, ft, -60, 5, LevelMeterScalar.eOrientation.Vertical, LevelMeterScalar.textSide.left);
                lms_R = new LevelMeterScalar(288, 25, ft, -60, 5, LevelMeterScalar.eOrientation.Vertical, LevelMeterScalar.textSide.right);

                lms_L_Analyser.showLevelMeterScalar();
                //lms_R_Analyser.showLevelMeterScalar();

                // Analyser ON
                ThreadAnalyserMeter.Resume();
                readAnalyserPage = true;

            }
            else 
            {
                // Analyser OFF
                readAnalyserPage = false;
                ThreadAnalyserMeter.Suspend();
            }

        }


        //------------------------------------------------------------------------------
        // Aktivieren und Deaktiven von Elementen anderer Seiten
        //------------------------------------------------------------------------------
        static void deaktivEqElements(bool val) 
        {
            try
            {
                sl_eq0.isDeaktive = val;
                sl_eq1.isDeaktive = val;
                sl_eq2.isDeaktive = val;
                sl_eq3.isDeaktive = val;
                sl_eq4.isDeaktive = val;
                sl_eq5.isDeaktive = val;
                sl_eq6.isDeaktive = val;
                sl_eq7.isDeaktive = val;
                sl_eq8.isDeaktive = val;
                sl_eq9.isDeaktive = val;

                txt_btn_eq0.isDeaktive = val;
                txt_btn_eq1.isDeaktive = val;
                txt_btn_eq2.isDeaktive = val;
                txt_btn_eq3.isDeaktive = val;
                txt_btn_eq4.isDeaktive = val;
                txt_btn_eq5.isDeaktive = val;
                txt_btn_eq6.isDeaktive = val;
                txt_btn_eq7.isDeaktive = val;
                txt_btn_eq8.isDeaktive = val;
                txt_btn_eq9.isDeaktive = val;
            }
            catch (Exception ex) { }
        }

        static void deaktivInputElements(bool val) 
        {
            try
            {
            sl_GainR.isDeaktive = val;
            sl_GainL.isDeaktive = val;
            }
            catch (Exception ex) { }
        }


        //------------------------------------------------------------------------------
        // Eventhandler Elements
        //------------------------------------------------------------------------------

        static void sl_GainL_ValueChanged(int val)
        {
            // Setting Value zum Speichern
            setting_GainL = val;
            
            // Read Threads anhalten
            readLevelStartPage = false;
            Thread.Sleep(50);

            // Send Gain 
            // https://wiki.analog.com/resources/tools-software/sigmastudio/toolbox/basicdsp/lineargain
            dsp.send_Gain(DSP_Register.ADDR_GAIN1, val, true);

            // TextBox
            txt_GainL = new Label(40, 185, ft, "L: " + sl_GainL.Value.ToString());
            
            Thread.Sleep(50);

            // Save Eeprom & Set DSP I2C bus back
            Eeprom.writeRegister(EepromSettingAdress + 1, (byte)setting_GainL);
            dsp.initI2CBus();
            Thread.Sleep(100);

            // Read Threads weiter
            readLevelStartPage = true;

        }
        static void sl_GainR_ValueChanged(int val)
        {
            // Setting Value
            setting_GainR = val;

            // Read Threads anhalten
            readLevelStartPage = false;
            Thread.Sleep(50);

            // Send Gain DSP
            dsp.send_Gain(DSP_Register.ADDR_GAIN2, val, true);

            // TextBox
            txt_GainR = new Label(260, 185, ft, "R: " + sl_GainR.Value.ToString());
            
            Thread.Sleep(50);

            // Save Eeprom & Set DSP I2C bus back
            Eeprom.writeRegister(EepromSettingAdress + 2, (byte)setting_GainR);
            dsp.initI2CBus();
            Thread.Sleep(100);


            // Read Threads weiter
            readLevelStartPage = true;
        }
        static void sl_volume_ValueChanged(int val) 
        {
            // TextBox
            txt_Volumen_Value.Text = val.ToString();

            // Setting Value
            setting_Volumen = val;

            // Read Threads anhalten
            readLevelStartPage = false;
            Thread.Sleep(50);

            // Send Volumen DSP
            dsp.send_DB(DSP_Register.ADDR_GAIN_VOL, val, true);

            Thread.Sleep(150);
            
            // Save Eeprom & Set DSP I2C bus back
            Eeprom.writeRegister(EepromSettingAdress, (byte)setting_Volumen);
            dsp.initI2CBus();
            Thread.Sleep(100);

            // Read Threads weiter
            readLevelStartPage = true;        
        }
        
        static void dimThreadOnOff_StateChanged(StateButton.eCurrState new_state)
        {
            // ON
            if (new_state == StateButton.eCurrState.state2)
            {
                myDimThread.Resume();
                setting_dimThreadOnOff = true;
            }
            // OFF
            else 
            {
                myDimThread.Suspend();
                setting_dimThreadOnOff = false;
            }

            // Save Eeprom & Set DSP I2C bus back
            byte val = setting_dimThreadOnOff ? (byte)1 : (byte)0;
            Eeprom.writeRegister(EepromSettingAdress + 15, val);
            dsp.initI2CBus();
            Thread.Sleep(10);

        }

        static void btn_muteLine_StateChanged(StateButton.eCurrState new_state)
        {
            // Stop Read Thread
            ThreadInputLevelMeter.Suspend();

            // ON
            if (new_state == StateButton.eCurrState.state2)
            {
                // Send DSP                
                dsp.send_Mute(DSP_Register.ADDR_MUTE_Line, true);
                setting_btn_muteLine = true;
            }
            // OFF
            else
            {
                // Send DSP
                dsp.send_Mute(DSP_Register.ADDR_MUTE_Line, false);
                setting_btn_muteLine = false;
            }

            // Save Eeprom & Set DSP I2C bus back
            byte val = setting_btn_muteLine ? (byte)1 : (byte)0;
            Eeprom.writeRegister(EepromSettingAdress + 13, val);
            dsp.initI2CBus();
            Thread.Sleep(50);
            
            // Run Thread
            ThreadInputLevelMeter.Resume();
        }

        static void btn_muteSpeaker_StateChanged(StateButton.eCurrState new_state)
        {
            // Stop Read Thread
            ThreadInputLevelMeter.Suspend();

            // ON
            if (new_state == StateButton.eCurrState.state2)
            {
                // Send DSP
                dsp.send_Mute(DSP_Register.ADDR_MUTE_Speaker, true);
                setting_btn_muteSpeaker = true;
            }
            // OFF
            else
            {
                // Send DSP
                dsp.send_Mute(DSP_Register.ADDR_MUTE_Speaker, false);
                setting_btn_muteSpeaker = false;
            }

            // Save Eeprom & Set DSP I2C bus back
            byte val = setting_btn_muteSpeaker ? (byte)1 : (byte)0;
            Eeprom.writeRegister(EepromSettingAdress + 14, val);
            dsp.initI2CBus();
            Thread.Sleep(50);

            // Run Thread
            ThreadInputLevelMeter.Resume();
        }


        #region Eventhandler EQ Change
        //------------------------------------------------------------------------------
        // Eventhandler EQ
        //------------------------------------------------------------------------------
        // 46 Hz
        static void sl_eq0_ValueChanged(int val)
        {
           // int[] eq = dsp.calEq(46f, 1.41f, (float)val );
            dsp.sendEQSafeLoad(DSP_Register.EQ1940DualS10B1, 46f, (float)val);
            setting_Eq0 = val;

            // Save Eeprom & Set DSP I2C bus back
            Thread.Sleep(20);
            Eeprom.writeRegister(EepromSettingAdress + 3, (byte)setting_Eq0);
            dsp.initI2CBus();
            Thread.Sleep(20);
        }

        // 61.5 Hz
        static void sl_eq1_ValueChanged(int val)
        {
            //dsp.calEq(61.5f, 1.41f, (float)val);
            dsp.sendEQSafeLoad(DSP_Register.EQ1940DualS10B2, 61.5f, (float)val);
            setting_Eq1 = val;

            // Save Eeprom & Set DSP I2C bus back
            Thread.Sleep(20);
            Eeprom.writeRegister(EepromSettingAdress + 4, (byte)setting_Eq1);
            dsp.initI2CBus();
            Thread.Sleep(20);
        }

        // 168.7 Hz
        static void sl_eq2_ValueChanged(int val)
        {
            //dsp.calEq(168.7f, 1.41f, (float)val);
            dsp.sendEQSafeLoad(DSP_Register.EQ1940DualS10B3, 168.7f, (float)val);
            setting_Eq2 = val;

            // Save Eeprom & Set DSP I2C bus back
            Thread.Sleep(20);
            Eeprom.writeRegister(EepromSettingAdress + 5, (byte)setting_Eq2);
            dsp.initI2CBus();
            Thread.Sleep(20);
        }

        //343 Hz
        static void sl_eq3_ValueChanged(int val)
        {
            //dsp.calEq(343f, 1.41f, (float)val);
            dsp.sendEQSafeLoad(DSP_Register.EQ1940DualS10B4, 343f, (float)val);
            setting_Eq3 = val;

            // Save Eeprom & Set DSP I2C bus back
            Thread.Sleep(20);
            Eeprom.writeRegister(EepromSettingAdress + 6, (byte)setting_Eq3);
            dsp.initI2CBus();
            Thread.Sleep(20);
        }

        //615 Hz
        static void sl_eq4_ValueChanged(int val)
        {
            //dsp.calEq(615f, 1.41f, (float)val);
            dsp.sendEQSafeLoad(DSP_Register.EQ1940DualS10B5, 615f, (float)val);
            setting_Eq4 = val;

            // Save Eeprom & Set DSP I2C bus back
            Thread.Sleep(20);
            Eeprom.writeRegister(EepromSettingAdress + 7, (byte)setting_Eq4);
            dsp.initI2CBus();
            Thread.Sleep(20);
        }

        //1080
        static void sl_eq5_ValueChanged(int val)
        {
            //dsp.calEq(1080f, 1.41f, (float)val);
            dsp.sendEQSafeLoad(DSP_Register.EQ1940DualS10B6, 1080f, (float)val);
            setting_Eq5 = val;

            // Save Eeprom & Set DSP I2C bus back
            Thread.Sleep(20);
            Eeprom.writeRegister(EepromSettingAdress + 8, (byte)setting_Eq5);
            dsp.initI2CBus();
            Thread.Sleep(20);
        }

        // 3430
        static void sl_eq6_ValueChanged(int val)
        {
            //dsp.calEq(3430f, 1.41f, (float)val);
            dsp.sendEQSafeLoad(DSP_Register.EQ1940DualS10B7, 3430f, (float)val);
            setting_Eq6 = val;

            // Save Eeprom & Set DSP I2C bus back
            Thread.Sleep(20);
            Eeprom.writeRegister(EepromSettingAdress + 9, (byte)setting_Eq6);
            dsp.initI2CBus();
            Thread.Sleep(20);
        }

        // 6150
        static void sl_eq7_ValueChanged(int val)
        {
            //dsp.calEq(6150f, 1.41f, (float)val);
            dsp.sendEQSafeLoad(DSP_Register.EQ1940DualS10B8, 6150f, (float)val);
            setting_Eq7 = val;

            // Save Eeprom & Set DSP I2C bus back
            Thread.Sleep(20);
            Eeprom.writeRegister(EepromSettingAdress + 10, (byte)setting_Eq7);
            dsp.initI2CBus();
            Thread.Sleep(20);
        }

        // 10800
        static void sl_eq8_ValueChanged(int val)
        {
            //dsp.calEq(10800f, 1.41f, (float)val);
            dsp.sendEQSafeLoad(DSP_Register.EQ1940DualS10B9, 10800f, (float)val);
            setting_Eq8 = val;

            // Save Eeprom & Set DSP I2C bus back
            Thread.Sleep(20);
            Eeprom.writeRegister(EepromSettingAdress + 11, (byte)setting_Eq8);
            dsp.initI2CBus();
            Thread.Sleep(20);
        }

        // 15870
        static void sl_eq9_ValueChanged(int val)
        {
            //dsp.calEq(15870f, 1.41f, (float)val);
            dsp.sendEQSafeLoad(DSP_Register.EQ1940DualS10B10, 15870f, (float)val);
            setting_Eq9 = val;

            // Save Eeprom & Set DSP I2C bus back
            Thread.Sleep(20);
            Eeprom.writeRegister(EepromSettingAdress + 12, (byte)setting_Eq9);
            dsp.initI2CBus();
            Thread.Sleep(20);
        }
        #endregion

        //------------------------------------------------------------------------------
        // EQ TO NULL
        //------------------------------------------------------------------------------
        static void txt_btn_eq0_SetNull() 
        {
            sl_eq0.Value = 0;
        }
        static void txt_btn_eq1_SetNull()
        {
            sl_eq1.Value = 0;
        }
        static void txt_btn_eq2_SetNull()
        {
            sl_eq2.Value = 0;
        }
        static void txt_btn_eq3_SetNull()
        {
            sl_eq3.Value = 0;
        }
        static void txt_btn_eq4_SetNull()
        {
            sl_eq4.Value = 0;        
        }
        static void txt_btn_eq5_SetNull()
        {
            sl_eq5.Value = 0;
        }
        static void txt_btn_eq6_SetNull()
        {
            sl_eq6.Value = 0;
        }
        static void txt_btn_eq7_SetNull()
        {
            sl_eq7.Value = 0;
        }
        static void txt_btn_eq8_SetNull()
        {
            sl_eq8.Value = 0;
        }
        static void txt_btn_eq9_SetNull()
        {
            sl_eq9.Value = 0;
        }
        

        //------------------------------------------------------------------------------
        // Level Detector Threads
        //------------------------------------------------------------------------------
        public static void thread_Read_Input() 
        {
            while (true) 
            {
                if (readLevelStartPage == true)
                {
                    // DSP Adress über zwei Byte bei 1440x moglicherweise 1701 nur ein byte ????
                    // read  Left Side                    
                    int val = dsp.readLevelMeter(askLM_In_L1, askLM_In_L2);
                    if (lm_L.MinValue <= val && val <= lm_L.MaxValue) {
                        lm_L.Value = val;
                    }
                    else
                    {
                        if (lm_L.Value != lm_L.MinValue)
                        {
                            lm_L.Value = lm_L.MinValue;
                        }
                    }

                    // right
                    val = dsp.readLevelMeter(askLM_In_R1, askLM_In_R2);
                    if (lm_R.MinValue <= val && val <= lm_R.MaxValue)
                    {
                        lm_R.Value = val;
                    }
                    else
                    {
                        if (lm_R.Value != lm_R.MinValue)
                        {
                            lm_R.Value = lm_R.MinValue;
                        }
                    }

                }
                else
                {
                    Thread.Sleep(500);
                }
            }
        }


        //------------------------------------------------------------------------------
        // Analyser Threads
        //------------------------------------------------------------------------------
        public static void thread_AnalyserRead() 
        {
            while (true) 
            {
                if (readAnalyserPage == true)
                {
                    int val;

                    // DSP Adress über zwei Byte bei 1440x moglicherweise 1701 nur ein byte ????
                    // A0
                    val = dsp.readLevelMeter(askLM_A0_L1, askLM_A0_L2);
                    if (lm_a0.MinValue <= val && val <= lm_a0.MaxValue)
                    {
                        lm_a0.Value = val;
                    }
                    else
                    {
                        if (lm_a0.Value != lm_a0.MinValue)
                        {
                            lm_a0.Value = lm_a0.MinValue;
                        }
                    }
                    // A1
                    val = dsp.readLevelMeter(askLM_A1_L1, askLM_A0_L2);
                    if (lm_a1.MinValue <= val && val <= lm_a1.MaxValue)
                    {
                        lm_a1.Value = val;
                    }
                    else
                    {
                        if (lm_a1.Value != lm_a1.MinValue)
                        {
                            lm_a1.Value = lm_a1.MinValue;
                        }
                    }
                    // A2
                    val = dsp.readLevelMeter(askLM_A2_L1, askLM_A0_L2);
                    if (lm_a2.MinValue <= val && val <= lm_a2.MaxValue)
                    {
                        lm_a2.Value = val;
                    }
                    else
                    {
                        if (lm_a2.Value != lm_a2.MinValue)
                        {
                            lm_a2.Value = lm_a2.MinValue;
                        }
                    }
                    // A3
                    val = dsp.readLevelMeter(askLM_A3_L1, askLM_A0_L2);
                    if (lm_a3.MinValue <= val && val <= lm_a3.MaxValue)
                    {
                        lm_a3.Value = val;
                    }
                    else
                    {
                        if( lm_a3.Value != lm_a3.MinValue)
                        {
                            lm_a3.Value = lm_a3.MinValue;
                        }
                    }
                    // A4
                    val = dsp.readLevelMeter(askLM_A4_L1, askLM_A0_L2);
                    if (lm_a4.MinValue <= val && val <= lm_a4.MaxValue)
                    {
                        lm_a4.Value = val;
                    }
                    else
                    {
                        if (lm_a4.Value != lm_a4.MinValue)
                        {
                            lm_a4.Value = lm_a4.MinValue;
                        }
                    }
                    // A5
                    val = dsp.readLevelMeter(askLM_A5_L1, askLM_A0_L2);
                    if (lm_a5.MinValue <= val && val <= lm_a5.MaxValue)
                    {
                        lm_a5.Value = val;
                    }
                    else
                    {
                        if (lm_a5.Value != lm_a5.MinValue)
                        {
                            lm_a5.Value = lm_a5.MinValue;
                        }
                    }
                    // A6
                    val = dsp.readLevelMeter(askLM_A6_L1, askLM_A0_L2);
                    if (lm_a6.MinValue <= val && val <= lm_a6.MaxValue)
                    {
                        lm_a6.Value = val;
                    }
                    else
                    {
                        if (lm_a6.Value != lm_a6.MinValue)
                        {
                            lm_a6.Value = lm_a6.MinValue;
                        }
                    }
                    // A7
                    val = dsp.readLevelMeter(askLM_A7_L1, askLM_A0_L2);
                    if (lm_a7.MinValue <= val && val <= lm_a7.MaxValue)
                    {
                        lm_a7.Value = val;
                    }
                    else
                    {
                        if (lm_a7.Value != lm_a7.MinValue)
                        {
                            lm_a7.Value = lm_a7.MinValue;
                        }
                    }
                    // A8
                    val = dsp.readLevelMeter(askLM_A8_L1, askLM_A0_L2);
                    if (lm_a8.MinValue <= val && val <= lm_a8.MaxValue)
                    {
                        lm_a8.Value = val;
                    }
                    else
                    {
                        if (lm_a8.Value != lm_a8.MinValue)
                        {
                            lm_a8.Value = lm_a8.MinValue;
                        }
                    }
                    // A9
                    val = dsp.readLevelMeter(askLM_A9_L1, askLM_A0_L2);
                    if (lm_a9.MinValue <= val && val <= lm_a9.MaxValue)
                    {
                        lm_a9.Value = val;
                    }
                    else
                    {
                        if (lm_a9.Value != lm_a9.MinValue)
                        {
                            lm_a9.Value = lm_a9.MinValue;
                        }
                    }

                }
                else 
                {
                    Thread.Sleep(500);
                }
            }        
        }
        

        //------------------------------------------------------------------------------
        // INIT DISPLAY FKT's
        //------------------------------------------------------------------------------
        #region Region INIT DISPLAY FKT's
        // DIM Thread
        public static void Thread_DIM()
        {
            // dim, and then turn off, the display if not interrupted within idleTime
            ft.TouchDownEvent += new FEZ_Components.FEZTouch.TouchEventHandler(fezTouch_TouchDownEvent);

            displayMode = FEZ_Components.FEZTouch.DisplayMode.Normal;
            idleTimer = IDLE_TIME;
            while (true)
            {
                switch (displayMode)
                {
                    case FEZ_Components.FEZTouch.DisplayMode.Normal:
                        ft.SetDisplayMode(displayMode);
                        displayMode = FEZ_Components.FEZTouch.DisplayMode.Dim;
                        break;
                    case FEZ_Components.FEZTouch.DisplayMode.Dim:
                        ft.SetDisplayMode(displayMode);
                        displayMode = FEZ_Components.FEZTouch.DisplayMode.Off;
                        break;
                    case FEZ_Components.FEZTouch.DisplayMode.Off:
                        ft.SetDisplayMode(displayMode);
                        displayMode = FEZ_Components.FEZTouch.DisplayMode.Off;
                        break;
                }
                Thread.Sleep(idleTimer);
            }
        }

        // DIM EVENT - wake up
        static void fezTouch_TouchDownEvent(int x, int y)
        {
            displayMode = FEZ_Components.FEZTouch.DisplayMode.Normal;
            ft.SetDisplayMode(displayMode);
        }

        // INIT DISPLAY
        public static void InitGraphics()
        {
            FEZ_Components.FEZTouch.LCDConfiguration lcd_config = new FEZ_Components.FEZTouch.LCDConfiguration(
                            FEZ_Pin.Digital.Di28,
                            FEZ_Pin.Digital.Di20,
                            FEZ_Pin.Digital.Di22,
                            FEZ_Pin.Digital.Di23,
                            new FEZ_Pin.Digital[8] { FEZ_Pin.Digital.Di51, FEZ_Pin.Digital.Di50, FEZ_Pin.Digital.Di49, FEZ_Pin.Digital.Di48, FEZ_Pin.Digital.Di47, FEZ_Pin.Digital.Di46, FEZ_Pin.Digital.Di45, FEZ_Pin.Digital.Di44 },
                            FEZ_Pin.Digital.Di24,
                            FEZ_Pin.Digital.Di26,
                            FEZ_Components.FEZTouch.Orientation.LandscapeInverse
                            );

            // Touch
            FEZ_Components.FEZTouch.TouchConfiguration touch_config = new FEZ_Components.FEZTouch.TouchConfiguration(SPI.SPI_module.SPI2, FEZ_Pin.Digital.Di25, FEZ_Pin.Digital.Di34);
            ft = new FEZ_Components.FEZTouch(lcd_config, touch_config);

            // Clear screen with black BG
            ft.ClearScreen();
        }
        #endregion

    }
}
