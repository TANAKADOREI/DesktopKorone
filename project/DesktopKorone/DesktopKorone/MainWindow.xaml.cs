using DesktopKorone.Ref;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;
using WpfAnimatedGif;

using Image = System.Drawing.Image;
using Rectangle = System.Drawing.Rectangle;

namespace DesktopKorone
{
	public partial class MainWindow : Window
	{
		const string DIR_PLUGINS = "PLUGINS";
		const string DIR_RESOURCES = "RESOURCES";
		const string DIR_RESOURCES_ANIMATION_CONTROLLER = "RESOURCES_ANIMATION_CONTROLLER";
		const string PLUGIN_ROOT_CLASS = "KoroneDesktopPlugin";
		const string PLUGIN_BASE_FILE = "KoroneDesktopPlugin";

		public MainWindow()
		{
			InitializeComponent();
			Startup();
		}

		void CheckBaseDirs(string name)
		{
			if (!Directory.Exists(name)) Directory.CreateDirectory(name);
		}

		void Startup()
		{
			CheckBaseDirs(DIR_PLUGINS);
			CheckBaseDirs(DIR_RESOURCES);
			CheckBaseDirs(DIR_RESOURCES_ANIMATION_CONTROLLER);
			LoadPlugins();
			LoadResources();
		}

		void LoadPlugins()
		{
			List<Task> tasks = new List<Task>();

			m_plugins.Clear();
			foreach (var plugin in Directory.EnumerateFiles(DIR_PLUGINS, "*.dll").Append(PLUGIN_BASE_FILE))
			{
				tasks.Add(Task.Run(() =>
				{
					var asm = Assembly.LoadFile(plugin);
					var ins = asm.CreateInstance(PLUGIN_ROOT_CLASS) as KoroneDesktopPluginClass;

					if (ins == null)
					{
						MessageBox.Show($"{plugin}, error");
					}

					lock (m_lock)
					{
						m_plugins.Add(ins.Name, ins);
					}
				}));
			}

			Task.WaitAll(tasks.ToArray());
		}

		void LoadResources()
		{
			List<Task> tasks = new List<Task>();
			m_animations.Clear();
			foreach (var animation_file in Directory.EnumerateFiles(DIR_RESOURCES_ANIMATION_CONTROLLER))
			{
				tasks.Add(Task.Run(() =>
				{
					var animation = JsonConvert.DeserializeObject<KoroneAnimation>(File.ReadAllText(animation_file));

					if (animation == null || !animation.LoadAndCheck())
					{
						MessageBox.Show($"{animation_file}, error");
					}

					lock (m_lock)
					{
						m_animations.Add(animation.AnimationName, animation);
					}
				}));
			}

			Task.WaitAll(tasks.ToArray());
		}

		void StartTimer()
		{
			var timer = new DispatcherTimer();
			timer.Tick += Loop;
			timer.Start();
		}

		Tuple<string, KoroneDesktopPluginClass> GetTodo()
		{
			if (m_priority_0.Count != 0)
			{
				var o = m_priority_0[0];
				m_priority_0.RemoveAt(0);
				return o;
			}
			else if (m_priority_1.Count != 0)
			{
				var o = m_priority_1[0];
				m_priority_1.RemoveAt(0);
				return o;
			}
			else if (m_priority_2.Count != 0)
			{
				var o = m_priority_2[0];
				m_priority_2.RemoveAt(0);
				return o;
			}
			else if (m_priority_3.Count != 0)
			{
				var o = m_priority_3[0];
				m_priority_3.RemoveAt(0);
				return o;
			}
			else
			{
				return null;
			}
		}

		void Loop(object sender, EventArgs e)
		{
			var timer = sender as DispatcherTimer;
			timer.Dispatcher.
		}

		#region MemberVar

		object m_lock = new object();
		List<Tuple<string, KoroneDesktopPluginClass>> m_priority_0 = new List<Tuple<string, KoroneDesktopPluginClass>>();
		List<Tuple<string, KoroneDesktopPluginClass>> m_priority_1 = new List<Tuple<string, KoroneDesktopPluginClass>>();
		List<Tuple<string, KoroneDesktopPluginClass>> m_priority_2 = new List<Tuple<string, KoroneDesktopPluginClass>>();
		List<Tuple<string, KoroneDesktopPluginClass>> m_priority_3 = new List<Tuple<string, KoroneDesktopPluginClass>>();
		Dictionary<string, KoroneAnimation> m_animations = new Dictionary<string, KoroneAnimation>();
		Dictionary<string, KoroneDesktopPluginClass> m_plugins = new Dictionary<string, KoroneDesktopPluginClass>();

		#endregion

		#region Callable

		public void CALL_AnimationStart(string animation_name, bool force = false)
		{

		}

		public void CALL_AddTodoList(EisenhowerMatrixFlags flags, string animation_name, KoroneDesktopPluginClass plugin)
		{
			if (flags.HasFlag(EisenhowerMatrixFlags.URGENT | EisenhowerMatrixFlags.IMPORTANT))
			{
				m_priority_0.Add(new Tuple<string, KoroneDesktopPluginClass>(animation_name, plugin));
			}
			else if (flags.HasFlag(EisenhowerMatrixFlags.NOT_URGENT | EisenhowerMatrixFlags.IMPORTANT))
			{
				m_priority_1.Add(new Tuple<string, KoroneDesktopPluginClass>(animation_name, plugin));
			}
			else if (flags.HasFlag(EisenhowerMatrixFlags.URGENT | EisenhowerMatrixFlags.NOT_IMPORTANT))
			{
				m_priority_2.Add(new Tuple<string, KoroneDesktopPluginClass>(animation_name, plugin));
			}
			else if (flags.HasFlag(EisenhowerMatrixFlags.NOT_URGENT | EisenhowerMatrixFlags.NOT_IMPORTANT))
			{
				m_priority_3.Add(new Tuple<string, KoroneDesktopPluginClass>(animation_name, plugin));
			}
			else
			{
				return;
			}
		}

		#endregion
	}
}
