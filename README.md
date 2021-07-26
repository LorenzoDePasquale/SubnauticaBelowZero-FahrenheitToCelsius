## Subnautica Below Zero - Fahrenheit to Celsius

This tool will change the temperature shown in the player hud of the game Subnautica Below Zero from Fahrenheit to Celsius.

In particular, this tool will find and modify one method inside the Assembly-CSharp.dll file, meaning it should be able to work on future version of the game too, as long as that particular method is not changed.

Result:

![Fahrenheit vs Celsius](Fahrenheit%20vs%20Celsius.jpg)


The tool uses the [Cecil library](https://github.com/jbevain/cecil) to alter the game CIL code.
