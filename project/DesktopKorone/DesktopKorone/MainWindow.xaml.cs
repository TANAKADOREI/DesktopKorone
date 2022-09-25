using DesktopKorone.Ref;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
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
using Point = System.Windows.Point;

namespace DesktopKorone
{
	public partial class MainWindow : Window
	{
		[DllImport("gdi32.dll", EntryPoint = nameof(DeleteObject))]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DeleteObject([In] IntPtr hObject);
		[DllImport("User32")]
		public static extern int GetCursorPos(out __Point pt);

		public struct __Point
		{
			public int x;
			public int y;
			public __Point(int _x, int _y)
			{
				x = _x;
				y = _y;
			}
		}

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
			public int WindowSize = 128;

			[JsonIgnore]
			public int FPS_sleep_ms => 1000 / FPS;
		}

		public MainWindow()
		{
			InitializeComponent();
			Loaded += MainWindow_Loaded;
		}

		private void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
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

		public static Point PixelPosToUnit(int x,int y,Visual visual)
		{
			var transform = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice;
			var pos = transform.Transform(new Point(x, y));
			return pos;
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

			WindowSize = PixelPosToUnit(m_config.WindowSize, m_config.WindowSize, this);
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

				var animation = JsonConvert.DeserializeObject<KoroneAnimation>(File.ReadAllText(animation_file));

				#region tmp

				if (animation == null || !animation.LoadAndCheck())
				{
					Exit($"{animation_file}, error");
				}

				m_animations.Add(animation.AnimationName, animation);
				#endregion

				#region why?
				//tasks.Add(Task.Run(() =>
				//{
				//	var animation = JsonConvert.DeserializeObject<KoroneAnimation>(File.ReadAllText(animation_file));
				//
				//	if (animation == null || !animation.LoadAndCheck())
				//	{
				//		Exit($"{animation_file}, error");
				//	}
				//
				//	lock (m_lock)
				//	{
				//		m_animations.Add(animation.AnimationName, animation);
				//	}
				//}));
				#endregion
			}

			//Task.WaitAll(tasks.ToArray());

			if (m_animations.Count == 0)
			{
				Exit("null animation");
			}
		}

		public class Request
		{
			public bool ForceFindNewTodo = false;
		}

		public class AnimationInfo
		{
			public KoroneAnimation Animation;
			public KoroneDesktopPluginClass Plugin => CurrentTodo.Plugin;
			public IAnimationBehavior Behavior => CurrentTodo?.Behavior;
			public int CurrentFrameIndex;
			public System.Windows.Controls.Image ImageView;
			public TimeSpan DeltaTime;
			public Todo CurrentTodo;

			[Obsolete("dont access")]
			public int ID = 0;

			#region Not restored to default on next frame
			public bool TOGGLE_PauseAnimation = false;
			#endregion

			#region Restored to default on next frame
			public bool BUTTON_ForceAnimationEnd = false;
			#endregion

			public void Clear()
			{
				Animation = null;
				CurrentTodo = null;
				CurrentFrameIndex = 0;
				TOGGLE_PauseAnimation = false;
				BUTTON_ForceAnimationEnd = false;
			}

			public AnimationInfo(System.Windows.Controls.Image image_view)
			{
				ImageView = image_view;
			}
		}

		public class Todo
		{
			public IAnimationBehavior Behavior;
			public KoroneDesktopPluginClass Plugin;
			public string AnimationName;
			public int Priority;

			public Todo(IAnimationBehavior behavior, KoroneDesktopPluginClass plugin, string animationName)
			{
				Behavior = behavior;
				Plugin = plugin;
				AnimationName = animationName;
			}
		}

		void StartThread()
		{
			m_loop_thread_token = new CancellationTokenSource();
			m_loop_thread = new Thread(new ThreadStart(() =>
			{
				var sleep = m_config.FPS_sleep_ms;

				var todo = GetTodo();
				AnimationInfo info = new AnimationInfo(IMAGEVIEW_CHAR);

				info.Clear();
				long old_time = DateTime.UtcNow.Ticks;
				long anim_frame_old_time = DateTime.UtcNow.Ticks;
				long todo_find_time_old = DateTime.UtcNow.Ticks;
				int next_behavior_delay = 0;


				var ApplyTodo = new Action<Todo>((todo) =>
				{
					info.CurrentTodo = todo;
					info.CurrentFrameIndex = 0;
					info.Animation = m_animations[todo.AnimationName];
					info.ID++;

					if (info.Behavior != null) info.CurrentTodo.Behavior.Prepare(info, this);
				});

				var NewTodo = new Action<bool>((bool get_idle) =>
				{
					todo = GetTodo();

					if (info.Animation != null && todo.AnimationName == info.Animation.AnimationName)
					{
						return;
					}

					if ((info.CurrentTodo != null && todo.Priority > info.CurrentTodo.Priority) || info.CurrentTodo == null)
					{
						ApplyTodo(todo);
					}
				});

				var RenderImage = new Action(() =>
				{
					Dispatcher.Invoke(() =>
					{
						IMAGEVIEW_CHAR.Source = info.Animation.Frames[info.CurrentFrameIndex].Image;
						if (info.Behavior != null) info.Behavior.AnimtaionFrameUpdated(info, this);
					});
				});

				NewTodo(true);

				while (!m_loop_thread_token.IsCancellationRequested)
				{
					info.DeltaTime = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - old_time);
					old_time = DateTime.UtcNow.Ticks;

					var req = WindowFrameUpdate?.Invoke();

					{
						if (next_behavior_delay <= 0)
						{
							next_behavior_delay = Random.Next(m_config.BehaviorRandomDelay_Min_MS, m_config.BehaviorRandomDelay_Max_MS);
						}

						bool is_find_todo = req != null && req.ForceFindNewTodo;
						bool is_instant_anim = m_instant_animation != null;
						bool is_time = TimeSpan.FromTicks(DateTime.UtcNow.Ticks - todo_find_time_old).TotalMilliseconds > next_behavior_delay;

						if (is_instant_anim)
						{
							ApplyTodo(m_instant_animation);
							m_instant_animation = null;
						}
						else if (is_find_todo || is_time)
						{
							foreach (var p in m_plugins.Values)
							{
								p.TODO_EVENT();
							}

							NewTodo(false);
							todo_find_time_old = DateTime.UtcNow.Ticks;
						}
					}

					//animation proc
					if (info.Animation != null)
					{
						if ((!info.TOGGLE_PauseAnimation && TimeSpan.FromTicks(DateTime.UtcNow.Ticks - anim_frame_old_time).TotalMilliseconds > info.Animation.Frames[info.CurrentFrameIndex].Delay) || info.BUTTON_ForceAnimationEnd)
						{
							anim_frame_old_time = DateTime.UtcNow.Ticks;

							RenderImage();

							if (info.CurrentFrameIndex == 0)
							{
								if (info.Behavior != null) info.Behavior.FirstFrame(info, this);
							}

							if (info.CurrentFrameIndex + 1 >= info.Animation.Frames.Length || info.BUTTON_ForceAnimationEnd)
							{
								if (info.Behavior != null) info.Behavior.LastFrame(info, this);
								if (info.BUTTON_ForceAnimationEnd)
								{
									//animation end
									if (info.Behavior != null) info.Behavior.AnimationEnd(info, this);
									info.Clear();
									NewTodo(true);
									goto done;
								}
								info.CurrentFrameIndex = 0;
							}
							else
							{
								info.CurrentFrameIndex++;
							}
						}
					}

					if (info.Behavior != null) info.Behavior.WindowFrameUpdated(info, this);

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

		//<anim name> <plugin class>
		Todo GetTodo()
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
				var idle = new Todo(null, m_plugins[PLUGIN_BASE_NAME], ANIMATION_IDLE);
				idle.Priority = (int)EisenhowerMatrix.NOT_URGENT__NOT_IMPORTANT;
				idle.Priority--;
				return idle;
			}
		}

		#region MemberVar

		public readonly Random Random = new Random();
		public Point ScreenSize
		{
			get
			{
				return new Point((int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight);
			}
		}
		public Point WindowPosition
		{
			get
			{
				return new Point(Left, Top);
			}
			set
			{
				Left = value.X;
				Top = value.Y;
			}
		}
		public Point WindowSize
		{
			get
			{
				return new System.Windows.Point(Width, Height);
			}
			set
			{
				Width = value.X;
				Height = value.Y;
			}
		}
		public event Func<Request> WindowFrameUpdate;
		public Point MousePos
		{
			get
			{
				var pos = System.Windows.Forms.Control.MousePosition;
				return PixelPosToUnit(pos.X,pos.Y,this);
			}
		}
		object m_lock = new object();

		Config m_config;
		Thread m_loop_thread;
		CancellationTokenSource m_loop_thread_token;
		Todo m_instant_animation = null;
		List<Todo> m_priority_0 = new List<Todo>();
		List<Todo> m_priority_1 = new List<Todo>();
		List<Todo> m_priority_2 = new List<Todo>();
		List<Todo> m_priority_3 = new List<Todo>();
		Dictionary<string, KoroneAnimation> m_animations = new Dictionary<string, KoroneAnimation>();
		Dictionary<string, KoroneDesktopPluginClass> m_plugins = new Dictionary<string, KoroneDesktopPluginClass>();

		#endregion

		#region Callable

		public bool AddTodo(EisenhowerMatrix priority, Todo todo)
		{
			int priority_int = (int)priority;
			todo.Priority = priority_int;
			if (priority_int == 0)
			{
				m_priority_0.Add(todo);
			}
			else if (priority_int == 1)
			{
				m_priority_1.Add(todo);
			}
			else if (priority_int == 2)
			{
				m_priority_2.Add(todo);
			}
			else if (priority_int == 3)
			{
				m_priority_3.Add(todo);
			}
			else
			{
				return false;
			}

			return true;
		}

		public void InstantTodo(Todo todo)
		{
			m_instant_animation = todo;
		}

		#endregion
	}
}
