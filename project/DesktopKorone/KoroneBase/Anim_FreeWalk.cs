using System.Windows.Media;
using DesktopKorone;
using DesktopKorone.Ref;
using Point = System.Windows.Point;

public partial class KoroneDesktopPlugin
{
    public class Anim_FreeWalk : AnimationBehaviorClass
    {
        ScaleTransform flipTrans;
        const float SPEED = 30;
        Point m_goal_point;
        Point m_start_point;
        float m_time;

        protected override void _Prepare(MainWindow.AnimationInfo info, MainWindow window)
        {
            window.Dispatcher.Invoke(() =>
            {
                m_start_point = window.WindowPosition;
                m_goal_point = new Point(window.Random.NextDouble(0, window.ScreenSize.X - window.WindowSize.X),
                    window.Random.NextDouble(0, window.ScreenSize.Y - window.WindowSize.Y));
                m_time = 0;

                flipTrans = new ScaleTransform();

                flipTrans.ScaleX = (m_goal_point.X - window.WindowPosition.X > 0) ? -1 : 1;
                info.ImageView.RenderTransform = flipTrans;
            });
        }

        protected override void _WindowFrameUpdated(MainWindow.AnimationInfo info, MainWindow window)
        {
            window.Dispatcher.Invoke(() =>
            {
                var distance = Distance(m_start_point, m_goal_point);
                var total_time = distance / SPEED;
                var speed = (float)info.DeltaTime.TotalSeconds / total_time;

                m_time += speed;

                if (m_time > 1.0)
                {
                    info.BUTTON_ForceAnimationEnd = true;
                    return;
                }

                var pos = Lerp(m_start_point, m_goal_point, m_time);

                window.WindowPosition = pos;
            });
        }
    }
}

