# Unity Submodule   
1. Code for serial connection inside Serial_connection.cs adapted from https://social.msdn.microsoft.com/Forums/vstudio/en-US/2a9f98c4-0bf0-4cb4-a257-11520522e5bd/how-to-open-port-with-c?forum=csharpgeneral.
2. https://www.alanzucconi.com/2015/10/07/how-to-integrate-arduino-with-unity/ gave some guidance for using Serial connections inside c#.

Serial_connection.cs ([here](https://github.com/AHarmlessPyro/Unity-Arduino_controller/tree/master/Assets/Scripts)) itself is a combination of multiple things : 
1. Opening a Serial connection
2. Reading data in from the Arduino
3. Parsing data from Arduino and converting that to usable formats
4. Cleaning up data and making sure that correct number of frames are skipped on movement
