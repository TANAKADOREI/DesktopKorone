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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

using Image = System.Drawing.Image;
using Path = System.IO.Path;
using Rectangle = System.Drawing.Rectangle;

namespace DesktopKorone
{
	public partial class MainWindow : Window
	{
		[DllImport("gdi32.dll", EntryPoint = nameof(DeleteObject))]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeleteObject([In] IntPtr hObject);

		public const string DIR_PLUGINS = "PLUGINS";
		public const string DIR_RESOURCES = "RESOURCES";
		public const string DIR_RESOURCES_ANIMATION_CONTROLLER = "RESOURCES_ANIMATION_CONTROLLER";
		const string FILE_CONFIG = "Config.json";
		const string PLUGIN_BASE_FILE = "KoroneDesktopBasePlugin.dll";
		const string PLUGIN_BASE_NAME = "KoroneDesktopBasePlugin";
		const string FILE_ANIMATION_TEMPLATE = "template";
		const string ANIMATION_IDLE = "@IDLE";

		class Config
		{
			public int BehaviorRandomDelay_Min_MS = 1000;
			public int BehaviorRandomDelay_Max_MS = 3000;
			public int FPS = 60;

			[JsonIgnore]
			public int FPS_sleep_ms => 1000 / FPS;
		}

		public MainWindow()
		{
			InitializeComponent();
			Startup();
		}

		public static void Exit(string msg)
		{
			MessageBox.Show(msg);
			Environment.Exit(0);
		}

		public static BitmapSource ImageToBitmapSource(Image image)
		{
			Bitmap bitmap = image as Bitmap;
			var handle = bitmap.GetHbitmap();
			try
			{
				return Imaging.CreateBitmapSourceFromHBitmap(bitmap.GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
			}
			finally
			{
				DeleteObject(handle);
			}
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
			LoadConfig();
			LoadPlugins();
			LoadResources();
			StartThread();
		}

		void LoadConfig()
		{
			if (File.Exists(FILE_CONFIG))
			{
				m_config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(FILE_CONFIG));
			}
			else
			{
				m_config = new Config();
				File.WriteAllText(FILE_CONFIG, JsonConvert.SerializeObject(m_config, Formatting.Indented));
			}
		}

		void LoadPlugins()
		{
			List<Task> tasks = new List<Task>();

			m_plugins.Clear();
			foreach (var plugin in Directory.EnumerateFiles(DIR_PLUGINS, "*.dll").Append(PLUGIN_BASE_FILE))
			{
				tasks.Add(Task.Run(() =>
				{
					var asm = Assembly.LoadFile(Path.GetFullPath(plugin));
					foreach (var type in from t in asm.GetTypes() where Attribute.IsDefined(t, typeof(KoroneDesktopPluginAttr)) select t)
					{
						var attr = type.GetCustomAttribute(typeof(KoroneDesktopPluginAttr)) as KoroneDesktopPluginAttr;
						string plugin_name = attr.PluginName;
						KoroneDesktopPluginClass plugin_class = Activator.CreateInstance(type) as KoroneDesktopPluginClass;

						if (plugin_class == null)
						{
							Exit($"plugin_class : {plugin_class}, error");
						}

						if (plugin_name == null || plugin_name == "")
						{
							Exit($"plugin_name : {plugin_name}, error");
						}

						lock (m_lock)
						{
							m_plugins.Add(plugin_name, plugin_class);
						}
					}
				}));
			}

			Task.WaitAll(tasks.ToArray());

			foreach (var p in m_plugins.Values)
			{
				p.OAYO(this);
			}
		}

		void LoadResources()
		{
			List<Task> tasks = new List<Task>();
			m_animations.Clear();

			//create template file
			string template_animation_file_name = $"{DIR_RESOURCES_ANIMATION_CONTROLLER}/{FILE_ANIMATION_TEMPLATE}.json";
			if (true || !File.Exists(template_animation_file_name))
			{
				File.WriteAllText(template_animation_file_name, JsonConvert.SerializeObject(new KoroneAnimation()
				{
					AnimationName = "MyAnimationName",
					Frames = new KoroneAnimation.Frame[]
					{
					new KoroneAnimation.Frame(null,563,"event code",123)
					}
				}, Formatting.Indented));
			}

			//create idle anim file
			string idle_animation_file_name = $"{DIR_RESOURCES_ANIMATION_CONTROLLER}/{ANIMATION_IDLE}.json";
			if (!File.Exists(idle_animation_file_name))
			{
				File.WriteAllText(idle_animation_file_name, JsonConvert.SerializeObject(new KoroneAnimation()
				{
					AnimationName = ANIMATION_IDLE,
				}, Formatting.Indented));
			}

			foreach (var animation_file in Directory.EnumerateFiles(DIR_RESOURCES_ANIMATION_CONTROLLER))
			{
				if (Path.GetFileName(animation_file).ToLower().Contains(FILE_ANIMATION_TEMPLATE)) continue;

				tasks.Add(Task.Run(() =>
				{
					var animation = JsonConvert.DeserializeObject<KoroneAnimation>(File.ReadAllText(animation_file));

					if (animation == null || !animation.LoadAndCheck())
					{
						Exit($"{animation_file}, error");
					}

					lock (m_lock)
					{
						m_animations.Add(animation.AnimationName, animation);
					}
				}));
			}

			Task.WaitAll(tasks.ToArray());

			if (m_animations.Count == 0)
			{
				Exit("null animation");
			}
		}

		public class AnimationInfo
		{
			public KoroneAnimation Animation;
			public KoroneDesktopPluginClass Plugin;
			public int CurrentFrameIndex;

			#region Not restored to default on next frame
			public bool TOGGLE_PauseAnimation = false;
			#endregion

			#region Restored to default on next frame
			public bool BUTTON_ForceAnimationEnd = false;
			#endregion
		}

		void StartThread()
		{
			m_loop_thread_token = new CancellationTokenSource();
			m_loop_thread = new Thread(new ThreadStart(() =>
			{
				var sleep = m_config.FPS_sleep_ms;

				var todo = GetTodo();
				AnimationInfo info = new AnimationInfo()
				{
					CurrentFrameIndex = 0,
					Animation = m_animations[todo.Item1],
					Plugin = todo.Item2
				};
				long old_time = DateTime.UtcNow.Ticks;
				bool animation_running = true;
				int next_behavior_delay = 0;

				RenderFrame(info.Animation.Frames[info.CurrentFrameIndex]);

				while (!m_loop_thread_token.IsCancellationRequested)
				{
					info.Plugin.LOOP(ref info);
					if (!animation_running)
					{
						if (next_behavior_delay <= 0)
						{
							next_behavior_delay = Random.Next(m_config.BehaviorRandomDelay_Min_MS, m_config.BehaviorRandomDelay_Max_MS);
							foreach (var p in m_plugins.Values)
							{
								p.TODO_EVENT();
							}
						}

						if (DateTime.UtcNow.Ticks - old_time > next_behavior_delay)
						{
							todo = GetTodo();
							info.CurrentFrameIndex = 0;
							info.Animation = m_animations[todo.Item1];
							info.Plugin = todo.Item2;

							animation_running = true;
							goto done;
						}
					}
					else
					{
						if ((!info.TOGGLE_PauseAnimation && DateTime.UtcNow.Ticks - old_time > info.Animation.Frames[info.CurrentFrameIndex].Delay) || info.BUTTON_ForceAnimationEnd)
						{
							if (info.CurrentFrameIndex + 1 >= info.Animation.Frames.Length || info.BUTTON_ForceAnimationEnd)
							{
								if (info.BUTTON_ForceAnimationEnd)
								{
									//animation end
									next_behavior_delay = 0;
									info.CurrentFrameIndex = 0;
									animation_running = false;
									info.BUTTON_ForceAnimationEnd = false;
									goto done;
								}
								else
								{
									//loop
									info.CurrentFrameIndex = 0;
								}
							}
							else
							{
								info.CurrentFrameIndex++;
							}

							old_time = DateTime.UtcNow.Ticks;
							RenderFrame(info.Animation.Frames[info.CurrentFrameIndex]);
						}
					}

				done:
					Thread.Sleep(sleep);
				}
			}));

			Closing += MainWindow_Closing;
			m_loop_thread.Start();
		}

		private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			m_loop_thread_token.Cancel();
			foreach (var p in m_plugins.Values)
			{
				p.OTSUKORON();
			}
			m_loop_thread.Join();
		}

		void RenderFrame(KoroneAnimation.Frame frame)
		{
			Dispatcher.Invoke(() =>
			{
				IMAGEVIEW_CHAR.Source = frame.Image;
			});
		}

		//<anim name> <plugin class>
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
				return new Tuple<string, KoroneDesktopPluginClass>(ANIMATION_IDLE, m_plugins[PLUGIN_BASE_NAME]);
			}
		}

		#region MemberVar

		public readonly Random Random = new Random();
		object m_lock = new object();

		Config m_config;
		Thread m_loop_thread;
		CancellationTokenSource m_loop_thread_token;
		List<Tuple<string, KoroneDesktopPluginClass>> m_priority_0 = new List<Tuple<string, KoroneDesktopPluginClass>>();
		List<Tuple<string, KoroneDesktopPluginClass>> m_priority_1 = new List<Tuple<string, KoroneDesktopPluginClass>>();
		List<Tuple<string, KoroneDesktopPluginClass>> m_priority_2 = new List<Tuple<string, KoroneDesktopPluginClass>>();
		List<Tuple<string, KoroneDesktopPluginClass>> m_priority_3 = new List<Tuple<string, KoroneDesktopPluginClass>>();
		Dictionary<string, KoroneAnimation> m_animations = new Dictionary<string, KoroneAnimation>();
		Dictionary<string, KoroneDesktopPluginClass> m_plugins = new Dictionary<string, KoroneDesktopPluginClass>();

		#endregion

		#region Callable

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
