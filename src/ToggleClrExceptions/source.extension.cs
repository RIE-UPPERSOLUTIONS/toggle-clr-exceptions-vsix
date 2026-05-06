using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

[assembly: PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
[assembly: InstalledProductRegistration("Toggle CLR Exceptions", "Toggle CLR exception break behavior", "1.0")]
[assembly: ProvideMenuResource("Menus.ctmenu", 1)]
[assembly: Guid(ToggleClrExceptions.GuidList.PackageString)]
