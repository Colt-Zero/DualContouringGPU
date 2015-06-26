using UnityEngine;
using System.Collections;
using System;
using Code.Noise;

public class DensityFunctions
{
	static float Sphere(Vector3 worldPosition, Vector3 origin, float radius)
	{
		return (worldPosition - origin).magnitude - radius;
	}

	static float Cuboid(Vector3 worldPosition, Vector3 origin, Vector3 halfDimensions)
	{
		Vector3 local_pos = worldPosition - origin;
		Vector3 pos = local_pos;

		Vector3 d = new Vector3(Mathf.Abs(pos.x), Mathf.Abs(pos.y), Mathf.Abs(pos.z)) - halfDimensions;
		float m = Mathf.Max(d.x, Mathf.Max(d.y, d.z));
		return Mathf.Min(m, Vector3.Max(d, Vector3.zero).magnitude);
	}

	static float Torus(Vector3 worldPosition, Vector3 origin)
	{
		Vector3 local_pos = worldPosition - origin;
		float xt = local_pos.x;
		float yt = local_pos.y;
		float zt = local_pos.z;
		float _radius = 10.0f;
		float _radius2 = 2.33f;

		float x = xt;
		float y = yt;
		float z = zt;

		float x2 = Mathf.Sqrt(x * x + z * z) - _radius / 2.0f;
		float d = x2 * x2 + y * y - _radius2 * _radius2;

		return d;
	}

	static float Torus2(Vector3 worldPosition, Vector3 origin)
	{
		Vector3 local_pos = worldPosition - origin;
		float xt = local_pos.x;
		float yt = local_pos.y;
		float zt = local_pos.z;
		float _radius = 10.0f;
		float _radius2 = 2.33f;
		
		/*if (xt < origin.x - _radius * 3 || xt > origin.x + _radius * 3 ||
		    yt < origin.y - _radius * 3 || yt > origin.y + _radius * 3 ||
		    zt < origin.z - _radius * 3 || zt > origin.z + _radius * 3)
			return 0.01f;*/
		
		float step = 11.0f;
		//double y = fmod((double) abs(yt), step) - step / 2.0f;
		float y = yt;
		
		double mod = Math.IEEERemainder(Math.Abs(zt), (double) step);
		float z1 = (float) mod;
		//float x1 = xt;
		float x2 = Mathf.Sqrt(xt * xt + z1 * z1) - _radius / 2.0f;
		float d = x2 * x2 + (y) * (y/* - cos(xt)*/) - _radius2 * _radius2;
		
		//double x = sqrt(xt * xt + zt * zt) - _radius / 2.0f;
		//double d = x * x + yt * yt - _radius2 * _radius2;
		return d;
	}

	static float Torus3(Vector3 worldPosition, Vector3 origin)
	{
		Vector3 local_pos = worldPosition - origin;
		float xt = local_pos.x;
		float yt = local_pos.y;
		float zt = local_pos.z;
		float _radius = 9.0f;
		float _radius2 = 2.33f;

		float step = 10.0f;
		//double y = fmod((double) abs(yt), step) - step / 2.0f;
		float y = yt;


		double mod = Math.IEEERemainder(Math.Abs(zt), (double) step);
		float z1 = (float) mod;
		mod = Math.IEEERemainder(Math.Abs(xt), (double) step);
		float x1 = (float) mod;
		float x2 = Mathf.Sqrt(x1 * x1 + z1 * z1) - _radius / 2.0f;
		float d = x2 * x2 + y * y - _radius2 * _radius2;
		return d;
	}

	public static float FractalNoise(int octaves, float frequency, float lacunarity, float persistence, Vector2 position)
	{
		float SCALE = 1.0f / 128.0f;
		Vector2 p = position * SCALE;
		float noise = 0.0f;

		float amplitude = 1.0f;
		p *= frequency;

		for (int i = 0; i < octaves; i++)
		{
			noise += Noise.Perlin(p.x, p.y) * amplitude;
			p *= lacunarity;
			amplitude *= persistence;
		}

		// move into [0, 1] range
		return 0.5f + (0.5f * noise);
	}

	public static float Density_Func(Vector3 worldPosition)
	{
		float MAX_HEIGHT = 64.0f;
		float noise = FractalNoise(4, 0.5343f, 2.2324f, 0.68324f, new Vector2(worldPosition.x, worldPosition.z));
		float terrain = worldPosition.y - (MAX_HEIGHT * noise);
		//float terrain = worldPosition.y - (MAX_HEIGHT);

		//float cube = Cuboid(worldPosition, new Vector3(-4.0f, 20.0f, -4.0f), new Vector3(4.0f, 4.0f, 4.0f));
		//float sphere = Sphere(worldPosition, new Vector3(15.0f, 2.5f, 1.0f), 10.0f);
		//float torus = Torus(worldPosition, new Vector3(0.0f, 9.5f, -4.0f));
		//float torus2 = Torus2(worldPosition, new Vector3(0.0f, 5.0f, -4.0f));

		//float torus2 = Torus3(worldPosition, new Vector3(0.0f, 2.0f, -4.0f));
		float torus3 = Torus3(worldPosition, new Vector3(10.0f, 20.5f, 20.0f));
		return Mathf.Max(-torus3, terrain);
		//return Mathf.Max(-torus2, Mathf.Max(-torus3, terrain));
		//return terrain;
	}
}
