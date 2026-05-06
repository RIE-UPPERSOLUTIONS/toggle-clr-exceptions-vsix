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

            var menuCommandID = new CommandID(new Guid(GuidList.CommandSetString), PackageIds.ToggleClrExceptionsCommandId);
            var menuItem = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                _ = new ToggleClrExceptionsCommand(package, commandService);
            }
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _ = ExecuteAsync();
        }

        private async Task ExecuteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await _package.GetServiceAsync(typeof(DTE)) as DTE2;
            if (dte == null)
            {
                await SetStatusBarAsync("Toggle CLR Exceptions: DTE unavailable.");
                return;
            }

            var clrGroup = FindClrExceptionGroup(dte.Debugger);
            if (clrGroup == null)
            {
                await SetStatusBarAsync("Toggle CLR Exceptions: CLR exception group not found.");
                return;
            }

            var exceptions = clrGroup.Cast<ExceptionSetting>().ToList();
            if (exceptions.Count == 0)
            {
                await SetStatusBarAsync("Toggle CLR Exceptions: no CLR exception entries found.");
                return;
            }

            bool isCurrentlyAllThrown = exceptions.All(x => x.BreakWhenThrown);
            bool nextBreakWhenThrown = !isCurrentlyAllThrown;

            foreach (var exception in exceptions)
            {
                exception.BreakWhenThrown = nextBreakWhenThrown;
            }

            await SetStatusBarAsync(nextBreakWhenThrown
                ? "CLR exceptions: break on all thrown."
                : "CLR exceptions: default CLR behavior restored.");
        }

        private static ExceptionSettings? FindClrExceptionGroup(Debugger debugger)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            foreach (ExceptionSettings group in debugger.ExceptionGroups)
            {
                var name = group.Name ?? string.Empty;
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

            var dte = await _package.GetServiceAsync(typeof(DTE)) as DTE2;
            if (dte != null)
            {
                dte.StatusBar.Text = message;
            }
        }
    }
}
