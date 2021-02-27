﻿using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;

// A small console app to load a specific assembly via ALC and precompile specified methods
//   "Disasmo.Loader MyDll.dll MyType MyMethod true"

public class DisasmoLoader
{
    public static void Main(string[] args)
    {
        PrecompileAllMethodsInType(args);
    }

    public static void PrecompileAllMethodsInType(string[] args)
    {
        if (args.Length != 4)
            throw new InvalidOperationException();

        string assemblyName = args[0];
        string typeName = args[1];
        string methodName = args[2];
        bool run = bool.Parse(args[3]);

        var alc = new AssemblyLoadContext(null);
        Assembly asm = alc.LoadFromAssemblyPath(Path.Combine(Environment.CurrentDirectory, assemblyName));
        foreach (Type type in asm.GetTypes())
        {
            // We replace pluses with dots because 'typeName' is a C#'y name of the type
            // Unfortunately, Roslyn doesn't have a display option to output the runtime name of the type
            // And we do not want to complicate things by formatting the type's name ourselves
            // This is the easiest solution to that problem
            if (type.FullName.Replace('+', '.').Contains(typeName))
            {
                foreach (var method in type.GetMethods((BindingFlags)60))
                {
                    if (method.DeclaringType == type && !method.IsGenericMethod)
                    {
                        if (methodName == "*" || method.Name == methodName)
                        {
                            if (run)
                            {
                                if (!method.IsStatic || method.GetParameters().Length != 0)
                                {
                                    // TODO: add some sort of a special attribute to set arguments? e.g. BDN's [Arguments(...)]
                                    throw new Exception("ERROR: Only static parameter-less methods are supported in the 'run method (experimental)' mode at the moment.");
                                }

                                // invoke it 40 times to hit the threshold (we could set our own but let's not bother)
                                for (int i = 0; i < 40; i++)
                                {
                                    method.Invoke(null, null);
                                    Thread.Sleep(15);
                                }
                            }
                            else
                            {
                                // precompile it
                                RuntimeHelpers.PrepareMethod(method.MethodHandle);
                            }
                        }
                    }
                }
            }
        }
    }
}