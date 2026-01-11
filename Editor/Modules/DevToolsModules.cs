using System;
using System.Linq;

namespace Kebolder.DevTools.Editor.Modules
{
    public static class DevToolsModules
    {
        private readonly struct Module
        {
            public readonly int Order;
            public readonly Action Draw;

            public Module(int order, Action draw)
            {
                Order = order;
                Draw = draw;
            }
        }

        // Register modules here to control what renders in the main window.
        private static readonly Module[] Modules =
        {
            new Module(Sniffer.Order, Sniffer.Draw),
            new Module(ConstraintTool.Order, ConstraintTool.Draw)
        };

        public static void DrawAll()
        {
            // Order controls vertical placement: lower values render first.
            foreach (var module in Modules.OrderBy(m => m.Order))
            {
                module.Draw?.Invoke();
            }
        }
    }
}
