using Newtonsoft.Json;
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
using WpfAnimatedGif;

namespace DesktopKorone
{

	class Config
	{
		public class Motion
		{
			public Motion(string kibun_name, params Koudou[] koudou)
			{

			}
		}

		public class Koudou
		{
			//custom dll example : "<dll file name>:full class name(<namespace1>.<namespace2>.Foo)"
			//BaseMotion.dll:MyNamespace.MainClass
			public Koudou(string koudou_name, string dll)
			{

			}
			public Koudou(string koudou_name, Func<>)
		}

		[JsonIgnore]
		public readonly static Koudou[] KIBAN_KOUDOU = new Koudou[]
		{
			new Koudou("OAYO"),
			new Koudou("OTSUKORON"),
			new Koudou("TWEET"),
			new Koudou("YOUTUBE"),
			new Koudou("MOVE"),
			new Koudou("STAND"),
			new Koudou("CATCH"),
		};

        [JsonProperty("Motions")]
		public Motion[] m_motions = new Motion[]
		{
			new Motion("FUTSU",KIBAN_KOUDOU),
			new Motion("HAPPY",KIBAN_KOUDOU),
			new Motion("ANGRY",KIBAN_KOUDOU),
			new Motion("BORING",KIBAN_KOUDOU),
			new Motion("SAD",KIBAN_KOUDOU),
			new Motion("ASEELPY",KIBAN_KOUDOU),
		};
	}

	abstract class RenderClass
	{
		public abstract void Rendering(System.Windows.Controls.Image image_view);
	}

	class GIF_RenderClass : RenderClass
	{
		public GIF_RenderClass(string gif_file_path)
		{

		}

		public override void Rendering(System.Windows.Controls.Image image_view)
		{
			throw new NotImplementedException();
		}
	}

	class PNG_RenderClass : RenderClass
	{
		public PNG_RenderClass(string mokuhyou_dir_path)
		{

		}

		public override void Rendering(System.Windows.Controls.Image image_view)
		{
			throw new NotImplementedException();
		}
	}

	public partial class MainWindow : Window
	{
		public const string PATH_ROOT = "KoroneSouko";
		private Dictionary<KoroneKibun, Dictionary<KoroneMokuhyou, RenderClass>> m_image_sets;

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

							foreach (var _random_preset in Directory.GetDirectories(path).Append("Base"))
							{
								string random_preset = System.IO.Path.GetFileName(_random_preset);
								path = $"{PATH_ROOT}/Char/{fuku}/{anim_id}/{random_preset}";
								CheckDir(path);

								var files = Directory.GetFiles(path);

								if (files.Length == 0)
								{
									//image x
									if (Enum.GetName(typeof(KoroneKibun), KoroneKibun.FUTSU_DOG) == kibun)
									{
										//必要Images set
										YesByeBye($"Please fill in all images in the " +
											$"{Enum.GetName(typeof(KoroneKibun), KoroneKibun.FUTSU_DOG)} image set");
									}
								}
								else if (files.Length == 1)
								{
									//gif file
									GIF_RenderClass render = new GIF_RenderClass(files[0]);
									var dict_mokuhyou = new Dictionary<KoroneMokuhyou, RenderClass>();
									dict_mokuhyou.Add((KoroneMokuhyou)Enum.Parse(typeof(KoroneMokuhyou), mokuhyou), render);
									m_image_sets.Add((KoroneKibun)Enum.Parse(typeof(KoroneKibun), kibun), dict_mokuhyou);
								}
								else
								{
									//png files
								}
							}
						}
					}
				}
			}
		}

		void YesByeBye(string reason)
		{
			MessageBox.Show(reason);
			Environment.Exit(1);
		}

		void Render()
		{
			var image = new BitmapImage();
			image.BeginInit();
			image.UriSource = new Uri("");
			image.EndInit();
			ImageBehavior.SetAnimatedSource(IMAGEVIEW_CHAR, image);
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
