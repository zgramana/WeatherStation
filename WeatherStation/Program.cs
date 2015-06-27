﻿using System;
using System.Collections;
using System.Threading;
using GHIElectronics.NETMF.Hardware;
using MicroLiquidCrystal;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using GHIElectronics.NETMF.FEZ;
using WeatherStation.Sensors;

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
            ShowMessage("Weather Station;1.0", display);

            String message;
            var readings = new System.Collections.Queue();

            var humidity = new AnalogIn(AnalogIn.Pin.Ain0);
                
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
                    (Cpu.Pin)FEZ_Pin.Digital.Di10,
                    (Cpu.Pin)FEZ_Pin.Digital.Di9,
                    (Cpu.Pin)FEZ_Pin.Digital.Di3,
                    (Cpu.Pin)FEZ_Pin.Digital.Di4,
                    (Cpu.Pin)FEZ_Pin.Digital.Di5,
                    (Cpu.Pin)FEZ_Pin.Digital.Di6
                    );

            var display = new Lcd(transferProvider);
            display.Begin(16, 2);
            display.BlinkCursor = false;
            display.ShowCursor = false;

            return display;
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
//using System;
//using System.Threading;

//using Microsoft.SPOT;
//using Microsoft.SPOT.Hardware;

//using GHIElectronics.NETMF.FEZ;
//using WeatherStation.Sensors;

//namespace WeatherStation
//{
//    public class Program
//    {
//        public static void Main()
//        {
//            // Blink board LED

//            var ledState = false;

//            var led = new OutputPort((Cpu.Pin)FEZ_Pin.Digital.LED, ledState);

//            while (true)
//            {
//                // Sleep for 500 milliseconds
//                Thread.Sleep(10000);

//                // toggle LED state
//                ledState = !ledState;
//                led.Write(ledState);

//                var sensor = new MPL1151A();
//                sensor.Start();
//                Debug.Print(sensor.TemperatureF.ToString());
//                sensor.Stop();
//            }
//        }

//    }
//}

