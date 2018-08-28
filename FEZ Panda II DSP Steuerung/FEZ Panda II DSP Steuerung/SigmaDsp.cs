using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using GHIElectronics.NETMF.System;  // Math COS und Sin für NETMF 4.1 notwendig
using SigmaDSP.Register;


namespace SigmaDSP
{
    public class SigmaDsp : IDisposable
    {
        // VARs
        private static ushort i2cAdress = 0x34;     // 7BIT; 104 << 1 = 52 == 0x34;   Adress: https://ez.analog.com/thread/94409-sigma-studio-ic-1-address-incorrect
        private static int i2cSpeed = 400;
        private static int sampleRate = 44100;

        private static I2CDevice.Configuration I2C_dev;   //config
        private static I2CDevice I2CcommBus;             // device

        private static bool waitForData = false;


        // Konstructor
        public SigmaDsp(ushort adress, int speed, int samplingRate)
        {
            i2cAdress = adress;
            i2cSpeed = speed;
            sampleRate = samplingRate;

            I2C_dev = new I2CDevice.Configuration(i2cAdress, i2cSpeed);         // Device Adress & Speed
            I2CcommBus = new I2CDevice(I2C_dev);        
        }

        // Destructor
        public void Dispose() 
        {
            I2CcommBus.Dispose();
        }

        // Return the i2c-bus
        public I2CDevice get_I2CBus()
        {
            return I2CcommBus;
        }

        // Init Bus, multible Devices
        public void initI2CBus() 
        {
            // Init Bus, multible Devices
            I2CcommBus.Config = I2C_dev;
        }


        // Send Level DB Val
        public void send_DB(UInt16 register, int value_db, bool format23)
        {
            // send byte - 6
            byte[] send = new byte[6];

            // Adresse
            send[0] = (byte)(register >> 8);        // hight byte
            send[1] = (byte)(register >> 0);        // low byte
            
            // DB Convert & set data
            byte[] data = convertDBtoHex(value_db, format23);
            send[2] = data[0];
            send[3] = data[1];
            send[4] = data[2];
            send[5] = data[3];

            // send
            this.send(send);  
        }

        // Send Gain Val
        public void send_Gain(UInt16 register, int value_db, bool format23) 
        {
            // send byte - 6
            byte[] send = new byte[6];

            // Adresse
            send[0] = (byte)(register >> 8);        // hight byte
            send[1] = (byte)(register >> 0);        // low byte
            
            // LiniearGain to FixedFloatPint
            int fixFP = convertFixedFlotPoint(value_db, format23);

            // 3. Int to Hex
            send[2] = (byte)(fixFP >> 24);
            send[3] = (byte)(fixFP >> 16);
            send[4] = (byte)(fixFP >> 8);
            send[5] = (byte)(fixFP >> 0);
            
            // send
            this.send(send);          
        }
         
        // Send Mute Val
        public void send_Mute(UInt16 register, bool state) 
        {
            // send byte - 6
            byte[] send = new byte[6];

            // Adresse
            send[0] = (byte)(register >> 8);        // hight byte
            send[1] = (byte)(register >> 0);        // low byte

            send[2] = 0x00;
            send[3] = 0x00;     // default "false"
            send[4] = 0x00;
            send[5] = 0x00;

            if (state == true) {
                send[3] = 0x80;
            }
            // send
            this.send(send);
        }


        /// <summary>
        /// Send EQ
        /// </summary>
        /// <param name="register">Basis Register für B1</param>
        /// <param name="band">Frequenz</param>
        /// <param name="db">db value</param>
        public void sendEQ(UInt16 register, float band, float db) 
        {
            // CAL EQ, Return Round Fixed Point 23
            int[] eq = this.calEq(band, 1.41f, db);
            foreach (int fixedVal in eq) 
            {
                // send byte - 6
                byte[] send = new byte[6];

                // Adresse
                send[0] = (byte)(register >> 8);        // hight byte
                send[1] = (byte)(register >> 0);        // low byte
                
                // Value to Hex
                send[2] = (byte)(fixedVal >> 24);
                send[3] = (byte)(fixedVal >> 16);
                send[4] = (byte)(fixedVal >> 8);
                send[5] = (byte)(fixedVal >> 0);

                // Send
                this.send(send);

                // Register Hochzählen
                register++;
            }
        }

        /// <summary>
        /// Send EQ Safe Load
        /// </summary>
        /// <param name="register">Basis Register für B1</param>
        /// <param name="band">Frequenz</param>
        /// <param name="db">db value</param>
        public void sendEQSafeLoad(UInt16 register, float band, float db) 
        {
            // Count SAFE Load Register, entspricht dem EQ Array
            int count = 0;  

            // CAL EQ, Return Round Fixed Point 23
            int[] eq = this.calEq(band, 1.41f, db);
            foreach (int fixedVal in eq)
            {
                // ADDR, Write target Def Register
                byte[] sendAddr = new byte[4];
                sendAddr[0] = (byte)((DSP_Register.SAFELOAD_ADDR + count) >> 8);    // hight byte
                sendAddr[1] = (byte)((DSP_Register.SAFELOAD_ADDR + count) >> 0);    // low byte

                sendAddr[2] = (byte)(register >> 8);
                sendAddr[3] = (byte)(register >> 0);
                
                // Send ADDR
                this.send(sendAddr);


                // DATA
                byte[] sendData = new byte[7];
                sendData[0] = (byte)((DSP_Register.SAFELOAD_DATA + count) >> 8);    // hight byte
                sendData[1] = (byte)((DSP_Register.SAFELOAD_DATA + count) >> 0);    // low byte

                sendData[2] = 0;    // Wichtig 3. Byte muss Null sein!

                // Value to Hex
                sendData[3] = (byte)(fixedVal >> 24);
                sendData[4] = (byte)(fixedVal >> 16);
                sendData[5] = (byte)(fixedVal >> 8);
                sendData[6] = (byte)(fixedVal >> 0);

                // Send DATA
                this.send(sendData);
                

                // Register und Counter Hochzählen
                register++;
                count++;
            }

            // Trigger SafeLoad, Set IST im Core Register
            // 1. read Core Control Register
            int controlRegister = DSP_Register.DSPcoreControl;
            byte[] val = this.readRegister(controlRegister);

            // 2. Mask Bit, SET Bit 5 to 1
            val[1] |= (byte)(1 << 5);   // 5. Bit im Register auf 1 setzten

            // 3 Send Register Data Back
            byte[] sRegister = new byte[4];
            sRegister[0] = (byte)(controlRegister >> 8);
            sRegister[1] = (byte)(controlRegister >> 0);
            sRegister[2] = val[0];
            sRegister[3] = val[1];

            this.send(sRegister);

        }
                
        /// <summary>
        /// I2C Send
        /// </summary>
        /// <param name="values">Byte Array</param>
        private void send( byte[] values ) 
        {
            // Init Bus, multible Devices
            I2CcommBus.Config = I2C_dev;

            I2CDevice.I2CTransaction[] xActions = new I2CDevice.I2CTransaction[1];
            xActions[0] = I2CDevice.CreateWriteTransaction(values);
            I2CcommBus.Execute( xActions, 50);            
        }

        
        // I2C Read Level Meter, Return 3Bytes
        public int readLevelMeter(byte[] send1, byte[] send2) 
        {
            /*
            this.send(send1);
            this.send(send2);
            
            
            //I2CDevice.I2CTransaction[] xActions = new I2CDevice.I2CTransaction[1];
            //xActions[0] = I2CDevice.CreateWriteTransaction(send1);
            //I2CcommBus.Execute(xActions, 10);

            //xActions[0] = I2CDevice.CreateWriteTransaction(send2);
            //I2CcommBus.Execute(xActions, 10);


            byte[] readLevel = new byte[3];
            I2CDevice.I2CTransaction[] rActions = new I2CDevice.I2CTransaction[1];
            rActions[0] = I2CDevice.CreateReadTransaction(readLevel);
            I2CcommBus.Execute(rActions, 1000);

             */

            // Init Bus, multible Devices
            I2CcommBus.Config = I2C_dev;

            byte[] readLevel = new byte[3];
            
            // Write
            I2CDevice.I2CTransaction[] xActions = new I2CDevice.I2CTransaction[1];
            xActions[0] = I2CDevice.CreateWriteTransaction(send1);
            I2CcommBus.Execute(xActions, 50);

            // Pause !

            // Write & Read
            xActions = new I2CDevice.I2CTransaction[2];
            xActions[0] = I2CDevice.CreateWriteTransaction(send2);
            xActions[1] = I2CDevice.CreateReadTransaction(readLevel);
            I2CcommBus.Execute(xActions, 1000);


            // TODO Return DB daten,
            // also gleich umrechnen!!!
            // vielleicht: int i = (int)(sbyte)b;

            // Achtung wenn 0x000000 dann nicht 0DB sondern -127DB
            if (readLevel[0] == 0x00 && readLevel[1] == 0x00 && readLevel[2] == 0x00) {
                return -127;
            }

            int db = convertHextoDB(readLevel, false);

            return db;
        }

        // Read Register Data, Ret 2bytes
        public byte[] readRegister(int register) 
        {
            // Init Bus, multible Devices
            I2CcommBus.Config = I2C_dev;

            // Register
            byte[] rRegister = new byte[2];
            rRegister[0] = (byte)(register >> 8);
            rRegister[1] = (byte)(register >> 0);

            // Read Data
            byte[] readVal = new byte[2];

            // Write & Read
            I2CDevice.I2CTransaction[] xActions = new I2CDevice.I2CTransaction[2];
            xActions[0] = I2CDevice.CreateWriteTransaction(rRegister);  // Ask Register
            xActions[1] = I2CDevice.CreateReadTransaction(readVal);     // Get DATA
            I2CcommBus.Execute(xActions, 1000);

            return readVal;
        }

        // Convert DB value to Hex Arrray: Look: https://ez.analog.com/thread/86003 ; https://wiki.analog.com/resources/tools-software/sigmastudio/usingsigmastudio/numericformats ; http://www.rfwireless-world.com/calculators/floating-vs-fixed-point-converter.html (script auf der Seite ansehen!!!)       
        public byte[] convertDBtoHex(int val, bool format23)
        {
            byte[] retByte = new byte[4];

            // 1. Dezibel to float, double ist besser aber NETMF kann es nicht besser
            // 20log(x) -> 10^(val/20)
            // tabelle mit werten wäre schneller aber für Volume Control ok            
            float dVal = ( (float)val / 20);
            dVal = (float)System.Math.Pow(10, dVal);

            //// 2. flot to Fixed Flotpoint 5.23, für 28BIT Daten 
            //// für 24BIT Daten 5.19 Format
            //// val*(2^23) oder val*(2^19); Volume ADAU1701 und 144x  => 5.23 Format 
            //int potenz = 23;
            //if (format23 == false) {
            //    potenz = 19;
            //}
            ////float fix = (floatVal * System.Math.Pow(2, potenz));
            //float fixpointVal = dVal * (float)System.Math.Pow(2, potenz);
            //int fixedVal = (int) System.Math.Round(fixpointVal);

            int fixedVal = convertFixedFlotPoint(dVal, format23);

            // 3. Int to Hex
            retByte[0] = (byte)(fixedVal >> 24);
            retByte[1] = (byte)(fixedVal >> 16);
            retByte[2] = (byte)(fixedVal >> 8);
            retByte[3] = (byte)(fixedVal >> 0);
            
            return retByte;
        }


        public int convertFixedFlotPoint(float val, bool format23) 
        {
            // 2. flot to Fixed Flotpoint 5.23, für 28BIT Daten 
            // für 24BIT Daten 5.19 Format
            // val*(2^23) oder val*(2^19); Volume ADAU1701 und 144x  => 5.23 Format 
            int potenz = 23;
            if (format23 == false)
            {
                potenz = 19;
            }
            //float fix = (floatVal * System.Math.Pow(2, potenz));
            float fixpointVal = val * (float)System.Math.Pow(2, potenz);
            int fixedVal = (int)System.Math.Round(fixpointVal);
            return fixedVal;
        }

        
        public int convertHextoDB( byte[] array, bool format23 ) 
        {                                    
            int val = 0;
            float fval;

            //flt_number  = fx_number/Math.pow(2,Q_frmt);
            int potenz = 23;
            if (format23 == false)
            {
               // potenz = 19;
                // Hex to Int 
                val = array[0] << 16 |
                      array[1] << 8 |
                      array[2] << 0;
                
                // Readback to DB
                // https://wiki.analog.com/resources/tools-software/sigmastudio/usingsigmastudio/numericformats
                
                // Für DB Werte Multiplikation dann 5.21 Format
                val = (int)(val * 16);

                // 5.19 fixpoint to float.
                //fval = (val / (float)(System.Math.Pow(2, potenz)));
                // Float to DB
               // val = (int)(96.32959861f * (val / 219 - 1));                                
               // return val;
            }
            else {
                // Hex to Int
                val = array[0] << 24 |
                      array[1] << 16 |
                      array[2] << 8 |
                      array[3] << 0;
            }

            fval = (val / (float)(System.Math.Pow(2, potenz)));
            val = (int)(20 * MathEx.Log10(fval/1));
            return val;
        }


        // Calculate EQ, genauigkeit nur float
        public int[] calEq(float frq, float qPoint, float boost )
        {
            // fixed Var
            float gain = 0.0f;

            // return Array
            int[] retArray = new int[5];

            // Intermediate Values 

            // =10^(boost/40)
            float ax = (float)System.Math.Pow(10, ((float)(boost / 40)));

            // =2 * PI() * frq / sampleRate
            float omega = (float)2 * (float)System.Math.PI * (float)(frq / sampleRate);

            // =SIN(omega)
            float sn = (float)MathEx.Sin(omega);

            // =COS(omega)
            float cs = (float)MathEx.Cos(omega);

            // = sn / (2 * qPoint)
            float alpha = sn / (2 * qPoint);

            // = 1 + ( alpha / ax )
            float a0 = 1 + ( alpha / ax );

            // = 10^( gain / 20 ) / a0
            float gainlinear = (float)(System.Math.Pow(10, (gain / 20)) / a0);


            // IIR Coefficient Values

            // = ( 1 + ( alpha * ax )) * gainlinear
            float b0 = (1 + (alpha * ax)) * gainlinear;
            
            // = -( 2 * cs ) * gainlinear
            float b1 = -( 2 * cs ) * gainlinear;

            // = ( 1 - (alpha * ax)) * gainlinear
            float b2 = (1 - (alpha * ax)) * gainlinear;

            // = (2 * cs ) / a0
            float a1 = (2 * cs) / a0;

            // = -( 1 - (alpha / ax)) / a0
            float a2 = -(1 - (alpha / ax)) / a0;

            // Convert to FixFloat 5.23

            retArray[0] = convertFixedFlotPoint(b0, true);
            retArray[1] = convertFixedFlotPoint(b1, true);
            retArray[2] = convertFixedFlotPoint(b2, true);
            retArray[3] = convertFixedFlotPoint(a1, true);
            retArray[4] = convertFixedFlotPoint(a2, true);

            return retArray;
        }

        // Hex Array to Int32 ACHTUNG Little Endian
        // Look: http://forums.netduino.com/index.php?/topic/308-bitconverter/
        public int HexToInt32(byte[] value, int index = 0)
        {
            return (
                value[0 + index] << 0 |
                value[1 + index] << 8 |
                value[2 + index] << 16 |
                value[3 + index] << 24);
        }


        // NETMF 4.1 hat kein COS und SIN -> GHI Electronics NETMF Library: MathEx.Cos Method 
        // also 
        // https://forum.byte-welt.net/t/sinus-ohne-math-sin-berechnen/11788/5
        // http://www.chemieonline.de/forum/archive/index.php/t-35087.html
        public static double sin(double x)
        {
            double EPSILON = 0.0000001;

            double value = x;
            double sum = x;
            for (int n = 1; System.Math.Abs( (int)value ) > EPSILON; n++)
            {
                value *= -x * x / (2 * n + 1) / (2 * n);
                sum += value;
            }
            return sum;
        }
    }
}
