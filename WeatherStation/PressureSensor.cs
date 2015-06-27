using System;
using System.Threading;
using GHIElectronics.NETMF.FEZ;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

namespace WeatherStation
{
    static class PressureSensor
    {
        /// <summary>
        /// 
        /// SPI interface for the MPL115A barometric sensor.
        /// Created by deepnarc, deepnarc at gmail.com
        /// 
        /// Netduino platform
        /// 
        /// Sensor Breakout---------Netduino
        /// ================================
        /// SDN---------------------optional
        /// CSN---------------------pin 0 (user definable)
        /// SDO---------------------pin 12
        /// SDI---------------------pin 11
        /// SCK---------------------pin 13
        /// GND---------------------GND
        /// VDO---------------------VCC (3.3V)
        /// 
        /// References: (1) Freescale Semiconductor, Application Note, AN3785, Rev 5, 7/2009 by John Young
        /// provided the code for the manipulations of the sensor coefficients; the original comments 
        /// were left in place without modification in that section. (2) Freescale Semiconductor, Document 
        /// Number: MPL115A1, Rev 6, 10/2011. (3) MPL115A1 SPI Digital Barometer Test Code Created on: 
        /// September 30, 2010 By: Jeremiah McConnell - miah at miah.com, Portions: Jim Lindblom - jim at 
        /// sparkfun.com. (4) MPL115A1 SPI Digital Barometer Test Code Created on: April 20, 2010 By: Jim 
        /// Lindblom - jim at sparkfun.com.
        /// 
        /// </summary>


        public static double calculatePressure()
        {
            SPI SPI_Out = new SPI(new SPI.Configuration(
                (Cpu.Pin)FEZ_Pin.Digital.Di7,    // SS-pin
                false,               // SS-pin active state
                0,                   // The setup time for the SS port
                0,                   // The hold time for the SS port
                false,               // The idle state of the clock
                true,                // The sampling clock edge
                1000,                // The SPI clock rate in KHz
                SPI.SPI_module.SPI1  // The used SPI bus (refers to a MOSI MISO and SCLK pinset)
                )
            );

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
            float siPcomp;//, decPcomp;

            // start pressure & temp conversions
            // command byte + r/w bit
            const byte com1_writeData = 0x24 & 0x7F;
            const byte com2_writeData = 0x00 & 0x7F;
            byte[] WriteBuffer = { com1_writeData, com2_writeData };
            SPI_Out.Write(WriteBuffer);
            Thread.Sleep(3);

            // write(0x24, 0x00);	// Start Both Conversions
            // write(0x20, 0x00);	// Start Pressure Conversion
            // write(0x22, 0x00);	// Start temperature conversion
            // delay_ms(10);	    // Typical wait time is 3ms

            // read pressure
            // address byte + r/w bit
            // data byte + r/w bit
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
            byte[] press_readBuffer = new byte[9];

            SPI_Out.WriteRead(press_writeData, press_readBuffer);

            uiPH = press_readBuffer[1];
            uiPL = press_readBuffer[3];
            uiTH = press_readBuffer[5];
            uiTL = press_readBuffer[7];

            uiPadc = (uint)uiPH << 8;
            uiPadc += (uint)uiPL & 0x00FF;
            uiTadc = (uint)uiTH << 8;
            uiTadc += (uint)uiTL & 0x00FF;

            // read coefficients
            // address byte + r/w bit
            // data byte + r/w bit
            const byte A0MSBw_writeData = 0x08 | 0x80;
            const byte A0MSBr_writeData = 0x00 | 0x80;
            const byte A0LSBw_writeData = 0x0A | 0x80;
            const byte A0LSBr_writeData = 0x00 | 0x80;
            const byte B1MSBw_writeData = 0x0C | 0x80;
            const byte B1MSBr_writeData = 0x00 | 0x80;
            const byte B1LSBw_writeData = 0x0E | 0x80;
            const byte B1LSBr_writeData = 0x00 | 0x80;
            const byte B2MSBw_writeData = 0x10 | 0x80;
            const byte B2MSBr_writeData = 0x00 | 0x80;
            const byte B2LSBw_writeData = 0x12 | 0x80;
            const byte B2LSBr_writeData = 0x00 | 0x80;
            const byte C12MSBw_writeData = 0x14 | 0x80;
            const byte C12MSBr_writeData = 0x00 | 0x80;
            const byte C12LSBw_writeData = 0x16 | 0x80;
            const byte C12LSBr_writeData = 0x00 | 0x80;
            const byte C11MSBw_writeData = 0x18 | 0x80;
            const byte C11MSBr_writeData = 0x00 | 0x80;
            const byte C11LSBw_writeData = 0x1A | 0x80;
            const byte C11LSBr_writeData = 0x00 | 0x80;
            const byte C22MSBw_writeData = 0x1C | 0x80;
            const byte C22MSBr_writeData = 0x00 | 0x80;
            const byte C22LSBw_writeData = 0x1E | 0x80;
            const byte C22LSBr_writeData = 0x00 | 0x80;
            const byte BLANK2r_writeData = 0x00 | 0x80;

            byte[] coeff_writeData = { A0MSBw_writeData, A0MSBr_writeData, A0LSBw_writeData, A0LSBr_writeData, B1MSBw_writeData, 
                                        B1MSBr_writeData, B1LSBw_writeData, B1LSBr_writeData, B2MSBw_writeData, B2MSBr_writeData, 
                                        B2LSBw_writeData, B2LSBr_writeData, C12MSBw_writeData, C12MSBr_writeData, C12LSBw_writeData,
                                        C12LSBr_writeData, C11MSBw_writeData, C11MSBr_writeData, C11LSBw_writeData, C11LSBr_writeData,
                                        C22MSBw_writeData, C22MSBr_writeData, C22LSBw_writeData, C22LSBr_writeData, BLANK2r_writeData };

            byte[] coeff_readBuffer = new byte[25];

            SPI_Out.WriteRead(coeff_writeData, coeff_readBuffer);

            //==================================================== 
            // MPL115A Placing Coefficients into 16 bit variables 
            //====================================================
            //coeff a0 16bit
            sia0MSB = (sbyte)coeff_readBuffer[1];
            sia0LSB = (sbyte)coeff_readBuffer[3];

            sia0 = (int)sia0MSB << 8;      //s16 type //Shift to MSB
            sia0 += (int)sia0LSB & 0x00FF; //Add LSB to 16bit number

            //coeff b1 16bit
            sib1MSB = (sbyte)coeff_readBuffer[5];
            sib1LSB = (sbyte)coeff_readBuffer[7];

            sib1 = (int)sib1MSB << 8;      //Shift to MSB
            sib1 += (int)sib1LSB & 0x00FF; //Add LSB to 16bit number

            //coeff b2 16bit
            sib2MSB = (sbyte)coeff_readBuffer[9];
            sib2LSB = (sbyte)coeff_readBuffer[11];

            sib2 = (int)sib2MSB << 8;      //Shift to MSB
            sib2 += (int)sib2LSB & 0x00FF; //Add LSB to 16bit number

            //coeff c12 14bit
            sic12MSB = (sbyte)coeff_readBuffer[13];
            sic12LSB = (sbyte)coeff_readBuffer[15];

            sic12 = (int)sic12MSB << 8;    //Shift to MSB only by 8 for MSB
            sic12 += (int)sic12LSB & 0x00FF;

            //coeff c11 11bit
            sic11MSB = (sbyte)coeff_readBuffer[17];
            sic11LSB = (sbyte)coeff_readBuffer[19];

            sic11 = (int)sic11MSB << 8;    //Shift to MSB only by 8 for MSB
            sic11 += (int)sic11LSB & 0x00FF;

            //coeff c22 11bit
            sic22MSB = (sbyte)coeff_readBuffer[21];
            sic22LSB = (sbyte)coeff_readBuffer[23];

            sic22 = (int)sic22MSB << 8;    //Shift to MSB only by 8 for MSB
            sic22 += (int)sic22LSB & 0x00FF;

            //=================================================== 
            //Coefficient 9 equation compensation 
            //=================================================== 
            //
            //Variable sizes:
            //For placing high and low bytes of the Memory addresses for each of the 6 coefficients:
            //signed char (S8) sia0MSB, sia0LSB, sib1MSB,sib1LSB, sib2MSB,sib2LSB, sic12MSB,sic12LSB, sic11MSB,sic11LSB, sic22MSB,sic22LSB; //
            //Variable for use in the compensation, this is the 6 coefficients in 16bit form, MSB+LSB.
            //signed int (S16) sia0, sib1, sib2, sic12, sic11, sic22;
            //
            //Variable used to do large calculation as 3 temp variables in the process below
            //signed long (S32) lt1, lt2, lt3;
            //
            //Variables used for Pressure and Temperature Raw.
            //unsigned int (U16) uiPadc, uiTadc.
            //signed (N=number of bits in coefficient, F-fractional bits) //s(N,F)
            //The below Pressure and Temp or uiPadc and uiTadc are shifted from the MSB+LSB values to remove the zeros in the LSB since this 
            // 10bit number is stored in 16 bits. i.e 0123456789XXXXXX becomes 0000000123456789

            uiPadc = uiPadc >> 6; //Note that the PressCntdec is the raw value from the MPL115A data address. Its shifted >>6 since its 10 bit.
            uiTadc = uiTadc >> 6; //Note that the TempCntdec is the raw value from the MPL115A data address. Its shifted >>6 since its 10 bit.

            //******* STEP 1 c11x1= c11 * Padc
            lt1 = (long)sic11;    // s(16,27) s(N,F+zeropad) goes from s(11,10)+11ZeroPad = s(11,22) => Left Justified = s(16,27)
            lt2 = (long)uiPadc;   // u(10,0) s(N,F)
            lt3 = lt1 * lt2;      // s(26,27) /c11*Padc
            si_c11x1 = (long)lt3; // s(26,27)- EQ 1 =c11x1 /checked

            //divide this hex number by 2^30 to get the correct decimal value.
            //b1 =s(14,11) => s(16,13) Left justified

            //******* STEP 2 a11= b1 + c11x1
            lt1 = ((long)sib1) << 14;   // s(30,27) b1=s(16,13) Shift b1 so that the F matches c11x1(shift by 14)
            lt2 = (long)si_c11x1;       // s(26,27) //ensure fractional bits are compatible
            lt3 = lt1 + lt2;            // s(30,27) /b1+c11x1
            si_a11 = (long)(lt3 >> 14); // s(16,13) - EQ 2 =a11 Convert this block back to s(16,X)

            //******* STEP 3 c12x2= c12 * Tadc
            // sic12 is s(14,13)+9zero pad = s(16,15)+9 => s(16,24) left justified
            lt1 = (long)sic12;    // s(16,24)
            lt2 = (long)uiTadc;   // u(10,0)
            lt3 = lt1 * lt2;      // s(26,24)
            si_c12x2 = (long)lt3; // s(26,24) - EQ 3 =c12x2 /checked

            //******* STEP 4 a1= a11 + c12x2
            lt1 = ((long)si_a11 << 11); // s(27,24) This is done by s(16,13) <<11 goes to s(27,24) to match c12x2's F part
            lt2 = (long)si_c12x2;       // s(26,24)
            lt3 = lt1 + lt2;            // s(27,24) /a11+c12x2
            si_a1 = (long)lt3 >> 11;    // s(16,13) - EQ 4 =a1 /check

            //******* STEP 5 c22x2= c22 * Tadc
            // c22 is s(11,10)+9zero pad = s(11,19) => s(16,24) left justified
            lt1 = (long)sic22;      // s(16,30) This is done by s(11,10) + 15 zero pad goes to s(16,15)+15, to s(16,30)
            lt2 = (long)uiTadc;     // u(10,0)
            lt3 = lt1 * lt2;        // s(26,30) /c22*Tadc
            si_c22x2 = (long)(lt3); // s(26,30) - EQ 5 /=c22x2

            //******* STEP 6 a2= b2 + c22x2
            //WORKS and loses the least in data. One extra execution. Note how the 31 is really a 32 due to possible overflow.
            // b2 is s(16,14) User shifted left to => s(31,29) to match c22x2 F value
            lt1 = ((long)sib2 << 15);    //s(31,29)
            lt2 = ((long)si_c22x2 >> 1); //s(25,29) s(26,30) goes to >>16 s(10,14) to match F from sib2
            lt3 = lt1 + lt2;             //s(32,29) but really is a s(31,29) due to overflow the 31 becomes a 32.
            si_a2 = ((long)lt3 >> 16);   //s(16,13)

            //******* STEP 7 a1x1= a1 * Padc
            lt1 = (long)si_a1;      // s(16,13)
            lt2 = (long)uiPadc;     // u(10,0)
            lt3 = lt1 * lt2;        // s(26,13) /a1*Padc
            si_a1x1 = (long)(lt3);  // s(26,13) - EQ 7 /=a1x1 /check

            //******* STEP 8 y1= a0 + a1x1
            // a0 = s(16,3)
            lt1 = ((long)sia0 << 10);  // s(26,13) This is done since has to match a1x1 F value to add. So S(16,3) <<10 = S(26,13)
            lt2 = (long)si_a1x1;       // s(26,13)
            lt3 = lt1 + lt2;           // s(26,13) /a0+a1x1
            si_y1 = ((long)lt3 >> 10); // s(16,3) - EQ 8 /=y1 /check

            //******* STEP 9 a2x2= a2 *Tadc
            lt1 = (long)si_a2;      // s(16,13)
            lt2 = (long)uiTadc;     // u(10,0)
            lt3 = lt1 * lt2;        // s(26,13) /a2*Tadc
            si_a2x2 = (long)(lt3);  // s(26,13) - EQ 9 /=a2x2

            //******* STEP 10 pComp = y1 +a2x2
            // y1= s(16,3)
            lt1 = ((long)si_y1 << 10); // s(26,13) This is done to match a2x2 F value so addition can match. s(16,3) <<10
            lt2 = (long)si_a2x2;       // s(26,13)
            lt3 = lt1 + lt2;           // s(26,13) /y1+a2x2

            // FIXED POINT RESULT WITH ROUNDING:
            siPcomp = (long)lt3 >> 13; // goes to no fractional parts since this is an ADC count.

            //decPcomp is defined as a floating point number.
            //Conversion to Decimal value from 1023 ADC count value. ADC counts are 0 to 1023. Pressure is 50 to 115kPa correspondingly.
            var decPcomp = (double)((65.0 / 1023.0) * siPcomp) + 50;
            var decPcompHg = decPcomp*0.295300586466965;
            decPcomp_out = decPcomp.ToString();
            Debug.Print("decPcomp: " + decPcomp_out + " millibars: " + decPcompHg);

            SPI_Out.Dispose();
            return decPcompHg;

        }
    }
}
