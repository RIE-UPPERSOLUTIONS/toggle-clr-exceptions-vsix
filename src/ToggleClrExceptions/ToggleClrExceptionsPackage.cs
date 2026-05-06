using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace ToggleClrExceptions
{
    [Guid(GuidList.PackageString)]
    public sealed class ToggleClrExceptionsPackage : AsyncPackage
    {
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await ToggleClrExceptionsCommand.InitializeAsync(this);
        }
    }
}
