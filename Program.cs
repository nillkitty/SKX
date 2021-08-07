using System;

namespace SKX
{
    /// <summary>
    /// This is the only part of the original source code release that I didn't write.
    /// This class is courtesy the MonoGame 'template'.
    ///     ~Nill
    /// </summary>

    public static class Program
    {
        [STAThread]
        static void Main()
        {
            using (var game = new Game())
                 game.Run();

        }
    }
}
