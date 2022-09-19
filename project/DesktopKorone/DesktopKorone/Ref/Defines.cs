using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

namespace DesktopKorone.Ref
{
	public class KoroneKibun
	{
		public const string NORMAL = nameof(NORMAL);
		public const string HAPPY = nameof(HAPPY);
		public const string ANGRY = nameof(ANGRY);
		public const string BORING = nameof(BORING);
		public const string SAD = nameof(SAD);
		public const string ASLEEPY = nameof(ASLEEPY);
	}

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class KoroneDesktopPluginAttr : Attribute
	{
		public string PluginName;

		public KoroneDesktopPluginAttr(string pluginName)
		{
			PluginName = pluginName;
		}
	}

	public abstract class KoroneDesktopPluginClass
	{
		public abstract void OAYO(MainWindow window);
		public abstract void OTSUKORON();

		public virtual void TODO_EVENT() { }
	}

	public interface IAnimationBehavior
	{
		void Prepare(MainWindow.AnimationInfo info, MainWindow window);
		void FirstFrame(MainWindow.AnimationInfo info, MainWindow window);
		void WindowFrameUpdated(MainWindow.AnimationInfo info, MainWindow window);
		void AnimtaionFrameUpdated(MainWindow.AnimationInfo info, MainWindow window);
		void LastFrame(MainWindow.AnimationInfo info, MainWindow window);
		void AnimationEnd(MainWindow.AnimationInfo info, MainWindow window);
	}

	public abstract class AnimationBehaviorClass : IAnimationBehavior
	{
		MainWindow m_window;
		MainWindow.AnimationInfo m_info;
		int ID;

		MainWindow Window => m_info.ID == ID ? m_window : null;
		MainWindow.AnimationInfo Info => m_info.ID == ID ? m_info : null;

		public void AnimationEnd(MainWindow.AnimationInfo info, MainWindow window)
		{
			if (ID == info.ID) _AnimationEnd(Info, Window);
		}

		public void AnimtaionFrameUpdated(MainWindow.AnimationInfo info, MainWindow window)
		{
			if (ID == info.ID) _AnimtaionFrameUpdated(Info, Window);
		}

		public void FirstFrame(MainWindow.AnimationInfo info, MainWindow window)
		{
			if (ID == info.ID) _FirstFrame(Info, Window);
		}

		public void LastFrame(MainWindow.AnimationInfo info, MainWindow window)
		{
			if (ID == info.ID) _LastFrame(Info, Window);
		}

		public void Prepare(MainWindow.AnimationInfo info, MainWindow window)
		{
			m_window = window;
			m_info = info;
			ID = info.ID;

			if (ID == info.ID) _Prepare(Info, Window);
		}

		public void WindowFrameUpdated(MainWindow.AnimationInfo info, MainWindow window)
		{
			if (ID == info.ID) _WindowFrameUpdated(Info, Window);
		}

		protected virtual void _Prepare(MainWindow.AnimationInfo info, MainWindow window)
		{

		}

		protected virtual void _AnimationEnd(MainWindow.AnimationInfo info, MainWindow window)
		{
		}

		protected virtual void _AnimtaionFrameUpdated(MainWindow.AnimationInfo info, MainWindow window)
		{
		}

		protected virtual void _FirstFrame(MainWindow.AnimationInfo info, MainWindow window)
		{
		}

		protected virtual void _LastFrame(MainWindow.AnimationInfo info, MainWindow window)
		{
		}

		protected virtual void _WindowFrameUpdated(MainWindow.AnimationInfo info, MainWindow window)
		{
		}
	}

	public enum AnimationState
	{
		START,
		UPDATE,
		END
	}

	public enum EisenhowerMatrix
	{
		NOT_URGENT__NOT_IMPORTANT = 0,
		URGENT__NOT_IMPORTANT,
		NOT_URGENT__IMPORTANT,
		URGENT__IMPORTANT,
	}

	public class KoroneAnimation
	{
		public class Frame
		{
			[JsonIgnore]
			public BitmapSource Image;

			[JsonIgnore]
			public string ImagePath { get => $"{MainWindow.DIR_RESOURCES}/{imagePath}"; }

			public readonly string Notice = "";

			public readonly string Notice_ImagePath = "Image path (png, gif)";
			[JsonProperty(nameof(ImagePath))]
			private string imagePath = "";

			public readonly string Notice_Delay = "the time the image will be displayed";
			public int Delay = 100;

			public readonly string Notice_EventCode = "Tells the plugin this code when the image is displayed";
			public string EventCode = "";

			public readonly string Notice_LoopCount = "How many times to repeat the motion of the frame";
			public int LoopCount = 0;


			[Obsolete]
			public Frame() { }

			public Frame(Image image, int delay, string eventCode, int loopCount)
			{
				Delay = delay;
				EventCode = eventCode;
				LoopCount = loopCount;

				if (image != null)
				{
					Image = MainWindow.ImageToBitmapSource(image);
				}
			}
		}

		public readonly uint VERSION = 1;
		public string AnimationName = "";
		public Frame[] Frames = new Frame[0];
		public readonly string Notice_Loop = "Loop <= 0 == 'loop', Loop > 0 == 'repeat'";
		public int Loop = 0;

		public bool LoadAndCheck()
		{
			if (AnimationName == "") return false;
			if (Frames == null) return false;
			if (Frames.Length == 0) return false;
			for (int current_frame_index = 0; current_frame_index < Frames.Length; current_frame_index++)
			{
				if (Frames[current_frame_index].Delay <= 0) return false;
				if (Frames[current_frame_index].LoopCount < 0) return false;

				try
				{
					var raw_image = Image.FromFile(Frames[current_frame_index].ImagePath);

					if (raw_image.RawFormat.Equals(ImageFormat.Png))
					{
						Frames[current_frame_index].Image = MainWindow.ImageToBitmapSource(raw_image);
					}
					else if (raw_image.RawFormat.Equals(ImageFormat.Gif) && ImageAnimator.CanAnimate(raw_image))
					{
						var dimension = new FrameDimension(raw_image.FrameDimensionsList[0]);
						int gif_frame_count = raw_image.GetFrameCount(dimension);
						List<Frame> frames = new List<Frame>();
						int index = 0;

						for (int gif_frame_index = 0; gif_frame_index < gif_frame_count; gif_frame_index++)
						{
							raw_image.SelectActiveFrame(dimension, gif_frame_index);
							var img = raw_image.Clone() as Image;
							var delay = BitConverter.ToInt32(raw_image.GetPropertyItem(20736).Value, index) * 10;
							delay = (delay < 100 ? 100 : delay);
							index += 4;

							frames.Add(new Frame(img, delay, ((gif_frame_index == 0 || gif_frame_index + 1 == gif_frame_count) ? Frames[current_frame_index].EventCode : ""), Frames[current_frame_index].LoopCount));
						}

						Array.Resize(ref Frames, Frames.Length + frames.Count - 1);//resize
						Array.Copy(Frames, current_frame_index + 1,
							Frames, current_frame_index + frames.Count - 1,
							Frames.Length - current_frame_index + 1);//move
						Array.Copy(frames.ToArray(), 0, Frames, current_frame_index, frames.Count);
						//F 8 [1][2][3(gif)][4][5][6][7][8]
						//f 3 [11][12][13]
						//c 2 cur
						//[1][2][3(gif)][4][5][6][7][8]
						//[1][2][3(gif)][4][5][6][7][8][][] <- resized
						//[1][2][3(gif)][][][4][5][6][7][8] <- moved
						//[1][2][11][12][13][4][5][6][7][8] <- f copyed
						//
					}
					else
					{
						throw new Exception("not supported");
					}
				}
				catch (Exception e)
				{
					return false;
				}
			}

			return true;
		}
	}
}
