using System.Windows.Input;
using DesktopKorone;
using DesktopKorone.Ref;

public partial class KoroneDesktopPlugin
{
    public class Anim_Catch : AnimationBehaviorClass
    {
        protected override void _WindowFrameUpdated(MainWindow.AnimationInfo info, MainWindow window)
        {
            window.Dispatcher.Invoke(() =>
            {
                if (Mouse.LeftButton == MouseButtonState.Released)
                {
                    info.BUTTON_ForceAnimationEnd = true;
                    return;
                }

                var pos = window.MousePos;
                pos.X -= window.WindowSize.X / 2;
                pos.Y -= window.WindowSize.Y / 2;
                window.WindowPosition = pos;
            });
        }
    }
}

