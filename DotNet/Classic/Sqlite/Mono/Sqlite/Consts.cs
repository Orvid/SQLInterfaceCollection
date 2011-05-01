//
// Consts.cs.in
//
// Author:
//   Kornél Pál <http://www.kornelpal.hu/>
//
// Copyright (C) 2005-2006 Kornél Pál
//

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

static class Consts
{
    //
    // Use these assembly version constants to make code more maintainable.
    //

    public const string MonoVersion = "@MONO_VERSION@";
    public const string MonoCompany = "MONO development team";
    public const string MonoProduct = "MONO Common language infrastructure";
    public const string MonoCopyright = "(c) various MONO Authors";

    // Versions of .NET Framework 2.0 RTM
    public const string FxVersion = "2.0.0.0";
    public const string VsVersion = "8.0.0.0";
    public const string FxFileVersion = "2.0.50727.1433";
    public const string VsFileVersion = "8.0.50727.1433";

    //
    // Use these assembly name constants to make code more maintainable.
    //

    public const string AssemblyI18N = "I18N, Version=" + FxVersion + ", Culture=neutral, PublicKeyToken=0738eb9f132ed756";
    public const string AssemblyMicrosoft_VisualStudio = "Microsoft.VisualStudio, Version=" + FxVersion + ", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
    public const string AssemblyMicrosoft_VisualStudio_Web = "Microsoft.VisualStudio.Web, Version=" + VsVersion + ", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
    public const string AssemblyMicrosoft_VSDesigner = "Microsoft.VSDesigner, Version=" + VsVersion + ", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
    public const string AssemblyMono_Http = "Mono.Http, Version=" + FxVersion + ", Culture=neutral, PublicKeyToken=0738eb9f132ed756";
    public const string AssemblyMono_Posix = "Mono.Posix, Version=" + FxVersion + ", Culture=neutral, PublicKeyToken=0738eb9f132ed756";
    public const string AssemblyMono_Security = "Mono.Security, Version=" + FxVersion + ", Culture=neutral, PublicKeyToken=0738eb9f132ed756";
    public const string AssemblyMono_Messaging_RabbitMQ = "Mono.Messaging.RabbitMQ, Version=" + FxVersion + ", Culture=neutral, PublicKeyToken=0738eb9f132ed756";
    public const string AssemblyCorlib = "mscorlib, Version=" + FxVersion + ", Culture=neutral, PublicKeyToken=b77a5c561934e089";
    public const string AssemblySystem = "System, Version=" + FxVersion + ", Culture=neutral, PublicKeyToken=b77a5c561934e089";
    public const string AssemblySystem_Data = "System.Data, Version=" + FxVersion + ", Culture=neutral, PublicKeyToken=b77a5c561934e089";
    public const string AssemblySystem_Design = "System.Design, Version=" + FxVersion + ", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
    public const string AssemblySystem_DirectoryServices = "System.DirectoryServices, Version=" + FxVersion + ", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
    public const string AssemblySystem_Drawing = "System.Drawing, Version=" + FxVersion + ", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
    public const string AssemblySystem_Drawing_Design = "System.Drawing.Design, Version=" + FxVersion + ", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
    public const string AssemblySystem_Messaging = "System.Messaging, Version=" + FxVersion + ", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
    public const string AssemblySystem_Security = "System.Security, Version=" + FxVersion + ", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
    public const string AssemblySystem_ServiceProcess = "System.ServiceProcess, Version=" + FxVersion + ", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
    public const string AssemblySystem_Web = "System.Web, Version=" + FxVersion + ", Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
    public const string AssemblySystem_Windows_Forms = "System.Windows.Forms, Version=" + FxVersion + ", Culture=neutral, PublicKeyToken=b77a5c561934e089";
    public const string AssemblySystem_Core = "System.Core, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
}