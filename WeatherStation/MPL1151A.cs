using System;
using System.Threading;
using GHIElectronics.NETMF.FEZ;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace WeatherStation.Sensors
{
    public class MPL1151A
    {
        static readonly byte[] StartCommand = new byte[2] { 0x24 & 0x7F, 0x00 & 0x7F };

        protected SPI.Configuration Configuration { get; set; }
        protected SPI spi { get; set; }

        public MPL1151A() : this(null) { }

        public MPL1151A(SPI.Configuration spiConfiguration)
        {
            /*
                         SPI.Configuration MyConfig = new SPI.Configuration((Cpu.Pin)FEZ_Pin.Digital.Di21, false, 0, 0, false, true, 1000, SPI.SPI_module.SPI1);

            SPI MySPI = new SPI(MyConfig);
            byte[] tx_data = new byte[8];
            byte[] rx_data = new byte[8];

                     
            MySPI.WriteRead(tx_data, rx_data);
- See more at: https://www.ghielectronics.com/community/forum/topic?id=3150#sthash.XZf01nUh.dpuf
             * 
             */
            Configuration = spiConfiguration ?? new SPI.Configuration(
                          (Cpu.Pin) FEZ_Pin.Digital.Di10, 
                          false,
                          0,
                          0,
                          false,
                          true,
                          1000,
                          SPI.SPI_module.SPI1
                          );
        }

        /*
         * // Pin definitions
#define MPL115A1_ENABLE_PIN 9
#define MPL115A1_SELECT_PIN 10

// Masks for MPL115A1 SPI i/o
#define MPL115A1_READ_MASK  0x80
#define MPL115A1_WRITE_MASK 0x7F 

// MPL115A1 register address map
#define PRESH   0x00    // 80
#define PRESL   0x02    // 82
#define TEMPH   0x04    // 84
#define TEMPL   0x06    // 86

#define A0MSB   0x08    // 88
#define A0LSB   0x0A    // 8A
#define B1MSB   0x0C    // 8C
#define B1LSB   0x0E    // 8E
#define B2MSB   0x10    // 90
#define B2LSB   0x12    // 92
#define C12MSB  0x14    // 94
#define C12LSB  0x16    // 96
#define C11MSB  0x18    // 98
#define C11LSB  0x1A    // 9A
#define C22MSB  0x1C    // 9C
#define C22LSB  0x1E    // 9E
         */
        //protected byte PressureMSB { get { return 0x80; } }
        //protected byte PressureLSB { get { return 0x82; } }
        protected byte TempMSB { get { return 0x04 | 0x80; } }
        protected byte TempLSB { get { return 0x06 | 0x80; } }

        public void Start()
        {
            string decPcomp_out;
            sbyte sia0MSB, sia0LSB;
            sbyte sib1MSB, sib1LSB;
            sbyte sib2MSB, sib2LSB;
            sbyte sic12MSB, sic12LSB;
            sbyte sic11MSB, sic11LSB;
            sbyte sic22MSB, sic22LSB;
            int sia0, sib1, sib2, sic12, sic11, sic22;
            uint uiPadc, uiTadc;
            byte uiPH, uiPL, uiTH, uiTL;
            long lt1, lt2, lt3, si_c11x1, si_a11, si_c12x2;
            long si_a1, si_c22x2, si_a2, si_a1x1, si_y1, si_a2x2;
            float siPcomp, decPcomp;

            // start pressure & temp conversions
            // command byte + r/w bit
            const byte com1_writeData = 0x24;
            const byte com2_writeData = 0x00;
            byte[] WriteBuffer = { com1_writeData, com2_writeData };
            spi = new SPI(Configuration);
            spi.Write(WriteBuffer);
            Thread.Sleep(3);
        }

        /// <summary>
        /// Temperature in degrees Celcius.
        /// </summary>
        public double Temperature
        {
            get
            {
                //spi = new SPI(Configuration);
                //var responseBuffer = new byte[1];
                //spi = new SPI(Configuration);
                //spi.Write(new byte[] { 0x24, 0x00 });
                //Thread.Sleep(16); // Chip needs 1.8 time to initialize.
                //uint samples;
                var commandBuffer = new byte[1];

                //commandBuffer[0] = 0x80 ;
                //spi.WriteRead(commandBuffer, responseBuffer); // Most significant temperature byte

                //commandBuffer[0] = 0x82;
                //spi.WriteRead(commandBuffer, responseBuffer); // Most significant temperature byte

                //commandBuffer[0] = 0x84;
                //samples = (uint)responseBuffer[0] << 8;
                
                //commandBuffer[0] = 0x86 ;
                //responseBuffer[0] = 0;
                //spi.WriteRead(commandBuffer, responseBuffer); // Least significant temperature byte.
                //samples = samples & responseBuffer[0];
                //spi.Dispose();
                const byte PRESHw_writeData = 0x00 | 0x80;
                const byte PRESHr_writeData = 0x00 | 0x80;
                const byte PRESLw_writeData = 0x02 | 0x80;
                const byte PRESLr_writeData = 0x00 | 0x80;
                const byte TEMPHw_writeData = 0x04 | 0x80;
                const byte TEMPHr_writeData = 0x00 | 0x80;
                const byte TEMPLw_writeData = 0x06 | 0x80;
                const byte TEMPLr_writeData = 0x00 | 0x80;
                const byte BLANK1r_writeData = 0x00 | 0x80;

                byte[] press_writeData = { PRESHw_writeData, PRESHr_writeData, PRESLw_writeData, PRESLr_writeData,
                                       TEMPHw_writeData, TEMPHr_writeData, TEMPLw_writeData, TEMPLr_writeData, BLANK1r_writeData };
                //var press_writeData = new byte[] { 0x80, 0x00, 0x82, 0x00, 0x84, 0x00, 0x86, 0x00, 0x00 };
                byte[] press_readBuffer = new byte[9];
                spi.WriteRead(press_writeData, press_readBuffer);

                var samples = (press_readBuffer[5] << 8) + (press_readBuffer[7] & 0x00FF);

                samples = samples >> 6;
                var offset = 515.0; //  472.0 ///498.0; //  526.5
                //var temp = 25 - (samples - offset) * -0.187;
                var temp = 25 + ((samples - offset) / -5.35);
                return temp;
            }
        }

        /// <summary>
        /// Temperature in Fahrenheit.
        /// </summary>
        public double TemperatureF
        {
            get { return 1.8f * Temperature + 32; }
        }

        internal void Stop()
        {
            spi.Dispose();
        }
    }
}
