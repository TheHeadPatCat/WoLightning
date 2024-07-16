using Dalamud.Interface.Windowing;
using System;

namespace WoLightning.Windows;

public class BufferWindow : Window, IDisposable
{

    // This window only exists to get registered right at launch.
    // If we do not do this, the plugin will crash and not load,
    // As we do not register a Window right on launch otherwise.
    // Yes this is silly.
    public BufferWindow()
        : base("BufferWindow")
    {

    }

    public void Dispose() { }
    public override async void Draw()
    {


    }
}
