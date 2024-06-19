using Dalamud.Interface.Windowing;
using System;

namespace WoLightning.Windows;

public class BufferWindow : Window, IDisposable
{

    // We give this window a hidden ID using ##
    // So that the user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public BufferWindow()
        : base("BufferWindow")
    {

    }

    public void Dispose() { }
    public override async void Draw()
    {


    }
}
