﻿using System.Runtime.InteropServices.JavaScript;

// Creates "Main" to please the toolset
return;

public partial class FileProcessor
{
    // Make the method accessible from JS
    [JSExport]
    internal static int Add(int a, int b)
    {
        return a + b;
    }
}