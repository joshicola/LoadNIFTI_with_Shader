// loads the raw binary data into a texture saved as a Unity asset 
// (so can be de-activated after a given data cube has been converted)
// adapted from a XNA project by Kyle Hayward 
// http://graphicsrunner.blogspot.ca/2009/01/volume-rendering-101.html
// Gilles Ferrand, University of Manitoba 2016

#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using System.IO; // to get BinaryReader
using System.Linq; // to get array's Min/Max
using System;
using itk.simple;

public class Loader : MonoBehaviour {

	public string path = @"Assets/";
	public string filename = "skull";
	public string extension = ".raw";
	public uint[] size = new uint[3] {512, 512, 512};
	private uint[] size2p = new uint[3] {512, 512, 512};
	private float[,,] data;
	public bool mipmap;


	void Start() {
		// load the raw data
		Color[] colors = LoadNIFTIFile();
		// create the texture
		Texture3D texture = new Texture3D ((int)size2p[0],(int)size2p[1],(int)size2p[2], TextureFormat.Alpha8, mipmap);
		texture.SetPixels (colors);
		texture.Apply ();

//		Texture2D text2D = new Texture2D (512, 512, TextureFormat.Alpha8, false);
//		colors = new Color[512*512];
//		for (int i = 0; i < 147; i++) {
//			for (int j = 0; j < 185; j++) {
//				colors [i + 512 * j].a = data [i, j, 114];
//			}
//		}
//		text2D.SetPixels(colors);
//		text2D.Apply ();
		// assign it to the material of the parent object
		GetComponent<Renderer>().material.SetTexture ("_Data", texture);
		// save it as an asset for re-use
		#if UNITY_EDITOR
		////AssetDatabase.CreateAsset(texture, path+filename+".asset");
//		AssetDatabase.CreateAsset(text2D, path+"texture"+".asset");
		#endif
	}

	private Color[] LoadRAWFile()
	{
		Color[] colors;

		Debug.Log ("Opening file "+path+filename+extension);
		FileStream file = new FileStream(path+filename+extension, FileMode.Open);
		Debug.Log ("File length = "+file.Length+" bytes, Data size = "+size[0]*size[1]*size[2]+" points -> "+file.Length/(size[0]*size[1]*size[2])+" byte(s) per point");

		BinaryReader reader = new BinaryReader(file);
		byte[] buffer = new byte[size[0] * size[1] * size[2]]; // assumes 8-bit data
		reader.Read(buffer, 0, sizeof(byte) * buffer.Length);
		reader.Close();

		colors = new Color[buffer.Length];
		Color color = Color.black;
		for (int i = 0; i < buffer.Length; i++)
		{
			color.a = (float)buffer[i] / byte.MaxValue; //scale the scalar values to [0, 1]
			colors [i] = color;
		}

		return colors;
	}

	private uint ClosestPowOfTwo(uint num){
		float pot=Mathf.Log ((float)num, 2.0f);
		if (pot == Mathf.Floor (pot)) {
			return (uint)Mathf.Pow (2.0f, pot);
		} else {
			return (uint)Mathf.Pow (2.0f, Mathf.Floor(pot)+1);
		}
	}

	private Color[] LoadNIFTIFile(){
		Color[] colors;
		Image input=SimpleITK.ReadImage("/home/josher/Dropbox/UnityResources/2016.11.29.Brain.Tumor.in.Unity3D/Data/MRI_After_Surgery/AX_T1_3D_MPRAGE_NAVIGATION.nii.gz");

		MinimumMaximumImageFilter filter = new MinimumMaximumImageFilter();

		filter.Execute(input);
		uint max = (uint)filter.GetMaximum();
		uint min = (uint)filter.GetMinimum();

		size[0] = input.GetWidth ();
		size[1] = input.GetHeight ();
		size[2] = input.GetDepth ();

		size2p [0] = ClosestPowOfTwo (size [0]);
		size2p [1] = ClosestPowOfTwo (size [1]);
		size2p [2] = ClosestPowOfTwo (size [2]);

		//data = new float[size [0], size [1], size [2]];

		colors = new Color[size2p[0]*size2p[1]*size2p[2]];
		Color color = Color.black;
		for (uint x = 0; x < size2p[0]; x++) {
			for (uint y = 0; y < size2p[1]; y++) {
				for (uint z = 0; z < size2p[2]; z++) {
					float temp = (float)input.GetPixelAsInt16(new VectorUInt32 (new uint[] {x*size[0]/size2p[0],y*size[1]/size2p[1],z*size[2]/size2p[2]}));

//					if(temp ==-1.0f) {
//						temp=1100.0f;
//					}
					//data [x, y, z] = (temp-min)/(max-min);
					color.a = (temp-min)/(max-min);
					colors[x+y*size2p[0]+z*size2p[0]*size2p[1]] = color;
				}
			}
		}
		return colors;

	}


}
