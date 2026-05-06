using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace ToggleClrExceptions
{
    internal sealed class ToggleClrExceptionsCommand
    {
        private readonly AsyncPackage _package;

        private ToggleClrExceptionsCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package;

            CommandID menuCommandID = new CommandID(new Guid(GuidList.CommandSetString), PackageIds.ToggleClrExceptionsCommandId);
            MenuCommand menuItem = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                _ = new ToggleClrExceptionsCommand(package, commandService);
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            ThreadHelper.JoinableTaskFactory
                .RunAsync(async delegate
                {
                    await ExecuteAsync();
                })
                .FileAndForget("ToggleClrExceptions/Execute");
        }

       private async Task ExecuteAsync()
       {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            DTE2 dte = await _package.GetServiceAsync(typeof(DTE)) as DTE2;
            if (dte == null)
            {
                await SetStatusBarAsync("Toggle CLR Exceptions: DTE unavailable.");
                return;
            }

            ExceptionSettings clrGroup = FindClrExceptionGroup(dte.Debugger);
            if (clrGroup == null)
        {
            await SetStatusBarAsync("Toggle CLR Exceptions: CLR exception group not found.");
            return;
        }

        ExceptionSetting[] exceptions = clrGroup.Cast<ExceptionSetting>().ToArray();
        if (exceptions.Length == 0)
        {
            await SetStatusBarAsync("Toggle CLR Exceptions: no CLR exception entries found.");
            return;
        }

        bool isCurrentlyAllThrown = exceptions.All(static x => x.BreakWhenThrown);

        if (!isCurrentlyAllThrown)
        {
            foreach (ExceptionSetting exceptionSetting in exceptions)
            {
                exceptionSetting.BreakWhenThrown = true;
            }

            await SetStatusBarAsync("CLR exceptions: break on all thrown.");
        }
        else
        {
            dte.ExecuteCommand("DebuggerContextMenus.ExceptionSettingsWindow.RestoreDefaults");
            await SetStatusBarAsync("CLR exceptions: defaults restored.");
        }
        }

        private static ExceptionSettings FindClrExceptionGroup(Debugger debugger)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (ExceptionSettings group in debugger.ExceptionGroups)
            {
                string name = group.Name ?? string.Empty;
                if (name.IndexOf("Common Language Runtime", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    name.IndexOf("CLR", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return group;
                }
            }

            return null;
        }

        private async Task SetStatusBarAsync(string message)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            DTE2 dte = await _package.GetServiceAsync(typeof(DTE)) as DTE2;
            if (dte != null)
            {
                dte.StatusBar.Text = message;
            }
        }
    }
}
