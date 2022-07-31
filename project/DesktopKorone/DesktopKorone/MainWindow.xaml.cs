using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DesktopKorone
{

    enum KoroneKibun
    {
        FUTSU_DOG = 0,
        HAPPY_DOG,
        ANGRY_DOG,
        BORING_DOG,
        SAD_DOG,
        SLEEPY_DOG,
    }

    enum KoroneMokuhyou
    {
        OAYO,
        OTSUKORON,
        OSHIRASE_TWEET,
        OSHIRASE_YOUTUBE,
        MOVING_DOG,
        STANDING_DOG,
        CAUGHT_DOG,
    }

    class KoroneFrame
    {
        public Bitmap Frame;
        public int Duration;
    }

    public partial class MainWindow : Window
    {
        public const string PATH_ROOT = "KoroneSouko";

        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        static string GetAnimID(KoroneKibun kibun, KoroneMokuhyou mokuhyou)
        {
            return GetAnimID(Enum.GetName(typeof(KoroneKibun), kibun), Enum.GetName(typeof(KoroneMokuhyou), mokuhyou));
        }

        static string GetAnimID(string kibun, string mokuhyou)
        {
            return $"{kibun}/{mokuhyou}";
        }

        private void Init()
        {
            //dirs
            {
                string char_root_dir = $"{PATH_ROOT}/Char";
                CheckDir(char_root_dir);
                foreach (var _fuku in Directory.GetDirectories(char_root_dir).Append("Base"))
                {
                    string fuku = System.IO.Path.GetFileName(_fuku);

                    foreach (var kibun in Enum.GetNames(typeof(KoroneKibun)))
                    {
                        foreach (var mokuhyou in Enum.GetNames(typeof(KoroneMokuhyou)))
                        {
                            string anim_id = GetAnimID(kibun, mokuhyou);
                            string path = $"{char_root_dir}/{fuku}/{anim_id}";
                            CheckDir(path);

                            foreach(var _random_preset in Directory.GetDirectories(path).Append("Base"))
                            {
                                string random_preset = System.IO.Path.GetFileName(_random_preset);
                                path = $"{PATH_ROOT}/Char/{fuku}/{anim_id}/{random_preset}";
                                CheckDir(path);

                                foreach (var frame in Directory.GetFiles(path))
                                {

                                }
                            }
                        }
                    }
                }
            }
        }

        private static void CheckDir(string dir)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
    }
}
