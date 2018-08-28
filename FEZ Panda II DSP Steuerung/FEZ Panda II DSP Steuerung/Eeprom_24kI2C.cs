using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Threading;

namespace SigmaDSP.Eeprom
{
    class Eeprom_24kI2C : IDisposable
    {
        // VARs
        private static ushort i2cAdress = 0xA0 >> 1;     // 7BIT Adresse; 160 << 1 = 80 == 0x50
        private static int i2cSpeed = 100;

        private static UInt16 maxAdress = 0xf777;

        private static I2CDevice.Configuration I2C_dev;   //config
        private static I2CDevice I2CcommBus;             // device

        /// <summary>
        /// Konstruktor
        /// </summary>
        public Eeprom_24kI2C()
        {
            I2C_dev = new I2CDevice.Configuration(i2cAdress, i2cSpeed);         // Device Adress & Speed
            I2CcommBus = new I2CDevice(I2C_dev);   
        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="adress"></param>
        /// <param name="speed"></param>
        public Eeprom_24kI2C(ushort adress, int speed, I2CDevice bus) 
        {
            i2cAdress = adress;
            i2cSpeed = speed;

            I2C_dev = new I2CDevice.Configuration(i2cAdress, i2cSpeed);         // Device Adress & Speed
            I2CcommBus = bus;
        }

        /// <summary>
        /// Konstruktor
        /// </summary>
        /// <param name="adress"></param>
        /// <param name="speed"></param>
        public Eeprom_24kI2C(I2CDevice bus)
        {
            I2C_dev = new I2CDevice.Configuration(i2cAdress, i2cSpeed);         // Device Adress & Speed
            I2CcommBus = bus;
        }


        /// <summary>
        /// Destructor
        /// </summary>
        public void Dispose()
        {
            I2CcommBus.Dispose();
        }




        /// <summary>
        /// I2C Send
        /// </summary>
        /// <param name="values">Byte Array</param>
        private void send(byte[] values)
        {
            // Init Bus, multible Devices
            I2CcommBus.Config = I2C_dev;

            I2CDevice.I2CTransaction[] xActions = new I2CDevice.I2CTransaction[1];
            xActions[0] = I2CDevice.CreateWriteTransaction(values);
            I2CcommBus.Execute(xActions, 50);
        }
                
        /// <summary>
        /// I2C Read, Return Daten Array mit länge "lenght"
        /// </summary>
        /// <param name="register"></param>
        /// <param name="lenght"></param>
        /// <returns></returns>
        public byte[] readRegister(uint register, int lenght)
        {
            // Init Bus, multible Devices
            I2CcommBus.Config = I2C_dev;

            // Register
            byte[] rRegister = new byte[2];
            rRegister[0] = (byte)(register >> 8);   // MSB
            rRegister[1] = (byte)(register >> 0);   // LSB

            // Read Data
            byte[] readVal = new byte[lenght];

            //// Write & Read
            I2CDevice.I2CTransaction[] xActions = new I2CDevice.I2CTransaction[2];
            xActions[0] = I2CDevice.CreateWriteTransaction(rRegister);  // Ask Register
            xActions[1] = I2CDevice.CreateReadTransaction(readVal);     // Get DATA
            I2CcommBus.Execute(xActions, 1000);

            return readVal;
        }

        public void writeRegister(uint register, byte value)
        {
            // Register
            byte[] send = new byte[3];
            send[0] = (byte)(register >> 8);   // MSB
            send[1] = (byte)(register & 0xFF);   // LSB
            send[2] = value;

            this.send(send);
        }


        /// <summary>
        /// Debug Print all Register
        /// </summary>
        public void debug_read_all_register() 
        {
            for (uint i = 0; i < maxAdress; i++)
            {
                byte[] daten = readRegister(i, 1);
                Debug.Print("Register: " + i.ToString() + " Daten: " + daten[0].ToString());
            }        
        }


    }
}
