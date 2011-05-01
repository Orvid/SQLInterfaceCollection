using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Resources;
using System.Runtime.ConstrainedExecution;
using System.Security.Permissions;
using System;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("SQLInterfaces")]
[assembly: AssemblyDescription("A combined Sql interfacing dll for many types of databases.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Orvid Corp.")]
[assembly: AssemblyProduct("SQLInterfaces")]
[assembly: AssemblyCopyright("Copyright ©  Orvid Corp. 2011, All Components are Copyright © by Their Respective Owners.")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Extra settings for MySql
[assembly: AllowPartiallyTrustedCallers()]

// Extra settings for System.Data.Sqlite
[assembly: ComVisible(false)]
[assembly: NeutralResourcesLanguage("en")]
[assembly: ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]



// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("c94a049c-f9b1-4ef7-bf8a-97db7cde289c")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
