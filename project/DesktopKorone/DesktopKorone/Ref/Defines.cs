using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows;

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

	public class KoroneDesktopPluginSupport
	{
		public delegate void OAYO(MainWindow window, KoroneAnimation[] using_animations);
		public delegate MainWindow OTSUKORON();
		public delegate void EVENT(KoroneAnimation animation, AnimationState animation_state, string event_code);
	}

	public abstract class KoroneDesktopPluginClass
	{
		public abstract string Name { get; }
		public abstract void OAYO(MainWindow window);
		public abstract void OTSUKORON();

		protected virtual void EVENT(KoroneAnimation animation, AnimationState animation_state, string event_code)
		{

		}
	}

	public enum AnimationState
	{
		START,
		UPDATE,
		END
	}

	public enum EisenhowerMatrixFlags
	{
		URGENT,
		IMPORTANT,
		NOT_URGENT,
		NOT_IMPORTANT
	}

	public class KoroneAnimation
	{
		public class Frame
		{
			[JsonIgnore]
			public Image Image;

			public readonly string Notice = "";

			public readonly string Notice_ImagePath = "Image path (png, gif)";
			public string ImagePath = "";

			public readonly string Notice_Delay = "the time the image will be displayed";
			public int Delay = 100;

			public readonly string Notice_EventCode = "Tells the plugin this code when the image is displayed";
			public string EventCode = "";

			public readonly string Notice_LoopCount = "How many times to repeat the motion of the frame";
			public int LoopCount = 0;

			[Obsolete]
			public Frame() { }

			[Obsolete]
			public Frame(string TEMPLATE)
			{
				Notice = TEMPLATE;
			}

			public Frame(Image image, int delay, string eventCode, int loopCount)
			{
				Image = image;
				Delay = delay;
				EventCode = eventCode;
				LoopCount = loopCount;
			}
		}

		public readonly uint VERSION = 1;
		public string AnimationName = "";
		public List<Frame> Frames = new List<Frame>(new Frame[] { new Frame("FrameTemplate") });

		public bool LoadAndCheck()
		{
			if (AnimationName == "") return false;
			if (Frames == null) return false;
			if (Frames.Count == 0) return false;
			for (int current_frame_index = 0; current_frame_index < Frames.Count; current_frame_index++)
			{
				if (Frames[current_frame_index].Delay <= 0) return false;
				if (Frames[current_frame_index].LoopCount < 0) return false;

				try
				{
					Frames[current_frame_index].Image = Image.FromFile(Frames[current_frame_index].ImagePath);

					if (Frames[current_frame_index].Image.RawFormat.Equals(ImageFormat.Png))
					{
						//continue
					}
					else if (Frames[current_frame_index].Image.RawFormat.Equals(ImageFormat.Gif) && ImageAnimator.CanAnimate(Frames[current_frame_index].Image))
					{
						var dimension = new FrameDimension(Frames[current_frame_index].Image.FrameDimensionsList[0]);
						int gif_frame_count = Frames[current_frame_index].Image.GetFrameCount(dimension);
						List<Frame> frames = new List<Frame>();
						int index = 0;

						for (int gif_frame_index = 0; gif_frame_index < gif_frame_count; gif_frame_index++)
						{
							Frames[current_frame_index].Image.SelectActiveFrame(dimension, gif_frame_index);
							var img = Frames[current_frame_index].Image.Clone() as Image;
							var delay = BitConverter.ToInt32(Frames[current_frame_index].Image.GetPropertyItem(20736).Value, index) * 10;
							delay = (delay < 100 ? 100 : delay);
							index += 4;

							frames.Add(new Frame(img,delay, ((gif_frame_index == 0 || gif_frame_index+1 == gif_frame_count) ? Frames[current_frame_index].EventCode : ""), Frames[current_frame_index].LoopCount));
						}
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
