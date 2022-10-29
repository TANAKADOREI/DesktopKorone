using System;
using System.Windows.Input;
using DesktopKorone;
using DesktopKorone.Ref;
using Point = System.Windows.Point;

[KoroneDesktopPluginAttr("KoroneDesktopBasePlugin")]
public partial class KoroneDesktopPlugin : KoroneDesktopPluginClass
{
    #region utils

    static float Clamp(float o)
    {
        return 0 <= o ? 1 >= o ? o : 1 : 0;
    }

    static double Lerp(double a, double b, double t)
    {
        return (a + (b - a) * t);
    }

    static Point Lerp(Point a, Point b, float t)
    {
        return new Point(Lerp(a.X, b.X, t), Lerp(a.Y, b.Y, t));
    }

    static float Abs(float f)
    {
        return (f < 0) ? -f : f;
    }

    static float Distance(Point a, Point b)
    {
        return Abs((float)Math.Sqrt(Math.Pow(b.X - a.X, 2) + Math.Pow(b.Y - a.Y, 2)));
    }
    #endregion

    MainWindow m_window;

    public override void OAYO(MainWindow window)
    {
        m_window = window;
        m_window.PreviewMouseDown += M_window_MouseDown;
        m_window.MouseEnter += M_window_MouseEnter;
    }

    private void M_window_MouseEnter(object sender, MouseEventArgs e)
    {
        m_window.InstantTodo(new MainWindow.Todo(new Anim_What(), this, "@CATCH"));
    }

    private void M_window_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
        {
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
}

