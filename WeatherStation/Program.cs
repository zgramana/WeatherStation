using System;
using System.Collections;
using System.Threading;
using GHIElectronics.NETMF.Hardware;
using MicroLiquidCrystal;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using GHIElectronics.NETMF.FEZ;
using WeatherStation.Sensors;
using GHIElectronics.NETMF.System;

namespace WeatherStation
{
    public class Program
    {
        private const string MainModeHeader = "Temp    Humidity;";

        public static void Main()
        {
            // Blink board LED

            var ledState = false;

            var led = new OutputPort((Cpu.Pin)FEZ_Pin.Digital.LED, ledState);

            // Enable LCD.
            var display = ConfigureLiquidCrystalDisplay();
            display.Clear();
            // Turn on backlight
            var greenBacklight = new OutputPort((Cpu.Pin)FEZ_Pin.Digital.Di51, true);
            
            ShowMessage("Weather Station;1.0", display);

            String message;
            var readings = new System.Collections.Queue();

            var humidity = new AnalogIn(AnalogIn.Pin.Ain0);
            var thermistor = new AnalogIn(AnalogIn.Pin.Ain5);

            while (true)
            {
                Thread.Sleep(1000);
                
                // toggle LED state
                ledState = !ledState;
                led.Write(ledState);

                var sensor = new MPL1151A();
                sensor.Start();
                var temp = sensor.TemperatureF;
                sensor.Stop();
                //var temp = PressureSensor.calculatePressure();

                Debug.Print(temp.ToString("F1"));

                var value = thermistor.Read();
                temp = System.Math.Round(ConvertADCToTemperature(value) * 10) / 10;
                Debug.Print(temp.ToString("F1"));

                if (temp < 235.8D)
                    readings.Enqueue(temp);
                else
                    Debug.Print("Ignoring reading out of variance: " + temp);

                if (readings.Count < 5) continue;

                var temps = readings.ToArray();
                var averageTemp = ArrayExtensions.Average(temps);
                readings.Dequeue(); 
                var tempStr = averageTemp.ToString("F1");
                var padLength = 8 - tempStr.Length;
                for (int i = 0; i < padLength; i++)
                {
                    tempStr += " ";
                }
                var humidityStr = ((humidity.Read() / 1024F)* 100F).ToString("F1") + "%";
                message = MainModeHeader + tempStr + humidityStr;
                Debug.Print(message);
                ShowMessage(message, display);          
            }
        }

        private static void ShowMessage(string message, Lcd display)
        {
            display.Clear();
            var lines = message.Split(';');
            display.SetCursorPosition(0, 0);
            display.Write(lines[0]);

            if (lines.Length != 2) return;

            display.SetCursorPosition(0, 1);
            display.Write(lines[1]);
        }

        private static Lcd ConfigureLiquidCrystalDisplay()
        {
            var transferProvider = new GpioLcdTransferProvider(
                    (Cpu.Pin)FEZ_Pin.Digital.Di21,
                    (Cpu.Pin)FEZ_Pin.Digital.Di23,
                    (Cpu.Pin)FEZ_Pin.Digital.Di47,
                    (Cpu.Pin)FEZ_Pin.Digital.Di46,
                    (Cpu.Pin)FEZ_Pin.Digital.Di49,
                    (Cpu.Pin)FEZ_Pin.Digital.Di48
                    );

            var display = new Lcd(transferProvider);
            display.Begin(16, 2);
            display.BlinkCursor = false;
            display.ShowCursor = false;

            return display;
        }

        // B 3434K
        // R1 = 10000, R2 = 1452.2, T2=84.999
        // alpha tcr = -4.6025 %/C
        // operating current @ 25: 0.12mA, rated power @ 25: 7.5mW, Dissipation @ 25: 1.5mW, Thermal time const @ 25: 4C/s
        // R25 is zero-power resistance at 25°C.

        // a = 0.8837602138 * e-3, b = 2.519657770 e-4, c = 1.914277212 e-7

        // a = 0.000883760213800
        // b = 0.000251965777
        // c = 0.000000191427721
        static double ConvertADCToTemperature(int value)
        {
            // Inputs ADC Value from Thermistor and outputs Temperature in Celsius
            // Utilizes the Steinhart-Hart Thermistor Equation:
            //    Temperature in Kelvin = 1 / {A + B[ln(R)] + C[ln(R)]^3}
            //    where A = 0.001129148, B = 0.000234125 and C = 8.76741E-08
            double Resistance; double Temp;  // Dual-Purpose variable to save space.
            Resistance = 11452.2 * ((1024.0 / value) - 1);  // Assuming a 10k Thermistor.  Calculation is actually: Resistance = (1024 /ADC -1) * BalanceResistor
            // For a GND-Thermistor-PullUp--Varef circuit it would be Rtherm=Rpullup/(1024.0/ADC-1)
            Temp = MathEx.Log(Resistance); // Saving the Log(resistance) so not to calculate it 4 times later. // "Temp" means "Temporary" on this line.
            Temp = 1 / (0.000883760213800 + (0.000251965777 * Temp) + (0.000000191427721 * Temp * Temp * Temp));   // Now it means both "Temporary" and "Temperature"
            Temp = Temp - 273.15;  // Convert Kelvin to Celsius                                         // Now it only means "Temperature"
            Temp = 1.8f * Temp + 32;
            return Temp;
        }
    }

    public static class ArrayExtensions
    {
        public static double Sum(object[] array)
        {
            var result = 0D;
            for (int i = 0; i < array.Length; i++)
            {
                result += (double)array[i];
            }
            return result ;
        }

        public static double Average(object[] array) { return Sum(array)/array.Length; }
    }
}