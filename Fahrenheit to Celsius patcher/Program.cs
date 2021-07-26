using Microsoft.Win32;
using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


class FahrenheitCelsiusPatcher
{
    static void Main()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("Subnautica Below Zero - Fahrenheit  to Celsius patcher");
        Console.ResetColor();

        // Find game path and assembly path
        var (gamePath, assemblyPath) = GetPaths();

        // Open the assembly and find the method that needs to be patched
        ReaderParameters readParameters = new ReaderParameters
        {
            ReadWrite = true,
            AssemblyResolver = new CustomResolver(gamePath)
        };
        ModuleDefinition moduleDefinition = ModuleDefinition.ReadModule(assemblyPath, readParameters);
        TypeDefinition typeDefinition = moduleDefinition.GetType("uGUI_BodyHeatMeter");
        MethodDefinition methodDefinition = typeDefinition.Methods.First(m => m.Name == "SetValue");
        MethodBody methodBody = methodDefinition.Body;

        // Check method instructions to see if it matches original method body (this check is not very accurate and migth fail, but probably won't)
        if (methodBody.Instructions.Count == 42 && methodBody.Instructions[38].OpCode == OpCodes.Call)
        {
            Console.Write("Press enter to patch Assembly-CSharp.dll");
            Console.ReadLine();

            ILProcessor ilProcessor = methodBody.GetILProcessor();

            // Fahrenheit to Celsius conversion formula: C = (F - 32) / 1.8
            // After instruction 37 the first item on the stack is the Fahrenheit temperature
            // Push 32 on the stack, subtract it, then push 1.8 and divide the result by it
            ilProcessor.InsertAfter(37, Instruction.Create(OpCodes.Ldc_R4, 32f));
            ilProcessor.InsertAfter(38, Instruction.Create(OpCodes.Sub));
            ilProcessor.InsertAfter(39, Instruction.Create(OpCodes.Ldc_R4, 1.8f));
            ilProcessor.InsertAfter(40, Instruction.Create(OpCodes.Div));

            // Make a backup of original file
            File.Copy(assemblyPath, assemblyPath + ".old", true);

            // Save changes to disc
            moduleDefinition.Write();

            Console.Write("Assembly-CSharp.dll has been patched! A backup copy has been created in the same folder named Assembly-CSharp.dll.old");
        }
        // Check if method has already been patched by this program
        else if (methodBody.Instructions.Count == 46 && methodBody.Instructions[38].OpCode == OpCodes.Ldc_R4 && (float)methodBody.Instructions[38].Operand == 32f)
        {
            Console.Write("Assembly-CSharp.dll has already been patched!");
        }
        // Method was changed by a game update and this patch is not valid anymore
        else
        {
            Console.Write("Can't apply patch to Assembly-CSharp.dll. Can't recognize the SetValue method");
        }

        Console.ReadLine();
    }

    static (string gamePath, string assemblyPath) GetPaths()
    {
        string gamePath = FindGamePath("SubnauticaZero");
        string assemblyPath;

        if (gamePath == null)
        {
            // Keep asking path until a valid path is provided
            while (true)
            {
                Console.WriteLine(@"Insert game path (e.g. C:\Program Files (x86)\Steam\steamapps\common\SubnauticaZero): ");
                gamePath = Console.ReadLine();

                if (Directory.Exists(gamePath))
                {
                    assemblyPath = Path.Combine(gamePath, @"SubnauticaZero_Data\Managed\Assembly-CSharp.dll");

                    if (File.Exists(assemblyPath))
                    {
                        // Valid path found, exit while loop
                        break;
                    }
                }
            }
        }
        else
        {
            Console.WriteLine($"Game found at {gamePath}");

            assemblyPath = Path.Combine(gamePath, @"SubnauticaZero_Data\Managed\Assembly-CSharp.dll");
        }

        return (gamePath, assemblyPath);
    }

    // Given a Steam game folder name, returns it's full installation path
    static string FindGamePath(string gameFolderName)
    {
        RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"Software\WOW6432Node\Valve\Steam");

        if (regKey != null)
        {
            string steamPath = regKey.GetValue("InstallPath").ToString();

            // Get list of all Steam library paths
            List<string> steamLibraryPaths = new List<string>();
            steamLibraryPaths.Add(steamPath.Replace("/", "\\"));
            string[] configFile = File.ReadAllLines(Path.Combine(steamPath, "config\\config.vdf"));
            foreach (var item in configFile)
            {
                if (item.Contains("BaseInstallFolder"))
                {
                    steamLibraryPaths.Add(item.Split(new char[] { '"' })[3].Replace("\"", "").Replace("\\\\", "\\"));
                }
            }

            // Find game path
            foreach (var libraryPath in steamLibraryPaths)
            {
                string gameFoldersPath = Path.Combine(libraryPath, "steamapps\\common");
                IEnumerable<string> gameFolders = Directory.EnumerateDirectories(gameFoldersPath);

                if (gameFolders.FirstOrDefault(f => f.EndsWith(gameFolderName)) != null)
                {
                    return Path.Combine(gameFoldersPath, gameFolderName);
                }
            }

            // Path not found
            return null;
        }
        else
        {
            return null;
        }
    }
}


// A custom resolver is needed since Cecil sometimes fails to locate the other assembly files even though they are on the same folder
class CustomResolver : BaseAssemblyResolver
{
    private DefaultAssemblyResolver _defaultResolver;

    public CustomResolver(string gamePath)
    {
        _defaultResolver = new DefaultAssemblyResolver();
        _defaultResolver.AddSearchDirectory(Path.Combine(gamePath, "SubnauticaZero_Data\\Managed"));
    }

    public override AssemblyDefinition Resolve(AssemblyNameReference name)
    {
        return _defaultResolver.Resolve(name); ;
    }
}
