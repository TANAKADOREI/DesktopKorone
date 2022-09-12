using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace DesktopKorone.Ref
{
	public class KoudouReturnType
	{
		public readonly Action<Window> DLL_Awake;
		public readonly Action DLL_Asleep;
		public readonly Action<string, string> AnimStart = null;
		public readonly Action<string, string> AnimUpdate = null;
		public readonly Action<string, string> AnimEnd = null;
		public readonly Dictionary<string, HashSet<string>> m_motions = new Dictionary<string, HashSet<string>>();

		public KoudouReturnType(Action<Window> dLL_Awake, Action dLL_Asleep, Action<string, string> animStart, Action<string, string> animUpdate, Action<string, string> animEnd)
		{
			DLL_Awake = dLL_Awake;
			DLL_Asleep = dLL_Asleep;
			AnimStart = animStart;
			AnimUpdate = animUpdate;
			AnimEnd = animEnd;
		}

		public void AddKoudou(string kibun_name, string koudou_name)
		{
			if (!m_motions.ContainsKey(kibun_name))
			{
				m_motions[kibun_name] = new HashSet<string>();
			}
			m_motions[kibun_name].Add(koudou_name);
		}
	}
}
