using DesktopKorone;
using DesktopKorone.Ref;

public partial class KoroneDesktopPlugin
{
    public class Anim_Nade : AnimationBehaviorClass
    {
        protected override void _LastFrame(MainWindow.AnimationInfo info, MainWindow window)
        {
            info.BUTTON_ForceAnimationEnd = true;
        }
    }
}

