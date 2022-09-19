﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Net.NetworkInformation;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using DesktopKorone;
using DesktopKorone.Ref;

using Point = System.Drawing.Point;

[KoroneDesktopPluginAttr("KoroneDesktopBasePlugin")]
public class KoroneDesktopPlugin : KoroneDesktopPluginClass
{
    #region utils

    static float Clamp(float o)
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

<<<<<<< Updated upstream
=======
    static float Abs(float f)
    {
        return (f < 0) ? -f : f;
    }

    static float Distance(Point a, Point b)
    {
        return Abs((float)Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2)));
    }

>>>>>>> Stashed changes
    #endregion

    MainWindow m_window;

    public override void OAYO(MainWindow window)
    {
        m_window = window;
        //m_window.MouseDown += M_window_MouseDown;
    }

    private void M_window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if(e.ChangedButton == MouseButton.Left)
        {
            m_window.DragMove();
            m_window.InstantTodo(new MainWindow.Todo(new Anim_Catch(), this, "@CATCH"));
        }
        else if (e.ChangedButton == MouseButton.Right)
        {
            m_window.InstantTodo(new MainWindow.Todo(new Anim_Nade(), this, "@NADE"));
        }
    }

    public override void OTSUKORON()
    {
    }

    public override void TODO_EVENT()
    {
        switch (m_window.Random.Next(0, 4))
        {
            case 1:
                m_window.AddTodo(EisenhowerMatrix.NOT_URGENT__NOT_IMPORTANT,
                    new MainWindow.Todo(new Anim_FreeWalk(), this, "@WALK"));
                break;
        }
    }

    class Anim_Catch : AnimationBehaviorClass
    {
        protected override void _AnimtaionFrameUpdated(MainWindow.AnimationInfo info, MainWindow window)
        {
            if (Mouse.LeftButton != MouseButtonState.Pressed)
            {
                info.BUTTON_ForceAnimationEnd = false;
            }
        }
    }

    class Anim_Nade : AnimationBehaviorClass
    {
        protected override void _LastFrame(MainWindow.AnimationInfo info, MainWindow window)
        {
            info.BUTTON_ForceAnimationEnd = true;
        }
    }

    class Anim_FreeWalk : AnimationBehaviorClass
    {
        ScaleTransform flipTrans;
        const float SPEED = 0.1f;
        Point m_goal_point;
        Point m_start_point;
        float m_time;

        protected override void _Prepare(MainWindow.AnimationInfo info, MainWindow window)
        {
            window.Dispatcher.Invoke(() =>
            {
                m_start_point = new Point((int)window.Left, (int)window.Top);
                m_goal_point = new Point(window.Random.Next(0, window.ScreenWidth - (int)window.Width),
                    window.Random.Next(0, window.ScreenHeight - (int)window.Height));
                m_time = 0;

                flipTrans = new ScaleTransform();

                flipTrans.ScaleX = (m_goal_point.X - window.Position.X > 0) ? -1 : 1;
                info.ImageView.RenderTransform = flipTrans;
            });
        }

        protected override void _WindowFrameUpdated(MainWindow.AnimationInfo info, MainWindow window)
        {
            window.Dispatcher.Invoke(() =>
            {
<<<<<<< Updated upstream
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

                m_time += SPEED * (float)info.DeltaTime.TotalSeconds;
=======
                var distance = Distance(m_start_point, m_goal_point);
                var total_time = distance / SPEED;
                var speed = (float)info.DeltaTime.TotalSeconds / total_time;

                m_time += speed;
>>>>>>> Stashed changes

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

