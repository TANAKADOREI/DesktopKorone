using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using DesktopKorone.Ref;

namespace DesktopKorone
{
    internal static class BaseKoudou
    {
        //korone itself
        public const string KOUDOU_OAYO = nameof(KOUDOU_OAYO);
        public const string KOUDOU_OTSUKORON = nameof(KOUDOU_OTSUKORON);
        public const string KOUDOU_TWEET = nameof(KOUDOU_TWEET);
        public const string KOUDOU_YOUTUBE = nameof(KOUDOU_YOUTUBE);
        public const string KOUDOU_MOVE = nameof(KOUDOU_MOVE);
        public const string KOUDOU_WAIT = nameof(KOUDOU_WAIT);
        //optional
        public const string KOUDOU_HANGOVERWINDOW = nameof(KOUDOU_HANGOVERWINDOW);
        public const string KOUDOU_ONWINDOW = nameof(KOUDOU_ONWINDOW);

        //interaction
        public const string KOUDOU_CATCH = nameof(KOUDOU_CATCH);
        public const string KOUDOU_TOUCH = nameof(KOUDOU_TOUCH);

        public static KoudouReturnType Register()
        {
            var ret = new KoudouReturnType(Awake, Asleep, AnimStart, AnimUpdate, AnimEnd);
            AddBaseKoudou(ret, KoroneKibun.NORMAL, KOUDOU_HANGOVERWINDOW, KOUDOU_ONWINDOW);
            AddBaseKoudou(ret, KoroneKibun.BORING, KOUDOU_HANGOVERWINDOW, KOUDOU_ONWINDOW);
            AddBaseKoudou(ret, KoroneKibun.HAPPY, KOUDOU_HANGOVERWINDOW, KOUDOU_ONWINDOW);
            AddBaseKoudou(ret, KoroneKibun.ANGRY);
            AddBaseKoudou(ret, KoroneKibun.SAD);
            return ret;
        }

        private static void Awake(Window window)
        {
        }

        private static void Asleep()
        {

        }

        private static void AnimStart(string kibun, string koudou)
        {

        }

        private static void AnimUpdate(string kibun, string koudou)
        {

        }

        private static void AnimEnd(string kibun, string koudou)
        {

        }

        private static void AddBaseKoudou(KoudouReturnType ret,string kibun, params string[] add)
		{
            ret.AddKoudou(kibun, KOUDOU_OAYO);
            ret.AddKoudou(kibun, KOUDOU_OTSUKORON);
            ret.AddKoudou(kibun, KOUDOU_TWEET);
            ret.AddKoudou(kibun, KOUDOU_YOUTUBE);
            ret.AddKoudou(kibun, KOUDOU_MOVE);
            ret.AddKoudou(kibun, KOUDOU_WAIT);

            foreach(var i in add)
			{
                ret.AddKoudou(kibun, i);
			}
		}
    }
}
