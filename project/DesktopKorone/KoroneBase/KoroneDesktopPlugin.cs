using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows;
using System.Windows.Media;
using DesktopKorone;
using DesktopKorone.Ref;

using Point = System.Drawing.Point;

[KoroneDesktopPluginAttr("KoroneDesktopBasePlugin")]
public class KoroneDesktopPlugin : KoroneDesktopPluginClass
{
    #region utils

    static float Clamp01(float o)
    {
        return 0 <= o ? 1 >= o ? o : 1 : 0;
    }

    static int Lerp(int a, int b, float t)
    {
        return (int)((float)a + ((float)b - (float)a) * t);
    }

    static Point Lerp(Point a, Point b, float t)
    {
        return new Point(Lerp(a.X, b.X, t), Lerp(a.Y, b.Y, t));
    }

    static float Abs(float f)
    {
        return (f < 0) ? -f : f;
    }

    static float Distance(Point a,Point b)
    {
        return Abs((float)Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y,2)));
    }

    #endregion

    MainWindow m_window;

    public override void OAYO(MainWindow window)
    {
        m_window = window;
    }

    public override void OTSUKORON()
    {
    }

    public override void TODO_EVENT()
    {
        switch (m_window.Random.Next(0, 2))
        {
            case 1:
                m_window.CALL_AddTodoList(EisenhowerMatrix.NOT_URGENT__NOT_IMPORTANT,
                    new MainWindow.Todo(new Anim_FreeWalk(), this, "@WALK"));
                break;
        }
    }

    class Anim_FreeWalk : IAnimationBehavior
    {
        ScaleTransform flipTrans;
        const float SPEED = 30f;
        Point m_goal_point;
        Point m_start_point;
        float m_time;


        public void AnimtaionFrameUpdated(MainWindow.AnimationInfo info, MainWindow window)
        {
        }

        public void End(MainWindow.AnimationInfo info, MainWindow window)
        {
        }

        public void Start(MainWindow.AnimationInfo info, MainWindow window)
        {
            
        }

        public void WindowFrameUpdated(MainWindow.AnimationInfo info, MainWindow window)
        {
            window.Dispatcher.Invoke(() =>
            {
                if(flipTrans == null)
                {
                    m_start_point = new Point((int)window.Left, (int)window.Top);
                    m_goal_point = new Point(window.Random.Next(0, window.ScreenWidth - (int)window.Width),
                        window.Random.Next(0, window.ScreenHeight - (int)window.Height));
                    m_time = 0;

                    flipTrans = new ScaleTransform();
                    
                    flipTrans.ScaleX = (m_goal_point.X - window.Position.X > 0) ? -1 : 1;
                    info.ImageView.RenderTransform = flipTrans;
                }

                var distance = Distance(m_start_point, m_goal_point);
                var total_time = distance / SPEED;
                var speed = (float)info.DeltaTime.TotalSeconds/ total_time;

                m_time += speed;

                if (m_time > 1f)
                {
                    info.BUTTON_ForceAnimationEnd = true;
                    return;
                }

                var pos = Lerp(m_start_point, m_goal_point, m_time);
                
                window.Left = pos.X;
                window.Top = pos.Y;
            });
        }
    }
}

